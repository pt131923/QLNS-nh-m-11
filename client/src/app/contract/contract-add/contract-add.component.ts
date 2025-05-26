import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Contract } from 'src/app/_model/contract';
import { ContractService } from 'src/app/_services/contract.service';

@Component({
  selector: 'app-contract-add',
  templateUrl: './contract-add.component.html',
  styleUrls: ['./contract-add.component.css']
})
export class ContractAddComponent implements OnInit {
  @ViewChild('addForm2') addForm2!: NgForm;

  contract: Contract = {
    ContractId: 0,
    ContractName: '',
    ContractType: '',
    EmployeeId: 0,
    StartDate: '',
    EndDate: ''
  };

  contracts: Contract[] = [];

  constructor(
    private contractService: ContractService,
    private toastr: ToastrService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.getContracts();
  }

  AddContract(): void {
    if (this.addForm2.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    let Contract: Contract = { ...this.contract };

    // Validation
    if (!Contract.ContractName || !Contract.StartDate || !Contract.EndDate) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    if (Contract.ContractType.trim() === '') {
      this.toastr.error('Contract Type is required');
      return;
    }

    if (Contract.EmployeeId === 0) {
      this.toastr.error('Please select an employee');
      return;
    }

    if (new Date(Contract.StartDate) >= new Date(Contract.EndDate)) {
      this.toastr.error('Start date must be before end date');
      return;
    }

    // Call the service
    this.contractService.AddContract(Contract).subscribe({
      next: (response) => {
        this.toastr.success('Contract added successfully!');
        this.resetForm();
        this.router.navigate(['/contracts']);
      },
      error: (error) => {
        console.error('Error adding contract:', error);
        this.toastr.error('Failed to add contract!');
      }
    });
  }

  onCancel(): void {
    this.resetForm();
    this.router.navigate(['/contracts']);
  }

  private resetForm(): void {
    this.contract = {
      ContractId: 0,
      ContractName: '',
      ContractType: '',
      EmployeeId: 0,
      StartDate: '',
      EndDate: ''
    };
    this.addForm2.resetForm();
  }

  getContracts(): void {
    this.contractService.getContracts().subscribe({
      next: (contracts) => {
        this.contracts = contracts;
      },
      error: (err) => {
        console.error('Error fetching contracts:', err);
        this.toastr.error('Failed to fetch contracts');
      }
    });
  }
}
