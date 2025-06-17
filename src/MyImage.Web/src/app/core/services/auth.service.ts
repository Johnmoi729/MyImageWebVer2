import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/api.models';
import { AuthResponse, LoginRequest, RegisterRequest } from '../../shared/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<AuthResponse | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    // Load user from token on startup
    this.loadUserFromToken();
  }

  register(request: RegisterRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/register`, request)
      .pipe(
        tap(response => {
          if (response.success) {
            this.setCurrentUser(response.data);
          }
        })
      );
  }

  login(request: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/login`, request)
      .pipe(
        tap(response => {
          if (response.success) {
            this.setCurrentUser(response.data);
          }
        })
      );
  }

  logout(): void {
    localStorage.removeItem(environment.tokenKey);
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): AuthResponse | null {
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'admin';
  }

  getToken(): string | null {
    return localStorage.getItem(environment.tokenKey);
  }

  private setCurrentUser(authResponse: AuthResponse): void {
    localStorage.setItem(environment.tokenKey, authResponse.token);
    this.currentUserSubject.next(authResponse);
  }

  private loadUserFromToken(): void {
    const token = this.getToken();
    if (token) {
      // Get user info from token
      this.http.get<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/me`)
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.currentUserSubject.next(response.data);
            }
          },
          error: () => {
            // Token invalid, clear it
            this.logout();
          }
        });
    }
  }
}
