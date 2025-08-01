import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Department } from 'src/app/_model/department';
import { DepartmentService } from 'src/app/_services/department.service';

@Component({
  selector: 'app-department-add',
  templateUrl: './department-add.component.html',
  styleUrls: ['./department-add.component.css']
})
export class DepartmentAddComponent implements OnInit {
  @ViewChild('addForm2') addForm2!: NgForm;

  department: Department = {
    DepartmentId: 0,
    Name: '',
    SlNhanVien: 0,
    Description: '',
    Addresses: '',
    Notes: '',
    DepartmentImage: null
  };

  departments: Department[] = [];

  constructor(
    private departmentService: DepartmentService,
    private toastr: ToastrService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.getDepartments();
  }

  AddDepartment(): void {
    if (this.addForm2.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    // Sử dụng giá trị từ model thay vì form.value
    let newDepartment: Department = {
      DepartmentId: 0,
      Name: this.department.Name,
      SlNhanVien: this.department.SlNhanVien || 0, // Đảm bảo luôn có giá trị số
      Description: this.department.Description || '',
      Addresses: this.department.Addresses || '',
      Notes: this.department.Notes || '',
      DepartmentImage: this.department.DepartmentImage || null
    };

    this.departmentService.AddDepartment(newDepartment).subscribe({
      next: (response) => {
        this.toastr.success('Department added successfully!');
        this.resetForm();
        this.router.navigate(['/departments']);
      },
      error: (error) => {
        console.error('Error adding department:', error);
        this.toastr.error('Failed to add department!');
      }
    });
  }

  onCancel(): void {
    this.resetForm();
    this.router.navigate(['/departments']);
  }

  private resetForm(): void {
    this.department = {
      DepartmentId: 0,
      Name: '',
      SlNhanVien: 0,
      Description: '',
      Addresses: '',
      Notes: '',
      DepartmentImage: null
    };
    this.addForm2.resetForm();
  }

  getDepartments(): void {
    this.departmentService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
      },
      error: (err) => {
        console.error('Error fetching departments:', err);
        this.toastr.error('Failed to fetch departments');
      }
    });
  }
}
