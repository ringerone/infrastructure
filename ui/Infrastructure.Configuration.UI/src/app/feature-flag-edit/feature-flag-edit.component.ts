import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ConfigurationService, FeatureFlag } from '../configuration.service';

@Component({
  selector: 'app-feature-flag-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="feature-flag-edit">
      <h2>{{ isEditMode ? 'Edit Feature Flag' : 'Add Feature Flag' }}</h2>
      
      <form (ngSubmit)="saveFeatureFlag()">
        <div class="form-group">
          <label for="name">Name:</label>
          <input 
            type="text" 
            id="name"
            [(ngModel)]="editingFlag.name" 
            name="name" 
            required
            [readonly]="isEditMode">
          <small *ngIf="isEditMode" class="form-text">Name cannot be changed when editing</small>
        </div>
        
        <div class="form-group">
          <label for="enabled">
            <input 
              type="checkbox" 
              id="enabled"
              [(ngModel)]="editingFlag.enabled" 
              name="enabled">
            Enabled
          </label>
        </div>
        
        <div class="form-group">
          <label for="rolloutPercentage">Rollout Percentage (0-100):</label>
          <input 
            type="number" 
            id="rolloutPercentage"
            [(ngModel)]="editingFlag.rolloutPercentage" 
            name="rolloutPercentage" 
            min="0" 
            max="100"
            required>
        </div>
        
        <div class="form-group">
          <label for="variant">Variant:</label>
          <input 
            type="text" 
            id="variant"
            [(ngModel)]="editingFlag.variant" 
            name="variant"
            placeholder="Optional variant name">
        </div>
        
        <div class="form-group">
          <label for="scope">Scope:</label>
          <select 
            id="scope"
            [(ngModel)]="editingFlag.scope" 
            name="scope" 
            required>
            <option value="Global">Global</option>
            <option value="Environment">Environment</option>
            <option value="Region">Region</option>
            <option value="Tenant">Tenant</option>
            <option value="User">User</option>
          </select>
        </div>
        
        <div class="form-group">
          <label for="scopeIdentifier">Scope Identifier:</label>
          <input 
            type="text" 
            id="scopeIdentifier"
            [(ngModel)]="editingFlag.scopeIdentifier" 
            name="scopeIdentifier"
            placeholder="Optional (e.g., tenant ID, environment name)">
        </div>
        
        <div class="form-actions">
          <button type="submit" class="btn-primary">Save</button>
          <button type="button" (click)="cancel()" class="btn-secondary">Cancel</button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .feature-flag-edit {
      padding: 20px;
      max-width: 600px;
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
    
    label[for="enabled"] {
      display: flex;
      align-items: center;
      gap: 8px;
      font-weight: normal;
    }
    
    input[type="text"],
    input[type="number"],
    select {
      width: 100%;
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      box-sizing: border-box;
    }
    
    input[type="checkbox"] {
      width: auto;
      margin: 0;
      cursor: pointer;
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
export class FeatureFlagEditComponent implements OnInit {
  editingFlag: FeatureFlag = {
    name: '',
    enabled: false,
    scope: 'Global',
    rolloutPercentage: 100,
    variant: ''
  };
  isEditMode = false;
  private flagName: string | null = null;

  constructor(
    private configService: ConfigurationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      const name = params['name'];
      if (name) {
        this.isEditMode = true;
        this.flagName = name;
        this.loadFeatureFlag(name);
      } else {
        this.isEditMode = false;
        this.flagName = null;
      }
    });
  }

  loadFeatureFlag(name: string) {
    this.configService.getFeatureFlag(name).subscribe(
      data => {
        this.editingFlag = {
          name: data.featureName || name,
          enabled: data.enabled ?? false,
          variant: data.variant || '',
          scope: data.scope || 'Global',
          rolloutPercentage: data.rolloutPercentage ?? 100,
          scopeIdentifier: data.scopeIdentifier || ''
        };
      },
      error => {
        console.error('Error loading feature flag:', error);
        // If error, navigate back
        this.router.navigate(['/feature-flags']);
      }
    );
  }

  saveFeatureFlag() {
    // Validation
    if (!this.editingFlag.name || this.editingFlag.name.trim() === '') {
      alert('Name is required');
      return;
    }

    if (this.editingFlag.scope === undefined || this.editingFlag.scope === null || this.editingFlag.scope === '') {
      alert('Scope is required');
      return;
    }

    const rolloutPercentage = this.editingFlag.rolloutPercentage ?? 100;
    if (rolloutPercentage < 0 || rolloutPercentage > 100) {
      alert('Rollout percentage must be between 0 and 100');
      return;
    }

    // Ensure rolloutPercentage is set
    if (this.editingFlag.rolloutPercentage === undefined || this.editingFlag.rolloutPercentage === null) {
      this.editingFlag.rolloutPercentage = 100;
    }

    this.configService.setFeatureFlag(this.editingFlag).subscribe(
      () => {
        // Navigate back to feature flags list
        this.router.navigate(['/feature-flags']);
      },
      error => {
        console.error('Error saving feature flag:', error);
        const errorMessage = error.error?.error || error.message || 'Error saving feature flag. Please try again.';
        alert(errorMessage);
      }
    );
  }

  cancel() {
    this.router.navigate(['/feature-flags']);
  }
}

