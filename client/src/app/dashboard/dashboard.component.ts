import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent {
  registerMode = false;
  users: any;
  $event: any;

  title = 'HR Management System';

  constructor() {}

  ngOnInit(): void {
  }

  registerToggle() {
    this.registerMode = !this.registerMode
  }

  cancelRegisterMode(event: boolean) {
    this.registerMode = event;
    if (this.registerMode) {
      this.users = null;
    }
  }
  onUserSelected(user: any) {
    this.users = user;
 }
}
