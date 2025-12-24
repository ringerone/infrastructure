import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfigurationService, FeatureFlag } from '../configuration.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-feature-flag-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="feature-flag-list">
      <div class="header-section">
        <h2>Feature Flags</h2>
        <button (click)="addFeatureFlag()" class="btn-add">Add Feature Flag</button>
      </div>
      
      <div class="filters">
        <input 
          type="text" 
          [(ngModel)]="searchTerm" 
          placeholder="Search by name, variant, or scope identifier..." 
          (keyup.enter)="onSearch()"
          (blur)="onSearch()"
          class="search-input">
        <select [(ngModel)]="selectedScope" (change)="loadFeatureFlags()">
          <option value="">All Scopes</option>
          <option value="Global">Global</option>
          <option value="Environment">Environment</option>
          <option value="Region">Region</option>
          <option value="Tenant">Tenant</option>
          <option value="User">User</option>
        </select>
        <input type="text" [(ngModel)]="scopeIdentifier" placeholder="Scope Identifier" (blur)="loadFeatureFlags()">
        <button (click)="loadFeatureFlags()">Refresh</button>
      </div>

      <div class="flags-table">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Enabled</th>
              <th>Variant</th>
              <th>Rollout %</th>
              <th>Scope</th>
              <th>Scope Identifier</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of pagedData">
              <td>{{ item.key }}</td>
              <td>{{ getEnabled(item.value) ? 'Yes' : 'No' }}</td>
              <td>{{ getVariant(item.key) }}</td>
              <td>{{ getRolloutPercentage(item.key) }}%</td>
              <td>{{ getScope(item.key) }}</td>
              <td>{{ getScopeIdentifier(item.key) }}</td>
              <td>
                <button (click)="editFeatureFlag(item.key)" class="btn-edit">Edit</button>
              </td>
            </tr>
            <tr *ngIf="pagedData.length === 0">
              <td colspan="7" class="no-data">No feature flags found</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination" *ngIf="totalCount > 0">
        <div class="pagination-info">
          Showing {{ (pageNumber - 1) * pageSize + 1 }} to {{ Math.min(pageNumber * pageSize, totalCount) }} of {{ totalCount }} feature flags
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
    .feature-flag-list {
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
    .flags-table {
      overflow-x: auto;
      margin-top: 20px;
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
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      transition: background-color 0.3s;
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
      background-color: #007bff;
      color: white;
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
export class FeatureFlagListComponent implements OnInit, OnDestroy {
  pagedData: any[] = [];
  featureFlags: Record<string, any> = {};
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
    this.loadFeatureFlags();
    
    // Subscribe to feature flag changes via SignalR
    this.subscription = this.configService.onFeatureFlagChanged().subscribe(flag => {
      console.log('Feature flag changed:', flag);
      this.loadFeatureFlags();
    });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  loadFeatureFlags() {
    this.pageNumber = 1; // Reset to first page when filters change
    this.fetchFeatureFlags();
  }

  onSearch() {
    this.pageNumber = 1; // Reset to first page when searching
    this.fetchFeatureFlags();
  }

  onPageSizeChange() {
    this.pageNumber = 1; // Reset to first page when page size changes
    this.fetchFeatureFlags();
  }

  fetchFeatureFlags() {
    this.configService.getFeatureFlags(
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
          this.featureFlags = {};
          response.items.forEach((item: any) => {
            this.featureFlags[item.key] = item.value;
          });
        } else {
          // Fallback for non-paginated response (backward compatibility)
          this.featureFlags = response;
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
        console.error('Error loading feature flags:', error);
      }
    );
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.fetchFeatureFlags();
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

  addFeatureFlag() {
    this.router.navigate(['/feature-flags/new']);
  }

  editFeatureFlag(name: string) {
    this.router.navigate(['/feature-flags/edit', name]);
  }

  getEnabled(flagValue: any): boolean {
    // Handle nested structure: { enabled: ..., variant: ..., scope: ..., etc. }
    if (flagValue && typeof flagValue === 'object' && 'enabled' in flagValue) {
      return flagValue.enabled || false;
    }
    // Handle direct boolean value (backward compatibility)
    return flagValue === true;
  }

  getVariant(name: string): string {
    const flag = this.featureFlags[name];
    if (flag && typeof flag === 'object' && 'variant' in flag) {
      return flag.variant || '-';
    }
    return '-';
  }

  getRolloutPercentage(name: string): number {
    const flag = this.featureFlags[name];
    if (flag && typeof flag === 'object' && 'rolloutPercentage' in flag) {
      return flag.rolloutPercentage ?? 100;
    }
    return 100;
  }

  getScope(name: string): string {
    const flag = this.featureFlags[name];
    if (flag && typeof flag === 'object' && 'scope' in flag) {
      return flag.scope || 'Global';
    }
    return 'Global';
  }

  getScopeIdentifier(name: string): string {
    const flag = this.featureFlags[name];
    if (flag && typeof flag === 'object' && 'scopeIdentifier' in flag) {
      return flag.scopeIdentifier || '';
    }
    return '';
  }
}
