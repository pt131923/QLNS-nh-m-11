import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../_services/auth.service';

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
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
      rememberMe: [false]
    });

    // Nếu đã login → chuyển về Dashboard luôn
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  // -------------------------------------------------------
  // SUBMIT LOGIN
  // -------------------------------------------------------
  onSubmit(): void {
    this.submitted = true;
    this.errorMessage = '';

    if (this.loginForm.invalid) {
      this.errorMessage = 'Vui lòng nhập đầy đủ thông tin.';
      return;
    }

    this.loading = true;

    const { username, password, rememberMe } = this.loginForm.value;

    this.authService.login({ username, password }).subscribe({
      next: (res: any) => {
        this.loading = false;

        if (!res || !res.token) {
          this.errorMessage = 'Token không hợp lệ từ server.';
          return;
        }

        // Lưu token
        this.authService.saveToken(res.token, rememberMe);

        // Điều hướng
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },

      error: (err) => {
        this.loading = false;

        switch (err.status) {
          case 400:
          case 401:
            this.errorMessage = 'Sai username hoặc password.';
            break;

          case 0:
            this.errorMessage = 'Không thể kết nối server.';
            break;

          default:
            this.errorMessage = 'Đã xảy ra lỗi. Vui lòng thử lại.';
            break;
        }
      }
    });
  }

  // -------------------------------------------------------
  // RESET FORM
  // -------------------------------------------------------
  onReset(): void {
    this.loginForm.reset();
    this.submitted = false;
    this.errorMessage = '';
  }

  // -------------------------------------------------------
  // KEYBOARD SHORTCUT
  // -------------------------------------------------------
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') this.onSubmit();
    if (event.key === 'Escape') this.onReset();
  }

  // -------------------------------------------------------
  // LOGOUT
  // -------------------------------------------------------
  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // -------------------------------------------------------
  // CANCEL
  // -------------------------------------------------------
  cancel() {
    this.cancelLogin.emit(false);
  }

  gotoDashboard() {
    this.router.navigate(['/dashboard']);
  }
}
