import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  @Output() cancelRegister = new EventEmitter<boolean>();
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
      this.errorMessage = 'Please fill in all required fields.';
      this.successMessage = '';
      return;
    }

    // Check if passwords match
    if (this.registerForm.password !== this.registerForm.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      this.successMessage = '';
      return;
    }

    // Fake registration logic
    console.log('Register successful:', this.registerForm);
    this.errorMessage = '';
    this.successMessage = 'Registration successful!';

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

  cancel(){
    this.cancelRegister.emit(false);
  }

  gotoDashboard() {
    this.router.navigate(['/dashboard']);
  }
}

