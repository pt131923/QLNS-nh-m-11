import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) { }

  // Lấy danh sách người dùng
  getUsers() {
    return this.http.get(this.apiUrl);
  }

  // Lấy user theo ID
  getUserById(id: number) {
    return this.http.get(`${this.apiUrl}/${id}`);
  }

  // Tạo user
  CreateUser(user: any) {
    return this.http.post(this.apiUrl, user);
  }

  // Cập nhật user
  UpdateUser(id: number, user: any) {
    return this.http.put(`${this.apiUrl}/${id}`, user);
  }

  // Xóa user
  DeleteUser(id: number) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // 👉 API đổi mật khẩu
  updatePassword(userId: number, data: any) {
    return this.http.put(`${this.apiUrl}/${userId}/password`, data);
  }

  // 👉 Upload avatar cho user hiện tại
  uploadMyAvatar(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/me/avatar`, formData);
  }

}
