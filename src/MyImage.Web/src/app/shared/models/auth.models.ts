export interface LoginRequest {
  identifier: string; // Can be email or userId
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  token: string;
  expiresIn: number;
  tokenType: string;
}
