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
    ContractImage: '',
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
    if (!this.addForm2 || this.addForm2.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    let Contract: Contract = { ...this.contract };

    // Validation cơ bản
    if (!Contract.ContractName?.trim() ||
        !Contract.EmployeeName?.trim() ||
        !Contract.ContractType?.trim() ||
        !Contract.StartDate || !Contract.EndDate) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    // Ngày bắt đầu phải trước ngày kết thúc
    if (new Date(Contract.StartDate) >= new Date(Contract.EndDate)) {
      this.toastr.error('Start date must be before end date');
      return;
    }

    // Ngày kết thúc không được nhỏ hơn ngày hiện tại
    if (new Date(Contract.EndDate) < new Date()) {
      this.toastr.error('End date cannot be in the past');
      return;
    }

    // Lương và phụ cấp
    if (Contract.BasicSalary < 0) {
      this.toastr.error('Basic Salary cannot be negative');
      return;
    }
    if (Contract.Allowance < 0) {
      this.toastr.error('Allowance cannot be negative');
      return;
    }

    // Chuẩn hoá dữ liệu
    Contract.ContractName = Contract.ContractName.trim();
    Contract.ContractType = Contract.ContractType.trim();
    Contract.EmployeeName = Contract.EmployeeName.trim();
    Contract.StartDate = new Date(Contract.StartDate).toISOString();
    Contract.EndDate = new Date(Contract.EndDate).toISOString();
    Contract.CreateAt = new Date().toISOString();
    Contract.UpdateAt = new Date().toISOString();
    Contract.JobDescription = Contract.JobDescription?.trim() || '';
    Contract.ContractTerm = Contract.ContractTerm?.trim() || '';
    Contract.WorkLocation = Contract.WorkLocation?.trim() || '';
    Contract.Leaveofabsence = Contract.Leaveofabsence?.trim() || '';

    // Gọi API
    this.contractService.AddContract(Contract).subscribe({
      next: () => {
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
      ContractImage: '',
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
    if (this.addForm2) {
      this.addForm2.resetForm();
    }
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
