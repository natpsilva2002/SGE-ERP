import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AdminRole,
  AdminUser,
  CreateAdminUser,
  UpdateAdminUser
} from '../models/administration.models';

@Injectable({
  providedIn: 'root'
})
export class AdministrationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.apiUrl}/User`);
  }

  createUser(dto: CreateAdminUser): Observable<AdminUser> {
    return this.http.post<AdminUser>(`${this.apiUrl}/User`, dto);
  }

  updateUser(id: string, dto: UpdateAdminUser): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.apiUrl}/User/${id}`, dto);
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/User/${id}`);
  }

  getRoles(): Observable<AdminRole[]> {
    return this.http.get<AdminRole[]>(`${this.apiUrl}/Role`);
  }
}
