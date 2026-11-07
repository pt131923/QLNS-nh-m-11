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

  message = '';
  isSuccess = false;

  loading = false;
  checkedIn = false;

  currentTime = new Date();
  workDuration = '00:00:00';

  todayRecord: AttendanceRecord | null = null;
  timekeepingRecords: AttendanceRecord[] = [];

  historyLoading = false;
  historyError = '';

  private timerId?: ReturnType<typeof setInterval>;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.updateClock();
    this.timerId = setInterval(() => this.updateClock(), 1000);
    this.loadAttendanceHistory();
  }

  ngOnDestroy(): void {
    if (this.timerId) clearInterval(this.timerId);
  }

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
    const now = this.currentTime.getTime();

    if (!Number.isFinite(startTime) || now < startTime) {
      this.workDuration = '00:00:00';
      return;
    }

    const totalSeconds = Math.floor((now - startTime) / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    this.workDuration = [
      hours.toString().padStart(2, '0'),
      minutes.toString().padStart(2, '0'),
      seconds.toString().padStart(2, '0')
    ].join(':');
  }

  private isToday(dateValue: string): boolean {
    if (!dateValue) return false;

    const dateOnlyMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateValue);
    const today = new Date();

    if (dateOnlyMatch) {
      const y = Number(dateOnlyMatch[1]);
      const m = Number(dateOnlyMatch[2]);
      const d = Number(dateOnlyMatch[3]);
      return y === today.getFullYear() && m === today.getMonth() + 1 && d === today.getDate();
    }

    const date = new Date(dateValue);
    if (Number.isNaN(date.getTime())) return false;
    return (
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate()
    );
  }

  private syncAttendanceState(records: AttendanceRecord[]): void {
    this.timekeepingRecords = records;
    this.todayRecord = records.find(r => this.isToday(r.date)) ?? null;
    this.checkedIn = !!(this.todayRecord?.checkInTime && !this.todayRecord?.checkOutTime);
    this.updateWorkDuration();
  }

  loadAttendanceHistory(): void {
    if (this.historyLoading) return;

    this.historyLoading = true;
    this.historyError = '';

    this.http.get<AttendanceRecord[]>('/api/TimeKeeping/history')
      .subscribe({
        next: (data) => {
          const records = Array.isArray(data) ? data : [];
          this.syncAttendanceState(records);
          this.historyLoading = false;
        },
        error: (err) => {
          console.error('Failed to load attendance history:', err);
          this.historyError = err?.error?.message ?? 'Không thể tải lịch sử chấm công.';
          this.historyLoading = false;
          // Vẫn cho phép chấm công dù lỗi lịch sử
          if (!this.todayRecord) {
            this.checkedIn = false;
          }
        }
      });
  }

  checkIn(): void {
    if (this.loading || this.checkedIn) return;

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.http.post<AttendanceResponse>('/api/TimeKeeping/check-in', {})
      .subscribe({
        next: (res) => {
          this.message = res?.message ?? 'Chấm công vào thành công.';
          this.isSuccess = true;
          this.loading = false;
          this.loadAttendanceHistory();
        },
        error: (err) => {
          console.error('Check-in failed:', err);
          this.message = err?.error?.message ?? 'Chấm công vào thất bại. Vui lòng thử lại.';
          this.isSuccess = false;
          this.loading = false;
        }
      });
  }

  checkOut(): void {
    if (this.loading || !this.checkedIn) return;

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.http.post<AttendanceResponse>('/api/TimeKeeping/check-out', {})
      .subscribe({
        next: (res) => {
          this.message = res?.message ?? 'Chấm công ra thành công.';
          this.isSuccess = true;
          this.loading = false;
          this.loadAttendanceHistory();
        },
        error: (err) => {
          console.error('Check-out failed:', err);
          this.message = err?.error?.message ?? 'Chấm công ra thất bại. Vui lòng thử lại.';
          this.isSuccess = false;
          this.loading = false;
        }
      });
  }

  getTimekeepingWithEmployees(): void {
    this.http.get<AttendanceRecord[]>('/api/TimeKeeping/timekeeping-with-employees')
      .subscribe({
        next: (data) => {
          const records = Array.isArray(data) ? data : [];
          this.syncAttendanceState(records);
        },
        error: (err) => console.error('Failed to load timekeeping with employees:', err)
      });
  }
}