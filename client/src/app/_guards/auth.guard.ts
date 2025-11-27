import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '../_services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(
    private auth: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean | UrlTree {

    const currentUrl = state.url;

    // ✅ Nếu đang đăng nhập → cho vào
    if (this.auth.isLoggedIn()) {
      return true;
    }

    // ✅ Các route công khai (không cần login)
    const publicRoutes = ['/login', '/register'];

    if (publicRoutes.includes(currentUrl)) {
      return true;
    }

    // ❌ Chưa login → chuyển hướng login + lưu returnUrl
    return this.router.createUrlTree(['/login'], {
      queryParams: { returnUrl: currentUrl }
    });
  }
}
