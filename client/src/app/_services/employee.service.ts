import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Employee } from '../_model/employee';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Department } from '../_model/department';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  [x: string]: any;
  private baseUrl = `${environment.apiUrl}/employees`;

  constructor(private http: HttpClient) {  }

  private employeeData!: Employee;

  private employeeDataSubject = new BehaviorSubject<Employee | null>(null);
  employeeData$ = this.employeeDataSubject.asObservable();

  setEmployeeData(data: Employee) {
    this.employeeData = data;
  }

  getEmployeeData(): Employee {
    return this.employeeData;
  }

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.baseUrl);
  }

  getEmployeeById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/${id}`);
  }

  AddEmployee(employee: Employee): Observable<Employee> {
    // Backend AddEmployee is mapped to POST /api/employees
    return this.http.post<Employee>(this.baseUrl, employee);
  }

  UpdateEmployee(id: number, employee: Employee): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, employee);
  }

  DeleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/delete-employee/${id}`);
  }

  uploadExcel(file: File): Observable<void> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<void>(`${this.baseUrl}/upload-excel`, formData);
  }

  importEmployees(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    // Backend import endpoint: POST /api/employees/import
    return this.http.post<any>(`${this.baseUrl}/import`, formData);
  }

  getDepartment(): Observable<Department[]> {
    // Call the dedicated Departments API instead of a non-existent Employees sub-route
    return this.http.get<Department[]>(`${environment.apiUrl}/departments`);
  }
}
