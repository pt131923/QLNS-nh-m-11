import { Component, OnInit } from '@angular/core';
import { UserActivityService, LeaveRequest } from '../../_services/user-activity.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-leave-history',
  templateUrl: './leave-history.component.html',
  styleUrls: ['./leave-history.component.css']
})
export class LeaveHistoryComponent implements OnInit {
  history: LeaveRequest[] = [];
  isLoading = false;

  constructor(
    private userActivity: UserActivityService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.isLoading = true;
    this.userActivity.getLeaveRequestHistory().subscribe({
      next: (data) => {
        this.history = data || [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load leave history');
      }
    });
  }
}
