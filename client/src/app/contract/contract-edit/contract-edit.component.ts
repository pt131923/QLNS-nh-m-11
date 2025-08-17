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
    this.toastr.error('No contract data found.');
    this.router.navigate(['/contracts']);
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
      EmployeeName: updatedContractData.EmployeeName,
      StartDate: updatedContractData.startDate,
      EndDate: updatedContractData.endDate,
       BasicSalary: 0,
      Allowance: 0,
      CreateAt: '',
      UpdateAt: '',
      JobDescription: '',
      ContractTerm: '',
      WorkLocation: '',
      Leaveofabsence: ''
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

    //verify các trường còn lại
    if (!updatedContract.BasicSalary || !updatedContract.Allowance || !updatedContract.CreateAt || !updatedContract.UpdateAt || !updatedContract.JobDescription || !updatedContract.ContractTerm || !updatedContract.WorkLocation || !updatedContract.Leaveofabsence) {
      console.error('Some required fields are missing in the updated contract:', updatedContract);
      this.toastr.error('Please fill in all required fields.');
      return;
    }

    // Ensure the contract ID is valid
    if (updatedContract.ContractId <= 0) {
      console.error('Invalid Contract ID:', updatedContract.ContractId);
      this.toastr.error('Invalid Contract ID. Please check the form.');
      return;
    }

    // Ensure the contract name is not empty
    if (!updatedContract.ContractName.trim()) {
      console.error('Contract Name is empty:', updatedContract.ContractName);
      this.toastr.error('Contract Name cannot be empty.');
      return;
    }

    // Ensure the contract type is not empty
    if (!updatedContract.ContractType.trim()) {
      console.error('Contract Type is empty:', updatedContract.ContractType);
      this.toastr.error('Contract Type cannot be empty.');
      return;
    }

    // Ensure the employee name is not empty
    if (!updatedContract.EmployeeName.trim()) {
      console.error('Employee Name is empty:', updatedContract.EmployeeName);
      this.toastr.error('Employee Name cannot be empty.');
      return;
    }

    // Ensure the start date is a valid date
    if (isNaN(new Date(updatedContract.StartDate).getTime())) {
      console.error('Invalid Start Date:', updatedContract.StartDate);
      this.toastr.error('Invalid Start Date. Please check the date format.');
      return;
    }

    // Ensure the end date is a valid date
    if (isNaN(new Date(updatedContract.EndDate).getTime())) {
      console.error('Invalid End Date:', updatedContract.EndDate);
      this.toastr.error('Invalid End Date. Please check the date format.');
      return;
    }

    // Ensure the start date is before the end date
    if (new Date(updatedContract.StartDate) >= new Date(updatedContract.EndDate)) {
      console.error('Start date must be before end date:', updatedContract);
      this.toastr.error('Start date must be before end date.');
      return;
    }

    // Ensure the basic salary is a valid number
    if (isNaN(updatedContract.BasicSalary) || updatedContract.BasicSalary < 0) {
      console.error('Invalid Basic Salary:', updatedContract.BasicSalary);
      this.toastr.error('Basic Salary must be a valid number.');
      return;
    }

    // Ensure the allowance is a valid number
    if (isNaN(updatedContract.Allowance) || updatedContract.Allowance < 0) {
      console.error('Invalid Allowance:', updatedContract.Allowance);
      this.toastr.error('Allowance must be a valid number.');
      return;
    }

    // Ensure the create date is a valid date
    if (isNaN(new Date(updatedContract.CreateAt).getTime())) {
      console.error('Invalid Create Date:', updatedContract.CreateAt);
      this.toastr.error('Invalid Create Date. Please check the date format.');
      return;
    }

    // Ensure the update date is a valid date
    if (isNaN(new Date(updatedContract.UpdateAt).getTime())) {
      console.error('Invalid Update Date:', updatedContract.UpdateAt);
      this.toastr.error('Invalid Update Date. Please check the date format.');
      return;
    }

    // Ensure the job description is not empty
    if (!updatedContract.JobDescription.trim()) {
      console.error('Job Description is empty:', updatedContract.JobDescription);
      this.toastr.error('Job Description cannot be empty.');
      return;
    }

    // Ensure the contract term is not empty
    if (!updatedContract.ContractTerm.trim()) {
      console.error('Contract Term is empty:', updatedContract.ContractTerm);
      this.toastr.error('Contract Term cannot be empty.');
      return;
    }

    // Ensure the work location is not empty
    if (!updatedContract.WorkLocation.trim()) {
      console.error('Work Location is empty:', updatedContract.WorkLocation);
      this.toastr.error('Work Location cannot be empty.');
      return;
    }

    // Ensure the leave of absence is not empty
    if (!updatedContract.Leaveofabsence.trim()) {
      console.error('Leave of Absence is empty:', updatedContract.Leaveofabsence);
      this.toastr.error('Leave of Absence cannot be empty.');
      return;
    }

    // If all validations pass, proceed to update the contract
    updatedContract = {
      ...updatedContract,
      CreateAt: new Date().toISOString(),
      UpdateAt: new Date().toISOString()
    };

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
