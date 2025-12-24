import { Routes } from '@angular/router';
import { ConfigurationListComponent } from './configuration-list/configuration-list.component';
import { ConfigurationEditComponent } from './configuration-edit/configuration-edit.component';
import { FeatureFlagListComponent } from './feature-flag-list/feature-flag-list.component';
import { FeatureFlagEditComponent } from './feature-flag-edit/feature-flag-edit.component';
import { TenantListComponent } from './tenant-list/tenant-list.component';
import { TenantEditComponent } from './tenant-edit/tenant-edit.component';

export const routes: Routes = [
  { path: '', redirectTo: '/configurations', pathMatch: 'full' },
  { path: 'configurations', component: ConfigurationListComponent },
  { path: 'configurations/new', component: ConfigurationEditComponent },
  { path: 'configurations/edit/:key', component: ConfigurationEditComponent },
  { path: 'feature-flags', component: FeatureFlagListComponent },
  { path: 'feature-flags/new', component: FeatureFlagEditComponent },
  { path: 'feature-flags/edit/:name', component: FeatureFlagEditComponent },
  { path: 'tenants', component: TenantListComponent },
  { path: 'tenants/new', component: TenantEditComponent },
  { path: 'tenants/edit/:identifier', component: TenantEditComponent }
];

