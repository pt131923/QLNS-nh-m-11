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
import { UsersAdminComponent } from './users-admin/users-admin.component';

const routes: Routes = [
  // 🔄 Public routes — không cần đăng nhập
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: '', redirectTo: '/login', pathMatch: 'full' },

  // 🔄 Protected routes — cần đăng nhập (AuthGuard áp dụng cho tất cả con)
  {
    path: '',
    canActivate: [AuthGuard],
    children: [
      // Dashboard
      { path: 'dashboard', component: DashboardComponent },
      { path: 'overview', component: DashboardComponent },

      // 👥 Employees
      { path: 'employees', component: EmployeeListComponent },
      { path: 'employee-add', component: EmployeeAddComponent },
      { path: 'employee-edit/:id', component: EmployeeEditComponent },
      { path: 'employee/:id', component: EmployeeDetailComponent },

      // 🏢 Departments
      { path: 'departments', component: DepartmentListComponent },
      { path: 'department-add', component: DepartmentAddComponent },
      { path: 'department-edit/:id', component: DepartmentEditComponent },
      { path: 'department/:id', component: DepartmentDetailComponent },

      // 📝 Contracts
      { path: 'contracts', component: ContractListComponent },
      { path: 'contract-add', component: ContractAddComponent },
      { path: 'contract-edit/:id', component: ContractEditComponent },
      { path: 'contract/:id', component: ContractDetailComponent },

      // 💰 Salary
      { path: 'salaries', component: SalaryListComponent },
      { path: 'salary-add', component: SalaryAddComponent },
      { path: 'salary-edit/:id', component: SalaryEditComponent },

      // 📋 Other features
      { path: 'settings', component: SettingComponent },
      { path: 'contact', component: ContactComponent },
      { path: 'help', component: HelpComponent },
      { path: 'contact-history', component: ContactHistoryComponent },
      { path: 'timekeeping', component: TimekeepingComponent },
      { path: 'trainings', component: TrainingComponent },
      { path: 'recruitments', component: RecruitmentComponent },
      { path: 'benefits', component: BenefitsComponent },
      { path: 'leaves', component: LeaveComponent },
      { path: 'users', component: UsersAdminComponent },
      { path: 'users-admin', component: UsersAdminComponent },
    ]
  },

  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
