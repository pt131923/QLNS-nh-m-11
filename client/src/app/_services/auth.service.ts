import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  login(token: string, remember: boolean): void {
    // Clear token cũ trước
    localStorage.removeItem('token');
    sessionStorage.removeItem('token');

    // Lưu token mới
    if (remember) {
      localStorage.setItem('token', token);
    } else {
      sessionStorage.setItem('token', token);
    }
  }

  logout(): void {
    localStorage.removeItem('token');
    sessionStorage.removeItem('token');
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('token') || !!sessionStorage.getItem('token');
  }

  getToken(): string | null {
    return localStorage.getItem('token') || sessionStorage.getItem('token');
  }
}
