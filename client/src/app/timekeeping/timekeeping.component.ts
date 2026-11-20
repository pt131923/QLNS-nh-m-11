import { Component, OnInit, OnDestroy } from '@angular/core';
import { TimekeepingService } from '../_services/timekeeping.service';
import { DepartmentService } from '../_services/department.service';
import { AuthService } from '../_services/auth.service';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import * as XLSX from 'xlsx';

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

export interface EmployeeAttendanceRecord {
  timeKeepingId: number;
  employeeId: number;
  employeeName: string;
  employeeEmail: string;
  departmentId: number;
  departmentName: string;
  date: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  totalHoursWorked: any;
  totalHours: number;
  status: string;
  note: string;
}

@Component({
  selector: 'app-timekeeping',
  templateUrl: './timekeeping.component.html',
  styleUrls: ['./timekeeping.component.css']
})
export class TimekeepingComponent implements OnInit, OnDestroy {

  // Active Tab: 'stats' (Thống kê giờ làm nhân viên) hoặc 'personal' (Chấm công cá nhân)
  activeTab: 'stats' | 'personal' = 'personal';

  // Role-based access
  isAdmin = false;
  currentUser: any = null;

  // Personal Timekeeping state
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

  // Employee Statistics state
  allEmployeeRecords: EmployeeAttendanceRecord[] = [];
  departments: any[] = [];
  employeeLoading = false;
  employeeError = '';

  // Filters for Employee Statistics
  selectedDate: string = '';
  selectedDepartment: string = 'all';
  statusFilter: string = 'all';
  searchTerm: string = '';

  private timerId?: ReturnType<typeof setInterval>;
  private hasWarnedMissingData = false;

  constructor(
    private timekeepingService: TimekeepingService,
    private departmentService: DepartmentService,
    private authService: AuthService,
    public translate: TranslateService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    // Load user role
    this.loadCurrentUser();

    // Set default date to today YYYY-MM-DD
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    this.selectedDate = `${yyyy}-${mm}-${dd}`;

    this.updateClock();
    this.timerId = setInterval(() => this.updateClock(), 1000);

    this.loadAttendanceHistory();
    this.loadDepartments();
  }

  private loadCurrentUser(): void {
    const saved = localStorage.getItem('user');
    if (saved) {
      try {
        this.currentUser = JSON.parse(saved);
        const role = (this.currentUser?.role || '').toLowerCase();
        this.isAdmin = role === 'admin' || role === 'manager';
      } catch {
        this.isAdmin = false;
      }
    }
    // Admin mặc định mở tab thống kê, user thường mở tab cá nhân
    this.activeTab = this.isAdmin ? 'stats' : 'personal';
    // Chỉ admin mới load dữ liệu tất cả nhân viên
    if (this.isAdmin) {
      this.loadEmployeeAttendance();
    }
  }

  ngOnDestroy(): void {
    if (this.timerId) clearInterval(this.timerId);
  }

  setTab(tab: 'stats' | 'personal'): void {
    this.activeTab = tab;
  }

  private updateClock(): void {
    this.currentTime = new Date();
    this.updateWorkDuration();
  }

  // ==========================================
  // EMPLOYEE STATISTICS METHODS & GETTERS
  // ==========================================

  loadDepartments(): void {
    this.departmentService.getDepartments().subscribe({
      next: (data) => {
        this.departments = Array.isArray(data) ? data : [];
      },
      error: (err) => {
        console.error('Failed to load departments:', err);
      }
    });
  }

  loadEmployeeAttendance(): void {
    this.employeeLoading = true;
    this.employeeError = '';

    this.timekeepingService.getTimekeepingWithEmployees().subscribe({
      next: (data) => {
        const rawList = Array.isArray(data) ? data : [];
        this.allEmployeeRecords = rawList.map(r => this.normalizeEmployeeRecord(r));
        this.employeeLoading = false;
      },
      error: (err) => {
        console.error('Failed to load employee attendance records:', err);
        this.employeeError = err?.error?.message || 'Không thể tải dữ liệu thống kê nhân viên.';
        this.employeeLoading = false;
      }
    });
  }

