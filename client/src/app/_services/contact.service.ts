import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { Contact } from '../_model/contact';

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  private baseUrl = environment.apiUrl7;

  constructor(private http: HttpClient) {}

  getContactRecords(): Observable<Contact[]> {
    return this.http.get<Contact[]>(this.baseUrl);
  }

  getContactWithEmployees(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/contact-with-employees`);
  }

  searchContact(employeeName?: string, id?: number): Observable<Contact[]> {
    const params = new URLSearchParams();
    if (employeeName) params.append('employeeName', employeeName);
    if (id !== undefined) params.append('id', id.toString());

    return this.http.get<Contact[]>(`${this.baseUrl}/search?${params.toString()}`);
  }

  AddContact(contact: Contact): Observable<Contact> {
    return this.http.post<Contact>(`${this.baseUrl}/add-contact`, contact);
  }

  getContactById(id: number): Observable<Contact> {
    return this.http.get<Contact>(`${this.baseUrl}/${id}`);
  }

  getContactHistory(): Observable<any> {
    return this.http.get<any[]>(`${this.baseUrl}/history`);
  }
}



