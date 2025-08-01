import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { Timekeeping } from '../_model/timekeeping';

@Injectable({
  providedIn: 'root'
})
export class TimekeepingService {
  private baseUrl = environment.apiUrl5;

  constructor(private http: HttpClient) {}

  getTimekeepingRecords(): Observable<Timekeeping[]> {
    return this.http.get<Timekeeping[]>(this.baseUrl);
  }

  getTimekeepingWithEmployees(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/timekeeping-with-employees`);
  }

  searchTimekeeping(employeeName?: string, id?: number): Observable<Timekeeping[]> {
    const params = new URLSearchParams();
    if (employeeName) params.append('employeeName', employeeName);
    if (id !== undefined) params.append('id', id.toString());

    return this.http.get<Timekeeping[]>(`${this.baseUrl}/search?${params.toString()}`);
  }

  AddTimekeeping(timekeeping: Timekeeping): Observable<Timekeeping> {
    return this.http.post<Timekeeping>(`${this.baseUrl}/add-timekeeping`, timekeeping);
  }

  UpdateTimekeeping(id: number, timekeeping: Timekeeping): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, timekeeping);
  }

  getTimekeepingById(id: number): Observable<Timekeeping> {
    return this.http.get<Timekeeping>(`${this.baseUrl}/${id}`);
  }
}


