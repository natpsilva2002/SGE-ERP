import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Dashboard, WorkCosts } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${this.apiUrl}/Dashboard`);
  }

  getWorkCosts(params: { workId?: string; startDate?: string; endDate?: string }): Observable<WorkCosts> {
    let httpParams = new HttpParams();
    if (params.workId) httpParams = httpParams.set('workId', params.workId);
    if (params.startDate) httpParams = httpParams.set('startDate', params.startDate);
    if (params.endDate) httpParams = httpParams.set('endDate', params.endDate);
    return this.http.get<WorkCosts>(`${this.apiUrl}/Dashboard/work-costs`, { params: httpParams });
  }
}
