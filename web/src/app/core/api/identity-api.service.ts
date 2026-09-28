// This is the single browser adapter for Identity's HTTP endpoints.
// AuthService owns session state; this adapter only sends typed requests.
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LoginRequest, LoginResponse, RegisterRequest, User, Role } from '../models/auth.models';
@Injectable({ providedIn: 'root' })
export class IdentityApiService {
  private readonly http = inject(HttpClient);
  // Verify credentials without retrying a login request.
  login(body: LoginRequest) {
    return this.http.post<LoginResponse>('/api/identity/auth/login', body);
  }
  // Identity assigns Attendee; the browser never submits a role.
  register(body: RegisterRequest) {
    return this.http.post<LoginResponse>('/api/identity/auth/register', body);
  }
  // Admin reads safe user profiles; passwords never cross this boundary.
  users() {
    return this.http.get<User[]>('/api/identity/users');
  }
  // Stored role changes affect newly issued tokens, not existing sessions.
  changeRole(id: string, role: Role) {
    return this.http.put<User>(`/api/identity/users/${id}/role`, { role });
  }
  // Let the API validate the signed token restored after a refresh.
  me() {
    return this.http.get<User>('/api/identity/auth/me');
  }
}
