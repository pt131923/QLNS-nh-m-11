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
    if (s === '' || s === '--:--:--') return false;
    if (/^00:00(:00(\.0+)?)?$/.test(s)) return false;
    return true;
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
    const match = /^(\d{1,2}):(\d{2})(:(\d{2}))?/.exec(s);
    if (match) {
      const h = match[1].padStart(2, '0');
      const m = match[2];
      const sec = match[4] ? match[4] : '00';
      return `${h}:${m}:${sec}`;
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

    if (timeStr.includes('T')) {
      startTime = new Date(timeStr).getTime();
    } else if (timeStr.includes(':')) {
      const parts = timeStr.split(':');
      const hours = parseInt(parts[0], 10) || 0;
      const minutes = parseInt(parts[1], 10) || 0;
      const seconds = parseFloat(parts[2]) || 0;
      const start = new Date(this.currentTime);
      start.setHours(hours, minutes, Math.floor(seconds), 0);
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

  private isToday(dateValue: any): boolean {
    if (!dateValue) return false;

    const today = new Date();
    const todayY = today.getFullYear();
    const todayM = today.getMonth() + 1;
    const todayD = today.getDate();

    if (dateValue instanceof Date) {
      return (
        dateValue.getFullYear() === todayY &&
        dateValue.getMonth() + 1 === todayM &&
        dateValue.getDate() === todayD
      );
    }

    const str = String(dateValue).trim();

    const d = new Date(str);
    if (!isNaN(d.getTime())) {
      if (
        d.getFullYear() === todayY &&
        d.getMonth() + 1 === todayM &&
        d.getDate() === todayD
      ) {
        return true;
      }
    }

    const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(str);
    if (m) {
      if (
        Number(m[1]) === todayY &&
        Number(m[2]) === todayM &&
        Number(m[3]) === todayD
      ) {
        return true;
      }
    }

    return false;
  }

  private normalizeRecord(raw: any): AttendanceRecord {
    if (!raw) return raw;
    const rawDate = raw.date ?? raw.Date ?? '';
    const rawCheckIn = raw.checkInTime ?? raw.CheckInTime ?? raw.checkIn ?? raw.CheckIn ?? null;
    const rawCheckOut = raw.checkOutTime ?? raw.CheckOutTime ?? raw.checkOut ?? raw.CheckOut ?? null;
    const rawTotalWorked = raw.totalHoursWorked ?? raw.TotalHoursWorked ?? null;
    const rawTotalHours = raw.totalHours ?? raw.TotalHours ?? null;
    const rawStatus = raw.status ?? raw.Status ?? '';
    const rawNote = raw.note ?? raw.Note ?? '';
    const id = raw.id ?? raw.timeKeepingId ?? raw.TimeKeepingId;

    return {
      id,
      timeKeepingId: raw.timeKeepingId ?? raw.TimeKeepingId ?? id,
      employeeId: raw.employeeId ?? raw.EmployeeId,
      date: typeof rawDate === 'string' ? rawDate : (rawDate ? new Date(rawDate).toISOString() : ''),
      checkInTime: rawCheckIn != null ? String(rawCheckIn) : null,
      checkOutTime: rawCheckOut != null ? String(rawCheckOut) : null,
      totalHoursWorked: rawTotalWorked,
      totalHours: rawTotalHours != null ? Number(rawTotalHours) : null,
      status: rawStatus,
      note: rawNote
    };
  }

  private syncAttendanceState(records: AttendanceRecord[]): void {
    this.timekeepingRecords = records;
    this.todayRecord = records.find(r => this.isToday(r.date)) ??
      (records.length > 0 && records[0].status === 'Đang làm việc' ? records[0] : null);
    this.checkedIn = this.isCheckedIn;
    this.updateWorkDuration();
  }

  loadAttendanceHistory(): void {
    if (this.historyLoading) return;

    this.historyLoading = true;
    this.historyError = '';

    this.timekeepingService.getHistory().subscribe({
      next: (data) => {
        const rawList = Array.isArray(data) ? data : [];
        const records = rawList.map(r => this.normalizeRecord(r));
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
        this.checkedIn = true;
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
        this.loadAttendanceHistory();
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
        this.checkedIn = false;
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
        this.loadAttendanceHistory();
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
