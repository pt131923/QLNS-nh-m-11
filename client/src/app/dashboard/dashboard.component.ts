import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { AuthService } from '../_services/auth.service';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { DashboardService } from '../_services/dashboard.service';

interface SidebarItem {
  icon: string;
  value: number;
  label: string;
  link: string;
  colorClass?: string;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {

  user: any = null;
  stats: any = {};
  sidebarStats: SidebarItem[] = [];

  private subs: Subscription[] = [];

  constructor(
    public authService: AuthService,
    private dashboardService: DashboardService,
    private router: Router,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadUser();

    // Subscribe data summary
    this.subs.push(
      this.dashboardService.summary$.subscribe(res => {
        console.log('📈 Dashboard component received summary data:', res);
        this.stats = res || {};
        console.log('📊 Stats object:', this.stats);
        this.buildSidebarStats();
        console.log('📋 Sidebar stats built:', this.sidebarStats);
      })
    );

    // Load summary immediately if user is logged in (for direct access)
    // Chỉ load nếu có token hợp lệ
    if (this.authService.isLoggedIn()) {
      // Đảm bảo token đã được lưu trước khi gọi API
      const token = this.authService.getToken();
      if (token) {
        console.log('✅ Token found, will load summary after delay');
        // Tăng delay để đảm bảo interceptor đã sẵn sàng
        setTimeout(() => {
          const tokenCheck = this.authService.getToken();
          if (tokenCheck) {
            console.log('🔄 Loading dashboard summary after token verification...');
            this.dashboardService.loadSummaryIfAuthenticated();
          } else {
            console.warn('⚠️ Token disappeared, redirecting to login');
            this.authService.logout();
          }
        }, 300); // Tăng delay lên 300ms
      } else {
        console.warn('⚠️ User marked as logged in but no token found, redirecting to login');
        this.authService.logout();
      }
    }

    // Also load summary when navigating to dashboard
    this.subs.push(
      this.router.events
        .pipe(filter(event => event instanceof NavigationEnd))
        .subscribe((e: any) => {
          if (e.url === '/dashboard' || e.urlAfterRedirects === '/dashboard') {
            this.loadUser();
            // Chỉ load summary nếu có token hợp lệ
            const token = this.authService.getToken();
            if (this.authService.isLoggedIn() && token) {
              console.log('🔄 Navigation to dashboard detected, loading summary...');
              // Tăng delay để đảm bảo authentication được xử lý đầy đủ
              setTimeout(() => {
                const tokenCheck = this.authService.getToken();
                if (tokenCheck) {
                  this.dashboardService.loadSummaryIfAuthenticated();
                } else {
                  console.warn('⚠️ Token disappeared during navigation');
                }
              }, 300); // Tăng delay lên 300ms
            } else {
              console.warn('⚠️ No token available during navigation to dashboard');
            }
          }
        })
    );

    // Rebuild sidebar on language change
    this.subs.push(
      this.translate.onLangChange.subscribe(() => this.buildSidebarStats())
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  /* ================================
     🔵 LOAD USER LOGIN
  =================================*/
  loadUser() {
    if (!this.authService.isLoggedIn()) {
      this.user = null;
      localStorage.removeItem('user');
      return;
    }

    // Load user từ cache
    const saved = localStorage.getItem('user');
    if (saved) {
      this.user = JSON.parse(saved);
    }

    // Refresh user từ server
    this.subs.push(
      this.authService.getUser().subscribe({
        next: (user: any) => {
          this.user = user;
          localStorage.setItem('user', JSON.stringify(user));
        },
        error: () => {
          this.authService.logout();
          this.router.navigate(['/login']);
        }
      })
    );
  }

 /* ================================
    🔵 SIDEBAR ITEMS
=================================*/
buildSidebarStats() {
    this.sidebarStats = [
        // Sử dụng Bootstrap Icons và Color Classes để đồng bộ với giao diện mới
        { icon: 'bi-people-fill', value: this.stats.TotalEmployees ?? 0, label: this.t('dashboard.employees'), link: '/employees', colorClass: 'text-primary' },
        { icon: 'bi-building', value: this.stats.TotalDepartments ?? 0, label: this.t('dashboard.departments'), link: '/departments', colorClass: 'text-warning' },
        { icon: 'bi-file-earmark-text', value: this.stats.TotalContracts ?? 0, label: this.t('dashboard.contracts'), link: '/contracts', colorClass: 'text-success' },
        { icon: 'bi-cash-stack', value: this.stats.TotalSalaries ?? 0, label: this.t('dashboard.salaries'), link: '/salaries', colorClass: 'text-danger' },
        { icon: 'bi-clock-history', value: this.stats.TotalTimekeeping ?? 0, label: this.t('dashboard.timekeeping'), link: '/timekeeping', colorClass: 'text-info' },
        { icon: 'bi-person-plus', value: this.stats.TotalRecruitments ?? 0, label: this.t('dashboard.recruitments'), link: '/recruitments', colorClass: 'text-primary' },
        { icon: 'bi-gift', value: this.stats.TotalBenefits ?? 0, label: this.t('dashboard.benefits'), link: '/benefits', colorClass: 'text-danger' },
        { icon: 'bi-book', value: this.stats.TotalTrainings ?? 0, label: this.t('dashboard.trainings'), link: '/trainings', colorClass: 'text-secondary' },
        { icon: 'bi-calendar-x', value: this.stats.TotalLeaves ?? 0, label: this.t('dashboard.leaves'), link: '/leaves', colorClass: 'text-warning' },

        // 📞 Sửa lỗi Contact (Theo JSON trả về)
        { icon: 'bi-envelope', value: this.stats.TotalContacts ?? 0, label: this.t('dashboard.contact'), link: '/contact', colorClass: 'text-dark' },

        // Các giá trị cứng (Settings, Help)
        { icon: 'bi-gear', value: this.stats.Settings ?? 1, label: this.t('dashboard.settings'), link: '/settings', colorClass: 'text-secondary' },
        { icon: 'bi-question-circle', value: this.stats.Help ?? 1, label: this.t('dashboard.help'), link: '/help', colorClass: 'text-info' },
      ];
   }

  t(key: string) {
    return this.translate.instant(key);
  }

  /* ================================
     🔵 NAVIGATION
  =================================*/
  navigateTo(link: string) {
    if (this.authService.isLoggedIn()) {
      this.router.navigate([link]);
    } else {
      this.router.navigate(['/login']);
    }
  }

  loginNavigate() { this.router.navigate(['/login']); }
  registerNavigate() { this.router.navigate(['/register']); }

  logout() {
    this.authService.logout();
    this.user = null;
    this.router.navigate(['/dashboard']);
  }

  // Debug method to test API
  testAPI() {
    console.log('🧪 Testing API connection...');
    this.dashboardService.testConnection().subscribe({
      next: (result) => {
        console.log('✅ API test result:', result);
        alert('API Test Result: ' + JSON.stringify(result, null, 2));
      },
      error: (error) => {
        console.error('❌ API test error:', error);
        alert('API Test Error: ' + JSON.stringify(error, null, 2));
      }
    });
  }

  // Debug method to force load summary
  forceLoadSummary() {
    console.log('🔄 Force loading summary...');
    this.dashboardService.loadSummaryIfAuthenticated();
  }

  goToProfile() {
    this.router.navigate(['/settings']);
  }
}
