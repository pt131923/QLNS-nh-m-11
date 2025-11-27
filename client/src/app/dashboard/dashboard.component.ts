import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router'; // 1. Import Router

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  // Bỏ các biến registerMode và loginMode vì ta dùng Router

  users: any;
  title = 'HR Management System';

  // 2. Inject Router vào constructor
  constructor(private router: Router) {}

  ngOnInit(): void {
    // Thường dùng để gọi dữ liệu ban đầu
  }

  // Chức năng chuyển hướng sang Form Đăng ký
  registerNavigate() {
    // Chuyển hướng đến URL /register (Giả định bạn đã cấu hình route này)
    this.router.navigate(['/register']);
  }

  // Chức năng chuyển hướng sang Form Đăng nhập
  loginNavigate() {
    // Chuyển hướng đến URL /login (Giả định bạn đã cấu hình route này)
    this.router.navigate(['/login']);
  }

  // Hàm xử lý khi người dùng được chọn (giữ nguyên)
  onUserSelected(user: any) {
    this.users = user;
  }
}
