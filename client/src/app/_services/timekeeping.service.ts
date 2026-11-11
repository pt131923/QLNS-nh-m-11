import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { Timekeeping } from '../_model/timekeeping';

@Injectable({
  providedIn: 'root'
})
export class TimekeepingService {
  private baseUrl = `${environment.apiUrl}/TimeKeeping`;

  constructor(private http: HttpClient) {}

  getTimekeepingRecords(): Observable<Timekeeping[]> {
    // Ghi thẳng để loại bỏ mọi nghi vấn
    const url = 'http://localhost:5002/api/TimeKeeping/history';
    console.log('Gọi đến:', url);
    return this.http.get<Timekeeping[]>(url);
  }
  getTimekeepingWithEmployees(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/timekeeping-with-employees`);
  }
  getTimekeepingById(id: number): Observable<Timekeeping> {
    return this.http.get<Timekeeping>(`${this.baseUrl}/${id}`);
  }

  CheckIn(): Observable<{ message?: string; Message?: string }> {
    return this.http.post<{ message?: string; Message?: string }>(`${this.baseUrl}/check-in`, {});
  }
  CheckOut(): Observable<{ message?: string; Message?: string }> {
    return this.http.post<{ message?: string; Message?: string }>(`${this.baseUrl}/check-out`, {});
  }
  getTimekeepingWithEmployeesByDate(date: Date): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/timekeeping-with-employees-by-date?date=${date}`);
  }
}


