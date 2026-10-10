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
  loading: boolean | undefined;
  errorMessage: string | undefined;
  leaveService: any;

  constructor(
    private contractService: ContractService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.contract = this.contractService.getContractDataForLeave();
    if (!this.contract) {
      // Nếu không có contract nào được truyền thì điều hướng về lại contract list
      this.router.navigate(['/contracts']);
      return;
    }

    this.LoadLeaves();
  }

  LoadLeaves(): void{
    if(!this.contract){
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.leaveService.getLeaveList().subscribe({
      next: (data: Leave[]) => {
        // Controller lưu ContractId vào trường EmployeeId
        this.leaves = data.filter(
          leave => leave.EmployeeId === this.contract!.ContractId
        );

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
