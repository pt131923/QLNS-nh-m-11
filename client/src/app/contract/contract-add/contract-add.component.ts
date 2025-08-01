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
    ContractImage: undefined,
    ContractId: 0,
    ContractName: '',
    ContractType: '',
    EmployeeName:'',
    StartDate: '',
    EndDate: '',
    BasicSalary: 0,
      Allowance: 0,
      CreateAt: '',
      UpdateAt: '',
      JobDescription: '',
      ContractTerm: '',
      WorkLocation: '',
      Leaveofabsence: ''
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
    if (!Contract.ContractName || !Contract.StartDate || !Contract.EndDate|| !Contract.CreateAt|| !Contract.UpdateAt || !Contract.ContractType || !Contract.EmployeeName|| !Contract.BasicSalary || !Contract.Allowance|| !Contract.JobDescription || !Contract.ContractTerm || !Contract.WorkLocation || !Contract.Leaveofabsence) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    if (Contract.ContractType.trim() === '') {
      this.toastr.error('Contract Type is required');
      return;
    }
    if (Contract.EmployeeName.trim() === '') {
      this.toastr.error('Employee Name is required');
      return;
    }

    if (new Date(Contract.StartDate) >= new Date(Contract.EndDate)) {
      this.toastr.error('Start date must be before end date');
      return;
    }

    if (new Date(Contract.EndDate) < new Date()) {
      this.toastr.error('End date cannot be in the past');
      return;
    }
    if (Contract.BasicSalary < 0) {
      this.toastr.error('Basic Salary cannot be negative');
      return;
    }
    if (Contract.Allowance < 0) {
      this.toastr.error('Allowance cannot be negative');
      return;
    }

    if(!Contract.JobDescription || !Contract.ContractTerm || !Contract.WorkLocation || !Contract.Leaveofabsence) {
      this.toastr.error('Job Description, Contract Term, Work Location, and Leave of Absence are required');
      return;
    }

    if (Contract.ContractTerm.trim() === '') {
      this.toastr.error('Contract Term is required');
      return;
    }
    if (Contract.WorkLocation.trim() === '') {
      this.toastr.error('Work Location is required');
      return;
    }
    if (Contract.Leaveofabsence.trim() === '') {
      this.toastr.error('Leave of Absence is required');
      return;
    }

    Contract.BasicSalary = Contract.BasicSalary || 0;
    Contract.Allowance = Contract.Allowance || 0;
    Contract.CreateAt = Contract.CreateAt || new Date().toISOString();
    Contract.UpdateAt = new Date().toISOString();
    Contract.JobDescription = Contract.JobDescription?.trim() || '';
    Contract.ContractTerm = Contract.ContractTerm?.trim() || '';
    Contract.WorkLocation = Contract.WorkLocation?.trim() || '';
    Contract.Leaveofabsence = Contract.Leaveofabsence?.trim() || '';

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

  onSubmit(): void {
    if (this.addForm2.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.AddContract();
  }

  onCancel(): void {
    this.resetForm();
    this.router.navigate(['/contracts']);
  }

  private resetForm(): void {
    this.contract = {
      ContractImage:'',
      ContractId: 0,
      ContractName: '',
      ContractType: '',
      EmployeeName: '',
      StartDate: '',
      EndDate: '',
      BasicSalary: 0,
      Allowance: 0,
      CreateAt: '',
      UpdateAt: '',
      JobDescription: '',
      ContractTerm: '',
      WorkLocation: '',
      Leaveofabsence: ''
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
