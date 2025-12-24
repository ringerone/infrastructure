import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ConfigurationService } from '../configuration.service';

@Component({
  selector: 'app-configuration-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="configuration-edit">
      <h2>{{ isEditMode ? 'Edit Configuration' : 'Add Configuration' }}</h2>
      
      <form (ngSubmit)="saveConfiguration()">
        <div class="form-group">
          <label for="key">Key:</label>
          <input 
            type="text" 
            id="key"
            [(ngModel)]="editingConfig.key" 
            name="key" 
            required
            [readonly]="isEditMode">
          <small *ngIf="isEditMode" class="form-text">Key cannot be changed when editing</small>
        </div>
        
        <div class="form-group">
          <label for="value">Value:</label>
          <input 
            type="text" 
            id="value"
            [(ngModel)]="editingConfig.value" 
            name="value" 
            required>
        </div>
        
        <div class="form-group">
          <label for="scope">Scope:</label>
          <select 
            id="scope"
            [(ngModel)]="editingConfig.scope" 
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
            [(ngModel)]="editingConfig.scopeIdentifier" 
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
    .configuration-edit {
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
    
    input[type="text"],
    select {
      width: 100%;
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      box-sizing: border-box;
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
export class ConfigurationEditComponent implements OnInit {
  editingConfig: any = { key: '', value: '', scope: 'Global', scopeIdentifier: '' };
  isEditMode = false;
  private configKey: string | null = null;

  constructor(
    private configService: ConfigurationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      const key = params['key'];
      if (key) {
        this.isEditMode = true;
        this.configKey = key;
        this.loadConfiguration(key);
      } else {
        this.isEditMode = false;
        this.configKey = null;
      }
    });
  }

  loadConfiguration(key: string) {
    // Get the configuration from the service
    this.configService.getConfiguration(key).subscribe(
      data => {
        this.editingConfig = {
          key: data.key || key,
          value: data.value,
          scope: data.scope || 'Global',
          scopeIdentifier: data.scopeIdentifier || ''
        };
      },
      error => {
        console.error('Error loading configuration:', error);
        // If error, navigate back
        this.router.navigate(['/configurations']);
      }
    );
  }

  saveConfiguration() {
    // Validation
    if (!this.editingConfig.key || this.editingConfig.key.trim() === '') {
      alert('Key is required');
      return;
    }

    if (this.editingConfig.value === undefined || this.editingConfig.value === null || 
        (typeof this.editingConfig.value === 'string' && this.editingConfig.value.trim() === '')) {
      alert('Value is required');
      return;
    }

    if (this.editingConfig.scope === undefined || this.editingConfig.scope === null || this.editingConfig.scope === '') {
      alert('Scope is required');
      return;
    }

    this.configService.setConfiguration(
      this.editingConfig.key,
      this.editingConfig.value,
      this.editingConfig.scope,
      this.editingConfig.scopeIdentifier
    ).subscribe(
      () => {
        // Navigate back to configurations list
        this.router.navigate(['/configurations']);
      },
      error => {
        console.error('Error saving configuration:', error);
        const errorMessage = error.error?.error || error.message || 'Error saving configuration. Please try again.';
        alert(errorMessage);
      }
    );
  }

  cancel() {
    this.router.navigate(['/configurations']);
  }
}

