import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-setting',
  templateUrl: './setting.component.html',
  styleUrls: ['./setting.component.css']
})
export class SettingComponent {
   settings = {
    defaultShiftStart: '08:00',
    allowedLate: 15,
    adminEmail: 'admin@yourcompany.com',
    enableOvertime: true,
    enableNotifications: false,
    defaultLanguage: 'en'
  };

  languages = [
    { code: 'en', label: 'English' },
    { code: 'vi', label: 'Tiếng Việt' }
  ];

  constructor(private router: Router, private translate: TranslateService) {
    this.loadSettings();
  }

   loadSettings(): void {
    const savedSettings = localStorage.getItem('app-settings');
    if (savedSettings) {
      this.settings = JSON.parse(savedSettings);
      this.translate.use(this.settings.defaultLanguage);
    }
  }

  onSaveSettings() {
    localStorage.setItem('app-settings', JSON.stringify(this.settings));
    localStorage.setItem('language', this.settings.defaultLanguage);
    this.translate.use(this.settings.defaultLanguage);

    alert('Settings saved successfully!');
    this.router.navigate(['/employees']);
  }

  onResetSettings() {
    this.settings = {
      defaultShiftStart: '08:00',
      allowedLate: 15,
      adminEmail: 'admin.DHXD@gmail.com',
      enableOvertime: true,
      enableNotifications: false,
      defaultLanguage: 'en'
    };
  }
  onAdminEmailChange(email: string) {
    this.settings.adminEmail = email;
    localStorage.setItem('adminEmail', email);
    console.log('Admin email changed to:', email);
  }
  onLanguageChange(language: string) {
    this.settings.defaultLanguage = language;
    this.translate.use(language);
    localStorage.setItem('language', language);
    console.log('Language changed to:', language);
  }

  onEnableOvertimeChange() {
    this.settings.enableOvertime = !this.settings.enableOvertime;
  }

  ngOnInit(): void {
    setTimeout(() => {
      console.log('This will be executed after 0 seconds');
    }, 0);
    const storedLang = localStorage.getItem('language');
    if (storedLang) {
      this.translate.use(storedLang);
    }
  }
}
