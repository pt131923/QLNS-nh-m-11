import { Component, ViewChild, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { Contract } from 'src/app/_model/contract';
import { ContractService } from 'src/app/_services/contract.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-contract-edit',
  templateUrl: './contract-edit.component.html',
  styleUrls: ['./contract-edit.component.css']
})
export class ContractEditComponent implements OnInit {
  @ViewChild('editForm') editForm!: NgForm;
  contract!: Contract;

  constructor(
    private contractService: ContractService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.contract = this.contractService.getContractData();
    if (!this.contract) {
      alert('No contract data found.');
    }
  }

  onRowSelected(event: any) {
    const selectedContract = event.data;

    if (typeof selectedContract === 'object' && selectedContract.hasOwnProperty('contractId')) {
      if (selectedContract.contractId === undefined || selectedContract.contractId === null) {
        alert("Selected contract does not have a valid ID.");
        return;
      }
      console.log(selectedContract.contractId);
    } else {
      alert("Selected contract object is not valid.");
    }
  }

  UpdateContract() {
    const updatedContractData = this.editForm?.value;

    console.log('Updated Contract Data:', updatedContractData);

    let updatedContract: Contract = {
      ContractId: updatedContractData.contractId,
      ContractName: updatedContractData.contractName,
      ContractType: updatedContractData.contractType,
      EmployeeId: updatedContractData.Id,
      StartDate: updatedContractData.startDate,
      EndDate: updatedContractData.endDate
    };

    // Log the updated contract before sending it to the service
    if (!updatedContract.ContractId) {
      console.error('Contract ID is missing in the updated contract:', updatedContract);
      this.toastr.error('Contract ID is missing. Please check the form.');
      return;
    }
    if (!updatedContract.ContractName || !updatedContract.ContractType || !updatedContract.StartDate || !updatedContract.EndDate) {
      console.error('Some required fields are missing in the updated contract:', updatedContract);
      this.toastr.error('Please fill in all required fields.');
      return;
    }
    if (new Date(updatedContract.StartDate) >= new Date(updatedContract.EndDate)) {
      console.error('Start date must be before end date:', updatedContract);
      this.toastr.error('Start date must be before end date.');
      return;
    }

    console.log('Updated Contract:', updatedContract);

    this.contractService.UpdateContract(updatedContract.ContractId, updatedContract).subscribe({
      next: () => {
        this.toastr.success('Contract updated successfully');
        this.editForm?.reset();
        this.router.navigate(['/contracts']);
      },
      error: (err: any) => {
        console.error(err);
        this.toastr.error('Failed to update contract');
      }
    });
  }
}
