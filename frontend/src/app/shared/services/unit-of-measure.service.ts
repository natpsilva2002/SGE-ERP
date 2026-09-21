import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateUnitOfMeasure,
  UnitOfMeasure,
  UpdateUnitOfMeasure
} from '../models/unit-of-measure.models';

@Injectable({ providedIn: 'root' })
export class UnitOfMeasureService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAll(activeOnly = false): Observable<UnitOfMeasure[]> {
    return this.http.get<UnitOfMeasure[]>(`${this.apiUrl}/UnitOfMeasure`, {
      params: { activeOnly }
    });
  }

  getActive(): Observable<UnitOfMeasure[]> {
    return this.getAll(true);
  }

  create(dto: CreateUnitOfMeasure): Observable<UnitOfMeasure> {
    return this.http.post<UnitOfMeasure>(`${this.apiUrl}/UnitOfMeasure`, dto);
  }

  update(id: string, dto: UpdateUnitOfMeasure): Observable<UnitOfMeasure> {
    return this.http.put<UnitOfMeasure>(`${this.apiUrl}/UnitOfMeasure/${id}`, dto);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/UnitOfMeasure/${id}`);
  }
}
