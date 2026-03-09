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

export interface ChartBarItem {
  label: string;
  value: number;
  heightPct: number;
  colorClass: string;
  link: string;
}

export interface RecentEmployeeItem {
  id: number;
  name: string;
  departmentName: string;
}

export interface UpcomingItem {
  title: string;
  subtitle: string;
  dateLabel: string;
  link: string;
  type: 'contract' | 'training' | 'leave';
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
  chartStats: ChartBarItem[] = [];
  /** Nhân viên mới / gần đây (lấy từ API, tối đa 5) */
  recentEmployees: RecentEmployeeItem[] = [];
  /** Hợp đồng sắp hết hạn / sự kiện sắp tới (mock hoặc từ API) */
  upcomingItems: UpcomingItem[] = [];
  loadingEmployees = false;
  /** Thời điểm cập nhật số liệu (từ API summary) */
  lastUpdated: string | null = null;

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
        this.lastUpdated = (res as any)?.LastUpdated ?? (res as any)?.lastUpdated ?? null;
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
            this.authService.logout('/login');
          }
        }, 300); // Tăng delay lên 300ms
      } else {
        console.warn('⚠️ User marked as logged in but no token found, redirecting to login');
        this.authService.logout('/login');
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
                  this.loadFullDashboard();
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

    // Rebuild sidebar and upcoming on language change
    this.subs.push(
      this.translate.onLangChange.subscribe(() => {
        this.buildSidebarStats();
        this.buildUpcomingItems();
      })
    );

    // Load dashboard full data khi đã đăng nhập (để phần dưới có dữ liệu thật)
    if (this.authService.isLoggedIn()) {
      this.loadFullDashboard();
    } else {
      this.buildUpcomingItems();
    }
  }

  /** Gọi BE lấy data dashboard đầy đủ (summary + recent employees + upcoming) */
  loadFullDashboard(): void {
    this.loadingEmployees = true;
    this.dashboardService.getFullDashboard().subscribe({
      next: (res: any) => {
        // Summary (để UI lên nhanh cả khi SignalR chưa kịp đẩy)
        if (res?.Summary) {
          this.stats = res.Summary;
          this.lastUpdated = res.Summary?.LastUpdated ?? res.Summary?.lastUpdated ?? null;
          this.buildSidebarStats();
        }

        // Recent employees
        this.recentEmployees = (res?.RecentEmployees || []).map((e: any) => ({
          id: e.EmployeeId ?? e.employeeId,
          name: e.EmployeeName ?? e.employeeName ?? '—',
          departmentName: e.DepartmentName ?? e.departmentName ?? '—',
        }));
        this.loadingEmployees = false;

        // Upcoming
        const daysLabel = this.translate.instant('dashboard.days');
        this.upcomingItems = (res?.Upcoming || []).map((u: any) => ({
          title: u.Title ?? u.title ?? '—',
          subtitle: u.Subtitle ?? u.subtitle ?? '',
          dateLabel: `${Math.max(0, u.DaysLeft ?? u.daysLeft ?? 0)} ${daysLabel}`,
          link: u.Link ?? u.link ?? '/dashboard',
          type: (u.Type ?? u.type ?? 'contract') as any,
        }));

        // fallback nếu BE trả rỗng
        if (!this.upcomingItems.length) this.buildUpcomingItems();
      },
      error: () => {
        this.loadingEmployees = false;
        this.recentEmployees = [];
        this.buildUpcomingItems();
      }
    });
  }

  /** Dữ liệu sắp tới: hợp đồng hết hạn, đào tạo, nghỉ phép (mock theo ngôn ngữ) */
  buildUpcomingItems(): void {
    const t = (key: string) => this.translate.instant(key);
    this.upcomingItems = [
      { title: t('dashboard.upcomingContract1'), subtitle: t('dashboard.upcomingContract1Desc'), dateLabel: '7 ' + t('dashboard.days'), link: '/contracts', type: 'contract' },
      { title: t('dashboard.upcomingTraining1'), subtitle: t('dashboard.upcomingTraining1Desc'), dateLabel: '14 ' + t('dashboard.days'), link: '/trainings', type: 'training' },
      { title: t('dashboard.upcomingLeave1'), subtitle: t('dashboard.upcomingLeave1Desc'), dateLabel: '3 ' + t('dashboard.days'), link: '/leaves', type: 'leave' },
    ];
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
          this.authService.logout('/login');
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

    // Dữ liệu cho biểu đồ: chỉ lấy các chỉ số đếm (bỏ Settings/Help và Salaries để scale đẹp)
    const chartItems: { key: string; value: number; label: string; colorClass: string; link: string }[] = [
      { key: 'TotalEmployees', value: this.stats.TotalEmployees ?? 0, label: this.t('dashboard.employees'), colorClass: 'primary', link: '/employees' },
      { key: 'TotalDepartments', value: this.stats.TotalDepartments ?? 0, label: this.t('dashboard.departments'), colorClass: 'warning', link: '/departments' },
      { key: 'TotalContracts', value: this.stats.TotalContracts ?? 0, label: this.t('dashboard.contracts'), colorClass: 'success', link: '/contracts' },
      { key: 'TotalTimekeeping', value: this.stats.TotalTimekeeping ?? 0, label: this.t('dashboard.timekeeping'), colorClass: 'info', link: '/timekeeping' },
      { key: 'TotalRecruitments', value: this.stats.TotalRecruitments ?? 0, label: this.t('dashboard.recruitments'), colorClass: 'primary', link: '/recruitments' },
      { key: 'TotalBenefits', value: this.stats.TotalBenefits ?? 0, label: this.t('dashboard.benefits'), colorClass: 'danger', link: '/benefits' },
      { key: 'TotalTrainings', value: this.stats.TotalTrainings ?? 0, label: this.t('dashboard.trainings'), colorClass: 'secondary', link: '/trainings' },
      { key: 'TotalLeaves', value: this.stats.TotalLeaves ?? 0, label: this.t('dashboard.leaves'), colorClass: 'warning', link: '/leaves' },
      { key: 'TotalContacts', value: this.stats.TotalContacts ?? 0, label: this.t('dashboard.contact'), colorClass: 'dark', link: '/contact' },
    ];
    const maxVal = Math.max(1, ...chartItems.map(i => i.value));
    this.chartStats = chartItems.map(i => ({
      label: i.label,
      value: i.value,
      heightPct: maxVal > 0 ? Math.round((i.value / maxVal) * 100) : 0,
      colorClass: i.colorClass,
      link: i.link,
    }));
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
    this.authService.logout('/dashboard');
    this.user = null;
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
