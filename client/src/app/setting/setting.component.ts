import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import { UserService } from '../_services/user.service';
import { AuthService } from '../_services/auth.service';

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

  // Lưu thông tin user hiện tại
  currentUser: any = null;

 constructor(
    private router: Router,
    private translate: TranslateService,
    private toastr: ToastrService,
    private userService: UserService,
    private authService: AuthService
 ) {
    this.loadSettings();
 }

 ngOnInit(): void {
    const storedLang = localStorage.getItem('language');
    if (storedLang) {
      this.settings.defaultLanguage = storedLang;
      this.translate.use(storedLang);
   }
    this.draftLanguage = this.settings.defaultLanguage;
    this.loadProfile();
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
    // Load user from API
    if (this.authService.isLoggedIn()) {
      this.authService.getUser().subscribe({
        next: (user: any) => {
          if (user) {
            this.currentUser = user; // Lưu thông tin user hiện tại
            this.profile.username = user.username || user.userName || user.UserName || '';
            this.profile.email = user.email || user.Email || '';
          }
        },
        error: (error: any) => {
          console.error('Error loading user profile:', error);
          // Fallback to localStorage if API fails
          const acc = localStorage.getItem('user-profile');
          if (acc) {
            const savedProfile = JSON.parse(acc);
            this.profile.username = savedProfile.username || '';
            this.profile.email = savedProfile.email || '';
          }
          // Thử lấy từ localStorage user
          const storedUser = localStorage.getItem('user');
          if (storedUser) {
            try {
              this.currentUser = JSON.parse(storedUser);
            } catch (e) {
              console.error('Error parsing stored user:', e);
            }
          }
        }
      });
    } else {
      // Fallback to localStorage if not logged in
      const acc = localStorage.getItem('user-profile');
      if (acc) {
        const savedProfile = JSON.parse(acc);
        this.profile.username = savedProfile.username || '';
        this.profile.email = savedProfile.email || '';
      }
      const storedUser = localStorage.getItem('user');
      if (storedUser) {
        try {
          this.currentUser = JSON.parse(storedUser);
        } catch (e) {
          console.error('Error parsing stored user:', e);
        }
      }
    }
 }

 // ==============================
 //          SAVE ALL
 // ==============================
 onSaveSettings() {
   // Lưu ngôn ngữ (GIỮ NGUYÊN)
    this.settings.defaultLanguage = this.draftLanguage;
    localStorage.setItem('app-settings', JSON.stringify(this.settings));
    localStorage.setItem('language', this.settings.defaultLanguage);
    
    // Apply language change immediately - this will trigger change detection for all components using translate pipe
    this.translate.use(this.settings.defaultLanguage).subscribe(() => {
      // Language has been changed, all components using translate pipe will automatically update
      console.log('Language changed to:', this.settings.defaultLanguage);
    });
    
    // Dispatch custom event to notify all components about language change
    window.dispatchEvent(new CustomEvent('languageChanged', {
      detail: { language: this.settings.defaultLanguage }
    }));
    
    // Lấy userId từ currentUser
    const userId = this.currentUser?.userId || this.currentUser?.id || this.currentUser?.UserId;
    
    // Cập nhật profile (username, email) nếu có thay đổi
    if (userId && (this.profile.username || this.profile.email)) {
      const userUpdateData = {
        UserId: userId,
        UserName: this.profile.username,
        Email: this.profile.email,
        PhoneNumber: this.currentUser?.phoneNumber || this.currentUser?.PhoneNumber || '',
        Address: this.currentUser?.address || this.currentUser?.Address || ''
      };

      this.userService.UpdateUser(userId, userUpdateData).subscribe({
        next: (response: any) => {
          // Cập nhật thành công profile
          localStorage.setItem('user-profile', JSON.stringify({
            username: this.profile.username,
            email: this.profile.email
          }));
          
          // Cập nhật currentUser trong localStorage
          if (this.currentUser) {
            this.currentUser.username = this.profile.username;
            this.currentUser.userName = this.profile.username;
            this.currentUser.email = this.profile.email;
            this.currentUser.Email = this.profile.email;
            localStorage.setItem('user', JSON.stringify(this.currentUser));
          }

          // Nếu có đổi mật khẩu, xử lý riêng
          if (this.profile.newPassword) {
            const passwordData = {
              username: this.profile.username,
              Password: this.profile.oldPassword,
              newPassword: this.profile.newPassword
            };
        
            this.userService.updatePassword(userId, passwordData).subscribe({
              next: (pwdResponse: any) => {
                this.translate.get('settings.passwordUpdateSuccess').subscribe(msg => {
                  this.toastr.success(msg || 'Cập nhật mật khẩu thành công!');
                });
        
                this.profile.oldPassword = '';
                this.profile.newPassword = '';
        
                setTimeout(() => {
                  this.onLogout();
                }, 1000);
              },
              error: (error: any) => {
                const errorMsg =
                  error?.error?.message ||
                  error?.message ||
                  'Lỗi khi cập nhật mật khẩu.';
        
                this.translate.get('settings.updateError').subscribe(msg => {
                  this.toastr.error(`${msg}: ${errorMsg}`);
                });
              }
            });
          } else {
            // Chỉ cập nhật profile, không đổi mật khẩu
            this.translate.get('settings.saveSuccess').subscribe(msg => {
              this.toastr.success(msg || 'Cập nhật thông tin thành công!');
            });
          }
        },
        error: (error: any) => {
          const errorMsg =
            error?.error?.message ||
            error?.message ||
            'Lỗi khi cập nhật thông tin.';
          
          this.translate.get('settings.updateError').subscribe(msg => {
            this.toastr.error(`${msg}: ${errorMsg}`);
          });
        }
      });
    } else if (this.profile.newPassword) {
      // Chỉ đổi mật khẩu, không cập nhật profile
      if (!userId) {
        this.translate.get('settings.updateError').subscribe(msg => {
          this.toastr.error(msg || 'Không tìm thấy thông tin người dùng.');
        });
        return;
      }

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
        error: (error: any) => {
          const errorMsg =
            error?.error?.message ||
            error?.message ||
            'Lỗi khi cập nhật mật khẩu.';
  
          this.translate.get('settings.updateError').subscribe(msg => {
            this.toastr.error(`${msg}: ${errorMsg}`);
          });
        }
      });
    } else {
      // Fallback: chỉ lưu vào localStorage nếu không có userId
      localStorage.setItem('user-profile', JSON.stringify({
        username: this.profile.username,
        email: this.profile.email
      }));
      this.translate.get('settings.saveSuccess').subscribe(msg => {
        this.toastr.success(msg || 'Đã lưu thông tin!');
      });
    }
}
// ==============================
//           LOGOUT
// ==============================
 onLogout() {
    this.authService.logout();
    this.router.navigate(['/dashboard']);
 }
}