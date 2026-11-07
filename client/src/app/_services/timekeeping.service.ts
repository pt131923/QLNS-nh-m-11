import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { Timekeeping } from '../_model/timekeeping';

@Injectable({
  providedIn: 'root'
})
export class TimekeepingService {
  private baseUrl = `${environment.apiUrl}/timekeeping`;

  constructor(private http: HttpClient) {}

  getTimekeepingRecords(): Observable<Timekeeping[]> {
    return this.http.get<Timekeeping[]>(`${this.baseUrl}/history`);
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


