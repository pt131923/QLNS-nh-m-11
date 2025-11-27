import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
 providedIn: 'root'
})
export class UserService {

 private apiUrl = 'http://localhost:5002/api/users';

 constructor(private http: HttpClient) { }

 getUsers() {
   return this.http.get(this.apiUrl);
 }

 getUserById(id: number) {
  return this.http.get(`${this.apiUrl}/${id}`);
 }

 CreateUser(user: any) {
   return this.http.post(this.apiUrl, user);
 }

 UpdateUser(id: number, user: any) {
   return this.http.put(`${this.apiUrl}/${id}`, user);
 }

 DeleteUser(id: number) {
    return this.http.delete(`${this.apiUrl}/${id}`);
 }

    // <== BỔ SUNG HÀM ĐỔI MẬT KHẨU ==>
 /**
   * Gửi yêu cầu cập nhật mật khẩu lên Server
   * @param userId - ID người dùng đang đăng nhập
   * @param data - Chứa oldPassword, newPassword, v.v.
   */
 updatePassword(userId: number, data: any) {
 // Giả sử Server Backend của bạn có endpoint là PUT /api/users/{id}/password
    return this.http.put(`${this.apiUrl}/${userId}/password`, data);
   }
    // <== KẾT THÚC BỔ SUNG ==>
}