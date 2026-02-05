import { Injectable, Injector } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { DashboardService } from './dashboard.service';
import { Observable } from 'rxjs/internal/Observable';
import { of } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private baseUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient, private router: Router, private injector: Injector) {}

  // ============================
  // 🔵 LOGIN
  // ============================
  login(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/login`, data);
  }

  // ============================
  // 🔵 TOKEN
  // ============================
  saveToken(token: string): void {
    localStorage.setItem('token', token);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // ============================
  // 🔵 USER (local + API)
  // ============================
  saveUser(user: any): void {
    localStorage.setItem('user', JSON.stringify(user));
  }

  /** 
   * 🚀 KHÔNG BAO GIỜ TRẢ NULL
   * Nếu không có user → trả of(null) để .subscribe() luôn an toàn
   */
  getUser(): Observable<any> {
    const token = this.getToken();
    const userJson = localStorage.getItem('user');
    
    // (1) Kiểm tra token và user đã lưu
    if (token && userJson) {
      try {
        // (2) Trả về user đã lưu NGAY LẬP TỨC để Dashboard tải nhanh hơn
        return of(JSON.parse(userJson));
      } catch (e) {
        console.error("Lỗi parse JSON user:", e);
        return of(null);
      }
    }
    // Nếu không có token hoặc userJson, trả về of(null) để .subscribe() luôn an toàn
    return of(null);
  }

  // ============================
  // 🔵 CHECK LOGIN
  // ============================
  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  // ============================
  // 🔵 LOGOUT
  // ============================
  logout(): void {
    // Stop SignalR connection trước khi logout (lazy injection to avoid circular dependency)
    try {
      const dashboardService = this.injector.get(DashboardService);
      dashboardService.stopSignalRConnection();
    } catch (error) {
      // DashboardService might not be available, ignore
      console.log('DashboardService not available for cleanup');
    }

    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.router.navigate(['/login']);
  }


}
