import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Salary } from 'src/app/_model/salary';
import { SalaryService } from 'src/app/_services/salary.service';
import { Employee } from 'src/app/_model/employee';
import { EmployeeService } from 'src/app/_services/employee.service';
import * as XLSX from 'xlsx';
import { Department } from 'src/app/_model/department';
import { DepartmentService } from 'src/app/_services/department.service';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-salary-add',
  templateUrl: './salary-add.component.html',
  styleUrls: ['./salary-add.component.css']
})
export class SalaryAddComponent implements OnInit {
  @ViewChild('addSalaryForm') addSalaryForm!: NgForm;
  @ViewChild('excelInput') excelInput?: ElementRef<HTMLInputElement>;

  salary: Salary = this.emptySalary();
  period = this.currentPeriod();
  previewing = false;
  formulaNotes = '';

  employees: Employee[] = [];
  departments: Department[] = [];
  selectedDepartmentId: number | null = null;

  selectedFile: File | null = null;
  selectedFileName = '';
  fileError: string | null = null;
  previewRows: Record<string, any>[] = [];
  previewColumns: string[] = [];
  isUploading = false;
  isImporting = false;
  loadingEmployees = false;
  loadingDepartments = false;

  private previewTimer?: ReturnType<typeof setTimeout>;
  private readonly allowedExcelMimeTypes = [
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/vnd.ms-excel'
  ];
  private readonly maxExcelSizeInBytes = 5 * 1024 * 1024;

  constructor(
    private salaryService: SalaryService,
    private employeeService: EmployeeService,
    private departService: DepartmentService,
    private toastr: ToastrService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.syncPeriod();
    this.getEmployees();
    this.getDepartments();
  }

  getEmployees(): void {
    this.loadingEmployees = true;
    this.employeeService.getEmployees()
      .pipe(finalize(() => this.loadingEmployees = false))
      .subscribe({
      next: (employees) => {
        this.employees = employees;
      },
      error: () => {
        this.toastr.error('Failed to fetch employees');
      }
    });
  }

  getDepartments(): void {
    this.loadingDepartments = true;
    this.departService.getDepartments()
      .pipe(finalize(() => this.loadingDepartments = false))
      .subscribe({
        next: (departments) => {
          this.departments = departments;
        },
        error: () => {
          this.toastr.error('Không thể tải danh sách phòng ban');
        }
      });
  }

  onPeriodChange(): void {
    this.syncPeriod();
    this.queuePreview();
  }

  queuePreview(): void {
    if (this.previewTimer) clearTimeout(this.previewTimer);
    this.previewTimer = setTimeout(() => this.previewPayroll(), 350);
  }

  previewPayroll(): void {
    this.syncPeriod();
    if (!this.salary.EmployeeId) return;

    this.previewing = true;
    this.salaryService.calculatePayroll({
      EmployeeId: Number(this.salary.EmployeeId),
      Year: this.salary.PeriodYear || 0,
      Month: this.salary.PeriodMonth || 0,
      BasicSalary: this.salary.BasicSalary || null,
      Allowance: this.salary.Allowance || null,
      InsuranceSalary: this.salary.InsuranceSalary || null,
      WorkedDays: this.salary.WorkedDays || null,
      StandardDays: this.salary.StandardDays || null,
      UnpaidLeaveDays: this.salary.UnpaidLeaveDays || 0,
      OvertimeHours: this.salary.OvertimeHours || 0,
      Bonus: this.salary.Bonus || 0,
      OtherDeductions: this.salary.OtherDeductions || 0,
      DependentCount: this.salary.DependentCount || 0,
      DeductUnionFee: !!this.salary.DeductUnionFee,
      Save: false
    }).pipe(finalize(() => this.previewing = false)).subscribe({
      next: (result) => this.applyPreview(result),
      error: (err) => {
        this.toastr.warning(err?.error?.message || 'Chưa tính được phiếu lương. Kiểm tra hợp đồng/lương cơ bản.');
      }
    });
  }

  AddSalary(): void {
    if (!this.addSalaryForm || this.addSalaryForm.invalid) {
      this.toastr.error('Vui lòng điền đầy đủ thông tin bảng lương.');
      this.addSalaryForm?.form.markAllAsTouched();
      return;
    }

    if (!this.salary.EmployeeId) {
      this.toastr.warning('Vui lòng chọn nhân viên.');
      return;
    }

    this.syncPeriod();
    const payload: Salary = {
      ...this.salary,
      SalaryId: 0,
      EmployeeId: Number(this.salary.EmployeeId),
      EmployeeName: this.employees.find(emp => emp.EmployeeId === Number(this.salary.EmployeeId))?.EmployeeName,
      Date: this.salary.Date,
      MonthlySalary: Number(this.salary.BasicSalary) || Number(this.salary.MonthlySalary) || 0
    };

    this.salaryService.AddSalary(payload).subscribe({
      next: () => {
        this.toastr.success('Thêm bảng lương thành công!');
        this.resetForm();
        this.router.navigate(['/salaries']);
      },
      error: (error) => {
        this.toastr.error(this.extractErrorMessage(error, 'Thêm bảng lương thất bại'));
      }
    });
  }

