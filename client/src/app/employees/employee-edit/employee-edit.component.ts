import { Component, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Department } from 'src/app/_model/department';
import { Employee } from 'src/app/_model/employee';
import { DepartmentService } from 'src/app/_services/department.service';
import { EmployeeService } from 'src/app/_services/employee.service';

@Component({
  selector: 'app-employee-edit',
  templateUrl: './employee-edit.component.html',
  styleUrls: ['./employee-edit.component.css']
})
export class EmployeeEditComponent {
  @ViewChild('editForm') editForm: NgForm | undefined;
  employee: Employee = {
    EmployeeId: 0,
    EmployeeName: '',
    EmployeeEmail: '',
    EmployeePhone: '',
    EmployeeAddress: '',
    EmployeeInformation: '',
    DepartmentId: 0,
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
  selectedDepartmentId!: number;

  constructor(
    private empService: EmployeeService,
    private toastr: ToastrService,
    private router: Router,
    private departService: DepartmentService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.getDepartments();
    this.getEmployee(); // Lấy thông tin nhân viên
  }

  getDepartments() {
    this.departService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
      },
      error: (err) => {
        console.error('Error fetching departments:', err);
        this.toastr.error('Failed to fetch departments');
      }
    });
  }

  getEmployee() {
    const id = this.route.snapshot.paramMap.get('id'); // Lấy ID từ URL
    if (id) {
      this.empService.getEmployeeById(+id).subscribe({
        next: (employee: Employee) => {
          this.employee = employee;
          this.selectedDepartmentId = employee.DepartmentId; // Gán ID phòng ban đã chọn
        },
        error: (err: any) => {
          console.error('Error fetching employee:', err);
          this.toastr.error('Failed to fetch employee');
        }
      });
    } else {
      alert('No employee ID found in the URL.');
    }
  }

  UpdateEmployee() {
    if (this.editForm?.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    const updatedEmployeeData = this.editForm?.value;

    const updatedEmployee: Employee = {
      ...this.employee,
      ...updatedEmployeeData,
      departmentId: +this.selectedDepartmentId
    };

    this.empService.UpdateEmployee(updatedEmployee.EmployeeId, updatedEmployee).subscribe({
      next: () => {
        this.toastr.success('Employee updated successfully');
        this.editForm?.reset(this.employee); // Reset form về trạng thái ban đầu
        this.router.navigate(['/employees']);
      },
      error: (err) => {
        console.error('Error updating employee:', err);
        this.toastr.error('Failed to update employee');
      }
    });
  }

  Cancel() {
    this.router.navigate(['/employee']);
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.toastr.clear();
    }, 500);
  }
}
