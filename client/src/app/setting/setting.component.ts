import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import { UserService } from '../_services/user.service';

@Component({
 selector: 'app-setting',
 templateUrl: './setting.component.html',
 styleUrls: ['./setting.component.css']
})
export class SettingComponent implements OnInit {

 // Ngôn ngữ tạm chọn
 draftLanguage: string = 'en';

 // Chỉ giữ phần ngôn ngữ
 settings = {
    defaultLanguage: 'en'
 };

 // Hồ sơ người dùng (Profile)
 profile = {
    username: '',
    email: '',
    oldPassword: '', // <== THÊM TRƯỜNG MẬT KHẨU CŨ
    newPassword: ''
  };

 constructor(
    private router: Router,
    private translate: TranslateService,
    private toastr: ToastrService,
    private userService: UserService // <== INJECT UserService
 ) {
    this.loadSettings();
    this.loadProfile();
 }

 ngOnInit(): void {
    const storedLang = localStorage.getItem('language');
    if (storedLang) {
      this.settings.defaultLanguage = storedLang;
      this.translate.use(storedLang);
   }
    this.draftLanguage = this.settings.defaultLanguage;
 }

 // ==============================
 //        LOAD SETTINGS
 // ==============================
 loadSettings(): void {
   const saved = localStorage.getItem('app-settings');
    if (saved) {
      this.settings = JSON.parse(saved);
      this.draftLanguage = this.settings.defaultLanguage;
      this.translate.use(this.settings.defaultLanguage);
   }
 }

 // ==============================
 //        LOAD PROFILE
 // ==============================
 loadProfile(): void {
    const acc = localStorage.getItem('user-profile');
   if (acc) {
     this.profile = JSON.parse(acc);
   }
 }

 // ==============================
 //          SAVE ALL
 // ==============================
 onSaveSettings() {
   // Lưu ngôn ngữ (GIỮ NGUYÊN)
    this.settings.defaultLanguage = this.draftLanguage;
    localStorage.setItem('app-settings', JSON.stringify(this.settings));
    localStorage.setItem('language', this.settings.defaultLanguage);
    this.translate.use(this.settings.defaultLanguage);
    
    if (this.profile.newPassword) {

      const userId = 1; 
      const passwordData = {
          username: this.profile.username,
          Password: this.profile.oldPassword,
          newPassword: this.profile.newPassword
      };
  
      this.userService.updatePassword(userId, passwordData).subscribe({
          next: (response: any) => {
              this.translate.get('settings.passwordUpdateSuccess').subscribe(msg => {
                  this.toastr.success(msg || 'Cập nhật mật khẩu thành công!');
              });
  
              this.profile.oldPassword = '';
              this.profile.newPassword = '';
  
              setTimeout(() => {
                  this.onLogout();
              }, 1000);
          },
  
          // ❗ Đã sửa: không đọc message từ object null
          error: (error: any) => {
              const errorMsg =
                  error?.error?.message ||        // nếu backend trả JSON message
                  error?.message ||               // lỗi HTTP chung
                  'Lỗi khi cập nhật mật khẩu.';   // fallback không crash
  
              this.translate.get('settings.updateError').subscribe(msg => {
                  this.toastr.error(`${msg}: ${errorMsg}`);
              });
          }
      });
  
  } else {
  
      localStorage.setItem('user-profile', JSON.stringify(this.profile)); 
      this.translate.get('settings.saveSuccess').subscribe(msg => {
          this.toastr.success(msg);
      });
  }  
}
// ==============================
//           LOGOUT
// ==============================
 onLogout() {
    localStorage.clear();
    this.router.navigate(['/login']);
 }
}