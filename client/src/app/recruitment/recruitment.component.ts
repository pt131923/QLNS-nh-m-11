import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthGuard } from '../_guards/auth.guard';
interface Job {
  id: number;
  title: string;
  description: string;
  location: string;
  postedDate: Date;
}

@Component({
  selector: 'app-recruitment',
  templateUrl: './recruitment.component.html',
  styleUrls: ['./recruitment.component.css']
})
export class RecruitmentComponent implements OnInit {

  loginMode = false;
  jobs: Job[] = [];

  constructor(private router: Router, private authGuard: AuthGuard) {}

  ngOnInit(): void {
    // Dữ liệu mẫu, sau này bạn có thể thay bằng API từ backend
    this.jobs = [
      {
        id: 1,
        title: 'Frontend Developer',
        description: 'Develop and maintain Angular applications.',
        location: 'Hà Nội, Việt Nam',
        postedDate: new Date('2025-09-10')
      },
      {
        id: 2,
        title: 'Backend Developer',
        description: 'Work with ASP.NET Core APIs and databases.',
        location: 'Hồ Chí Minh, Việt Nam',
        postedDate: new Date('2025-09-12')
      },
      {
        id: 3,
        title: 'HR Specialist',
        description: 'Manage recruitment and employee relations.',
        location: 'Đà Nẵng, Việt Nam',
        postedDate: new Date('2025-09-14')
      },

      {
        id: 4,
        title: 'Fullstack Developer',
        description: 'Develop both frontend and backend components with Java and ReactJS.',
        location: 'Đà Nẵng, Việt Nam',
        postedDate: new Date('2025-09-14')
      }
    ];
  }

  // Bật/tắt form login
  loginToggle() {
    this.loginMode = !this.loginMode;
  }

  // Hủy login khi nhận event từ app-login
  cancelLoginMode(event: boolean) {
    this.loginMode = event;
  }

  // Xử lý khi nhấn Apply Now
  applyJob(job: Job) {
    // Ví dụ: chuyển hướng sang trang apply với jobId
    this.router.navigate(['/apply', job.id]);
  }
}