  private normalizeEmployeeRecord(raw: any): EmployeeAttendanceRecord {
    const rawDate = raw.Date ?? raw.date ?? '';
    const rawCheckIn = raw.CheckInTime ?? raw.checkInTime ?? raw.CheckIn ?? raw.checkIn ?? null;
    const rawCheckOut = raw.CheckOutTime ?? raw.checkOutTime ?? raw.CheckOut ?? raw.checkOut ?? null;
    const rawTotalHours = raw.TotalHours ?? raw.totalHours ?? 0;
    const rawTotalWorked = raw.TotalHoursWorked ?? raw.totalHoursWorked ?? null;
    const status = raw.Status ?? raw.status ?? '';

    return {
      timeKeepingId: raw.TimeKeepingId ?? raw.timeKeepingId ?? 0,
      employeeId: raw.EmployeeId ?? raw.employeeId ?? 0,
      employeeName: raw.EmployeeName ?? raw.employeeName ?? `Nhân viên #${raw.EmployeeId ?? raw.employeeId}`,
      employeeEmail: raw.EmployeeEmail ?? raw.employeeEmail ?? '',
      departmentId: raw.DepartmentId ?? raw.departmentId ?? 0,
      departmentName: raw.DepartmentName ?? raw.departmentName ?? 'Chưa phân bổ',
      date: typeof rawDate === 'string' ? rawDate : (rawDate ? new Date(rawDate).toISOString() : ''),
      checkInTime: rawCheckIn != null ? String(rawCheckIn) : null,
      checkOutTime: rawCheckOut != null ? String(rawCheckOut) : null,
      totalHoursWorked: rawTotalWorked,
      totalHours: Number(rawTotalHours) || 0,
      status: status,
      note: raw.Note ?? raw.note ?? ''
    };
  }

  get filteredEmployeeRecords(): EmployeeAttendanceRecord[] {
    return this.allEmployeeRecords.filter(r => {
      // Date filter
      if (this.selectedDate) {
        if (!this.isSameDate(r.date, this.selectedDate)) return false;
      }

      // Department filter
      if (this.selectedDepartment !== 'all') {
        const matchId = String(r.departmentId) === this.selectedDepartment;
        const matchName = r.departmentName === this.selectedDepartment;
        if (!matchId && !matchName) return false;
      }

      // Status filter
      if (this.statusFilter === 'under8h') {
        if (!this.hasTime(r.checkOutTime) || r.totalHours >= 8) return false;
      } else if (this.statusFilter === 'standard') {
        if (r.totalHours < 8 || !this.hasTime(r.checkOutTime)) return false;
      } else if (this.statusFilter === 'working') {
        if (!this.hasTime(r.checkInTime) || this.hasTime(r.checkOutTime)) return false;
      } else if (this.statusFilter === 'missing') {
        if (r.status !== 'Quên chấm công ra' && (this.hasTime(r.checkOutTime) || this.isToday(r.date))) return false;
      }

      // Search term
      if (this.searchTerm.trim()) {
        const term = this.searchTerm.toLowerCase().trim();
        const nameMatch = r.employeeName?.toLowerCase().includes(term);
        const emailMatch = r.employeeEmail?.toLowerCase().includes(term);
        const idMatch = String(r.employeeId).includes(term);
        const deptMatch = r.departmentName?.toLowerCase().includes(term);
        if (!nameMatch && !emailMatch && !idMatch && !deptMatch) return false;
      }

      return true;
    });
  }

  get totalStatsEmployees(): number {
    const ids = new Set(this.filteredEmployeeRecords.map(r => r.employeeId));
    return ids.size;
  }

  get totalStatsHours(): number {
    return Math.round(this.filteredEmployeeRecords.reduce((sum, r) => sum + (r.totalHours || 0), 0) * 10) / 10;
  }

  get totalStatsStandard(): number {
    return this.filteredEmployeeRecords.filter(r => r.totalHours >= 8).length;
  }

  get totalStatsUnder8h(): number {
    return this.filteredEmployeeRecords.filter(r => this.hasTime(r.checkOutTime) && r.totalHours < 8).length;
  }

  get totalStatsWorking(): number {
    return this.filteredEmployeeRecords.filter(r => this.hasTime(r.checkInTime) && !this.hasTime(r.checkOutTime)).length;
  }

  setTodayDate(): void {
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    this.selectedDate = `${yyyy}-${mm}-${dd}`;
  }

  clearDateFilter(): void {
    this.selectedDate = '';
  }

  resetFilters(): void {
    this.setTodayDate();
    this.selectedDepartment = 'all';
    this.statusFilter = 'all';
    this.searchTerm = '';
  }

