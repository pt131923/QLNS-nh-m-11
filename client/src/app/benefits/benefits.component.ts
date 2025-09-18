
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-benefits',
  templateUrl: './benefits.component.html',
  styleUrls: ['./benefits.component.css']
})
export class BenefitsComponent {
  // Component logic goes here
  constructor(private router: Router, private HttpClient: HttpClient) {
  }

  ngOnInit(): void {
  }

  getlink()
  {
    this.router.navigate(['/benefit-list']);
  }
}
