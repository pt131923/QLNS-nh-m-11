import { Component, ViewChild, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Salary } from 'src/app/_model/salary';
import { SalaryService } from 'src/app/_services/salary.service';

@Component({
  selector: 'app-salary-edit',
  templateUrl: './salary-edit.component.html',
  styleUrls: ['./salary-edit.component.css']
})
export class SalaryEditComponent implements OnInit {
  @ViewChild('editSalaryForm') editSalaryForm!: NgForm;

  salary: Salary = {
    SalaryId: 0,
    EmployeeId: 0,
    EmployeeName: '',
    Date: '',
    MonthlySalary: 0,
    Bonus: 0,
    TotalSalary: 0,
    SalaryNotes: ''
  };
  period = '';
  previewing = false;
  formulaNotes = '';
  private previewTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private salaryService: SalaryService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.getSalary();
  }

  getSalary(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.salaryService.getSalaryById(+id).subscribe({
        next: (salaryData) => {
          this.salary = { ...this.salary, ...salaryData };
          const year = salaryData.PeriodYear || (salaryData.Date ? new Date(salaryData.Date).getFullYear() : 0);
          const month = salaryData.PeriodMonth || (salaryData.Date ? new Date(salaryData.Date).getMonth() + 1 : 0);
          if (year && month) {
            this.period = `${year}-${month.toString().padStart(2, '0')}`;
          }
          this.formulaNotes = salaryData.FormulaNotes || '';
        },
        error: () => {
          this.toastr.error('Failed to fetch salary');
        }
      });
    } else {
      this.toastr.warning('No salary ID found in the URL.');
    }
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
      BasicSalary: this.salary.BasicSalary || this.salary.MonthlySalary || null,
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
    }).subscribe({
      next: (result) => {
        this.previewing = false;
        this.salary = {
          ...this.salary,
          ...result,
          SalaryId: this.salary.SalaryId,
          EmployeeId: this.salary.EmployeeId,
          EmployeeName: this.salary.EmployeeName,
          SalaryNotes: this.salary.SalaryNotes
        };
        this.syncPeriod();
        this.formulaNotes = result.FormulaNotes || '';
      },
      error: (err) => {
        this.previewing = false;
        this.toastr.warning(err?.error?.message || 'Không tính được phiếu lương');
      }
    });
  }

  // Loại bỏ số 0 ở đầu ngay khi gõ
  removeLeadingZero(field: string) {
  const value = this.salary.OvertimeHours;
  // Bỏ nếu là chuỗi bắt đầu bằng 0 và có nhiều hơn 1 chữ số
  if (typeof value === 'string' && (value as string).startsWith('0')) {
    // Chuyển thành số tự động bỏ 0 ở đầu
    this.salary.OvertimeHours = Number(value);
  }
} 

  normalizeNumber(field: string, value: any) {
    if (value === null || value === undefined || value === '') return;
    const cleaned = String(value).replace(/^0+/, '') || '0';
    this.salary.OvertimeHours = Number(cleaned);
    this.queuePreview();
  }
  UpdateSalary(): void {
    if (this.editSalaryForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.syncPeriod();
    this.salaryService.UpdateSalary(this.salary.SalaryId, this.salary).subscribe({
      next: () => {
        this.toastr.success('Salary updated successfully');
        this.router.navigate(['/salaries']);
      },
      error: (err) => {
        this.toastr.error(err?.error?.message || 'Failed to update salary');
      }
    });
  }

  money(value?: number | null): string {
    return Number(value || 0).toLocaleString('vi-VN') + ' ₫';
  }

  cancel(): void {
    this.router.navigate(['/salaries']);
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