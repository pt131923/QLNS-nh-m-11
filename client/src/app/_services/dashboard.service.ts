import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, tap, shareReplay } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

// Định nghĩa Interface dữ liệu trả về để đảm bảo tính an toàn kiểu (Type Safety)
// Phải khớp CHÍNH XÁC với DashboardSummary Record trong C#
export interface DashboardSummary {
  totalEmployees: number;
  totalDepartments: number;
  totalContracts: number;
  totalSalaries: number;
  totalTimekeeping: number;
  totalRecruitments: number;
  totalBenefits: number;
  totalTrainings: number;
  totalLeaves: number;
  
  // Các trường Contact mới
  totalContacts: number;
  contactsProcessed: number;
  contactsPending: number;
  
  settings: number;
  help: number; // Đã đặt lại thành giá trị cố định (1)
  lastUpdated: string; // DateTime trong C# sẽ là string trong JSON
}

export interface DashboardRecentEmployeeDto {
  EmployeeId: number;
  EmployeeName: string;
  DepartmentName: string;
}

export interface DashboardUpcomingItemDto {
  Type: 'contract' | 'training' | 'leave' | string;
  Title: string;
  Subtitle: string;
  Date: string;
  DaysLeft: number;
  Link: string;
}

export interface DashboardFullResponseDto {
  Summary: any;
  RecentEmployees: DashboardRecentEmployeeDto[];
  Upcoming: DashboardUpcomingItemDto[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  getDashboard() {
    throw new Error('Method not implemented.');
  }

  // Địa chỉ API và SignalR Hub
  private apiUrl = `${environment.apiUrl}/dashboard`;
  // FIX: Đảm bảo hubUrl khớp CHÍNH XÁC với ánh xạ trong Program.cs
  private hubUrl = environment.hubUrl; 

  private summarySource = new BehaviorSubject<DashboardSummary | null>(null);
  summary$ = this.summarySource.asObservable().pipe(shareReplay(1));

  private hubConnection: signalR.HubConnection | null = null;
  private isConnected = false;

  constructor(private http: HttpClient, private authService: AuthService) {
    // Không tự động kết nối SignalR, chỉ kết nối sau login
  }

  getFullDashboard(): Observable<DashboardFullResponseDto> {
    return this.http.get<DashboardFullResponseDto>(`${this.apiUrl}/full`);
  }

  // --- QUẢN LÝ KẾT NỐI SIGNALR ---
  
  private startConnection(): void {
    if (this.isConnected) return;

    // Lấy token từ AuthService
    const token = this.authService.getToken();

    // Chỉ tạo kết nối SignalR nếu có token hợp lệ
    if (!token) {
      console.log('⏳ Không có token, bỏ qua SignalR connection và không gọi API');
      // Không gọi fetchSummary() khi chưa có token
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token // Truyền token vào SignalR connection
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => {
        console.log('✔️ SignalR Connection started');
        this.isConnected = true;
        this.registerRealTimeEvents();
        this.fetchSummary(); // Lấy dữ liệu ban đầu
      })
      .catch(err => {
        console.error('❌ Error while starting SignalR connection:', err);
        // Nếu không kết nối được SignalR, vẫn cố gắng lấy dữ liệu ban đầu
        this.fetchSummary(); 
      });
  }

  private registerRealTimeEvents(): void {
    if (!this.hubConnection) return;
    
    // Tên sự kiện phải khớp CHÍNH XÁC với Hub SendAsync
    this.hubConnection.on('ReceiveDashboardUpdate', (data: DashboardSummary) => {
      console.log('⚡ Real-time Update received:', data);
      this.summarySource.next(data);
    });
  }
  
  // --- XỬ LÝ DỮ LIỆU ---

  /**
   * Gọi API lấy dữ liệu tổng hợp và cập nhật BehaviorSubject.
   */
  fetchSummary(): void {
    // Kiểm tra token trước khi gọi API
    const token = this.authService.getToken();
    if (!token) {
      console.warn('⚠️ No token available, skipping dashboard summary fetch');
      return;
    }

    // Log chi tiết token để debug
    console.log('📡 Fetching dashboard summary from:', `${this.apiUrl}/summary`);
    console.log('🔐 Token info - Length:', token.length, 'Preview:', token.substring(0, 20) + '...');
    
    this.http.get<DashboardSummary>(`${this.apiUrl}/summary`)
      .pipe(
        tap(res => {
          console.log('✔️ Dashboard summary received from API:', res);
          console.log('📊 Summary data keys:', Object.keys(res || {}));
        }),
        catchError(err => {
          console.error("❌ Dashboard summary API error:", err);
          console.error("❌ Error status:", err.status, "Error message:", err.message);
          
          // Nếu là lỗi 401, kiểm tra lại token và thử lại một lần
          if (err.status === 401) {
            console.warn("⚠️ 401 Unauthorized - checking token...");
            const currentToken = this.authService.getToken();
            
            if (!currentToken) {
              console.warn("⚠️ Token was removed, cannot retry");
              return of(null);
            }
            
            // Nếu token vẫn còn, có thể là vấn đề timing
            // Không retry ngay, để component quyết định
            console.warn("⚠️ 401 but token still exists - may be timing issue or token invalid");
          }
          
          // Để tránh lỗi API làm crash, chúng ta trả về null và log lỗi
          return of(null);
        })
      )
      .subscribe(res => {
        if (res) {
          console.log('💾 Updating dashboard summary in BehaviorSubject');
          this.summarySource.next(res);
        } else {
          console.log('⚠️ No data received from API');
        }
      });
  }

  /**
   * Khởi động SignalR connection sau khi đăng nhập thành công
   */
  startSignalRConnection(): void {
    if (this.isConnected) {
      console.log('SignalR already connected');
      return;
    }

    // Lấy token từ AuthService
    const token = this.authService.getToken();

    if (!token) {
      console.log('⏳ Không có token, không thể khởi động SignalR connection');
      return;
    }

    console.log('🚀 Starting SignalR connection...');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => this.authService.getToken()! // Token exists at this point, non-null assertion
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.start()
      .then(() => {
        console.log('✔️ SignalR Connection started successfully');
        this.isConnected = true;
        this.registerRealTimeEvents();
        // Lấy dữ liệu ban đầu sau khi kết nối thành công
        this.fetchSummary();
      })
      .catch(err => {
        console.error('❌ Error while starting SignalR connection:', err);
        this.isConnected = false;
        // Ngay cả khi SignalR thất bại, vẫn cố gắng lấy dữ liệu từ API
        console.log('⚠️ SignalR failed, falling back to HTTP polling');
        this.fetchSummary();
      });
  }

  /**
   * Ngắt kết nối SignalR (khi logout)
   */
  stopSignalRConnection(): void {
    if (this.hubConnection && this.isConnected) {
      console.log('🔌 Stopping SignalR connection...');
      this.hubConnection.stop()
        .then(() => {
          console.log('✔️ SignalR connection stopped');
          this.isConnected = false;
        })
        .catch(err => {
          console.error('❌ Error stopping SignalR connection:', err);
        });
    }
  }

  /**
   * Kích hoạt Real-time Update từ phía Server
   */
  triggerRealtimeUpdate(): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/trigger-realtime-update`, {})
      .pipe(
        tap(res => console.log('⚡ Real-time update triggered:', res)),
        catchError(err => {
          console.error("❌ triggerRealtimeUpdate() error:", err);
          return of({ status: 'failed' });
        })
      );
  }

  /**
   * Test API connection (public endpoint)
   */
  testConnection(): Observable<any> {
    console.log('🧪 Testing API connection...');
    return this.http.get<any>(`${this.apiUrl}/test`).pipe(
      tap(res => console.log('✅ API test successful:', res)),
      catchError(err => {
        console.error('❌ API test failed:', err);
        return throwError(() => err);
      })
    );
  }

  /**
   * Test authentication
   */
  testAuth(): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/auth/test`);
  }

  /**
   * Phương thức để Component có thể gọi khi cần refresh data
   */
  loadSummaryIfAuthenticated(): void {
    const token = this.authService.getToken();
    if (token) {
      console.log('🔄 Loading dashboard summary...');
      this.fetchSummary();
    } else {
      console.log('❌ No token available, cannot load dashboard summary');
    }
  }

  // Phương thức để Component có thể lấy trực tiếp dữ liệu nếu cần
  getCurrentSummary(): DashboardSummary | null {
    return this.summarySource.getValue();
  }

}