// (TOÀN BỘ CODE CỦA BẠN GIỮ NGUYÊN — CHỈ THÊM 1 BIẾN + 3 ĐOẠN XỬ LÝ)

import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import * as XLSX from 'xlsx';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';

import { EmployeeService } from './../../_services/employee.service';
import { DepartmentService } from './../../_services/department.service';
import { Employee } from 'src/app/_model/employee';
import { Department } from 'src/app/_model/department';

@Component({
  selector: 'app-employee-add',
  templateUrl: './employee-add.component.html',
  styleUrls: ['./employee-add.component.css']
})
export class EmployeeAddComponent implements OnInit, OnDestroy {
  @ViewChild('addForm') addForm!: NgForm;
  @ViewChild('excelInput') excelInput?: ElementRef<HTMLInputElement>;

  // UI state
  apiError: string | null = null;
  fileError: string | null = null;
  fileUploadError: string | null = null;

  // 👉 Thêm cho hiển thị lỗi CCCD trên UI
  cccdError: string | null = null;

  // dữ liệu
  employee: Employee = this.getEmptyEmployee();
  departments: Department[] = [];

  // file/preview state
  selectedFile: File | null = null;
  selectedFileName = '';
  previewRows: any[] = [];
  previewColumns: string[] = [];
  isUploading = false;
  isImporting = false;

  // internal
  private lastSuccessfulFileName = '';
  private readonly allowedExcelMimeTypes = [
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/vnd.ms-excel'
  ];
  private readonly maxExcelSizeInBytes = 5 * 1024 * 1024;
  private destroy$ = new Subject<void>();

