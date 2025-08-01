import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  submitted = false;
  errorMessage = '';
  successMessage = '';

  constructor(private fb: FormBuilder, private router: Router, private route: ActivatedRoute) {}

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
    this.errorMessage = 'Please fill in all required fields.';
    this.successMessage = '';
    return;
  }

  const { username, password } = this.loginForm.value;

  // Fake login logic
  if (username === 'admin' && password === 'password') {
    console.log('Login successful');
    this.errorMessage = '';
    this.successMessage = 'Login successful!';

    setTimeout(() => {
      localStorage.setItem('token', 'Api');
       const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
      this.router.navigateByUrl(returnUrl);
    }, 100);
  } else {
    this.errorMessage = 'Invalid username or password.';
    this.successMessage = '';
  }
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
