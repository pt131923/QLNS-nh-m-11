import { Component, OnInit } from '@angular/core';
import { UserActivityService, TimeKeepingChangeRequest } from '../../_services/user-activity.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-timekeeping-history',
  templateUrl: './timekeeping-history.component.html',
  styleUrls: ['./timekeeping-history.component.css']
})
export class TimekeepingHistoryComponent implements OnInit {
  history: TimeKeepingChangeRequest[] = [];
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
    this.userActivity.getTimeKeepingChangeRequestHistory().subscribe({
      next: (data) => {
        this.history = data || [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load timekeeping history');
      }
    });
  }
}
