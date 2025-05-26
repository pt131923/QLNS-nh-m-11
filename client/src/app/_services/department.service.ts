import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Department} from '../_model/department';
import { map, Observable } from 'rxjs';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})

export class DepartmentService {
  [x: string]: any;
  departments: Department[] = [];
  baseUrl1 = environment.apiUrl1;

  private departmentData!: Department;

  constructor(private http: HttpClient) {
    this.getDepartments().subscribe(data => {
      this.departments = data;
    });
  }

  setDepartmentData(data: Department) {
    this.departmentData = data;
  }

  getDepartmentData(): Department  {
    return this.departmentData;
  }

  getDepartments() {
    return this.http.get<Department[]>(this.baseUrl1);
  }

  getDepartment(id: number){
    return this.http.get<Department>(this.baseUrl1 + id);
  }

  AddDepartment(department: Department): Observable<Department> {
      // ✅ Gọi đúng endpoint add-department
      return this.http.post<Department>(`${this.baseUrl1}/add-department`, department);
    }

  UpdateDepartment(id: number, department: Department): Observable<void> {
      return this.http.put<void>(`${this.baseUrl1}/${id}`, department);
    }

    DeleteDepartment(id: number): Observable<void> {
      return this.http.delete<void>(`${this.baseUrl1}/delete-department/${id}`);
    }

  SearchDepartment(departName: string): Observable<Department[]> {
    return this.http.get<Department[]>(this.baseUrl1 + '?departName=' + departName).pipe(
      map((response: Department[]) => {
        return response;
      })
    );
  }
}
