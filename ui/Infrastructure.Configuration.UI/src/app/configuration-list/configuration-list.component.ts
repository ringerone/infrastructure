import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfigurationService, Configuration } from '../configuration.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-configuration-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="configuration-list">
      <h2>Configurations</h2>
      
      <div class="filters">
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
            <tr *ngFor="let config of configurations | keyvalue">
              <td>{{ config.key }}</td>
              <td>{{ config.value | json }}</td>
              <td>{{ getScope(config.key) }}</td>
              <td>{{ getScopeIdentifier(config.key) }}</td>
              <td>
                <button (click)="editConfiguration(config.key)">Edit</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="add-config" *ngIf="showAddForm">
        <h3>Add/Edit Configuration</h3>
        <form (ngSubmit)="saveConfiguration()">
          <div>
            <label>Key:</label>
            <input type="text" [(ngModel)]="editingConfig.key" name="key" required>
          </div>
          <div>
            <label>Value:</label>
            <input type="text" [(ngModel)]="editingConfig.value" name="value" required>
          </div>
          <div>
            <label>Scope:</label>
            <select [(ngModel)]="editingConfig.scope" name="scope" required>
              <option value="Global">Global</option>
              <option value="Environment">Environment</option>
              <option value="Region">Region</option>
              <option value="Tenant">Tenant</option>
              <option value="User">User</option>
            </select>
          </div>
          <div>
            <label>Scope Identifier:</label>
            <input type="text" [(ngModel)]="editingConfig.scopeIdentifier" name="scopeIdentifier">
          </div>
          <button type="submit">Save</button>
          <button type="button" (click)="cancelEdit()">Cancel</button>
        </form>
      </div>

      <button (click)="showAddForm = true" *ngIf="!showAddForm">Add Configuration</button>
    </div>
  `,
  styles: [`
    .configuration-list {
      padding: 20px;
    }
    .filters {
      margin-bottom: 20px;
      display: flex;
      gap: 10px;
    }
    .filters select, .filters input {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
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
    .add-config {
      margin-top: 20px;
      padding: 20px;
      border: 1px solid #ddd;
      border-radius: 4px;
      background-color: #f8f9fa;
    }
    .add-config form div {
      margin-bottom: 15px;
    }
    .add-config label {
      display: block;
      margin-bottom: 5px;
      font-weight: bold;
    }
    .add-config input, .add-config select {
      width: 100%;
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
  `]
})
export class ConfigurationListComponent implements OnInit, OnDestroy {
  configurations: Record<string, any> = {};
  selectedScope = '';
  scopeIdentifier = '';
  showAddForm = false;
  editingConfig: any = { key: '', value: '', scope: 'Global', scopeIdentifier: '' };
  private subscription?: Subscription;

  constructor(private configService: ConfigurationService) {}

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
    this.configService.getConfigurations(this.selectedScope, this.scopeIdentifier).subscribe(
      data => {
        this.configurations = data;
      },
      error => {
        console.error('Error loading configurations:', error);
      }
    );
  }

  editConfiguration(key: string) {
    this.configService.getConfiguration(key, this.selectedScope, this.scopeIdentifier).subscribe(
      data => {
        this.editingConfig = { key: data.key, value: data.value, scope: data.scope, scopeIdentifier: data.scopeIdentifier };
        this.showAddForm = true;
      }
    );
  }

  saveConfiguration() {
    this.configService.setConfiguration(
      this.editingConfig.key,
      this.editingConfig.value,
      this.editingConfig.scope,
      this.editingConfig.scopeIdentifier
    ).subscribe(
      () => {
        this.loadConfigurations();
        this.cancelEdit();
      },
      error => {
        console.error('Error saving configuration:', error);
      }
    );
  }

  cancelEdit() {
    this.showAddForm = false;
    this.editingConfig = { key: '', value: '', scope: 'Global', scopeIdentifier: '' };
  }

  getScope(key: string): string {
    // This would need to be implemented based on actual data structure
    return 'Global';
  }

  getScopeIdentifier(key: string): string {
    // This would need to be implemented based on actual data structure
    return '';
  }
}

