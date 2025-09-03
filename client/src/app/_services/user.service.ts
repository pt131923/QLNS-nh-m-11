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
}
