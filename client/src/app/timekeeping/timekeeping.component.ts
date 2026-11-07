
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

interface AttendanceResponse {
  message?: string;
}

@Component({
  selector: 'app-timekeeping',
  templateUrl: './timekeeping.component.html',
  styleUrls: ['./timekeeping.component.css']
})
export class TimekeepingComponent implements OnInit, OnDestroy {

  // Thông báo
  message = '';
  isSuccess = false;

  // Trạng thái xử lý chấm công
  loading = false;

  // Trạng thái làm việc
  checkedIn = false;

  // Đồng hồ hiện tại
  currentTime = new Date();
  workDuration = '00:00:00';

  // Dữ liệu chấm công
  todayRecord: AttendanceRecord | null = null;
  timekeepingRecords: AttendanceRecord[] = [];

  // Trạng thái tải lịch sử
  historyLoading = false;
  historyError = '';

  // Timer
  private timerId?: ReturnType<typeof setInterval>;

  constructor(private http: HttpClient) {}

  // =====================================
  // Lifecycle
  // =====================================

  ngOnInit(): void {
    this.updateClock();

    this.timerId = setInterval(() => {
      this.updateClock();
    }, 1000);

    this.loadAttendanceHistory();
  }

  ngOnDestroy(): void {
    if (this.timerId !== undefined) {
      clearInterval(this.timerId);
    }
  }

  // =====================================
  // Đồng hồ
  // =====================================

  private updateClock(): void {
    this.currentTime = new Date();
    this.updateWorkDuration();
  }

  private updateWorkDuration(): void {
    const record = this.todayRecord;

    if (!record?.checkInTime || record.checkOutTime) {
      this.workDuration = '00:00:00';
      return;
    }

    const startTime = new Date(record.checkInTime).getTime();
    const currentTimestamp = this.currentTime.getTime();

    if (!Number.isFinite(startTime) || currentTimestamp < startTime) {
      this.workDuration = '00:00:00';
      return;
    }

    const totalSeconds = Math.floor(
      (currentTimestamp - startTime) / 1000
    );

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    this.workDuration = [
      hours.toString().padStart(2, '0'),
      minutes.toString().padStart(2, '0'),
      seconds.toString().padStart(2, '0')
    ].join(':');
  }

  // =====================================
  // Xử lý ngày
  // =====================================

  private isToday(dateValue: string): boolean {
    if (!dateValue) {
      return false;
    }

    const today = new Date();

    // Ngày dạng YYYY-MM-DD
    // Xử lý trực tiếp để tránh lệch ngày do UTC.
    const dateOnlyMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(
      dateValue
    );

    if (dateOnlyMatch) {
      const year = Number(dateOnlyMatch[1]);
      const month = Number(dateOnlyMatch[2]);
      const day = Number(dateOnlyMatch[3]);

      return (
        year === today.getFullYear() &&
        month === today.getMonth() + 1 &&
        day === today.getDate()
      );
    }

    // Ngày dạng ISO datetime hoặc timestamp string.
    const date = new Date(dateValue);

    if (Number.isNaN(date.getTime())) {
      return false;
    }

    return (
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate()
    );
  }

  // =====================================
  // Đồng bộ trạng thái chấm công
  // =====================================

  private syncAttendanceState(
    records: AttendanceRecord[]
  ): void {
    this.timekeepingRecords = records;

    this.todayRecord =
      records.find(record => this.isToday(record.date)) ?? null;

    this.checkedIn = !!(
      this.todayRecord?.checkInTime &&
      !this.todayRecord?.checkOutTime
    );

    this.updateWorkDuration();
  }

  // =====================================
  // Tải lịch sử chấm công
  // =====================================

  loadAttendanceHistory(): void {
    if (this.historyLoading) {
      return;
    }

    this.historyLoading = true;
    this.historyError = '';

    this.http.get<AttendanceRecord[]>('/api/history')
      .subscribe({
        next: (data) => {
          const records = Array.isArray(data) ? data : [];

          this.syncAttendanceState(records);
          this.historyLoading = false;
        },

        error: (err) => {
          console.error(
            'Failed to load attendance history:',
            err
          );

          this.historyError =
            err?.error?.message ??
            'Không thể tải lịch sử chấm công.';

          this.historyLoading = false;
        }
      });
  }

  // =====================================
  // Chấm công vào
  // =====================================

  checkIn(): void {
    if (this.loading || this.checkedIn) {
      return;
    }

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    // Không gửi employeeId từ frontend.
    // Backend xác định nhân viên từ thông tin xác thực.
    this.http.post<AttendanceResponse>(
      '/api/check-in',
      {}
    ).subscribe({
      next: (res) => {
        this.message =
          res?.message ?? 'Chấm công vào thành công.';

        this.isSuccess = true;
        this.loading = false;

        // Đồng bộ lại dữ liệu sau khi chấm công.
        this.loadAttendanceHistory();
      },

      error: (err) => {
        console.error('Check-in failed:', err);

        this.message =
          err?.error?.message ??
          'Chấm công vào thất bại. Vui lòng thử lại.';

        this.isSuccess = false;
        this.loading = false;
      }
    });
  }

  // =====================================
  // Chấm công ra
  // =====================================

  checkOut(): void {
    if (this.loading || !this.checkedIn) {
      return;
    }

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.http.post<AttendanceResponse>(
      '/api/check-out',
      {}
    ).subscribe({
      next: (res) => {
        this.message =
          res?.message ?? 'Chấm công ra thành công.';

        this.isSuccess = true;
        this.loading = false;

        // Đồng bộ lại dữ liệu sau khi chấm công.
        this.loadAttendanceHistory();
      },

      error: (err) => {
        console.error('Check-out failed:', err);

        this.message =
          err?.error?.message ??
          'Chấm công ra thất bại. Vui lòng thử lại.';

        this.isSuccess = false;
        this.loading = false;
      }
    });
  }

  // =====================================
  // Lịch sử chấm công kèm nhân viên
  // Dành cho màn hình quản trị nếu cần
  // =====================================

  getTimekeepingWithEmployees(): void {
    this.http.get<AttendanceRecord[]>(
      '/api/timekeeping-with-employees'
    ).subscribe({
      next: (data) => {
        const records = Array.isArray(data) ? data : [];

        this.syncAttendanceState(records);
      },

      error: (err) => {
        console.error(
          'Failed to load timekeeping with employees:',
          err
        );
      }
    });
  }
}
