import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { EmployeeService } from 'src/app/_services/employee.service';
import { DepartmentService } from 'src/app/_services/department.service';
import { Employee } from 'src/app/_model/employee';

@Component({
  selector: 'app-employee-detail',
  templateUrl: './employee-detail.component.html',
  styleUrls: ['./employee-detail.component.css']
})
export class EmployeeDetailComponent implements OnInit {
  employee: any;
  departmentName: string = 'Không xác định';
  editForm: any;
  toastr: any;
  selectedDepartmentId: any;
  empService: any;
  router: any;

  constructor(
    private route: ActivatedRoute,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.employeeService.getEmployeeById(+id).subscribe({
        next: (data) => {
          this.employee = data;

          // Lấy tên phòng ban nếu có DepartmentId
          if (this.employee?.DepartmentId) {
            this.getDepartmentName(this.employee.DepartmentId);
          }
        },
        error: () => {
          console.error('Unable to load employee data');
        }
      });
    }
  }

  getDepartmentName(departmentId: number): void {
    this.departmentService.getDepartmentById(departmentId).subscribe({
      next: (dep: { Name: string }) => {
        this.departmentName = dep?.Name || 'Unknown';
      },
      error: () => {
        this.departmentName = 'Unknown';
      }
    });
  }

  goBack(): void {
    window.history.back();
  }
}
