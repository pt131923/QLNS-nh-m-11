import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import {
  FormBuilder,
  Validators,
  FormGroup
} from '@angular/forms';
import { Router } from '@angular/router';

// Custom validator kiểm tra password và confirmPassword
export function MustMatch(controlName: string, matchingControlName: string) {
  return (formGroup: FormGroup) => {
    const control = formGroup.controls[controlName];
    const matchingControl = formGroup.controls[matchingControlName];

    if (matchingControl.errors && !matchingControl.errors['mustMatch']) {
      return;
    }

    if (control.value !== matchingControl.value) {
      matchingControl.setErrors({ mustMatch: true });
    } else {
      matchingControl.setErrors(null);
    }
  };
}

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {

  @Output() cancelRegister = new EventEmitter<boolean>();

  registerForm!: FormGroup;
  submitted = false;

  errorMessage = '';
  successMessage = '';

  constructor(private router: Router, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm() {
    this.registerForm = this.fb.group(
      {
        username: ['', Validators.required],
        password: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]]
      },
      {
        validator: MustMatch('password', 'confirmPassword')
      }
    );
  }

  // Getter để dùng f['username'] trong HTML
  get f() {
    return this.registerForm.controls;
  }

  onSubmit() {
    this.submitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    // 🔥 Đánh dấu tất cả field là touched để hiển thị lỗi ngay
    Object.values(this.registerForm.controls).forEach(control => {
      control.markAsTouched();
      control.markAsDirty();
    });

    // Nếu form invalid → hiển thị thông báo lỗi chung
    if (this.registerForm.invalid) {
      // Kiểm tra từng trường để hiển thị thông báo lỗi phù hợp
      if (this.registerForm.controls['username'].errors?.['required']) {
        this.errorMessage = 'Vui lòng nhập tên người dùng.';
      } else if (this.registerForm.controls['password'].errors?.['required']) {
        this.errorMessage = 'Vui lòng nhập mật khẩu.';
      } else if (this.registerForm.controls['password'].errors?.['minlength']) {
        this.errorMessage = 'Mật khẩu phải có ít nhất 6 ký tự.';
      } else if (this.registerForm.controls['confirmPassword'].errors?.['required']) {
        this.errorMessage = 'Vui lòng xác nhận mật khẩu.';
      } else if (this.registerForm.controls['confirmPassword'].errors?.['mustMatch']) {
        this.errorMessage = 'Mật khẩu xác nhận không khớp.';
      } else if (this.registerForm.controls['email'].errors?.['required']) {
        this.errorMessage = 'Vui lòng nhập email.';
      } else if (this.registerForm.controls['email'].errors?.['email']) {
        this.errorMessage = 'Email không hợp lệ.';
      } else {
        this.errorMessage = 'Vui lòng điền đầy đủ thông tin.';
      }
      return;
    }

    // Thành công
    console.log('Register successful:', this.registerForm.value);

    this.successMessage = 'Registration successful! Redirecting...';

    setTimeout(() => {
      this.router.navigate(['/login']);
    }, 1000);
  }

  onReset() {
    this.registerForm.reset();
    this.submitted = false;
    this.errorMessage = '';
    this.successMessage = '';
  }

  cancel() {
    this.cancelRegister.emit(false);
  }

  gotoDashboard() {
    this.router.navigate(['/dashboard']);
  }
}
