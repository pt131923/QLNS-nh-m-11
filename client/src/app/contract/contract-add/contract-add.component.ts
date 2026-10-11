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
@ViewChild('addForm') addForm!: NgForm;

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
    if (this.addForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    let contractData: Contract = {
      ContractId: 0,
      ContractName: this.addForm.value.ContractName?.trim(),
      ContractType: this.addForm.value.ContractType?.trim(),
      EmployeeName: this.addForm.value.EmployeeName?.trim(),
      StartDate: this.addForm.value.StartDate,
      EndDate: this.addForm.value.EndDate,
      BasicSalary: this.addForm.value.BasicSalary,
      Allowance: this.addForm.value.Allowance,
      CreateAt: this.addForm.value.CreateAt,
      UpdateAt: this.addForm.value.UpdateAt,
      JobDescription: this.addForm.value.JobDescription?.trim() || '',
      ContractTerm: this.addForm.value.ContractTerm?.trim() || '',
      WorkLocation: this.addForm.value.WorkLocation?.trim() || '',
      Leaveofabsence: this.addForm.value.Leaveofabsence?.trim() || ''
    };

    console.log(contractData);

    // Validation logic
    if (!contractData.ContractName || !contractData.EmployeeName || !contractData.ContractType) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    if (contractData.StartDate && contractData.EndDate && new Date(contractData.StartDate) >= new Date(contractData.EndDate)) {
      this.toastr.error('Start date must be before end date');
      return;
    }

    if (contractData.EndDate && new Date(contractData.EndDate) < new Date()) {
      this.toastr.error('End date cannot be in the past');
      return;
    }

    // Check CreateAt
if (!contractData.CreateAt || isNaN(Date.parse(contractData.CreateAt.toString()))) {
  this.toastr.error('CreateAt is required and must be a valid date');
  return;
}

// Check UpdateAt
if (!contractData.UpdateAt || isNaN(Date.parse(contractData.UpdateAt.toString()))) {
  this.toastr.error('UpdateAt is required and must be a valid date');
  return;
}

// Business logic: CreateAt <= today
if (new Date(contractData.CreateAt) > new Date()) {
  this.toastr.error('CreateAt cannot be in the future');
  return;
}

// Business logic: UpdateAt >= CreateAt
if (new Date(contractData.UpdateAt) < new Date(contractData.CreateAt)) {
  this.toastr.error('UpdateAt cannot be before CreateAt');
  return;
}


    if (contractData.ContractTerm && contractData.ContractTerm.length > 100) {
      this.toastr.error('Contract Term cannot exceed 100 characters');
      return;
    }

    if (contractData.BasicSalary < 0) {
      this.toastr.error('Basic Salary cannot be negative');
      return;
    }

    if (contractData.Allowance < 0) {
      this.toastr.error('Allowance cannot be negative');
      return;
    }

    if (contractData.WorkLocation && contractData.WorkLocation.length > 100) {
      this.toastr.error('Work Location cannot exceed 100 characters');
      return;
    }

    if (contractData.JobDescription && contractData.JobDescription.length > 500) {
      this.toastr.error('Job Description cannot exceed 500 characters');
      return;
    }

    // Gọi API
    this.contractService.AddContract(contractData).subscribe({
      next: (response: Contract) => {
        console.log('Phản hồi từ BE:', response);
        
        const newContractId = response.ContractId;
        
        if (newContractId && newContractId !== 0) {
          this.toastr.success(`Tạo hợp đồng thành công! Mã: ${newContractId}`);
          
          // Tải lại danh sách mới nhất
          this.getContracts();
          
          this.addForm.reset();
          
          // Chuyển trang sau khi đảm bảo cập nhật xong
          setTimeout(() => {
            this.router.navigate(['/contracts']);
          }, 100);
        } else {
          this.toastr.warning("Đã lưu nhưng không nhận được mã hợp đồng!");
        }
      },
      error: (err) => {
        console.error('Lỗi thêm hợp đồng:', err);
        this.toastr.error('Thêm hợp đồng thất bại!');
      }
    });
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

  onCancel(): void {
    this.addForm.reset();
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.toastr.clear();
    }, 50);
  }
}
