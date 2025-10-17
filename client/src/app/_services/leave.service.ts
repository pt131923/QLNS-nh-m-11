import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { Timekeeping } from '../_model/timekeeping';
import { Leave } from '../_model/leave';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private baseUrl = environment.apiUrl6;

  constructor(private http: HttpClient) {}

  getLeaveRecords(): Observable<Leave[]> {
    return this.http.get<Leave[]>(this.baseUrl);
  }

  getLeaveWithEmployees(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/leave-with-employees`);
  }

  searchLeave(employeeName?: string, id?: number): Observable<Leave[]> {
    const params = new URLSearchParams();
    if (employeeName) params.append('employeeName', employeeName);
    if (id !== undefined) params.append('id', id.toString());

    return this.http.get<Leave[]>(`${this.baseUrl}/search?${params.toString()}`);
  }

  getLeaveDataForContract(id: number): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.baseUrl}/contract/${id}/leaves`);
  }
}


