import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(private router: Router) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean | UrlTree | Observable<boolean | UrlTree> | Promise<boolean | UrlTree> {
    const token = localStorage.getItem('token');
    if (token) {
      return true;
    }

    // Nếu không có token, kiểm tra xem có phải đang truy cập vào trang đăng nhập không
    if (state.url === '/login') {
      return true;
    }

    // Chuyển về login nếu chưa đăng nhập
    return this.router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
}
