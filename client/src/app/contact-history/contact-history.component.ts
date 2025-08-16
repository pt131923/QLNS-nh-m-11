import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-contact-history',
  templateUrl: './contact-history.component.html',
  styleUrls: ['./contact-history.component.css']
})
export class ContactHistoryComponent {
  contacts: any[] = [];
  constructor(private http: HttpClient) {}

  ngOnInit() {
  this.http.get<any[]>('http://localhost:5002/api/contact').subscribe(data => {
    this.contacts = data;
    });
  }
  viewContact(contact: any) {
   //xử lý logic để xem chi tiết liên hệ
    console.log('Viewing contact:', contact);
    alert(`Viewing contact: ${contact.Name}`);
  }

  deleteContact(contactId: number) {
    console.log('Deleting contact with ID:', contactId);
  }
}
