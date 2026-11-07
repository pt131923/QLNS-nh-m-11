import { Component, OnInit, OnDestroy } from '@angular/core';
import { TimekeepingService } from '../_services/timekeeping.service';

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

  historyLoading = false;
  historyError = '';

  private timerId?: ReturnType<typeof setInterval>;

  constructor(private timekeepingService: TimekeepingService) {}

  get canCheckIn(): boolean {
    return !this.checkedIn && !this.hasFinishedToday;
  }

  get canCheckOut(): boolean {
    return this.checkedIn;
  }

  get hasFinishedToday(): boolean {
    return !!(this.todayRecord?.checkInTime && this.todayRecord?.checkOutTime);
  }

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

  formatTime(value: string | null | undefined): string {
    if (!this.hasTimeValue(value)) {
      return '--:--:--';
    }

    const clock = this.asClock(value as string);
    return clock ?? '--:--:--';
  }

  formatDate(value: string | null | undefined): string {
    const date = this.asDate(value);
    if (!date) {
      return '--/--/----';
    }

    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }

  formatHours(value: number | null | undefined): string {
    if (value == null || Number.isNaN(value)) {
      return '--';
    }

    return `${Number(value).toFixed(2)} giờ`;
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

    const startTime = this.toTimestamp(record.date, record.checkInTime);
    const currentTimestamp = this.currentTime.getTime();

    if (!Number.isFinite(startTime) || currentTimestamp < startTime) {
      this.workDuration = '00:00:00';
      return;
    }

    const totalSeconds = Math.floor((currentTimestamp - startTime) / 1000);
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
    const date = this.asDate(dateValue);
    if (!date) {
      return false;
    }

    const today = new Date();
    return (
      date.getFullYear() === today.getFullYear() &&
      date.getMonth() === today.getMonth() &&
      date.getDate() === today.getDate()
    );
  }

  private syncAttendanceState(records: AttendanceRecord[]): void {
    this.timekeepingRecords = records;
    this.todayRecord =
      records.find(record => this.isToday(record.date)) ?? null;

    this.checkedIn = !!(
      this.todayRecord?.checkInTime &&
      !this.todayRecord?.checkOutTime
    );

    this.updateWorkDuration();
  }

  loadAttendanceHistory(): void {
    if (this.historyLoading) {
      return;
    }

    this.historyLoading = true;
    this.historyError = '';

    this.timekeepingService.getTimekeepingRecords().subscribe({
      next: (data) => {
        const records = this.mapRecords(data);
        this.syncAttendanceState(records);
        this.historyLoading = false;
      },
      error: (err) => {
        console.error('Failed to load attendance history:', err);
        this.historyError =
          err?.error?.message ??
          'Không thể tải lịch sử chấm công.';
        this.historyLoading = false;
      }
    });
  }

  checkIn(): void {
    if (this.loading || !this.canCheckIn) {
      return;
    }

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.timekeepingService.CheckIn().subscribe({
      next: (res) => {
        this.message = res?.message ?? res?.Message ?? 'Chấm công vào thành công.';
        this.isSuccess = true;
        this.loading = false;
        this.applyLocalCheckIn();
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

  checkOut(): void {
    if (this.loading || !this.canCheckOut) {
      return;
    }

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.timekeepingService.CheckOut().subscribe({
      next: (res) => {
        this.message = res?.message ?? res?.Message ?? 'Chấm công ra thành công.';
        this.isSuccess = true;
        this.loading = false;
        this.applyLocalCheckOut();
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

  private applyLocalCheckIn(): void {
    const clock = this.formatClock(this.currentTime);
    this.todayRecord = {
      ...(this.todayRecord ?? { date: this.toDateOnly(this.currentTime), status: '' }),
      date: this.todayRecord?.date ?? this.toDateOnly(this.currentTime),
      checkInTime: clock,
      checkOutTime: null,
      totalHoursWorked: null,
      status: 'Đang làm việc'
    };
    this.checkedIn = true;
    this.updateWorkDuration();
  }

  private applyLocalCheckOut(): void {
    if (!this.todayRecord) {
      return;
    }

    const clock = this.formatClock(this.currentTime);
    const hours = this.hoursBetween(this.todayRecord.date, this.todayRecord.checkInTime, clock);

    this.todayRecord = {
      ...this.todayRecord,
      checkOutTime: clock,
      totalHoursWorked: hours,
      status: 'Đã chấm công'
    };
    this.checkedIn = false;
    this.updateWorkDuration();
  }

  private mapRecords(data: unknown): AttendanceRecord[] {
    const rows = Array.isArray(data) ? data : [];

    return rows.map((item: any) => {
      const checkIn = this.normalizeTime(
        item?.CheckInTime ?? item?.checkInTime ?? item?.CheckIn ?? item?.checkIn
      );
      const checkOut = this.normalizeTime(
        item?.CheckOutTime ?? item?.checkOutTime ?? item?.CheckOut ?? item?.checkOut
      );
      const totalHours = this.normalizeHours(
        item?.TotalHours ?? item?.totalHours ?? item?.TotalHoursWorked ?? item?.totalHoursWorked
      );

      return {
        id: item?.TimeKeepingId ?? item?.id ?? item?.timeKeepingId,
        date: item?.Date ?? item?.date ?? '',
        checkInTime: checkIn,
        checkOutTime: checkOut,
        totalHoursWorked: totalHours,
        status: item?.Status ?? item?.status ?? ''
      } as AttendanceRecord;
    });
  }

  private normalizeTime(value: unknown): string | null {
    if (value == null || value === '') {
      return null;
    }

    if (typeof value === 'number') {
      return this.clockFromSeconds(value);
    }

    const text = String(value).trim();
    if (!text || text === '00:00:00' || text.startsWith('00:00:00')) {
      return null;
    }

    const clock = this.asClock(text);
    return clock;
  }

  private normalizeHours(value: unknown): number | null {
    if (value == null || value === '') {
      return null;
    }

    if (typeof value === 'number') {
      return value > 0 ? value : null;
    }

    const text = String(value).trim();
    if (!text || text === '00:00:00') {
      return null;
    }

    const numeric = Number(text);
    if (Number.isFinite(numeric)) {
      return numeric > 0 ? numeric : null;
    }

    const clock = this.asClock(text);
    if (!clock) {
      return null;
    }

    const [hours, minutes, seconds] = clock.split(':').map(part => Number(part));
    const total = hours + minutes / 60 + seconds / 3600;
    return total > 0 ? total : null;
  }

  private hasTimeValue(value: string | null | undefined): boolean {
    return !!value && value !== '00:00:00';
  }

  private asClock(value: string): string | null {
    const timeMatch = /(\d{1,2}):(\d{2})(?::(\d{2}))?/.exec(value);
    if (!timeMatch) {
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) {
        return null;
      }
      return this.formatClock(date);
    }

    return [
      timeMatch[1].padStart(2, '0'),
      timeMatch[2],
      (timeMatch[3] ?? '00').padStart(2, '0')
    ].join(':');
  }

  private asDate(value: string | null | undefined): Date | null {
    if (!value) {
      return null;
    }

    const dateOnlyMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    if (dateOnlyMatch) {
      return new Date(
        Number(dateOnlyMatch[1]),
        Number(dateOnlyMatch[2]) - 1,
        Number(dateOnlyMatch[3])
      );
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private toTimestamp(dateValue: string, timeValue: string): number {
    const date = this.asDate(dateValue) ?? this.currentTime;
    const clock = this.asClock(timeValue);
    if (!clock) {
      return NaN;
    }

    const [hours, minutes, seconds] = clock.split(':').map(part => Number(part));
    const combined = new Date(date);
    combined.setHours(hours, minutes, seconds, 0);
    return combined.getTime();
  }

  private hoursBetween(dateValue: string, start: string | null, end: string): number | null {
    if (!start) {
      return null;
    }

    const startTime = this.toTimestamp(dateValue, start);
    const endTime = this.toTimestamp(dateValue, end);
    if (!Number.isFinite(startTime) || !Number.isFinite(endTime) || endTime < startTime) {
      return null;
    }

    return (endTime - startTime) / 3600000;
  }

  private formatClock(date: Date): string {
    return [
      date.getHours().toString().padStart(2, '0'),
      date.getMinutes().toString().padStart(2, '0'),
      date.getSeconds().toString().padStart(2, '0')
    ].join(':');
  }

  private toDateOnly(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private clockFromSeconds(totalSeconds: number): string | null {
    if (!Number.isFinite(totalSeconds) || totalSeconds <= 0) {
      return null;
    }

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = Math.floor(totalSeconds % 60);
    return [
      hours.toString().padStart(2, '0'),
      minutes.toString().padStart(2, '0'),
      seconds.toString().padStart(2, '0')
    ].join(':');
  }
}
