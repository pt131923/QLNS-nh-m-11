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
    SalaryListComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    FormsModule,
    AgGridAngular,
    ReactiveFormsModule,
    SharedModule,
    ToastrModule.forRoot()
  ],
  providers: [],
  bootstrap: [AppComponent],
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class AppModule { }
