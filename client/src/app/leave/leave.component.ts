import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Router } from '@angular/router';
import { Contract } from 'src/app/_model/contract';
import { ContractService } from 'src/app/_services/contract.service';
import { Leave } from '../_model/leave';
import { LeaveService }from '../_services/leave.service';

@Component({
  selector: 'app-leave',
  templateUrl: './leave.component.html',
  styleUrls: ['./leave.component.css']
})
export class LeaveComponent implements OnInit {
  contract: Contract | null = null;
  leaves: Leave[] = [];
  loading = false;
  errorMessage = '';

  constructor(
    private contractService: ContractService,
    private leaveService: LeaveService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.contract = this.contractService.getContractDataForLeave();
    this.LoadLeaves();
  }

  LoadLeaves(): void {
    this.loading = true;
    this.errorMessage = '';

    this.leaveService.getLeaveRecords().subscribe({
      next: (data: Leave[]) => {
        if (this.contract) {
          const empId = this.contract.EmployeeId || this.contract.ContractId;
          const empName = this.contract.EmployeeName?.toLowerCase().trim();
          this.leaves = data.filter(
            leave => leave.EmployeeId === empId || 
                     leave.EmployeeId === this.contract!.ContractId ||
                     (leave.EmployeeName && empName && leave.EmployeeName.toLowerCase().trim() === empName)
          );
        } else {
          this.leaves = data;
        }
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Lỗi khi lấy danh sách nghỉ phép:', error);
        this.errorMessage = 'Không thể tải danh sách nghỉ phép.';
        this.loading = false;
      }
    });
  }
}
