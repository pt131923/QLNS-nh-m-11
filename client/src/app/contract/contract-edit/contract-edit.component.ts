import { Component, ViewChild, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Contract } from 'src/app/_model/contract';
import { ContractService } from 'src/app/_services/contract.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-contract-edit',
  templateUrl: './contract-edit.component.html',
  styleUrls: ['./contract-edit.component.css']
})
export class ContractEditComponent implements OnInit {
  @ViewChild('editForm') editForm: NgForm | undefined;

  contract: Contract = {
    ContractId: 0,
    ContractName: '',
    ContractType: '',
    EmployeeName: '',
    StartDate: undefined,
    EndDate: undefined,
    BasicSalary: 0,
    Allowance: 0,
    CreateAt: undefined,
    UpdateAt: undefined,
    JobDescription: '',
    ContractTerm: '',
    WorkLocation: '',
    Leaveofabsence: ''
  };

  constructor(
    private contractService: ContractService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.getContract();
  }

  getContract() {
    const id = this.route.snapshot.paramMap.get('id'); // lấy id từ URL
    if (id) {
      this.contractService.getContractById(+id).subscribe({
        next: (contract: Contract) => {
          this.contract = contract;
        },
        error: (err) => {
          console.error('Error fetching contract:', err);
          this.toastr.error('Failed to fetch contract');
          this.router.navigate(['/contracts']);
        }
      });
    } else {
      alert('No contract ID found in the URL.');
      this.router.navigate(['/contracts']);
    }
  }

  UpdateContract() {
    if (this.editForm?.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    const updatedContractData = this.editForm?.value;

    const updatedContract: Contract = {
      ...this.contract,
      ...updatedContractData
    };

    // validate ngày
    if (new Date(updatedContract.StartDate!) >= new Date(updatedContract.EndDate!)) {
      this.toastr.error('Start date must be before end date');
      return;
    }

    this.contractService.UpdateContract(updatedContract.ContractId, updatedContract).subscribe({
      next: () => {
        this.toastr.success('Contract updated successfully');
        this.editForm?.reset(this.contract); // reset form về trạng thái ban đầu
        this.router.navigate(['/contracts']);
      },
      error: (err) => {
        console.error('Error updating contract:', err);
        this.toastr.error('Failed to update contract');
      }
    });
  }

  Cancel() {
    this.router.navigate(['/contracts']);
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.toastr.clear();
    }, 500);
  }
}
