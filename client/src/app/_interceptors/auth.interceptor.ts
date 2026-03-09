import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthService } from '../_services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(private authService: AuthService, private router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const url = req.url;
    const isLoginOrRegister = url.includes('/auth/login') || url.includes('/auth/register');

    let cloned = req;
    if (!isLoginOrRegister) {
      const token = this.authService.getToken();
      if (token) {
        cloned = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        });
      }
    }

    return next.handle(cloned).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          const isLoginOrRegisterRequest = url.includes('/auth/login') || url.includes('/auth/register');
          const currentUrl = this.router.url;
          const isPublicRoute = currentUrl.includes('/login') || currentUrl.includes('/register');

          // Never logout/redirect when the failed request was login or register (let the form show the error)
          if (isLoginOrRegisterRequest || isPublicRoute) {
            return throwError(() => error);
          }

          // Any other 401: clear token and redirect to login so user can re-authenticate
          this.authService.logout('/login');
        }

        return throwError(() => error);
      })
    );
  }
}
