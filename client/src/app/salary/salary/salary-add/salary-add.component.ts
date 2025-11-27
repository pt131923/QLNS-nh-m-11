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

  salary: Salary = {
    SalaryId: 0,
    EmployeeId: 0, // Đây sẽ là EmployeeId
    EmployeeName: '', // Frontend có thể hiển thị, nhưng backend không cần nó cho khóa ngoại
    Date: '',
    MonthlySalary: 0,
    Bonus: 0,
    TotalSalary: 0,
    SalaryNotes: ''
  };

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
      error: (err) => {
        console.error('Error fetching employees:', err);
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
        error: (err) => {
          console.error('Error fetching departments:', err);
          this.toastr.error('Không thể tải danh sách phòng ban');
        }
      });
  }

  updateTotalSalary(): void {
    const monthly = Number(this.salary.MonthlySalary) || 0;
    const bonus = Number(this.salary.Bonus) || 0;
    this.salary.TotalSalary = monthly + bonus;
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

    const monthly = Number(this.salary.MonthlySalary);
    if (Number.isNaN(monthly) || monthly <= 0) {
      this.toastr.warning('Lương cơ bản phải lớn hơn 0.');
      return;
    }

    const bonus = Number(this.salary.Bonus) || 0;
    if (bonus < 0) {
      this.toastr.warning('Bonus không được nhỏ hơn 0.');
      return;
    }

    this.updateTotalSalary();

    const payload: Salary = {
      SalaryId: 0,
      EmployeeId: this.salary.EmployeeId,
      Date: this.salary.Date,
      MonthlySalary: monthly,
      Bonus: bonus,
      TotalSalary: this.salary.TotalSalary,
      SalaryNotes: this.salary.SalaryNotes?.trim(),
      EmployeeName: this.employees.find(emp => emp.EmployeeId === this.salary.EmployeeId)?.EmployeeName
    };

    this.salaryService.AddSalary(payload).subscribe({
      next: () => {
        this.toastr.success('Thêm bảng lương thành công!');
        this.resetForm();
        this.router.navigate(['/salaries']);
      },
      error: (error) => {
        console.error('Error adding salary:', error);
        this.toastr.error(this.extractErrorMessage(error, 'Thêm bảng lương thất bại'));
      }
    });
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

  //hàm upload file excel
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
  //hàm import nhân viên từ file excel
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
          } else if (response && response.message) {
            this.toastr.success(response.message);
          } else {
            this.toastr.success('Import bảng lương thành công');
          }
          this.clearFileSelection();
        },
        error: (error: any) => this.handleImportError(error)
      });
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
      } catch (error) {
        console.error('Không thể đọc file excel:', error);
        this.setFileError('Không thể đọc dữ liệu file, vui lòng kiểm tra lại');
      }
    };

    reader.onerror = () => {
      this.setFileError('Xảy ra lỗi khi đọc file Excel');
    };

    reader.readAsArrayBuffer(file);
  }

  private handleUploadError(error: any) {
    console.error('Error uploading file:', error);
    this.toastr.error(this.extractErrorMessage(error, 'Upload file Excel thất bại'));
  }

  private handleImportError(error: any) {
    console.error('Error importing salaries:', error);
    this.toastr.error(this.extractErrorMessage(error, 'Import bảng lương thất bại'));
  }

  private extractErrorMessage(error: any, fallback: string): string {
    return error?.error?.message || error?.error?.title || error?.message || fallback;
  }

  onCancel(): void {
    this.resetForm(); // Reset form khi hủy
    this.router.navigate(['/salaries']); // Điều hướng về trang danh sách Salary
  }

  private resetForm(): void {
    this.salary = {
      SalaryId: 0,
      EmployeeId: 0,
      EmployeeName: '',
      Date: '',
      MonthlySalary: 0,
      Bonus: 0,
      TotalSalary: 0,
      SalaryNotes: '' 
    };
    this.selectedDepartmentId = null;
    this.clearFileSelection();
    setTimeout(() => this.addSalaryForm.resetForm(), 0);
  }
}
