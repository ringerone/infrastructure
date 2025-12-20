import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

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

@Injectable({
  providedIn: 'root'
})
export class ConfigurationService {
  private apiUrl = 'https://localhost:53012/api';
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

  getConfigurations(scope?: string, scopeIdentifier?: string): Observable<Record<string, any>> {
    let params = new HttpParams();
    if (scope) params = params.set('scope', scope);
    if (scopeIdentifier) params = params.set('scopeIdentifier', scopeIdentifier);

    return this.http.get<Record<string, any>>(`${this.apiUrl}/configuration`, { params });
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

  getFeatureFlags(): Observable<Record<string, boolean>> {
    return this.http.get<Record<string, boolean>>(`${this.apiUrl}/featureflag`);
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
}

