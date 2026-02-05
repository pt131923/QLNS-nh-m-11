import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthService } from '../_services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(private authService: AuthService, private router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    const url = req.url;

    // Log chi tiết để debug
    console.log(`🔐 Interceptor - URL: ${url}`);
    console.log(`🔐 Interceptor - Has token: ${!!token}`);
    if (token) {
      console.log(`🔐 Interceptor - Token length: ${token.length}, Token preview: ${token.substring(0, 20)}...`);
    }

    let cloned = req;
    if (token) {
      cloned = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      console.log(`✅ Interceptor - Added Authorization header to request`);
    } else {
      console.warn(`⚠️ Interceptor - No token available for request to ${url}`);
    }

    return next.handle(cloned).pipe(
      catchError((error: HttpErrorResponse) => {
        console.error(`❌ Interceptor - Error for ${url}:`, error.status, error.message);
        
        // Chỉ xử lý lỗi 401 nếu không phải đang ở trang login/register
        if (error.status === 401) {
          const currentUrl = this.router.url;
          const isPublicRoute = currentUrl.includes('/login') || currentUrl.includes('/register');
          
          console.warn(`⛔ 401 Unauthorized - URL: ${url}, Current route: ${currentUrl}, Is public: ${isPublicRoute}`);
          
          // Nếu đang ở trang public, không logout (có thể là lỗi từ API login)
          if (isPublicRoute) {
            console.warn("⛔ 401 Unauthorized on public route, skipping logout");
            return throwError(() => error);
          }

          // Kiểm tra xem có token hay không trước khi logout
          // Nếu không có token, có thể là request đầu tiên, không cần logout
          if (!token) {
            console.warn("⛔ 401 Unauthorized without token, redirecting to login");
            this.router.navigate(['/login'], { replaceUrl: true });
            return throwError(() => error);
          }

          // Kiểm tra xem có phải là lỗi từ dashboard summary không
          // Nếu có, có thể là token chưa được lưu kịp hoặc có vấn đề timing
          const isDashboardSummary = url.includes('/dashboard/summary');
          
          if (isDashboardSummary) {
            console.warn("⛔ 401 Unauthorized for dashboard summary - checking token again...");
            // Đợi một chút và kiểm tra lại token
            const currentToken = this.authService.getToken();
            if (!currentToken || currentToken !== token) {
              console.warn("⚠️ Token changed or removed, not logging out yet");
              return throwError(() => error);
            }
          }

          // Có token nhưng vẫn bị 401 -> token không hợp lệ hoặc đã hết hạn
          console.warn("⛔ 401 Unauthorized with token – token may be expired, logging out");
          this.authService.logout();
          this.router.navigate(['/login'], { replaceUrl: true });
        }

        return throwError(() => error);
      })
    );
  }
}
