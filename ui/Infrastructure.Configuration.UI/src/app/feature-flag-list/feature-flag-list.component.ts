import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfigurationService, FeatureFlag } from '../configuration.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-feature-flag-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="feature-flag-list">
      <h2>Feature Flags</h2>
      
      <button (click)="loadFeatureFlags()">Refresh</button>

      <div class="flags-table">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Enabled</th>
              <th>Variant</th>
              <th>Rollout %</th>
              <th>Scope</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let flag of featureFlags | keyvalue">
              <td>{{ flag.key }}</td>
              <td>{{ flag.value ? 'Yes' : 'No' }}</td>
              <td>{{ getVariant(flag.key) }}</td>
              <td>{{ getRolloutPercentage(flag.key) }}%</td>
              <td>{{ getScope(flag.key) }}</td>
              <td>
                <button (click)="editFeatureFlag(flag.key)">Edit</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="add-flag" *ngIf="showAddForm">
        <h3>Add/Edit Feature Flag</h3>
        <form (ngSubmit)="saveFeatureFlag()">
          <div>
            <label>Name:</label>
            <input type="text" [(ngModel)]="editingFlag.name" name="name" required>
          </div>
          <div>
            <label>Enabled:</label>
            <input type="checkbox" [(ngModel)]="editingFlag.enabled" name="enabled">
          </div>
          <div>
            <label>Rollout Percentage (0-100):</label>
            <input type="number" [(ngModel)]="editingFlag.rolloutPercentage" name="rolloutPercentage" min="0" max="100">
          </div>
          <div>
            <label>Variant:</label>
            <input type="text" [(ngModel)]="editingFlag.variant" name="variant">
          </div>
          <div>
            <label>Scope:</label>
            <select [(ngModel)]="editingFlag.scope" name="scope" required>
              <option value="Global">Global</option>
              <option value="Environment">Environment</option>
              <option value="Region">Region</option>
              <option value="Tenant">Tenant</option>
              <option value="User">User</option>
            </select>
          </div>
          <div>
            <label>Scope Identifier:</label>
            <input type="text" [(ngModel)]="editingFlag.scopeIdentifier" name="scopeIdentifier">
          </div>
          <button type="submit">Save</button>
          <button type="button" (click)="cancelEdit()">Cancel</button>
        </form>
      </div>

      <button (click)="showAddForm = true" *ngIf="!showAddForm">Add Feature Flag</button>
    </div>
  `,
  styles: [`
    .feature-flag-list {
      padding: 20px;
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
    button {
      padding: 8px 16px;
      background-color: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      margin-right: 10px;
    }
    button:hover {
      background-color: #0056b3;
    }
    .add-flag {
      margin-top: 20px;
      padding: 20px;
      border: 1px solid #ddd;
      border-radius: 4px;
      background-color: #f8f9fa;
    }
    .add-flag form div {
      margin-bottom: 15px;
    }
    .add-flag label {
      display: block;
      margin-bottom: 5px;
      font-weight: bold;
    }
    .add-flag input, .add-flag select {
      width: 100%;
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
  `]
})
export class FeatureFlagListComponent implements OnInit, OnDestroy {
  featureFlags: Record<string, boolean> = {};
  showAddForm = false;
  editingFlag: FeatureFlag = {
    name: '',
    enabled: false,
    scope: 'Global',
    rolloutPercentage: 100
  };
  private subscription?: Subscription;

  constructor(private configService: ConfigurationService) {}

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
    this.configService.getFeatureFlags().subscribe(
      data => {
        this.featureFlags = data;
      },
      error => {
        console.error('Error loading feature flags:', error);
      }
    );
  }

  editFeatureFlag(name: string) {
    this.configService.getFeatureFlag(name).subscribe(
      data => {
        this.editingFlag = {
          name: data.featureName,
          enabled: data.enabled,
          variant: data.variant,
          scope: 'Global',
          rolloutPercentage: 100
        };
        this.showAddForm = true;
      }
    );
  }

  saveFeatureFlag() {
    this.configService.setFeatureFlag(this.editingFlag).subscribe(
      () => {
        this.loadFeatureFlags();
        this.cancelEdit();
      },
      error => {
        console.error('Error saving feature flag:', error);
      }
    );
  }

  cancelEdit() {
    this.showAddForm = false;
    this.editingFlag = { name: '', enabled: false, scope: 'Global', rolloutPercentage: 100 };
  }

  getVariant(name: string): string {
    // This would need to be implemented based on actual data structure
    return '-';
  }

  getRolloutPercentage(name: string): number {
    // This would need to be implemented based on actual data structure
    return 100;
  }

  getScope(name: string): string {
    // This would need to be implemented based on actual data structure
    return 'Global';
  }
}

