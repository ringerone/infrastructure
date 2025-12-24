import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from '../environments/environment';

export interface Configuration {
  key: string;
  value: any;
  scope?: string;
  scopeIdentifier?: string;
}

export interface FeatureFlag {
  name: string;
  enabled: boolean;
  scope?: string;
  scopeIdentifier?: string;
  rolloutPercentage?: number;
  variant?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface Tenant {
  tenantIdentifier: string;
  name: string;
  status: 'Active' | 'Inactive' | 'Pending' | 'Suspended';
  comments?: string;
  salesTerms?: string;
  contactEmail?: string;
  contactPhone?: string;
  contactName?: string;
  createdAt?: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ConfigurationService {
  private apiUrl = environment.apiUrl;
  private hubConnection?: HubConnection;
  private configurationChanged$ = new Subject<Configuration>();
  private featureFlagChanged$ = new Subject<FeatureFlag>();

  constructor(private http: HttpClient) {
    this.initializeSignalR();
  }

  private async initializeSignalR() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${this.apiUrl.replace('/api', '')}/hubs/configuration`)
      .build();

    this.hubConnection.on('ConfigurationChanged', (data: any) => {
      this.configurationChanged$.next(data);
    });

    this.hubConnection.on('FeatureFlagChanged', (data: any) => {
      this.featureFlagChanged$.next(data);
    });

    try {
      await this.hubConnection.start();
      console.log('SignalR connection established');
    } catch (error) {
      console.error('Error establishing SignalR connection:', error);
    }
  }

  getConfigurations(scope?: string, scopeIdentifier?: string, pageNumber: number = 1, pageSize: number = 10, search?: string): Observable<any> {
    let params = new HttpParams();
    if (scope) params = params.set('scope', scope);
    if (scopeIdentifier) params = params.set('scopeIdentifier', scopeIdentifier);
    params = params.set('pageNumber', pageNumber.toString());
    params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);

    return this.http.get<any>(`${this.apiUrl}/configuration`, { params });
  }

  getConfiguration(key: string, scope?: string, scopeIdentifier?: string): Observable<any> {
    let params = new HttpParams();
    if (scope) params = params.set('scope', scope);
    if (scopeIdentifier) params = params.set('scopeIdentifier', scopeIdentifier);

    return this.http.get<any>(`${this.apiUrl}/configuration/${key}`, { params });
  }

  setConfiguration(key: string, value: any, scope: string, scopeIdentifier?: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/configuration`, {
      key,
      value,
      scope,
      scopeIdentifier
    });
  }

  getFeatureFlags(scope?: string, scopeIdentifier?: string, pageNumber: number = 1, pageSize: number = 10, search?: string): Observable<any> {
    let params = new HttpParams();
    if (scope) params = params.set('scope', scope);
    if (scopeIdentifier) params = params.set('scopeIdentifier', scopeIdentifier);
    params = params.set('pageNumber', pageNumber.toString());
    params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);

    return this.http.get<any>(`${this.apiUrl}/featureflag`, { params });
  }

  getFeatureFlag(name: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/featureflag/${name}`);
  }

  setFeatureFlag(flag: FeatureFlag): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/featureflag`, flag);
  }

  onConfigurationChanged(): Observable<Configuration> {
    return this.configurationChanged$.asObservable();
  }

  onFeatureFlagChanged(): Observable<FeatureFlag> {
    return this.featureFlagChanged$.asObservable();
  }

  getTenants(
    pageNumber: number,
    pageSize: number,
    search?: string,
    status?: string
  ): Observable<PagedResult<Tenant>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);

    return this.http.get<PagedResult<Tenant>>(`${this.apiUrl}/tenant`, { params });
  }

  getTenant(tenantIdentifier: string): Observable<Tenant> {
    return this.http.get<Tenant>(`${this.apiUrl}/tenant/${tenantIdentifier}`);
  }

  setTenant(tenant: Tenant): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/tenant`, tenant);
  }

  deleteTenant(tenantIdentifier: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/tenant/${tenantIdentifier}`);
  }
}

