import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Router } from '@angular/router';
import { Contract } from 'src/app/_model/contract';
import { ContractService } from 'src/app/_services/contract.service';
import { Leave } from '../_model/leave';

@Component({
  selector: 'app-leave',
  templateUrl: './leave.component.html',
  styleUrls: ['./leave.component.css']
})
export class LeaveComponent implements OnInit {
  contract: Contract | null = null;
  leaves: Leave[] = [];

  constructor(
    private contractService: ContractService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.contract = this.contractService.getContractDataForLeave();
    if (!this.contract) {
      // Nếu không có contract nào được truyền thì điều hướng về lại contract list
      this.router.navigate(['/contracts']);
    }

    // Lấy danh sách leave từ service
    if (this.contract) {
      this.leaves = this.contractService.getLeaveDataForContract(this.contract.ContractId);
    }
  }
}
