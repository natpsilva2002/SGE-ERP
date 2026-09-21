import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthUser, LoginResponse } from './auth.models';

const tokenKey = 'sge_token';
const userKey = 'sge_user';
const expiresAtKey = 'sge_token_expires_at';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly currentUserSubject = new BehaviorSubject<AuthUser | null>(
    this.loadStoredUser()
  );

  readonly currentUser$ = this.currentUserSubject.asObservable();

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/Auth/login`, {
        email,
        password
      })
      .pipe(
        tap((response) => this.storeSession(response))
      );
  }

  logout(redirect = true): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(userKey);
    localStorage.removeItem(expiresAtKey);
    this.currentUserSubject.next(null);

    if (redirect) {
      void this.router.navigate(['/login']);
    }
  }

  refreshCurrentUser(): Observable<AuthUser> {
    return this.http
      .get<AuthUser>(`${environment.apiUrl}/Auth/me`)
      .pipe(tap((user) => this.storeUser(user)));
  }

  getCurrentUser(): AuthUser | null {
    if (this.isTokenExpired()) {
      this.logout(false);
      return null;
    }

    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    return !!this.getToken() && !this.isTokenExpired();
  }

  hasRole(roles: readonly string[]): boolean {
    const user = this.getCurrentUser();

    if (!user) {
      return false;
    }

    return roles.includes(this.normalizeRole(user.role));
  }

  getToken(): string | null {
    return localStorage.getItem(tokenKey);
  }

  getTokenExpiresAt(): string | null {
    return localStorage.getItem(expiresAtKey);
  }

  private storeSession(response: LoginResponse): void {
    localStorage.setItem(tokenKey, response.token);
    localStorage.setItem(expiresAtKey, response.expiresAt);
    this.storeUser(response.user);
  }

  private storeUser(user: AuthUser): void {
    const normalizedUser = {
      ...user,
      role: this.normalizeRole(user.role)
    };
    localStorage.setItem(userKey, JSON.stringify(normalizedUser));
    this.currentUserSubject.next(normalizedUser);
  }

  private loadStoredUser(): AuthUser | null {
    if (this.isTokenExpired()) {
      this.clearStoredSession();
      return null;
    }

    const value = localStorage.getItem(userKey);

    if (!value) {
      return null;
    }

    try {
      const user = JSON.parse(value) as AuthUser;
      return { ...user, role: this.normalizeRole(user.role) };
    } catch {
      this.clearStoredSession();
      return null;
    }
  }

  private isTokenExpired(): boolean {
    const expiresAt = localStorage.getItem(expiresAtKey);

    if (!expiresAt) {
      return true;
    }

    return new Date(expiresAt).getTime() <= Date.now();
  }

  private clearStoredSession(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(userKey);
    localStorage.removeItem(expiresAtKey);
  }

  private normalizeRole(role: string | null | undefined): string {
    const normalized = (role ?? '').trim().toLowerCase();
    return normalized === 'administrador' || normalized === 'admin'
      ? 'Administrador'
      : normalized === 'compras' || normalized === 'buyer'
        ? 'Compras'
        : normalized === 'almoxarife' || normalized === 'warehouse' || normalized === 'requester'
          ? 'Almoxarife'
          : normalized === 'financeiro' || normalized === 'finance'
            ? 'Financeiro'
            : normalized === 'approver'
              ? 'Administrador'
              : role?.trim() ?? '';
  }
}
