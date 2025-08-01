import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Employee } from '../_model/employee';
import { Observable, BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private baseUrl = 'http://localhost:5002/api/employees'; // Địa chỉ API của bạn

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
    return this.http.post<Employee>(`${this.baseUrl}/add-employee`, employee);
  }

  UpdateEmployee(id: number, employee: Employee): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, employee);
  }

  DeleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/delete-employee/${id}`);
  }
}
