import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ConfigurationService, Tenant } from '../configuration.service';

@Component({
  selector: 'app-tenant-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tenant-edit">
      <h2>{{ isEditMode ? 'Edit Tenant' : 'Add Tenant' }}</h2>
      
      <form (ngSubmit)="saveTenant()">
        <div class="form-group">
          <label for="tenantIdentifier">Tenant Identifier:</label>
          <input 
            type="text" 
            id="tenantIdentifier"
            [(ngModel)]="editingTenant.tenantIdentifier" 
            name="tenantIdentifier" 
            required
            pattern="[a-zA-Z0-9_-]+"
            [readonly]="isEditMode"
            placeholder="e.g., acme-corp">
          <small *ngIf="isEditMode" class="form-text">Identifier cannot be changed when editing</small>
          <small class="form-text">Only letters, numbers, hyphens, and underscores allowed</small>
        </div>
        
        <div class="form-group">
          <label for="name">Name:</label>
          <input 
            type="text" 
            id="name"
            [(ngModel)]="editingTenant.name" 
            name="name" 
            required
            placeholder="e.g., Acme Corporation">
        </div>
        
        <div class="form-group">
          <label for="status">Status:</label>
          <select 
            id="status"
            [(ngModel)]="editingTenant.status" 
            name="status" 
            required>
            <option value="Pending">Pending</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
            <option value="Suspended">Suspended</option>
          </select>
        </div>
        
        <div class="form-group">
          <label for="contactName">Contact Name:</label>
          <input 
            type="text" 
            id="contactName"
            [(ngModel)]="editingTenant.contactName" 
            name="contactName"
            placeholder="Primary contact name">
        </div>
        
        <div class="form-group">
          <label for="contactEmail">Contact Email:</label>
          <input 
            type="email" 
            id="contactEmail"
            [(ngModel)]="editingTenant.contactEmail" 
            name="contactEmail"
            placeholder="contact@example.com">
        </div>
        
        <div class="form-group">
          <label for="contactPhone">Contact Phone:</label>
          <input 
            type="tel" 
            id="contactPhone"
            [(ngModel)]="editingTenant.contactPhone" 
            name="contactPhone"
            placeholder="+1-555-123-4567">
        </div>
        
        <div class="form-group">
          <label for="comments">Comments:</label>
          <textarea 
            id="comments"
            [(ngModel)]="editingTenant.comments" 
            name="comments"
            rows="4"
            placeholder="Internal notes about this tenant"></textarea>
        </div>
        
        <div class="form-group">
          <label for="salesTerms">Sales Terms:</label>
          <textarea 
            id="salesTerms"
            [(ngModel)]="editingTenant.salesTerms" 
            name="salesTerms"
            rows="4"
            placeholder="Contract details, pricing, terms, etc."></textarea>
        </div>
        
        <div class="form-actions">
          <button type="submit" class="btn-primary">Save</button>
          <button type="button" (click)="cancel()" class="btn-secondary">Cancel</button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .tenant-edit {
      padding: 20px;
      max-width: 800px;
      margin: 0 auto;
    }
    
    h2 {
      color: #007bff;
      margin-bottom: 20px;
    }
    
    .form-group {
      margin-bottom: 20px;
    }
    
    label {
      display: block;
      margin-bottom: 5px;
      font-weight: bold;
      color: #333;
    }
    
    input[type="text"],
    input[type="email"],
    input[type="tel"],
    select,
    textarea {
      width: 100%;
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      box-sizing: border-box;
      font-family: inherit;
    }
    
    textarea {
      resize: vertical;
      min-height: 80px;
    }
    
    input[readonly] {
      background-color: #f5f5f5;
      cursor: not-allowed;
    }
    
    .form-text {
      display: block;
      margin-top: 5px;
      color: #666;
      font-size: 12px;
    }
    
    .form-actions {
      display: flex;
      gap: 10px;
      margin-top: 30px;
    }
    
    button {
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      transition: background-color 0.3s;
    }
    
    .btn-primary {
      background-color: #007bff;
      color: white;
    }
    
    .btn-primary:hover {
      background-color: #0056b3;
    }
    
    .btn-secondary {
      background-color: #6c757d;
      color: white;
    }
    
    .btn-secondary:hover {
      background-color: #5a6268;
    }
  `]
})
export class TenantEditComponent implements OnInit {
  editingTenant: Tenant = {
    tenantIdentifier: '',
    name: '',
    status: 'Pending',
    comments: '',
    salesTerms: '',
    contactEmail: '',
    contactPhone: '',
    contactName: ''
  };
  isEditMode = false;
  private tenantIdentifier: string | null = null;

  constructor(
    private configService: ConfigurationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      const identifier = params['identifier'];
      if (identifier) {
        this.isEditMode = true;
        this.tenantIdentifier = identifier;
        this.loadTenant(identifier);
      } else {
        this.isEditMode = false;
        this.tenantIdentifier = null;
      }
    });
  }

  loadTenant(identifier: string) {
    this.configService.getTenant(identifier).subscribe(
      data => {
        this.editingTenant = {
          tenantIdentifier: data.tenantIdentifier || identifier,
          name: data.name || '',
          status: data.status || 'Pending',
          comments: data.comments || '',
          salesTerms: data.salesTerms || '',
          contactEmail: data.contactEmail || '',
          contactPhone: data.contactPhone || '',
          contactName: data.contactName || ''
        };
      },
      error => {
        console.error('Error loading tenant:', error);
        this.router.navigate(['/tenants']);
      }
    );
  }

  saveTenant() {
    if (!this.editingTenant.tenantIdentifier || !this.editingTenant.name) {
      alert('Tenant Identifier and Name are required');
      return;
    }

    // Validate tenant identifier format
    if (!/^[a-zA-Z0-9_-]+$/.test(this.editingTenant.tenantIdentifier)) {
      alert('Tenant Identifier can only contain letters, numbers, hyphens, and underscores');
      return;
    }

    this.configService.setTenant(this.editingTenant).subscribe(
      () => {
        this.router.navigate(['/tenants']);
      },
      error => {
        console.error('Error saving tenant:', error);
        alert('Error saving tenant. Please try again.');
      }
    );
  }

  cancel() {
    this.router.navigate(['/tenants']);
  }
}

