import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) { }

  // Lấy danh sách người dùng (có hỗ trợ phân trang & tìm kiếm)
  getUsers(page?: number, pageSize?: number, q?: string): Observable<any> {
    let params = new HttpParams();
    if (page != null) params = params.set('page', page.toString());
    if (pageSize != null) params = params.set('pageSize', pageSize.toString());
    if (q) params = params.set('q', q);
    return this.http.get(this.apiUrl, { params });
  }

  // Lấy user theo ID
  getUserById(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/${id}`);
  }

  // Tạo user
  CreateUser(user: any): Observable<any> {
    return this.http.post(this.apiUrl, user);
  }

  // Cập nhật user
  UpdateUser(id: number, user: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, user);
  }

  // Cập nhật role
  updateRole(userId: number, role: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/role`, { role });
  }

  // Cập nhật trạng thái Active/Inactive
  updateActive(userId: number, isActive: boolean): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/status`, { isActive });
  }

  // Cập nhật liên kết hồ sơ nhân viên
  updateEmployeeLink(userId: number, employeeId: number | null): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/employee-link`, { employeeId });
  }

  // Xóa user
  DeleteUser(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Đổi mật khẩu
  updatePassword(userId: number, data: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/password`, data);
  }

  // Upload avatar
  uploadMyAvatar(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/me/avatar`, formData);
  }
}
