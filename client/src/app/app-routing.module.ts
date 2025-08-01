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
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { AuthGuard } from './_guards/auth.guard';
import { EmployeeDetailComponent } from './employees/employee-detail/employee-detail.component';
import { DepartmentDetailComponent } from './departments/department-detail/department-detail.component';
import { SettingComponent } from './setting/setting.component';
import { ContactComponent } from './contact/contact.component';
import { HelpComponent } from './help/help.component';
import { ContractDetailComponent } from './contract/contract-detail/contract-detail.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { TimekeepingComponent } from './timekeeping/timekeeping.component';
import { TrainingComponent } from './training/training.component';
import { PerformanceComponent } from './performance/performance.component';
import { RecruitmentComponent } from './recruitment/recruitment.component';
import { BenefitsComponent } from './benefits/benefits.component';
import { LeaveComponent } from './leave/leave.component';

const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },

  // Employee routes
  { path: 'employee', component: EmployeeListComponent, canActivate: [AuthGuard] },
  { path: 'employee-add', component: EmployeeAddComponent, canActivate: [AuthGuard] },
  { path: 'employee-edit/:id', component: EmployeeEditComponent, canActivate: [AuthGuard] },
  { path: 'employee/:id', component: EmployeeDetailComponent, canActivate: [AuthGuard] },

  // Department routes
  { path: 'departments', component: DepartmentListComponent, canActivate: [AuthGuard] },
  { path: 'department-add', component: DepartmentAddComponent, canActivate: [AuthGuard] },
  { path: 'department/:id', component: DepartmentDetailComponent, canActivate: [AuthGuard] },
  { path: 'department-edit/:id', component: DepartmentEditComponent, canActivate: [AuthGuard] },

  // Contract routes
  { path: 'contracts', component: ContractListComponent, canActivate: [AuthGuard] },
  { path: 'contract-add', component: ContractAddComponent, canActivate: [AuthGuard] },
  { path: 'contract/:id', component: ContractDetailComponent, canActivate: [AuthGuard] },
  { path: 'contract-edit/:id', component: ContractEditComponent, canActivate: [AuthGuard] },

  // Salary routes (đã sửa tên thư mục đúng để tránh lặp)
  { path: 'salaries', component: SalaryListComponent, canActivate: [AuthGuard] },
  { path: 'salary-add', component: SalaryAddComponent, canActivate: [AuthGuard] },
  { path: 'salary-edit/:id', component: SalaryEditComponent, canActivate: [AuthGuard] },

  // Login route
  { path: 'login', component: LoginComponent },
  // Register route (nếu cần, có thể thêm vào)
  { path: 'register', component: RegisterComponent },
  { path: 'timekeeping', component: TimekeepingComponent, canActivate: [AuthGuard] },
  { path: 'training', component: TrainingComponent, canActivate: [AuthGuard] },
  { path: 'performance', component: PerformanceComponent, canActivate: [AuthGuard] },
  { path: 'recruitment', component: RecruitmentComponent, canActivate: [AuthGuard] },
  { path: 'benefits', component: BenefitsComponent, canActivate: [AuthGuard] },
  { path: 'leave', component: LeaveComponent, canActivate: [AuthGuard] },

  // User routes
  {
    path: 'user',
    loadChildren: () => import('./User/user/user.module').then(m => m.UserModule)
  },

  { path: 'settings', component: SettingComponent },
  { path: 'contact', component: ContactComponent },
  { path: 'help', component: HelpComponent },

  { path: '**', redirectTo: 'dashboard', pathMatch: 'full' }

];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

