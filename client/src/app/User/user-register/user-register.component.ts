import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  templateUrl: './user-register.component.html',
  styleUrls: ['./user-register.component.css']
})
export class UserRegisterComponent {
  registerForm: any; // Define the type according to your form structure
  submitted = false;
  errorMessage = '';
  successMessage = '';

  constructor( private router: Router, private fb: FormBuilder ) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm() {
    // Initialize your form here, e.g., using FormBuilder
    this.registerForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
      confirmPassword: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit() {
    this.submitted = true;

    // Check if the form is valid
    if (!this.registerForm.valid) {
      this.errorMessage = 'Vui lòng điền đầy đủ thông tin đăng ký.';
      this.successMessage = '';
      return;
    }

    // Check if passwords match
    if (this.registerForm.password !== this.registerForm.confirmPassword) {
      this.errorMessage = 'Mật khẩu không khớp.';
      this.successMessage = '';
      return;
    }

    // Fake registration logic
    console.log('Đăng ký thành công:', this.registerForm);
    this.errorMessage = '';
    this.successMessage = 'Đăng ký thành công!';

    setTimeout(() => {
      this.router.navigate(['/login']); // Redirect to login page after successful registration
    }
    , 1000); // Redirect after 1 seconds
  }

  onReset() {
    this.registerForm = {};
    this.submitted = false;
    this.errorMessage = '';
    this.successMessage = '';
  }
}

