import { Component, OnInit } from '@angular/core';
import { UserActivityService, TrainingHistory } from '../../_services/user-activity.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-training-history',
  templateUrl: './training-history.component.html',
  styleUrls: ['./training-history.component.css']
})
export class TrainingHistoryComponent implements OnInit {
  history: TrainingHistory[] = [];
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
    this.userActivity.getTrainingHistory().subscribe({
      next: (data) => {
        this.history = data || [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load training history');
      }
    });
  }
}
