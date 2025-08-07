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
      DepartmentId: this.addForm.value.DepartmentId,
      EmployeeEmail: this.addForm.value.EmployeeEmail,
      EmployeePhone: this.addForm.value.EmployeePhone,
      EmployeeAddress: this.addForm.value.EmployeeAddress,
      EmployeeInformation: this.addForm.value.EmployeeInformation,
      BirthDate:  this.addForm.value.BirthDate ? new Date(this.addForm.value.BirthDate) : undefined,
      PlaceOfBirth:  this.addForm.value.PlaceOfBirth,
      Gender: this.addForm.value.Gender,
      MaritalStatus: this.addForm.value.MaritalStatus,
      IdentityNumber: this.addForm.value.IdentityNumber,
      IdentityIssuedDate: this.addForm.value.IdentityIssuedDate ? new Date(this.addForm.value.IdentityIssuedDate) : undefined,
      IdentityIssuedPlace: this.addForm.value.IdentityIssuedPlace,
      Religion: this.addForm.value.Religion,
      Ethnicity: this.addForm.value.Ethnicity,
      Nationality: this.addForm.value.Nationality,
      EducationLevel: this.addForm.value.EducationLevel,
      Specialization: this.addForm.value.Specialization
    };

    this.empService.AddEmployee(employeeData).subscribe({
      next: (response) => {
        this.toastr.success('Employee added successfully');
        this.addForm.reset(employeeData); // Reset form về trạng thái ban đầu
        this.router.navigate(['/employee']);
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

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.toastr.clear();
    }, 500); // Xóa thông báo sau 500 ms
  }
}
