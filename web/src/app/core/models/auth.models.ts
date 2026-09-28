// These contracts describe Identity requests and safe user profiles.
// Keeping them together makes the browser's API boundary explicit and typed.
export type Role = 'Attendee' | 'Organizer' | 'Admin';
export interface User {
  id: string;
  email: string;
  fullName: string;
  role: Role;
}
export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest extends LoginRequest {
  fullName: string;
}
export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}
export const TOKEN_KEY = 'eventhub.accessToken';
export const ROLES = { attendee: 'Attendee', organizer: 'Organizer', admin: 'Admin' } as const;
