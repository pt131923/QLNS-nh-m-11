
import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface AttendanceRecord {
  id?: number;
  date: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  totalHoursWorked: number | null;
  status: string;
}

@Component({
  selector: 'app-timekeeping',
  templateUrl: './timekeeping.component.html',
  styleUrls: ['./timekeeping.component.css']
})
export class TimekeepingComponent implements OnInit, OnDestroy {
  message = '';
  isSuccess = false;
  loading = false;

  checkedIn = false;

  currentTime = new Date();
  workDuration = '00:00:00';

  todayRecord: AttendanceRecord | null = null;
  timekeepingRecords: AttendanceRecord[] = [];

  private timerId: ReturnType<typeof setInterval> | undefined;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.updateClock();

    this.timerId = setInterval(() => {
      this.updateClock();
    }, 1000);

    this.loadAttendanceHistory();
  }

  ngOnDestroy(): void {
    if (this.timerId) {
      clearInterval(this.timerId);
    }
  }

  private updateClock(): void {
    this.currentTime = new Date();
    this.updateWorkDuration();
  }

  private updateWorkDuration(): void {
    if (!this.todayRecord?.checkInTime ||
        this.todayRecord.checkOutTime) {
      return;
    }

    const start = new Date(this.todayRecord.checkInTime).getTime();
    const now = this.currentTime.getTime();

    if (isNaN(start) || now < start) {
      this.workDuration = '00:00:00';
      return;
    }

    const seconds = Math.floor((now - start) / 1000);
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;

    this.workDuration = [
      h.toString().padStart(2, '0'),
      m.toString().padStart(2, '0'),
      s.toString().padStart(2, '0')
    ].join(':');
  }

  loadAttendanceHistory(): void {
    this.http.get<AttendanceRecord[]>(
      '/api/attendance/history'
    ).subscribe({
      next: (data) => {
        this.timekeepingRecords = data;

        // Backend cần trả về dữ liệu của nhân viên đang đăng nhập
        // và lịch sử được sắp xếp theo ngày giảm dần.
        const today = new Date();

        const isToday = (dateValue: string) => {
          const d = new Date(dateValue);

          return d.getFullYear() === today.getFullYear() &&
            d.getMonth() === today.getMonth() &&
            d.getDate() === today.getDate();
        };

        this.todayRecord =
          data.find(r => isToday(r.date)) ?? null;

        this.checkedIn = !!(
          this.todayRecord?.checkInTime &&
          !this.todayRecord?.checkOutTime
        );

        this.updateWorkDuration();
      },
      error: () => {
        this.message = 'Failed to load attendance history.';
        this.isSuccess = false;
      }
    });
  }

  checkIn(): void {
    if (this.loading || this.checkedIn) return;

    this.loading = true;
    this.message = '';

    // Không gửi employeeId từ frontend.
    // Backend lấy nhân viên từ thông tin xác thực.
    this.http.post<any>(
      '/api/attendance/check-in',
      {}
    ).subscribe({
      next: (res) => {
        this.message = res.message || 'Check-in successful.';
        this.isSuccess = true;
        this.loadAttendanceHistory();
        this.loading = false;
      },
      error: (err) => {
        this.message =
          err.error?.message || 'Check-in failed.';
        this.isSuccess = false;
        this.loading = false;
      }
    });
  }

  checkOut(): void {
    if (this.loading || !this.checkedIn) return;

    this.loading = true;
    this.message = '';

    this.http.post<any>(
      '/api/attendance/check-out',
      {}
    ).subscribe({
      next: (res) => {
        this.message = res.message || 'Check-out successful.';
        this.isSuccess = true;
        this.loadAttendanceHistory();
        this.loading = false;
      },
      error: (err) => {
        this.message =
          err.error?.message || 'Check-out failed.';
        this.isSuccess = false;
        this.loading = false;
      }
    });
  }
}
