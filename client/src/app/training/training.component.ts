import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthGuard } from '../_guards/auth.guard';

interface Course {
  CourseId: number;
  Title: string;
  Description: string;
  Trainer: string;
  TimeOfCourse: Date;
}

@Component({
  selector: 'app-training',
  templateUrl: './training.component.html',
  styleUrls: ['./training.component.css']
})

export class TrainingComponent implements OnInit {

 loginMode = false;   // kiểm soát form login
   courses: Course[] = [];    // danh sách khóa học

   constructor(private router: Router, private authGuard: AuthGuard) {}

   ngOnInit(): void {
     // Dữ liệu mẫu, sau này bạn có thể thay bằng API từ backend
     this.courses = [
       {
         CourseId: 1,
         Title: 'Frontend Developer',
         Description: 'Develop and maintain Angular applications.',
         Trainer: 'John Doe',
         TimeOfCourse: new Date('2025-09-10')
       },
       {
         CourseId: 2,
         Title: 'Backend Developer',
         Description: 'Work with ASP.NET Core APIs and databases.',
         Trainer: 'Jane Smith',
         TimeOfCourse: new Date('2025-09-12')
       },
       {
         CourseId: 3,
         Title: 'HR Specialist',
         Description: 'Manage recruitment and employee relations.',
         Trainer: 'Alice Johnson',
         TimeOfCourse: new Date('2025-09-14')
       },

       {
         CourseId: 4,
         Title: 'Fullstack Developer',
         Description: 'Develop both frontend and backend components with Java and ReactJS.',
         Trainer: 'Bob Brown',
         TimeOfCourse: new Date('2025-09-14')
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
  applyJob(course: Course) {
    // Ví dụ: chuyển hướng sang trang apply với jobId
    this.router.navigate(['/training', course.CourseId]);
    //link sang các trang tương ứng 
  }
}