  constructor(
    private empService: EmployeeService,
    private departService: DepartmentService,
    private toastr: ToastrService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDepartments();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /* ==========================================================
   *          ADD EMPLOYEE — chỉ thêm xử lý lỗi CCCD  
   * ========================================================== */
  AddEmployee() {
    this.apiError = null;
    this.cccdError = null;
    this.fileUploadError = null;

    if (this.addForm.invalid) {
      this.toastr.error('Vui lòng điền đầy đủ các trường bắt buộc');
      return;
    }

    const cccdRegex = /^\d{12}$/;

    if (!cccdRegex.test(this.employee.IdentityNumber ?? '')) {
      this.cccdError = 'CCCD phải gồm đúng 12 chữ số.';
      try {
        this.addForm.form.controls['IdentityNumber'].setErrors({ invalidCccd: true });
      } catch {}
      return;
    }

    this.empService.AddEmployee(this.employee)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastr.success('Thêm nhân viên thành công');
          this.addForm.resetForm();
          this.employee = this.getEmptyEmployee();
          this.router.navigate(['/employees']);
        },

        error: (err) => {
          const msg = this.extractErrorMessage(err, 'Thêm nhân viên thất bại');

          // 👉 Nếu trùng CCCD — set lỗi UI
          if (this.isIdentityDuplication(err)) {
            this.cccdError = 'Số CCCD đã tồn tại trong hệ thống.';
            try {
              this.addForm.form.controls['IdentityNumber'].setErrors({ duplicate: true });
            } catch {}
          }

          this.apiError = msg;
        }
      });
  }

  onFileSelected(event: Event) {
    this.apiError = null;
    this.fileError = null;
    this.fileUploadError = null;

    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.clearFileSelection(true);
      this.setFileError('Vui lòng chọn file Excel (.xls, .xlsx)');
      return;
    }

    const file = input.files[0];

    // 👉 Thêm lỗi file đã từng upload
    if (this.lastSuccessfulFileName && file.name === this.lastSuccessfulFileName) {
      this.fileUploadError = `File "${file.name}" đã được tải lên và xử lý trước đó.`;
      return;
    }

    const ext = file.name.split('.').pop()?.toLowerCase() || '';
    const allowedExt = ['xls', 'xlsx'];

    if (!this.allowedExcelMimeTypes.includes(file.type) && !allowedExt.includes(ext)) {
      this.clearFileSelection(true);
      this.setFileError('File không đúng định dạng (.xls, .xlsx)');
      return;
    }

    if (file.size > this.maxExcelSizeInBytes) {
      this.clearFileSelection(true);
      this.setFileError(`File vượt quá 5MB`);
      return;
    }

    this.selectedFile = file;
    this.selectedFileName = file.name;
    this.generatePreview(file);
  }

  uploadExcel() {
    this.apiError = null;
    this.fileUploadError = null;

    if (!this.ensureFileReady('upload')) return;

    this.isUploading = true;
    this.empService.uploadExcel(this.selectedFile!)
      .pipe(finalize(() => this.isUploading = false), takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastr.success('Tải file Excel thành công');
          this.lastSuccessfulFileName = this.selectedFileName;
          this.clearFileSelection();
        },
        error: (err) => this.handleUploadError(err)
      });
  }

  importEmployees() {
    this.apiError = null;
    this.fileUploadError = null;

    if (!this.ensureFileReady('import')) return;

    this.isImporting = true;
    this.empService.importEmployees(this.selectedFile!)
      .pipe(finalize(() => this.isImporting = false), takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          if (res && typeof res === 'object') {
            const importedCount = res.importedCount ?? 0;
            const totalRows = res.totalRows ?? 0;
            const errors = res.errors ?? [];

            if (importedCount > 0) {
              this.toastr.success(`Import thành công ${importedCount}/${totalRows} nhân viên`);
            } else {
              this.toastr.warning('Không có nhân viên nào được import.');
            }

            if (errors.length > 0) {
              const preview = errors.slice(0, 5).map((e: { message: any; }) => e.message).join('; ');
              this.apiError = `Có ${errors.length} lỗi: ${preview}`;
            }
          }

          this.lastSuccessfulFileName = this.selectedFileName;
          this.clearFileSelection();
        },
        error: (err) => this.handleImportError(err)
      });
  }

  /* ============= REMAINING UTILITIES (GIỮ NGUYÊN) ============== */

  private ensureFileReady(action: 'upload' | 'import'): boolean {
    if (!this.selectedFile) {
      this.setFileError(action === 'upload'
        ? 'Vui lòng chọn file trước khi tải lên'
        : 'Vui lòng chọn file trước khi import');
      return false;
    }
    if (this.fileError) {
      this.toastr.error('File Excel chưa hợp lệ, vui lòng chọn lại');
      return false;
    }
    if (this.fileUploadError) {
      this.toastr.error(this.fileUploadError);
      return false;
    }
    return true;
  }

  private generatePreview(file: File) {
    const reader = new FileReader();
    reader.onload = (ev) => {
      try {
        const data = new Uint8Array(ev.target?.result as ArrayBuffer);
        const workbook = XLSX.read(data, { type: 'array' });
        const sheet = workbook.Sheets[workbook.SheetNames[0]];
        const json = XLSX.utils.sheet_to_json(sheet, { defval: '' });

        if (!json.length) {
          this.setFileError('File Excel không có dữ liệu');
          return;
        }

        this.previewRows = json.slice(0, 5);
        this.previewColumns = Object.keys(json[0] as object);

      } catch {
        this.setFileError('Không thể đọc dữ liệu file');
      }
    };

    reader.onerror = () => this.setFileError('Lỗi đọc file Excel');
    reader.readAsArrayBuffer(file);
  }

  private clearFileSelection(preserveError = false) {
    this.selectedFile = null;
    this.selectedFileName = '';
    this.previewRows = [];
    this.previewColumns = [];
    if (!preserveError) {
      this.fileError = null;
      this.fileUploadError = null;
    }
    if (this.excelInput?.nativeElement) {
      this.excelInput.nativeElement.value = '';
    }
  }

  private setFileError(msg: string) {
    this.fileError = msg;
    this.toastr.warning(msg);
  }

  private handleUploadError(err: any) {
    const msg = this.extractErrorMessage(err, 'Upload file Excel thất bại');
    this.apiError = msg;
  }

  private handleImportError(err: any) {
    const msg = this.extractErrorMessage(err, 'Import nhân viên thất bại');
    if (this.isFileAlreadyProcessed(err)) {
      this.fileUploadError = msg;
    } else {
      this.apiError = msg;
    }
  }

  private extractErrorMessage(error: any, fallback: string): string {
    const serverMessage = error?.error?.message || error?.error?.title || error?.message;

    if (serverMessage) return serverMessage;

    if (error?.error?.errors) {
      const values = Object.values(error.error.errors);
      if (Array.isArray(values) && values.length > 0) {
        const firstArr = values[0];
        if (Array.isArray(firstArr) && firstArr.length > 0 && typeof firstArr[0] === 'string') {
          return firstArr[0];
        }
      }
    }

    return fallback;
  }

  private isIdentityDuplication(error: any): boolean {
    const msg = (error?.error?.message || '').toLowerCase();
    return msg.includes('duplicate') && msg.includes('identity');
  }

  private isFileAlreadyProcessed(error: any): boolean {
    const msg = (error?.error?.message || '').toLowerCase();
    return msg.includes('file already');
  }

  private loadDepartments() {
    this.departService.getDepartments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (ds) => this.departments = ds,
        error: () => this.toastr.error('Không thể tải danh sách phòng ban')
      });
  }

  private getEmptyEmployee(): Employee {
    return {
      EmployeeId: 0,
      EmployeeName: '',
      DepartmentId: 0,
      EmployeeEmail: '',
      EmployeePhone: '',
      EmployeeAddress: '',
      EmployeeInformation: '',
      BirthDate: undefined,
      PlaceOfBirth: '',
      Gender: '',
      MaritalStatus: '',
      IdentityNumber: '',
      IdentityIssuedDate: undefined,
      IdentityIssuedPlace: '',
      Religion: '',
      Ethnicity: '',
      Nationality: '',
      EducationLevel: '',
      Specialization: ''
    };
  }
}
