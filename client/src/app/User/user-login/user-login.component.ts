import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../_services/auth.service';
import { DashboardService } from '../../_services/dashboard.service';

@Component({
  selector: 'app-user-login',
  templateUrl: './user-login.component.html',
  styleUrls: ['./user-login.component.css']
})
export class UserLoginComponent implements OnInit {
  loginForm!: FormGroup;
  submitted = false;
  errorMessage = '';
  successMessage = '';
  loading = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService,
    private dashboardService: DashboardService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
      rememberMe: [false]
    });

    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard'], { replaceUrl: true });
    }
  }

  onSubmit(): void {
    this.submitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.loginForm.invalid) {
      this.errorMessage = 'Vui lòng điền đầy đủ thông tin đăng nhập.';
      return;
    }

    const { username, password } = this.loginForm.value;
    this.loading = true;

    this.authService.login({ username, password }).subscribe({
      next: (res: any) => {
        this.loading = false;

        if (!res || !res.token) {
          this.errorMessage = 'Token không hợp lệ từ server.';
          return;
        }

        this.authService.saveToken(res.token);

        const userData = res.user || res;
        if (userData && !userData.token) {
          localStorage.setItem('user', JSON.stringify(userData));
        } else if (res.user) {
          localStorage.setItem('user', JSON.stringify(res.user));
        }

        this.successMessage = 'Đăng nhập thành công!';

        setTimeout(() => {
          this.dashboardService.startSignalRConnection();
          this.router.navigate(['/dashboard'], { replaceUrl: true });
        }, 100);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message || 'Sai tên đăng nhập hoặc mật khẩu.';
      }
    });
  }

  onReset(): void {
    this.loginForm.reset();
    this.submitted = false;
    this.errorMessage = '';
    this.successMessage = '';
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.onSubmit();
    } else if (event.key === 'Escape') {
      this.onReset();
    }
  }
}
