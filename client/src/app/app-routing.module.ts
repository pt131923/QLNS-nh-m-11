import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from './_guards/auth.guard';

import { DashboardComponent } from './dashboard/dashboard.component';
import { EmployeeListComponent } from './employees/employee-list/employee-list.component';
import { EmployeeAddComponent } from './employees/employee-add/employee-add.component';
import { EmployeeEditComponent } from './employees/employee-edit/employee-edit.component';
import { EmployeeDetailComponent } from './employees/employee-detail/employee-detail.component';

import { DepartmentListComponent } from './departments/department-list/department-list.component';
import { DepartmentAddComponent } from './departments/department-add/department-add.component';
import { DepartmentEditComponent } from './departments/department-edit/department-edit.component';
import { DepartmentDetailComponent } from './departments/department-detail/department-detail.component';

import { ContractListComponent } from './contract/contract-list/contract-list.component';
import { ContractAddComponent } from './contract/contract-add/contract-add.component';
import { ContractEditComponent } from './contract/contract-edit/contract-edit.component';
import { ContractDetailComponent } from './contract/contract-detail/contract-detail.component';

import { SalaryListComponent } from './salary/salary/salary-list/salary-list.component';
import { SalaryAddComponent } from './salary/salary/salary-add/salary-add.component';
import { SalaryEditComponent } from './salary/salary/salary-edit/salary-edit.component';

import { ContactComponent } from './contact/contact.component';
import { HelpComponent } from './help/help.component';
import { SettingComponent } from './setting/setting.component';

import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';

import { TimekeepingComponent } from './timekeeping/timekeeping.component';
import { TrainingComponent } from './training/training.component';
import { RecruitmentComponent } from './recruitment/recruitment.component';
import { BenefitsComponent } from './benefits/benefits.component';
import { LeaveComponent } from './leave/leave.component';
import { ContactHistoryComponent } from './history/contact-history/contact-history.component';

const routes: Routes = [

  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  // Dashboard
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },

  // Employee
  { path: 'employees', component: EmployeeListComponent, canActivate: [AuthGuard] },
  { path: 'employee-add', component: EmployeeAddComponent, canActivate: [AuthGuard] },
  { path: 'employee-edit/:id', component: EmployeeEditComponent, canActivate: [AuthGuard] },
  { path: 'employee/:id', component: EmployeeDetailComponent, canActivate: [AuthGuard] },

  // Department
  { path: 'departments', component: DepartmentListComponent, canActivate: [AuthGuard] },
  { path: 'department-add', component: DepartmentAddComponent, canActivate: [AuthGuard] },
  { path: 'department-edit/:id', component: DepartmentEditComponent, canActivate: [AuthGuard] },
  { path: 'department/:id', component: DepartmentDetailComponent, canActivate: [AuthGuard] },

  // Contract
  { path: 'contracts', component: ContractListComponent, canActivate: [AuthGuard] },
  { path: 'contract-add', component: ContractAddComponent, canActivate: [AuthGuard] },
  { path: 'contract-edit/:id', component: ContractEditComponent, canActivate: [AuthGuard] },
  { path: 'contract/:id', component: ContractDetailComponent, canActivate: [AuthGuard] },

  // Salary
  { path: 'salaries', component: SalaryListComponent, canActivate: [AuthGuard] },
  { path: 'salary-add', component: SalaryAddComponent, canActivate: [AuthGuard] },
  { path: 'salary-edit/:id', component: SalaryEditComponent, canActivate: [AuthGuard] },

  // User lazy module (cần guard)
  {
    path: 'user',
    canActivate: [AuthGuard],
    loadChildren: () => import('./User/user/user.module').then(m => m.UserModule)
  },

  // Other features
  { path: 'settings', component: SettingComponent, canActivate: [AuthGuard] },
  { path: 'contact', component: ContactComponent, canActivate: [AuthGuard] },
  { path: 'help', component: HelpComponent, canActivate: [AuthGuard] },
  { path: 'contact-history', component: ContactHistoryComponent, canActivate: [AuthGuard] },

  { path: 'timekeeping', component: TimekeepingComponent, canActivate: [AuthGuard] },
  { path: 'trainings', component: TrainingComponent, canActivate: [AuthGuard] },
  { path: 'recruitments', component: RecruitmentComponent, canActivate: [AuthGuard] },
  { path: 'benefits', component: BenefitsComponent, canActivate: [AuthGuard] },
  { path: 'leaves', component: LeaveComponent, canActivate: [AuthGuard] },

  // Login & Register (không cần guard)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  // Not found
  { path: '**', redirectTo: 'dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