  exportToExcel(): void {
    const records = this.filteredEmployeeRecords;
    if (!records || records.length === 0) {
      this.toastr.warning('Không có dữ liệu để xuất file Excel.');
      return;
    }

    const dataToExport = records.map((r, index) => ({
      'STT': index + 1,
      'Mã NV': r.employeeId,
      'Họ và tên': r.employeeName,
      'Email': r.employeeEmail,
      'Phòng ban': r.departmentName || 'Chưa phân bổ',
      'Ngày': this.formatDate(r.date),
      'Giờ vào': this.formatTime(r.checkInTime),
      'Giờ ra': this.formatTime(r.checkOutTime),
      'Tổng giờ làm (giờ)': r.totalHours,
      'Trạng thái': this.getStatusText(r.status),
      'Đánh giá': r.totalHours >= 8 ? 'Đạt chuẩn (≥8h)' : (this.hasTime(r.checkOutTime) ? 'Chưa đủ 8h' : 'Đang làm việc'),
      'Ghi chú': r.note || ''
    }));

    const worksheet = XLSX.utils.json_to_sheet(dataToExport);
    const workbook = { Sheets: { 'BaoCaoChamCong': worksheet }, SheetNames: ['BaoCaoChamCong'] };
    const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
    const blob = new Blob([excelBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

    const dateStr = this.selectedDate ? this.selectedDate : 'TatCa';
    const filename = `BaoCao_ChamCong_NhanVien_${dateStr}.xlsx`;

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    window.URL.revokeObjectURL(url);

    this.toastr.success('Xuất file Excel thành công!');
  }

  // ==========================================
  // PERSONAL ATTENDANCE METHODS & HELPERS
  // ==========================================

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

  formatDate(val: any): string {
    if (!val) return '--/--/----';
    const d = new Date(val);
    if (!isNaN(d.getTime())) {
      const day = d.getDate().toString().padStart(2, '0');
      const month = (d.getMonth() + 1).toString().padStart(2, '0');
      const year = d.getFullYear();
      return `${day}/${month}/${year}`;
    }
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(val));
    if (match) {
      return `${match[3]}/${match[2]}/${match[1]}`;
    }
    return String(val);
  }

  get isCheckedIn(): boolean {
    return !!(
      this.todayRecord &&
      this.hasTime(this.todayRecord.checkInTime) &&
      !this.hasTime(this.todayRecord.checkOutTime) &&
      this.todayRecord.status !== 'Quên chấm công ra'
    );
  }

  get isCheckedOut(): boolean {
    return !!(
      this.todayRecord &&
      this.hasTime(this.todayRecord.checkInTime) &&
      this.hasTime(this.todayRecord.checkOutTime)
    );
  }

  get isTodayReset(): boolean {
    return !!(
      this.todayRecord &&
      this.todayRecord.status === 'Quên chấm công ra' &&
      !this.hasTime(this.todayRecord.checkOutTime)
    );
  }

  get missingCheckOutRecords(): AttendanceRecord[] {
    return this.timekeepingRecords.filter(r =>
      !this.isToday(r.date) &&
      this.hasTime(r.checkInTime) &&
      (!this.hasTime(r.checkOutTime) || r.status === 'Quên chấm công ra')
    );
  }

  get latestMissingRecord(): AttendanceRecord | null {
    const list = this.missingCheckOutRecords;
    return list.length > 0 ? list[0] : null;
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

  private isSameDate(dateA: any, dateB: string): boolean {
    if (!dateA || !dateB) return false;
    const sA = String(dateA).slice(0, 10);
    const sB = String(dateB).slice(0, 10);
    if (sA === sB) return true;

    const dA = new Date(dateA);
    const dB = new Date(dateB);
    if (!isNaN(dA.getTime()) && !isNaN(dB.getTime())) {
      return (
        dA.getFullYear() === dB.getFullYear() &&
        dA.getMonth() === dB.getMonth() &&
        dA.getDate() === dB.getDate()
      );
    }
    return false;
  }

  isToday(dateValue: any): boolean {
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
    // Nút chấm công tự động reset nếu không checkout ngày hôm trước:
    // CHỈ lấy bản ghi thuộc ngày hôm nay (isToday). Tuyệt đối không fallback sang ngày cũ!
    this.todayRecord = records.find(r => this.isToday(r.date)) ?? null;
    this.checkedIn = this.isCheckedIn;
    this.updateWorkDuration();

    // Tự động kiểm tra và cảnh báo nếu có ngày trước đó quên checkout / thiếu dữ liệu
    this.checkMissingCheckOutAlert();
  }

  private checkMissingCheckOutAlert(): void {
    if (this.hasWarnedMissingData) return;
    const latest = this.latestMissingRecord;
    if (latest) {
      this.hasWarnedMissingData = true;
      const dateFormatted = this.formatDate(latest.date);
      const isVi = (this.translate.currentLang || this.translate.defaultLang) !== 'en';
      const warnTitle = isVi ? 'Cảnh báo thiếu dữ liệu chấm công' : 'Missing Attendance Data Warning';
      const warnMsg = isVi
        ? `⚠️ Bạn chưa chấm công ra vào ngày ${dateFormatted}. Nút chấm công đã được tự động đặt lại.`
        : `⚠️ You forgot to check out on ${dateFormatted}. Attendance button has been automatically reset.`;
      this.toastr.warning(warnMsg, warnTitle, {
        timeOut: 8000,
        progressBar: true,
        closeButton: true
      });
    }
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
        this.loadEmployeeAttendance();
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

  getCurrentWorkedHours(): number {
    const record = this.todayRecord;
    if (!record || !this.hasTime(record.checkInTime)) return 0;

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

    if (!Number.isFinite(startTime)) return 0;
    const now = this.currentTime.getTime();
    if (now < startTime) return 0;

    return (now - startTime) / 3600000;
  }

  checkOut(): void {
    if (this.loading || !this.checkedIn) return;

    const currentHours = this.getCurrentWorkedHours();
    if (currentHours < 8) {
      const hours = Math.floor(currentHours);
      const minutes = Math.floor((currentHours * 60) % 60);
      const timeDisplay = `${hours}h ${minutes}m`;
      const isVi = (this.translate.currentLang || this.translate.defaultLang) !== 'en';
      const confirmMsg = isVi
        ? `⚠️ CẢNH BÁO: Bạn mới làm việc được ${timeDisplay} (chưa đủ tiêu chuẩn 8 tiếng/ngày).\n\nBạn có chắc chắn muốn chấm công ra không?`
        : `⚠️ WARNING: You have only worked for ${timeDisplay} (less than standard 8 hours/day).\n\nAre you sure you want to check out?`;

      if (!confirm(confirmMsg)) {
        return;
      }
    }

    this.loading = true;
    this.message = '';
    this.isSuccess = false;

    this.timekeepingService.CheckOut().subscribe({
      next: (res: any) => {
        this.message = res?.message || res?.Message || this.translate.instant('timekeeping.checkOutSuccess');
        this.isSuccess = true;
        this.checkedIn = false;
        this.loading = false;

        const isUnder8Hours = res?.isUnder8Hours || (res?.totalHours != null ? res.totalHours < 8 : currentHours < 8);
        if (isUnder8Hours) {
          this.toastr.warning(this.message, this.translate.instant('timekeeping.warning') || 'Cảnh báo chấm công');
        } else {
          this.toastr.success(this.message);
        }

        this.loadAttendanceHistory();
        this.loadEmployeeAttendance();
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

  resetAttendance(): void {
    if (this.loading) return;

    const isVi = (this.translate.currentLang || this.translate.defaultLang) !== 'en';
    const confirmMsg = this.translate.instant('timekeeping.resetConfirm') ||
      (isVi
        ? '⚠️ Bạn có chắc chắn muốn đặt lại (reset) trạng thái chấm công của hôm nay không?\n\nNếu bạn không check-out, nút chấm công sẽ tự động reset về ban đầu và ghi nhận cảnh báo thiếu dữ liệu của hôm đó.'
        : '⚠️ Are you sure you want to reset today attendance? If you did not check out, the attendance button will reset and today record will be flagged with missing data.');

    if (!confirm(confirmMsg)) {
      return;
    }

    this.loading = true;
    this.message = '';

    this.timekeepingService.ResetToday().subscribe({
      next: (res: any) => {
        this.loading = false;
        this.checkedIn = false;
        const msg = res?.message || this.translate.instant('timekeeping.resetSuccess');
        this.toastr.warning(msg, this.translate.instant('timekeeping.warning') || 'Cảnh báo chấm công');
        this.loadAttendanceHistory();
        this.loadEmployeeAttendance();
      },
      error: (err) => {
        this.loading = false;
        console.error('Reset attendance failed:', err);
        const errMsg = err?.error?.message || err?.message || 'Không thể đặt lại chấm công.';
        this.toastr.error(errMsg);
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
    if (status === 'Quên chấm công ra') return this.translate.instant('timekeeping.missingDataStatus') || 'Quên chấm công ra';
    return status;
  }
}
