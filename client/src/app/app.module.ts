import {DepartmentListComponent} from './departments/department-list/department-list.component';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule} from '@angular/platform-browser/animations';
import { NavComponent } from './nav/nav.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { EmployeeListComponent } from './employees/employee-list/employee-list.component';
import { AgGridAngular } from 'ag-grid-angular';
import { EmployeeAddComponent } from './employees/employee-add/employee-add.component';
import { SharedModule } from './_modules/shared.module';
import { ToastrModule } from 'ngx-toastr';
import { EmployeeEditComponent } from './employees/employee-edit/employee-edit.component';
import { DepartmentAddComponent } from './departments/department-add/department-add.component';
import { DepartmentEditComponent } from './departments/department-edit/department-edit.component';
import { ContractAddComponent } from './contract/contract-add/contract-add.component';
import { ContractEditComponent } from './contract/contract-edit/contract-edit.component';
import { ContractListComponent } from './contract/contract-list/contract-list.component';
import { SalaryAddComponent } from './salary/salary/salary-add/salary-add.component';
import { SalaryEditComponent } from './salary/salary/salary-edit/salary-edit.component';
import { SalaryListComponent } from './salary/salary/salary-list/salary-list.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { UserModule } from './User/user/user.module';
import { EmployeeDetailComponent } from './employees/employee-detail/employee-detail.component';
import { SettingComponent } from './setting/setting.component';
import { HelpComponent } from './help/help.component';
import { ContactComponent } from './contact/contact.component';
import { DepartmentDetailComponent } from './departments/department-detail/department-detail.component';
import { ContractDetailComponent } from './contract/contract-detail/contract-detail.component';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { HttpClient } from '@angular/common/http';
import { DashboardComponent } from './dashboard/dashboard.component';
import { BenefitsComponent } from './benefits/benefits.component';
import { LeaveComponent } from './leave/leave.component';
import { TrainingComponent } from './training/training.component';
import { PerformanceComponent } from './performance/performance.component';
import { TimekeepingComponent } from './timekeeping/timekeeping.component';
import { RecruitmentComponent } from './recruitment/recruitment.component';
import { ContactHistoryComponent } from './contact-history/contact-history.component';


export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http);
}

@NgModule({
  declarations: [
    AppComponent,
    NavComponent,
    EmployeeListComponent,
    DepartmentListComponent,
    EmployeeAddComponent,
    EmployeeEditComponent,
    DepartmentAddComponent,
    DepartmentEditComponent,
    ContractAddComponent,
    ContractEditComponent,
    ContractListComponent,
    SalaryAddComponent,
    SalaryEditComponent,
    SalaryListComponent,
    LoginComponent,
    RegisterComponent,
    EmployeeDetailComponent,
    SettingComponent,
    HelpComponent,
    ContactComponent,
    DepartmentDetailComponent,
    ContractDetailComponent,
    DashboardComponent,
    BenefitsComponent,
    LeaveComponent,
    TrainingComponent,
    PerformanceComponent,
    TimekeepingComponent,
    RecruitmentComponent,
    ContactHistoryComponent,
  ],
  imports: [
    UserModule,
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    FormsModule,
    AgGridAngular,
    ReactiveFormsModule,
    SharedModule,
    ToastrModule.forRoot(),
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    }),
  ],
  providers: [],
  bootstrap: [AppComponent],
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class AppModule { }
