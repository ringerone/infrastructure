import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfigurationService, Configuration } from '../configuration.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-configuration-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="configuration-list">
      <div class="header-section">
        <h2>Configurations</h2>
        <button (click)="addConfiguration()" class="btn-add">Add Configuration</button>
      </div>
      
      <div class="filters">
        <input 
          type="text" 
          [(ngModel)]="searchTerm" 
          placeholder="Search by key or scope identifier..." 
          (keyup.enter)="onSearch()"
          (blur)="onSearch()"
          class="search-input">
        <select [(ngModel)]="selectedScope" (change)="loadConfigurations()">
          <option value="">All Scopes</option>
          <option value="Global">Global</option>
          <option value="Environment">Environment</option>
          <option value="Region">Region</option>
          <option value="Tenant">Tenant</option>
          <option value="User">User</option>
        </select>
        <input type="text" [(ngModel)]="scopeIdentifier" placeholder="Scope Identifier" (blur)="loadConfigurations()">
        <button (click)="loadConfigurations()">Refresh</button>
      </div>

      <div class="config-table">
        <table>
          <thead>
            <tr>
              <th>Key</th>
              <th>Value</th>
              <th>Scope</th>
              <th>Scope Identifier</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of pagedData">
              <td>{{ item.key }}</td>
              <td>{{ formatValue(getValue(item.value)) }}</td>
              <td>{{ getScope(item.key) }}</td>
              <td>{{ getScopeIdentifier(item.key) }}</td>
              <td>
                <button (click)="editConfiguration(item.key)" class="btn-edit">Edit</button>
              </td>
            </tr>
            <tr *ngIf="pagedData.length === 0">
              <td colspan="5" class="no-data">No configurations found</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination" *ngIf="totalCount > 0">
        <div class="pagination-info">
          Showing {{ (pageNumber - 1) * pageSize + 1 }} to {{ Math.min(pageNumber * pageSize, totalCount) }} of {{ totalCount }} configurations
        </div>
        <div class="pagination-controls" *ngIf="totalPages > 1">
          <button (click)="goToPage(1)" [disabled]="pageNumber === 1" class="btn-page">First</button>
          <button (click)="goToPage(pageNumber - 1)" [disabled]="!hasPreviousPage" class="btn-page">Previous</button>
          <span class="page-numbers">
            <button 
              *ngFor="let page of getPageNumbers()" 
              (click)="goToPage(page)"
              [class.active]="page === pageNumber"
              class="btn-page-number">
              {{ page }}
            </button>
          </span>
          <button (click)="goToPage(pageNumber + 1)" [disabled]="!hasNextPage" class="btn-page">Next</button>
          <button (click)="goToPage(totalPages)" [disabled]="pageNumber === totalPages" class="btn-page">Last</button>
        </div>
        <div class="page-size-selector">
          <label>Page Size:</label>
          <select [(ngModel)]="pageSize" (change)="onPageSizeChange()">
            <option value="10">10</option>
            <option value="25">25</option>
            <option value="50">50</option>
            <option value="100">100</option>
          </select>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .configuration-list {
      padding: 20px;
    }
    .header-section {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .header-section h2 {
      margin: 0;
    }
    .filters {
      margin-bottom: 20px;
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
    }
    .filters select, .filters input {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    .search-input {
      flex: 1;
      min-width: 200px;
    }
    .config-table {
      overflow-x: auto;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th, td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }
    th {
      background-color: #f8f9fa;
      font-weight: bold;
    }
    .no-data {
      text-align: center;
      color: #999;
      padding: 20px;
    }
    button {
      padding: 8px 16px;
      background-color: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    button:hover:not(:disabled) {
      background-color: #0056b3;
    }
    button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .btn-add {
      background-color: #28a745;
      color: white;
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      transition: background-color 0.3s;
    }
    .btn-add:hover {
      background-color: #218838;
    }
    .btn-edit {
      background-color: #007bff;
      color: white;
    }
    .btn-edit:hover {
      background-color: #0056b3;
    }
    .pagination {
      margin-top: 20px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 10px;
    }
    .pagination-info {
      color: #666;
    }
    .pagination-controls {
      display: flex;
      gap: 5px;
      align-items: center;
    }
    .page-numbers {
      display: flex;
      gap: 5px;
    }
    .btn-page, .btn-page-number {
      padding: 6px 12px;
      font-size: 14px;
    }
    .btn-page-number.active {
      background-color: #0056b3;
      font-weight: bold;
    }
    .page-size-selector {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .page-size-selector select {
      padding: 6px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
  `]
})
export class ConfigurationListComponent implements OnInit, OnDestroy {
  pagedData: any[] = [];
  configurations: Record<string, any> = {};
  selectedScope = '';
  scopeIdentifier = '';
  searchTerm = '';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  hasPreviousPage = false;
  hasNextPage = false;
  Math = Math;
  private subscription?: Subscription;

  constructor(
    private configService: ConfigurationService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadConfigurations();
    
    // Subscribe to configuration changes via SignalR
    this.subscription = this.configService.onConfigurationChanged().subscribe(config => {
      console.log('Configuration changed:', config);
      this.loadConfigurations();
    });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  loadConfigurations() {
    this.pageNumber = 1; // Reset to first page when filters change
    this.fetchConfigurations();
  }

  onSearch() {
    this.pageNumber = 1; // Reset to first page when searching
    this.fetchConfigurations();
  }

  onPageSizeChange() {
    this.pageNumber = 1; // Reset to first page when page size changes
    this.fetchConfigurations();
  }

  fetchConfigurations() {
    this.configService.getConfigurations(
      this.selectedScope || undefined,
      this.scopeIdentifier || undefined,
      this.pageNumber,
      this.pageSize,
      this.searchTerm || undefined
    ).subscribe(
      response => {
        // Handle paginated response
        if (response.items) {
          this.pagedData = response.items;
          this.totalCount = response.totalCount || 0;
          this.totalPages = response.totalPages || 0;
          this.hasPreviousPage = response.hasPreviousPage || false;
          this.hasNextPage = response.hasNextPage || false;
          
          // Convert items array back to dictionary for compatibility
          this.configurations = {};
          response.items.forEach((item: any) => {
            this.configurations[item.key] = item.value;
          });
        } else {
          // Fallback for non-paginated response (backward compatibility)
          this.configurations = response;
          this.pagedData = Object.keys(response).map(key => ({
            key,
            value: response[key]
          }));
          this.totalCount = this.pagedData.length;
          this.totalPages = Math.ceil(this.totalCount / this.pageSize);
          this.hasPreviousPage = false;
          this.hasNextPage = false;
        }
      },
      error => {
        console.error('Error loading configurations:', error);
      }
    );
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.fetchConfigurations();
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPages = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPages / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPages - 1);
    
    if (endPage - startPage < maxPages - 1) {
      startPage = Math.max(1, endPage - maxPages + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }

  addConfiguration() {
    this.router.navigate(['/configurations/new']);
  }

  editConfiguration(key: string) {
    this.router.navigate(['/configurations/edit', key]);
  }

  getScope(key: string): string {
    const config = this.configurations[key];
    if (config && typeof config === 'object' && 'scope' in config) {
      return config.scope || 'Global';
    }
    return 'Global';
  }

  getScopeIdentifier(key: string): string {
    const config = this.configurations[key];
    if (config && typeof config === 'object' && 'scopeIdentifier' in config) {
      return config.scopeIdentifier || '';
    }
    return '';
  }

  getValue(configValue: any): any {
    // Handle nested structure: { value: ..., scope: ..., scopeIdentifier: ... }
    if (configValue && typeof configValue === 'object' && 'value' in configValue) {
      return configValue.value;
    }
    // Handle direct value
    return configValue;
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '';
    }
    if (typeof value === 'string') {
      return value;
    }
    if (typeof value === 'object') {
      return JSON.stringify(value);
    }
    return String(value);
  }
}
