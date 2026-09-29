import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { FinanceQueueItem } from '../models/finance-queue.models';

@Injectable({
  providedIn: 'root'
})
export class FinanceQueueService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAll(): Observable<FinanceQueueItem[]> {
    return this.http.get<FinanceQueueItem[]>(`${this.apiUrl}/Finance/queue`);
  }
}
