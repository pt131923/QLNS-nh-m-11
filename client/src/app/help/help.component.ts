import { Component } from '@angular/core';

@Component({
  selector: 'app-help',
  templateUrl: './help.component.html',
  styleUrls: ['./help.component.css']
})
export class HelpComponent {
  activeIndex: number | null = null;

  faqs = [
    { question: 'help.q1', answer: 'help.a1' },
    { question: 'help.q2', answer: 'help.a2' },
    { question: 'help.q3', answer: 'help.a3' },
    { question: 'help.q4', answer: 'help.a4' },
    { question: 'help.q5', answer: 'help.a5' },
    { question: 'help.q6', answer: 'help.a6' },
    { question: 'help.q7', answer: 'help.a7' },
    { question: 'help.q8', answer: 'help.a8' },
    { question: 'help.q9', answer: 'help.a9' },
    { question: 'help.q10', answer: 'help.a10' },
    { question: 'help.q11', answer: 'help.a11' },
    { question: 'help.q12', answer: 'help.a12' },
    { question: 'help.q13', answer: 'help.a13' }
  ];

  toggle(index: number): void {
    this.activeIndex = this.activeIndex === index ? null : index;
  }

  getToggleSymbol(index: number): string {
    return this.activeIndex === index ? '−' : '+';
  }
}
