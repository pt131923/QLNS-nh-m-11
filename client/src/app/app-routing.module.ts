import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { EmployeeListComponent } from './employees/employee-list/employee-list.component';
import { EmployeeAddComponent } from './employees/employee-add/employee-add.component';
import { EmployeeEditComponent } from './employees/employee-edit/employee-edit.component';

import { DepartmentListComponent } from './departments/department-list/department-list.component';
import { DepartmentAddComponent } from './departments/department-add/department-add.component';
import { DepartmentEditComponent } from './departments/department-edit/department-edit.component';

import { ContractListComponent } from './contract/contract-list/contract-list.component';
import { ContractAddComponent } from './contract/contract-add/contract-add.component';
import { ContractEditComponent } from './contract/contract-edit/contract-edit.component';

import { SalaryListComponent } from './salary/salary/salary-list/salary-list.component';
import { SalaryAddComponent } from './salary/salary/salary-add/salary-add.component';
import { SalaryEditComponent } from './salary/salary/salary-edit/salary-edit.component';

const routes: Routes = [
  { path: '', redirectTo: '/employees', pathMatch: 'full' },

  // Employee routes
  { path: 'employees', component: EmployeeListComponent },
  { path: 'employee-add', component: EmployeeAddComponent },
  { path: 'employee-edit/:id', component: EmployeeEditComponent },

  // Department routes
  { path: 'departments', component: DepartmentListComponent },
  { path: 'department-add', component: DepartmentAddComponent },
  { path: 'department-edit/:id', component: DepartmentEditComponent },

  // Contract routes
  { path: 'contracts', component: ContractListComponent },
  { path: 'contract-add', component: ContractAddComponent },
  { path: 'contract-edit/:id', component: ContractEditComponent },

  // Salary routes (đã sửa tên thư mục đúng để tránh lặp)
  { path: 'salaries', component: SalaryListComponent },
  { path: 'salary-add', component: SalaryAddComponent },
  { path: 'salary-edit/:id', component: SalaryEditComponent },

  { path: '**', redirectTo: '/contracts' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

