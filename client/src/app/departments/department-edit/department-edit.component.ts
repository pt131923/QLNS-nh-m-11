import { Component, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { Department } from 'src/app/_model/department';
import { DepartmentService } from 'src/app/_services/department.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-department-edit',
  templateUrl: './department-edit.component.html',
  styleUrls: ['./department-edit.component.css']
})

export class DepartmentEditComponent {
  [x: string]: any;
  @ViewChild('editForm')
  editForm!: NgForm;
  department!: Department;

  constructor(private departService: DepartmentService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.department = this.departService.getDepartmentData();
    if (!this.department) {
      alert('No Department data found.');
    }
  }

  onRowSelected(event: any) {
    const selectedDepartment = event.data;

    // Check if selectedDepartment is an object and has an id property
    if (typeof selectedDepartment === 'object' && selectedDepartment.hasOwnProperty('id')) {
      if (selectedDepartment.id === undefined || selectedDepartment.id === null) {
        alert("Selected department does not have a valid ID.");
        return;
      }
      console.log(selectedDepartment.id);
    } else {
      alert("Selected department object is not valid.");
    }
  }

  UpdateDepartment() {
    let updatedDepartmentData = this.editForm?.value;

    let updatedDepartment: Department = {
      ...this.department,
      ...updatedDepartmentData,
      departmentId: updatedDepartmentData.departmentId,
      name: updatedDepartmentData.name,
      description: updatedDepartmentData.description,
      slNhanVien: updatedDepartmentData.slNhanVien,
      adresses: updatedDepartmentData.adresses,
      notes: updatedDepartmentData.notes
    };

    this.departService.UpdateDepartment(updatedDepartment.DepartmentId, updatedDepartment).subscribe({
      next: () => {
        this.toastr.success('Department updated successfully');
        this.editForm?.reset();
        this.router.navigate(['/departments']);
      },
      error: (err: any) => {
        console.error(err);
        this.toastr.error('Failed to update department');
      }
    });
  }
}

