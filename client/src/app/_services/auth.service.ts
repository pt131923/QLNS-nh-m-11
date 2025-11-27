import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private baseUrl = 'https://localhost:7001/api/auth'; // đổi theo API backend của bạn

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  // ------------------------------------------------------------
  // LOGIN
  // ------------------------------------------------------------
  login(model: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/login`, model);
  }

  // ------------------------------------------------------------
  // LOGOUT
  // ------------------------------------------------------------
  logout(): void {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }

  // ------------------------------------------------------------
  // LƯU TOKEN
  // ------------------------------------------------------------
  saveToken(token: string, rememberMe?: any): void {
    localStorage.setItem('token', token);
  }

  // ------------------------------------------------------------
  // LẤY TOKEN
  // ------------------------------------------------------------
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // ------------------------------------------------------------
  // KIỂM TRA LOGIN
  // ------------------------------------------------------------
  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;

    // có thể check thêm decode token (expire)
    return true;
  }

  // ------------------------------------------------------------
  // CHECK TOKEN HẾT HẠN (OPTIONAL)
  // ------------------------------------------------------------
  isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; // convert to ms
      return Date.now() > exp;
    } catch (error) {
      return true;
    }
  }
}
