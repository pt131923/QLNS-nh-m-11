import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent {
  registerMode = false;
  loginMode = false;
  users: any;
  $event: any;

  title = 'HR Management System';

  constructor() {}

  ngOnInit(): void {
  }

  registerToggle() {
    this.registerMode = !this.registerMode
  }

  loginToggle() {
    this.loginMode = !this.loginMode;
  }

  cancelRegisterMode(event: boolean) {
    this.registerMode = event;
    if (this.registerMode) {
      this.users = null;
    }
  }

  cancelLoginMode(event: boolean) {
    this.loginMode = event;
  }

  onUserSelected(user: any) {
    this.users = user;
 }

  loggedIn() {
    const token = localStorage.getItem('token');
    return !!token;
  }
}
