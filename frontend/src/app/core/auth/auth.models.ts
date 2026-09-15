export interface AuthUser {
  id: string;
  name: string;
  email: string;
  roleId: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: AuthUser;
}
