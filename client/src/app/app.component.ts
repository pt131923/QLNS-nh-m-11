import { HttpClient } from '@angular/common/http';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent implements OnInit, OnDestroy {
  [x: string]: any;
  title = 'HR Management System';
  private languageSubscription?: Subscription;
  private storageSubscription?: Subscription;

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
    // Load saved language from localStorage
    const savedLanguage = localStorage.getItem('language');
    if (savedLanguage) {
      this.translate.use(savedLanguage);
    }

    // Listen for language changes from TranslateService
    this.languageSubscription = this.translate.onLangChange.subscribe((event: any) => {
      // Language has changed, all components using translate pipe will automatically update
      console.log('Language changed to:', event.lang);
    });

    // Listen for storage changes (when language is changed in settings)
    window.addEventListener('storage', (e) => {
      if (e.key === 'language' && e.newValue) {
        this.translate.use(e.newValue);
      }
    });

    // Also listen for custom language change events
    window.addEventListener('languageChanged', ((e: CustomEvent) => {
      if (e.detail && e.detail.language) {
        this.translate.use(e.detail.language);
      }
    }) as EventListener);
  }

  ngOnDestroy(): void {
    if (this.languageSubscription) {
      this.languageSubscription.unsubscribe();
    }
  }
}

