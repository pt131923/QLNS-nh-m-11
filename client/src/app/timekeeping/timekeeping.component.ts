import { Component, OnInit, OnDestroy } from '@angular/core';
import { TimekeepingService } from '../_services/timekeeping.service';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';

export interface AttendanceRecord {
  id?: number;
  timeKeepingId?: number;
  employeeId?: number;
  date: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  totalHoursWorked: any;
  totalHours?: number | null;
  status: string;
  note?: string;
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

  constructor(
    private timekeepingService: TimekeepingService,
    public translate: TranslateService,
    private toastr: ToastrService
  ) {}

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

  hasTime(val: any): boolean {
    if (!val) return false;
    const s = String(val).trim();
    return s !== '' && s !== '00:00:00' && s !== '00:00';
  }

  formatTime(val: any): string {
    if (!this.hasTime(val)) return '--:--:--';
    const s = String(val).trim();
    if (s.includes('T')) {
      const d = new Date(s);
      if (!isNaN(d.getTime())) {
        return [
          d.getHours().toString().padStart(2, '0'),
          d.getMinutes().toString().padStart(2, '0'),
          d.getSeconds().toString().padStart(2, '0')
        ].join(':');
      }
    }
    return s.substring(0, 8);
  }

  get isCheckedIn(): boolean {
    return !!(this.todayRecord && this.hasTime(this.todayRecord.checkInTime) && !this.hasTime(this.todayRecord.checkOutTime));
  }

  get isCheckedOut(): boolean {
    return !!(this.todayRecord && this.hasTime(this.todayRecord.checkInTime) && this.hasTime(this.todayRecord.checkOutTime));
  }

  private updateWorkDuration(): void {
    const record = this.todayRecord;

    if (!record || !this.hasTime(record.checkInTime) || this.hasTime(record.checkOutTime)) {
      this.workDuration = '00:00:00';
      return;
    }

    const timeStr = String(record.checkInTime).trim();
    let startTime = NaN;

    if (timeStr.includes(':')) {
      const parts = timeStr.split(':');
      const start = new Date(this.currentTime);
      start.setHours(Number(parts[0]) || 0, Number(parts[1]) || 0, Number(parts[2]) || 0, 0);
      startTime = start.getTime();
    } else {
      startTime = new Date(timeStr).getTime();
    }

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

    const today = new Date();
    const todayY = today.getFullYear();
    const todayM = today.getMonth() + 1;
    const todayD = today.getDate();

    const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(dateValue);
    if (m) {
      return Number(m[1]) === todayY && Number(m[2]) === todayM && Number(m[3]) === todayD;
    }

    const d = new Date(dateValue);
    if (!isNaN(d.getTime())) {
      return d.getFullYear() === todayY && (d.getMonth() + 1) === todayM && d.getDate() === todayD;
    }

    return false;
  }

  private syncAttendanceState(records: AttendanceRecord[]): void {
    this.timekeepingRecords = records;
    this.todayRecord = records.find(r => this.isToday(r.date)) ?? null;
    this.checkedIn = this.isCheckedIn;
    this.updateWorkDuration();
  }

  loadAttendanceHistory(): void {
    if (this.historyLoading) return;

    this.historyLoading = true;
    this.historyError = '';

    this.timekeepingService.getHistory().subscribe({
      next: (data) => {
        const records = Array.isArray(data) ? data : [];
        this.syncAttendanceState(records);
        this.historyLoading = false;
      },
      error: (err) => {
        console.error('Failed to load attendance history:', err);
        this.historyError = err?.error?.message ?? this.translate.instant('timekeeping.historyError');
        this.historyLoading = false;
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

    this.timekeepingService.CheckIn().subscribe({
      next: (res) => {
        this.message = res?.message || res?.Message || this.translate.instant('timekeeping.checkInSuccess');
        this.isSuccess = true;
        this.loading = false;
        this.toastr.success(this.message);
        this.loadAttendanceHistory();
      },
      error: (err) => {
        console.error('Check-in failed:', err);
        this.message = err?.error?.message || err?.message || this.translate.instant('timekeeping.checkInFailed');
        this.isSuccess = false;
        this.loading = false;
        this.toastr.error(this.message);
      }
    });
  }

  checkOut(): void {
    if (this.loading || !this.checkedIn) return;

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.timekeepingService.CheckOut().subscribe({
      next: (res) => {
        this.message = res?.message || res?.Message || this.translate.instant('timekeeping.checkOutSuccess');
        this.isSuccess = true;
        this.loading = false;
        this.toastr.success(this.message);
        this.loadAttendanceHistory();
      },
      error: (err) => {
        console.error('Check-out failed:', err);
        this.message = err?.error?.message || err?.message || this.translate.instant('timekeeping.checkOutFailed');
        this.isSuccess = false;
        this.loading = false;
        this.toastr.error(this.message);
      }
    });
  }

  getStatusText(status: string): string {
    if (!status) return '';
    if (status === 'Đang làm việc') return this.translate.instant('timekeeping.working');
    if (status === 'Đã chấm công' || status === 'Đã hoàn thành') return this.translate.instant('timekeeping.completed');
    if (status === 'Có mặt') return this.translate.instant('timekeeping.present');
    if (status === 'Vắng mặt') return this.translate.instant('timekeeping.absent');
    if (status === 'Đi muộn') return this.translate.instant('timekeeping.late');
    if (status === 'Về sớm') return this.translate.instant('timekeeping.earlyLeave');
    return status;
  }
}
