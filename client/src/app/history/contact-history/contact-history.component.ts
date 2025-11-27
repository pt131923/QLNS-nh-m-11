import { Component, OnInit } from '@angular/core';
import { ContactService } from '../../_services/contact.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-contact-history',
  templateUrl: './contact-history.component.html',
  styleUrls: ['./contact-history.component.css']
})
export class ContactHistoryComponent implements OnInit {
  contacts: any[] = [];
  isLoading = false;

  constructor(
    private contactService: ContactService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.loadContacts();
  }

  loadContacts() {
    this.isLoading = true;
    this.contactService.getContactHistory().subscribe({
      next: (data: any[]) => {
        console.log('Contact history data received:', data);
        this.contacts = data || [];
        console.log('Contacts array after assignment:', this.contacts);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching contact history:', error);
        this.toastr.error('Failed to load contact history.');
        this.isLoading = false;
      }
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
