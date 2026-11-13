import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Department} from '../_model/department';
import { map, Observable } from 'rxjs';
import { Injectable } from '@angular/core';

// Thêm cái này vào, ở đâu cũng được miễn là bên ngoài class
export interface DepartmentFilter {
  name?: string;
  description?: string;
  addresses?: string;
  slNhanVien?: number;
  notes?: string;
}

@Injectable({
  providedIn: 'root'
})

export class DepartmentService {
  [x: string]: any;
  departments: Department[] = [];
  baseUrl1 = `${environment.apiUrl}/departments`;

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

  getDepartmentById(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.baseUrl1}/${id}`);
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

    searchDepartments(filter: DepartmentFilter): Observable<Department[]> {
      let params = new HttpParams();
      if (filter.name) params = params.set('name', filter.name);
      if (filter.description) params = params.set('description', filter.description);
      if (filter.addresses) params = params.set('diaDiem', filter.addresses);
      if (filter.notes) params = params.set('notes', filter.notes);
      return this.http.get<Department[]>(`${this.baseUrl1}/search`, { params });
    }

  exportExcel(filter: DepartmentFilter = {}): Observable<Blob> {
    let params = new HttpParams();
    if (filter.name) params = params.set('name', filter.name);
    if (filter.description) params = params.set('description', filter.description);
    if (filter.slNhanVien != null) params = params.set('slNhanVien', filter.slNhanVien.toString());
    if (filter.addresses) params = params.set('addresses', filter.addresses);
    if (filter.notes) params = params.set('notes', filter.notes);
    return this.http.get(`${this.baseUrl1}/export-excel`, { params, responseType: 'blob' });
  }
  
  uploadExcel(file: File): Observable<void> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<void>(`${this['baseUrl']}/import`, formData);
  }
  
}