  removeLeadingZero(field: string) {
    const value = this.salary.OvertimeHours;
    if (typeof value === 'string' && (value as string).startsWith('0')){
      this.salary.OvertimeHours = Number(value);
    }
  }

  normalizeNumber(field: string, value: any) {
    if (value === null || value === undefined || value === '') return;
    const cleaned = String(value).replace(/^0+/, '') || '0';
    this.salary.OvertimeHours = Number(cleaned);
    this.queuePreview();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.clearFileSelection(true);
      this.setFileError('Vui lòng chọn file Excel (.xls, .xlsx)');
      return;
    }

    const file = input.files[0];
    if (!this.allowedExcelMimeTypes.includes(file.type)) {
      this.clearFileSelection(true);
      this.setFileError('File không đúng định dạng, vui lòng chọn file .xls hoặc .xlsx');
      return;
    }

    if (file.size > this.maxExcelSizeInBytes) {
      this.clearFileSelection(true);
      this.setFileError('Kích thước file vượt quá 5MB');
      return;
    }

    this.selectedFile = file;
    this.selectedFileName = file.name;
    this.fileError = null;
    this.generatePreview(file);
  }

  uploadExcel() {
    if (!this.ensureFileReady('upload')) {
      return;
    }

    this.isUploading = true;
    this.salaryService.uploadExcel(this.selectedFile!, this.selectedDepartmentId!)
      .pipe(finalize(() => this.isUploading = false))
      .subscribe({
        next: () => {
          this.toastr.success('Tải file Excel thành công');
          this.clearFileSelection();
        },
        error: (error) => this.handleUploadError(error)
      });
  }

  importSalaries() {
    if (!this.ensureFileReady('import')) {
      return;
    }

    this.isImporting = true;
    this.salaryService.importSalaries(this.selectedFile!, this.selectedDepartmentId!)
      .pipe(finalize(() => this.isImporting = false))
      .subscribe({
        next: (response) => {
          if (response && response.importedCount !== undefined) {
            const importedCount = response.importedCount || 0;
            const totalRows = response.totalRows || 0;
            const errors = response.errors || [];

            if (importedCount > 0) {
              this.toastr.success(`Import thành công ${importedCount}/${totalRows} bảng lương`);
              if (errors.length > 0) {
                const errorMessage = errors.slice(0, 5).join('; ');
                this.toastr.warning(`Có ${errors.length} lỗi: ${errorMessage}${errors.length > 5 ? '...' : ''}`, 'Cảnh báo', {
                  timeOut: 10000
                });
              }
            } else {
              this.toastr.warning('Không có bảng lương nào được import. Vui lòng kiểm tra lại dữ liệu.');
              if (errors.length > 0) {
                const errorMessage = errors.slice(0, 5).join('; ');
                this.toastr.error(`Lỗi: ${errorMessage}${errors.length > 5 ? '...' : ''}`, 'Chi tiết lỗi', {
                  timeOut: 15000
                });
              }
            }
          } else {
            this.toastr.success('Import bảng lương thành công');
          }
          this.clearFileSelection();
        },
        error: (error: any) => this.handleImportError(error)
      });
  }

  money(value?: number | null): string {
    return Number(value || 0).toLocaleString('vi-VN') + ' ₫';
  }

  onCancel(): void {
    this.resetForm();
    this.router.navigate(['/salaries']);
  }

  private applyPreview(result: Salary): void {
    this.salary.GrossSalary = result.GrossSalary;
    this.salary.OvertimePay = result.OvertimePay;
    if (!this.salary.BasicSalary) this.salary.BasicSalary = result.BasicSalary;
    if (!this.salary.Allowance) this.salary.Allowance = result.Allowance;
    if (!this.salary.InsuranceSalary) this.salary.InsuranceSalary = result.InsuranceSalary;
    if (!this.salary.StandardDays) this.salary.StandardDays = result.StandardDays;
    if (!this.salary.WorkedDays) this.salary.WorkedDays = result.WorkedDays;
    this.salary.GrossSalary = result.GrossSalary;
    this.salary.BhxhEmployee = result.BhxhEmployee;
    this.salary.BhytEmployee = result.BhytEmployee;
    this.salary.BhtnEmployee = result.BhtnEmployee;
    this.salary.UnionFeeEmployee = result.UnionFeeEmployee;
    this.salary.TotalInsuranceEmployee = result.TotalInsuranceEmployee;
    this.salary.PersonalDeductionAmount = result.PersonalDeductionAmount;
    this.salary.DependentDeduction = result.DependentDeduction;
    this.salary.TaxableIncome = result.TaxableIncome;
    this.salary.PersonalIncomeTax = result.PersonalIncomeTax;
    this.salary.EmployerBhxh = result.EmployerBhxh;
    this.salary.EmployerBhyt = result.EmployerBhyt;
    this.salary.EmployerBhtn = result.EmployerBhtn;
    this.salary.EmployerUnion = result.EmployerUnion;
    this.salary.TotalEmployerInsurance = result.TotalEmployerInsurance;
    this.salary.CompanyCost = result.CompanyCost;
    this.salary.NetSalary = result.NetSalary;
    this.salary.MonthlySalary = result.GrossSalary || 0;
    this.salary.TotalSalary = result.NetSalary || 0;
    this.formulaNotes = result.FormulaNotes || '';
  }

  private ensureFileReady(action: 'upload' | 'import'): boolean {
    if (!this.selectedFile) {
      const message = action === 'upload'
        ? 'Vui lòng chọn file trước khi tải lên'
        : 'Vui lòng chọn file trước khi import bảng lương';
      this.setFileError(message);
      return false;
    }

    if (!this.selectedDepartmentId) {
      this.toastr.warning('Vui lòng chọn phòng ban trước khi thao tác với file.');
      return false;
    }

    if (this.fileError) {
      this.toastr.error('File Excel chưa hợp lệ, vui lòng chọn lại');
      return false;
    }

    return true;
  }

  private setFileError(message: string) {
    this.fileError = message;
    this.toastr.warning(message);
  }

  private clearFileSelection(preserveError = false) {
    this.selectedFile = null;
    this.selectedFileName = '';
    this.previewRows = [];
    this.previewColumns = [];
    if (!preserveError) {
      this.fileError = null;
    }
    if (this.excelInput?.nativeElement) {
      this.excelInput.nativeElement.value = '';
    }
  }

  private generatePreview(file: File) {
    const reader = new FileReader();
    reader.onload = (event) => {
      try {
        const data = new Uint8Array(event.target?.result as ArrayBuffer);
        const workbook = XLSX.read(data, { type: 'array' });
        const sheet = workbook.Sheets[workbook.SheetNames[0]];
        const json: Record<string, any>[] = XLSX.utils.sheet_to_json(sheet, { defval: '' });
        this.previewRows = json.slice(0, 5);
        this.previewColumns = json.length ? Object.keys(json[0] as object) : [];

        if (!json.length) {
          this.setFileError('File Excel không có dữ liệu');
        }
      } catch {
        this.setFileError('Không thể đọc dữ liệu file, vui lòng kiểm tra lại');
      }
    };

    reader.onerror = () => {
      this.setFileError('Xảy ra lỗi khi đọc file Excel');
    };

    reader.readAsArrayBuffer(file);
  }

  private handleUploadError(error: any) {
    this.toastr.error(this.extractErrorMessage(error, 'Upload file Excel thất bại'));
  }

  private handleImportError(error: any) {
    this.toastr.error(this.extractErrorMessage(error, 'Import bảng lương thất bại'));
  }

  private extractErrorMessage(error: any, fallback: string): string {
    return error?.error?.message || error?.error?.title || error?.message || fallback;
  }

  private resetForm(): void {
    this.salary = this.emptySalary();
    this.period = this.currentPeriod();
    this.syncPeriod();
    this.formulaNotes = '';
    this.selectedDepartmentId = null;
    this.clearFileSelection();
    setTimeout(() => this.addSalaryForm.resetForm(), 0);
  }

  private emptySalary(): Salary {
    return {
      SalaryId: 0,
      EmployeeId: 0,
      EmployeeName: '',
      Date: '',
      PeriodYear: 0,
      PeriodMonth: 0,
      MonthlySalary: 0,
      BasicSalary: 0,
      Allowance: 0,
      InsuranceSalary: 0,
      StandardDays: 0,
      WorkedDays: 0,
      UnpaidLeaveDays: 0,
      OvertimeHours: 0,
      OvertimePay: 0,
      Bonus: 0,
      OtherDeductions: 0,
      DependentCount: 0,
      DeductUnionFee: false,
      GrossSalary: 0,
      NetSalary: 0,
      TotalSalary: 0,
      SalaryNotes: ''
    };
  }

  private currentPeriod(): string {
    const now = new Date();
    return `${now.getFullYear()}-${(now.getMonth() + 1).toString().padStart(2, '0')}`;
  }

  private syncPeriod(): void {
    const [year, month] = (this.period || '').split('-').map(Number);
    this.salary.PeriodYear = year || 0;
    this.salary.PeriodMonth = month || 0;
    if (year && month) {
      this.salary.Date = `${year}-${month.toString().padStart(2, '0')}-01`;
    }
  }
}
