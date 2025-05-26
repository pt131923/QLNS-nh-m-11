import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Salary } from 'src/app/_model/salary';
import { SalaryService } from 'src/app/_services/salary.service';
import { Employee } from 'src/app/_model/employee';
import { EmployeeService } from 'src/app/_services/employee.service';

@Component({
  selector: 'app-salary-add',
  templateUrl: './salary-add.component.html',
  styleUrls: ['./salary-add.component.css']
})
export class SalaryAddComponent implements OnInit {
  @ViewChild('addSalaryForm') addSalaryForm!: NgForm;

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

  employees: Employee[] = []; // Danh sách nhân viên để đổ vào dropdown

  constructor(
    private salaryService: SalaryService,
    private employeeService: EmployeeService, // Dịch vụ để lấy danh sách nhân viên
    private toastr: ToastrService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.getEmployees(); // Load danh sách nhân viên khi component khởi tạo
  }

  getEmployees(): void {
    this.employeeService.getEmployees().subscribe({
      next: (employees) => {
        this.employees = employees;
      },
      error: (err) => {
        console.error('Error fetching employees:', err);
        this.toastr.error('Failed to fetch employees');
      }
    });
  }

  updateTotalSalary(): void {
    this.salary.TotalSalary = (this.salary.MonthlySalary || 0) + (this.salary.Bonus || 0);
  }

  AddSalary(): void {
    // Kiểm tra tính hợp lệ của form trước khi gửi
    if (this.addSalaryForm.invalid) {
      this.toastr.error('Please fill in all required fields correctly.');
      this.addSalaryForm.form.markAllAsTouched();
      return;
    }

    console.log(this.salary);

    let salaryToSend: Salary = {
      SalaryId: 0,
      EmployeeId: this.salary.EmployeeId,
      Date: this.salary.Date,
      MonthlySalary: this.salary.MonthlySalary,
      Bonus: this.salary.Bonus,
      TotalSalary: this.salary.TotalSalary,
      SalaryNotes: this.salary.SalaryNotes,
      EmployeeName: this.employees.find(emp => emp.EmployeeId === this.salary.EmployeeId)?.EmployeeName || ''
    };


    this.salaryService.AddSalary(salaryToSend).subscribe({
      next: (response) => {
        this.toastr.success('Salary added successfully!');
        this.resetForm();
        this.router.navigate(['/salaries']);
      },
      error: (error) => {
        console.error('Error adding salary:', error);
        // Hiển thị lỗi chi tiết hơn nếu có thể
        if (error.error && typeof error.error === 'string') {
          this.toastr.error(error.error); // Hiển thị thông báo lỗi từ backend
        } else {
          this.toastr.error('Failed to add salary! Please check the console for details.');
        }
      }
    });
  }

  onCancel(): void {
    this.resetForm(); // Reset form khi hủy
    this.router.navigate(['/salaries']); // Điều hướng về trang danh sách Salary
  }

  private resetForm(): void {
    this.salary = {
      SalaryId: 0,
      EmployeeId: 0, // Reset EmployeeId về 0 hoặc null
      EmployeeName: '',
      Date: '',
      MonthlySalary: 0,
      Bonus: 0,
      TotalSalary: 0,
      SalaryNotes: ''
    };
    setTimeout(() => this.addSalaryForm.resetForm(), 0);
  }
}
