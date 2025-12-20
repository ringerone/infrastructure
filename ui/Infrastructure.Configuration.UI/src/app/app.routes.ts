import { Routes } from '@angular/router';
import { ConfigurationListComponent } from './configuration-list/configuration-list.component';
import { FeatureFlagListComponent } from './feature-flag-list/feature-flag-list.component';

export const routes: Routes = [
  { path: '', redirectTo: '/configurations', pathMatch: 'full' },
  { path: 'configurations', component: ConfigurationListComponent },
  { path: 'feature-flags', component: FeatureFlagListComponent }
];

