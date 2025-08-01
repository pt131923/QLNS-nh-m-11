import { Component } from '@angular/core';

@Component({
  selector: 'app-help',
  templateUrl: './help.component.html',
  styleUrls: ['./help.component.css']
})
export class HelpComponent {
  activeIndex: number | null = null;

  faqs = [
    {
      question: 'How to clock in properly?',
      answer: 'You need to clock in during your assigned shift using the "Clock In" button on the main screen or use the biometric attendance machine at the office.'

    },
    {
      question: 'What if I forget to clock in?',
      answer: 'Please contact your manager or HR representative to manually update your attendance record.'
    },
    {
      question: 'How to contact technical support?',
      answer: 'Send an email to Công ty TNHH 1 thành viên ĐHXD.com or call extension 1234 during working hours.'
    }
  ];

  toggle(index: number): void {
    if (this.activeIndex === index) {
      this.activeIndex = null;
    } else {
      this.activeIndex = index;
    }
  }
}
