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
          this.salary = salaryData;
        },
        error: (err) => {
          console.error('Error fetching salary:', err);
          this.toastr.error('Failed to fetch salary');
        }
      });
    } else {
      this.toastr.warning('No salary ID found in the URL.');
    }
  }

  updateTotalSalary(): void {
    this.salary.TotalSalary = this.salary.MonthlySalary + this.salary.Bonus;
  }

  UpdateSalary(): void {
    if (this.editSalaryForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.salaryService.UpdateSalary(this.salary.SalaryId, this.salary).subscribe({
      next: () => {
        this.toastr.success('Salary updated successfully');
        this.router.navigate(['/salaries']);
      },
      error: (err) => {
        console.error('Error updating salary:', err);
        this.toastr.error('Failed to update salary');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/salaries']);
  }
}

