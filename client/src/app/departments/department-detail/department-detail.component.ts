import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Department } from 'src/app/_model/department';
import { DepartmentService } from 'src/app/_services/department.service';

@Component({
  selector: 'app-department-detail',
  templateUrl: './department-detail.component.html',
  styleUrls: ['./department-detail.component.css']
})
export class DepartmentDetailComponent implements OnInit {
  department!: Department;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private departmentService: DepartmentService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.departmentService.getDepartmentById(+id).subscribe({
        next: (data) => {
          this.department = data;
        },
        error: () => {
          console.error('Unable to load department data');
        }
      });
    }
  }

  getDepartmentId(departmentId: number): void {
  this.departmentService.getDepartmentById(departmentId).subscribe({
    next: (dep: Department) => {
      this.department = dep;
    },
    error: () => {
      this.department = {
        DepartmentId: 0,
        Name: 'Unknown',
        DepartmentImage: '',
        Description: '',
        SlNhanVien: 0,
        Addresses: '',
        Notes: ''
      };
    }
  });
  }


  goToDepartmentDetail(id: number): void {
  this.router.navigate(['/department', id]);
}
}

