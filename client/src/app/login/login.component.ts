import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../_services/auth.service';
import { DashboardService } from '../_services/dashboard.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  @Output() cancelLogin = new EventEmitter<boolean>();

  loginForm!: FormGroup;
  submitted = false;
  errorMessage = '';
  loading = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
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

  // Trong login.component.ts

onSubmit(): void {
  this.submitted = true;
  this.errorMessage = '';

  if (this.loginForm.invalid) {
    this.errorMessage = 'Vui lòng nhập đầy đủ thông tin.';
    return;
  }

  this.loading = true;

  const { username, password } = this.loginForm.value;

  this.authService.login({ username, password }).subscribe({
    next: (res: any) => {
      this.loading = false;

      if (!res || !res.token) {
        this.errorMessage = 'Token không hợp lệ từ server.';
        return;
      }

      // 1. Save token TRƯỚC TIÊN
      console.log('💾 Saving token to localStorage...');
      this.authService.saveToken(res.token);
      
      // Verify token was saved
      const savedToken = this.authService.getToken();
      if (!savedToken || savedToken !== res.token) {
        console.error('❌ Token was not saved correctly!');
        this.errorMessage = 'Lỗi lưu token. Vui lòng thử lại.';
        return;
      }
      console.log('✅ Token saved successfully, length:', savedToken.length);

      // 2. Save user info
      let userData = res.user || res;
      if (userData && !userData.token) {
        localStorage.setItem('user', JSON.stringify(userData));
      } else if (res.user) {
        localStorage.setItem('user', JSON.stringify(res.user));
      }

      console.log('✅ Login successful. Token and user saved.');
      
      // 3. Đợi một chút để đảm bảo token đã được lưu và interceptor sẵn sàng
      // Sau đó mới khởi động SignalR và redirect
      setTimeout(() => {
        console.log('🚀 Starting SignalR connection and redirecting...');
        // Khởi động SignalR connection sau khi đăng nhập thành công
        this.dashboardService.startSignalRConnection();

        // Chuyển hướng bằng Router của Angular
        // Điều này ngăn chặn việc tải lại toàn bộ ứng dụng và đảm bảo Interceptor hoạt động ngay lập tức
        this.router.navigate(['/dashboard'], { replaceUrl: true });
      }, 100); // Đợi 100ms để đảm bảo token đã được lưu
    },

    error: (err) => {
      this.loading = false;
      if (err.status === 400 || err.status === 401) {
        this.errorMessage = 'Sai username hoặc password.';
      } else if (err.status === 0) {
        this.errorMessage = 'Không thể kết nối server.';
      } else {
        this.errorMessage = 'Đã xảy ra lỗi. Vui lòng thử lại.';
      }
    }
  });
}

  onReset(): void {
    this.loginForm.reset();
    this.submitted = false;
    this.errorMessage = '';
  }

  onLogout(): void {
    this.authService.logout('/dashboard');
  }

  cancel() {
    this.cancelLogin.emit(false);
  }

  gotoDashboard() {
    this.router.navigate(['/dashboard']);
  }
}
