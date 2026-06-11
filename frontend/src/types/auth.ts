// Auth DTOs matching backend exactly

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface VerifyEmailRequest {
  email: string;
  token: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
  devPasswordResetToken?: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUserDto;
  devEmailVerificationToken?: string;
}

export interface AuthUserDto {
  id: string;
  email: string;
  fullName: string;
  isEmailVerified: boolean;
  roles: string[];
}
