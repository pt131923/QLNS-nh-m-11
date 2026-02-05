import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
// Trong ứng dụng thực tế, bạn sẽ cần import AuthService từ file service của mình
// import { AuthService } from '../auth.service'; 

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  
  // Thay thế khai báo thuộc tính bằng Dependency Injection qua constructor.
  // Đây là cách chuẩn để sử dụng Router (xử lý liên kết trang) và AuthService.
  
  // Lưu ý: Tôi đang giả định AuthService tồn tại để code biên dịch được, 
  // nhưng Router (để điều hướng) là trọng tâm của việc liên kết trang.
  constructor(
    private router: Router
    // Trong thực tế, bạn sẽ Inject AuthService vào đây:
    // private authService: AuthService 
  ) {}

  ngOnInit(): void {
    // Logic khởi tạo nếu cần
  }

  // Phương thức xử lý đăng xuất và liên kết trang
  logout() {
    // Nếu AuthService được Inject:
    // this.authService.logout(); 
    console.log('Đăng xuất (Logic giả định - Cần AuthService)');

    // Sử dụng Router đã được Inject để điều hướng đến trang đăng nhập
    this.router.navigate(['/login']);
  }

  // Thêm phương thức điều hướng chung nếu cần (ví dụ: được gọi từ menu/sidebar)
  navigateTo(route: string) {
    this.router.navigate([route]);
  }
}