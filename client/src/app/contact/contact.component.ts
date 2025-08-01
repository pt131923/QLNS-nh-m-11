import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-contact',
  templateUrl: './contact.component.html',
  styleUrls: ['./contact.component.css']
})
export class ContactComponent {
   contactForm: FormGroup;
  isSubmitting = false;
  lastSubmissionTime: number | null = null;
  readonly MIN_SUBMISSION_INTERVAL = 3000; // 30 giây

  constructor(private fb: FormBuilder) {
    this.contactForm = this.fb.group({
      name: ['', [Validators.required, Validators.pattern(/^[a-zA-ZÀ-ỹ\s]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      message: ['', [Validators.required, Validators.maxLength(1000)]]
    });
  }

  get f() {
    return this.contactForm.controls;
  }

  validateForm(): boolean {
    if (this.contactForm.invalid) {
      // Đánh dấu tất cả các trường là đã chạm vào để hiển thị lỗi
      Object.values(this.contactForm.controls).forEach(control => {
        control.markAsTouched();
      });
      return false;
    }

    // Kiểm tra chống spam
    const now = Date.now();
    if (this.lastSubmissionTime && now - this.lastSubmissionTime < this.MIN_SUBMISSION_INTERVAL) {
      alert('Please wait at least 30 seconds before sending new contacts.');
      return false;
    }

    return true;
  }

  onSubmit() {
    if (!this.validateForm()) {
      return;
    }

    this.isSubmitting = true;
    this.submitContact();
  }

  submitContact() {
    // Giả lập gửi dữ liệu đến backend
    console.log('Contact data:', this.contactForm.value);

    // Lưu thời gian gửi cuối cùng
    this.lastSubmissionTime = Date.now();
    // Reset form và trạng thái
    setTimeout(() => {
      this.contactForm.reset();
      this.isSubmitting = false;
      alert('Thank you for contacting us. We will respond as soon as possible.');
    }, 50);
  }
  viewContactHistory() {
    // Giả lập xem lịch sử liên hệ
    console.log('Viewing contact history...');
    alert('This feature is not implemented yet.');
    // Ví dụ: mở một modal hoặc điều hướng đến một trang khác
    // this.router.navigate(['/contact-history']);
    // this.modalService.open(ContactHistoryComponent);
  }
}
