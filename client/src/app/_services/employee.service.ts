import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Employee } from '../_model/employee';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Department } from '../_model/department';

export interface EmployeeFilter {
  name?: string;
  departmentId?: number;
  gender?: string;
  educationLevel?: string;
  contractStatus?: string;
}

export interface BatchDepartmentUpdate {
  employeeIds: number[];
  newDepartmentId: number;
}

export interface EmployeeStats {
  total: number;
  byDepartment: { departmentId: number; departmentName: string; count: number }[];
  byGender: { gender: string; count: number }[];
  byAgeGroup: { ageGroup: string; count: number }[];
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private baseUrl = `${environment.apiUrl}/employees`;

  constructor(private http: HttpClient) {}

  private employeeData!: Employee;
  private employeeDataSubject = new BehaviorSubject<Employee | null>(null);
  employeeData$ = this.employeeDataSubject.asObservable();

  setEmployeeData(data: Employee) { this.employeeData = data; }
  getEmployeeData(): Employee { return this.employeeData; }

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.baseUrl);
  }

  getEmployeeById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/${id}`);
  }

  searchEmployees(filter: EmployeeFilter): Observable<Employee[]> {
    let params = new HttpParams();
    if (filter.name) params = params.set('name', filter.name);
    if (filter.departmentId != null) params = params.set('departmentId', filter.departmentId.toString());
    if (filter.gender) params = params.set('gender', filter.gender);
    if (filter.educationLevel) params = params.set('educationLevel', filter.educationLevel);
    if (filter.contractStatus) params = params.set('contractStatus', filter.contractStatus);
    return this.http.get<Employee[]>(`${this.baseUrl}/search`, { params });
  }

  AddEmployee(employee: Employee): Observable<Employee> {
    return this.http.post<Employee>(this.baseUrl, employee);
  }

  UpdateEmployee(id: number, employee: Employee): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, employee);
  }

  DeleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  batchUpdateDepartment(payload: BatchDepartmentUpdate): Observable<{ updated: number }> {
    return this.http.post<{ updated: number }>(`${this.baseUrl}/batch-update-department`, payload);
  }

  getStatistics(): Observable<EmployeeStats> {
    return this.http.get<EmployeeStats>(`${this.baseUrl}/statistics`);
  }

  exportExcel(filter: EmployeeFilter = {}): Observable<Blob> {
    let params = new HttpParams();
    if (filter.name) params = params.set('name', filter.name);
    if (filter.departmentId != null) params = params.set('departmentId', filter.departmentId.toString());
    if (filter.gender) params = params.set('gender', filter.gender);
    if (filter.educationLevel) params = params.set('educationLevel', filter.educationLevel);
    return this.http.get(`${this.baseUrl}/export-excel`, { params, responseType: 'blob' });
  }

  uploadExcel(file: File): Observable<void> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<void>(`${this.baseUrl}/upload-excel`, formData);
  }

  importEmployees(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.baseUrl}/import`, formData);
  }

  getDepartment(): Observable<Department[]> {
    return this.http.get<Department[]>(`${environment.apiUrl}/departments`);
  }
}
