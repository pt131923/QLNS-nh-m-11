import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Contract } from 'src/app/_model/contract';
import { Department } from 'src/app/_model/department';
import { ContractService } from 'src/app/_services/contract.service';

@Component({
  selector: 'app-contract-detail',
  templateUrl: './contract-detail.component.html',
  styleUrls: ['./contract-detail.component.css']
})
export class ContractDetailComponent implements OnInit{
   contract!: Contract;

     constructor(
       private route: ActivatedRoute,
       private router: Router,
       private contractService: ContractService
     ) {}

     ngOnInit(): void {
       const id = this.route.snapshot.paramMap.get('id');
       if (id) {
         this.contractService.getContractById(+id).subscribe({
           next: (data) => {
             this.contract = data;
           },
           error: () => {
             console.error('Unable to load contract data');
           }
         });
       }
     }

     getDepartmentId(departmentId: number): void {
     this.contractService.getContractById(departmentId).subscribe({
       next: (con:Contract) => {
         this.contract = con;
       },
       error: () => {
         this.contract = {
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
       }
     });
     }

     goToContractDetail(id: number): void {
     this.router.navigate(['/contracts', id]);
  }
}
