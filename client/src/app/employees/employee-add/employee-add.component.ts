import { EmployeeService } from './../../_services/employee.service';
import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { Employee } from 'src/app/_model/employee';
import { Router } from '@angular/router';
import { Department } from 'src/app/_model/department';
import { DepartmentService } from 'src/app/_services/department.service';

@Component({
  selector: 'app-employee-add',
  templateUrl: './employee-add.component.html',
  styleUrls: ['./employee-add.component.css']
})
export class EmployeeAddComponent implements OnInit {
  @ViewChild('addForm') addForm!: NgForm;

  employee: Employee = {
    EmployeeId: 0,
    EmployeeName: '',
    DepartmentId: 0
  };

  departments: Department[] = [];

  constructor(
    private empService: EmployeeService,
    private toastr: ToastrService,
    private router: Router,
    private departService: DepartmentService
  ) {}

  ngOnInit(): void {
    this.getDepartments();
  }

  AddEmployee() {
    if (this.addForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    let employeeData: Employee = {
      EmployeeId: 0,
      EmployeeName: this.addForm.value.EmployeeName,
      DepartmentId: this.addForm.value.DepartmentId
    };


    this.empService.AddEmployee(employeeData).subscribe({
      next: (response) => {
        this.toastr.success('Employee added successfully');
        this.addForm.reset(employeeData); // Reset form về trạng thái ban đầu
        this.router.navigate(['/employees']);
      },
      error: (err) => {
        console.log('Sending data:', this.employee);
        console.error('Error adding employee:', err);
        this.toastr.error('Failed to add employee');
        this.addForm.reset();
      }
    });
  }

  getDepartments() {
    this.departService.getDepartments().subscribe({
      next: (departments) => {
        console.log('Departments:', departments); // Kiểm tra dữ liệu trả về
        this.departments = departments;
      },
      error: (err) => {
        console.error('Error fetching departments:', err);
        this.toastr.error('Failed to fetch departments');
      }
    });
  }
}
