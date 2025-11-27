import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent implements OnInit {
  [x: string]: any;
  title = 'HR Management System';

  constructor(
    private http: HttpClient,
    private translate: TranslateService
  ) {
    // Set default language
    this.translate.setDefaultLang('en');
    
    // Load saved language from localStorage
    const savedLanguage = localStorage.getItem('language') || 'en';
    this.translate.use(savedLanguage);
  }

  ngOnInit(): void {
    // Listen for language changes from other components
    const savedLanguage = localStorage.getItem('language');
    if (savedLanguage) {
      this.translate.use(savedLanguage);
    }
  }
}

