import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfigurationService, Tenant, PagedResult } from '../configuration.service';

@Component({
  selector: 'app-tenant-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tenant-list">
      <div class="header-section">
        <h2>Tenants</h2>
        <button (click)="addTenant()" class="btn-add">Add Tenant</button>
      </div>
      
      <div class="filters">
        <input 
          type="text" 
          [(ngModel)]="searchTerm" 
          placeholder="Search by identifier, name, email, or contact..." 
          (keyup.enter)="loadTenants()"
          (blur)="loadTenants()">
        <select [(ngModel)]="selectedStatus" (change)="loadTenants()">
          <option value="">All Statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
          <option value="Pending">Pending</option>
          <option value="Suspended">Suspended</option>
        </select>
        <button (click)="loadTenants()">Refresh</button>
      </div>

      <div class="tenants-table">
        <table>
          <thead>
            <tr>
              <th>Identifier</th>
              <th>Name</th>
              <th>Status</th>
              <th>Contact Name</th>
              <th>Contact Email</th>
              <th>Contact Phone</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let tenant of pagedData">
              <td>{{ tenant.tenantIdentifier }}</td>
              <td>{{ tenant.name }}</td>
              <td>
                <span [class]="'status-badge status-' + tenant.status.toLowerCase()">
                  {{ tenant.status }}
                </span>
              </td>
              <td>{{ tenant.contactName || '-' }}</td>
              <td>{{ tenant.contactEmail || '-' }}</td>
              <td>{{ tenant.contactPhone || '-' }}</td>
              <td>
                <button (click)="editTenant(tenant.tenantIdentifier)" class="btn-edit">Edit</button>
              </td>
            </tr>
            <tr *ngIf="pagedData.length === 0">
              <td colspan="7" class="no-data">No tenants found</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination" *ngIf="totalCount > 0">
        <div class="pagination-info">
          Showing {{ (pageNumber - 1) * pageSize + 1 }} to {{ Math.min(pageNumber * pageSize, totalCount) }} of {{ totalCount }} tenants
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
    .tenant-list {
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
    .filters input, .filters select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      flex-grow: 1;
      min-width: 150px;
    }
    .filters button {
      flex-shrink: 0;
    }
    .tenants-table {
      overflow-x: auto;
      margin-bottom: 20px;
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
      color: #666;
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
    button:hover {
      background-color: #0056b3;
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
    .status-badge {
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: bold;
      text-transform: uppercase;
    }
    .status-active {
      background-color: #28a745;
      color: white;
    }
    .status-inactive {
      background-color: #6c757d;
      color: white;
    }
    .status-pending {
      background-color: #ffc107;
      color: #000;
    }
    .status-suspended {
      background-color: #dc3545;
      color: white;
    }
    .pagination {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 10px;
    }
    .pagination-info {
      font-size: 14px;
      color: #555;
    }
    .pagination-controls {
      display: flex;
      gap: 5px;
      flex-wrap: wrap;
    }
    .btn-page, .btn-page-number {
      padding: 8px 12px;
      background-color: #f0f0f0;
      color: #333;
      border: 1px solid #ddd;
      border-radius: 4px;
      cursor: pointer;
      transition: background-color 0.2s, border-color 0.2s;
    }
    .btn-page:hover:not(:disabled), .btn-page-number:hover:not(:disabled) {
      background-color: #e0e0e0;
      border-color: #ccc;
    }
    .btn-page:disabled, .btn-page-number:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }
    .btn-page-number.active {
      background-color: #007bff;
      color: white;
      border-color: #007bff;
    }
    .page-size-selector {
      display: flex;
      align-items: center;
      gap: 5px;
    }
    .page-size-selector label {
      margin: 0;
      font-weight: normal;
      color: #555;
    }
    .page-size-selector select {
      width: auto;
      min-width: unset;
    }
  `]
})
export class TenantListComponent implements OnInit {
  pagedData: Tenant[] = [];
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  totalPages = 0;
  hasPreviousPage = false;
  hasNextPage = false;

  searchTerm = '';
  selectedStatus = '';
  Math = Math;

  constructor(
    private configService: ConfigurationService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadTenants();
  }

  loadTenants() {
    this.configService.getTenants(
      this.pageNumber,
      this.pageSize,
      this.searchTerm,
      this.selectedStatus
    ).subscribe(
      data => {
        this.pagedData = data.items;
        this.totalCount = data.totalCount;
        this.pageNumber = data.pageNumber;
        this.pageSize = data.pageSize;
        this.totalPages = data.totalPages;
        this.hasPreviousPage = data.hasPreviousPage;
        this.hasNextPage = data.hasNextPage;
      },
      error => {
        console.error('Error loading tenants:', error);
      }
    );
  }

  addTenant() {
    this.router.navigate(['/tenants/new']);
  }

  editTenant(tenantIdentifier: string) {
    this.router.navigate(['/tenants/edit', tenantIdentifier]);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.pageNumber = page;
      this.loadTenants();
    }
  }

  onPageSizeChange() {
    this.pageNumber = 1;
    this.loadTenants();
  }

  getPageNumbers(): number[] {
    const pageNumbers = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);

    if (endPage - startPage + 1 < maxPagesToShow) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pageNumbers.push(i);
    }
    return pageNumbers;
  }
}

