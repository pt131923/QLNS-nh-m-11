import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Salary } from '../_model/salary';
import { environment } from 'environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SalaryService {
  private baseUrl = environment.apiUrl4; // Đổi theo API thực tế

  constructor(private http: HttpClient) {}

  getSalaries(): Observable<Salary[]> {
    return this.http.get<Salary[]>(this.baseUrl);
  }

  getSalariesWithEmployees(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/salaries-with-employees`);
  }

  searchSalaries(employeeName?: string, id?: number): Observable<Salary[]> {
    const params = new URLSearchParams();
    if (employeeName) params.append('employeeName', employeeName);
    if (id !== undefined) params.append('id', id.toString());

    return this.http.get<Salary[]>(`${this.baseUrl}/search?${params.toString()}`);
  }

  AddSalary(salary: Salary): Observable<Salary> {
    return this.http.post<Salary>(`${this.baseUrl}/add-salary`, salary);
  }

  UpdateSalary(id: number, salary: Salary): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, salary);
  }

  DeleteSalary(id: number): Observable<string> {
    return this.http.delete<string>(`${this.baseUrl}/delete-salary/${id}`, {
      responseType: 'text' as 'json'
    });
  }

  getSalaryById(id: number): Observable<Salary> {
    return this.http.get<Salary>(`${this.baseUrl}/${id}`);
  }

  uploadExcel(file: File, departmentId?: number): Observable<void> {
    const formData = new FormData();
    formData.append('file', file);
    if (departmentId) {
      formData.append('departmentId', departmentId.toString());
    }
    return this.http.post<void>(`${this.baseUrl}/upload-excel`, formData);
  }

  importSalaries(file: File, departmentId?: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    if (departmentId) {
      formData.append('departmentId', departmentId.toString());
    }
    return this.http.post<any>(`${this.baseUrl}/import-salaries`, formData);
  }
}
