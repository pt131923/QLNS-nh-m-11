import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

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

  constructor(private fb: FormBuilder, private router: Router) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
      rememberMe: [false]
    });
  }

  onSubmit(): void {
    this.submitted = true;

    if (this.loginForm.invalid) {
      this.errorMessage = 'Vui lòng điền đầy đủ thông tin đăng nhập.';
      this.successMessage = '';
      return;
    }

    const { username, password } = this.loginForm.value;

    // Fake login logic
    if (username === 'admin' && password === 'password') {
      console.log('Đăng nhập thành công');
      this.errorMessage = '';
      this.successMessage = 'Đăng nhập thành công!';
      // Có thể chuyển hướng tại đây nếu cần
    } else {
      this.errorMessage = 'Sai tên đăng nhập hoặc mật khẩu.';
    }
    setTimeout(() => {
      localStorage.setItem('token', 'Api');
      this.router.navigate(['/employees']);
    }, 1000);
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
