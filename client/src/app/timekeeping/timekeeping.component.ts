import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';

@Component({
  selector: 'app-timekeeping',
  templateUrl: './timekeeping.component.html',
  styleUrls: ['./timekeeping.component.css']
})
export class TimekeepingComponent {
[x: string]: any;
     @ViewChild('video', { static: false }) videoElement!: ElementRef<HTMLVideoElement>;

  message: string = '';
  isSuccess: boolean = false;

  timekeepingRecords: any[] = [];

  TimekeepingForm!: FormGroup;

  constructor(private http: HttpClient, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.initializeForm();
    this.startCamera();
    this.loadAttendanceHistory();
  }

  // Khởi tạo form nhập liệu thủ công
  initializeForm(): void {
    this.TimekeepingForm = this.fb.group({
      employeeId: ['', Validators.required],
      checkInTime: ['', Validators.required],
      checkOutTime: ['', Validators.required],
      status: ['Present', Validators.required],
      note: ['']
    });
  }

  startCamera(): void {
    navigator.mediaDevices.getUserMedia({ video: true })
      .then(stream => {
        this.videoElement.nativeElement.srcObject = stream;
      })
      .catch(error => {
        this.message = 'Cannot access the camera.';
        this.isSuccess = false;
        console.error('Camera error:', error);
      });
  }

  // Dừng webcam
  stopCamera(): void {
    const video = this.videoElement.nativeElement;
    const stream = video.srcObject as MediaStream;
    if (stream) {
      stream.getTracks().forEach(track => track.stop());
      video.srcObject = null;
    }
    this.message = 'Camera stopped.';
    this.isSuccess = false;
  }

  // Chụp ảnh và gửi nhận diện khuôn mặt
  captureImage(): void {
    const video = this.videoElement.nativeElement;
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth || 720;
    canvas.height = video.videoHeight || 560;

    const context = canvas.getContext('2d');
    context?.drawImage(video, 0, 0, canvas.width, canvas.height);

    const base64Image = canvas.toDataURL('image/jpeg').split(',')[1];

    this.http.post<any>('/api/attendance/face-check', {
      imageBase64: base64Image
    }).subscribe({
      next: (res) => {
        this.message = res.message || 'Face recognition successful. Attendance recorded.';
        this.isSuccess = true;
        this.loadAttendanceHistory();
      },
      error: (err) => {
        this.message = err.error?.message || 'Face not recognized. Please try again.';
        this.isSuccess = false;
      }
    });
  }

  // Gửi chấm công thủ công
  submitTimekeeping(): void {
    if (this.TimekeepingForm.invalid) return;

    const formData = this.TimekeepingForm.value;

    this.http.post<any>('/api/attendance/manual-entry', formData)
      .subscribe({
        next: (res) => {
          this.message = res.message || 'Manual attendance entry successful.';
          this.isSuccess = true;
          this.TimekeepingForm.reset({
            status: 'Present'
          });
          this.loadAttendanceHistory();
        },
        error: (err) => {
          this.message = err.error?.message || 'Failed to submit manual attendance.';
          this.isSuccess = false;
        }
      });
  }

  // Tải lịch sử chấm công
  loadAttendanceHistory(): void {
    this.http.get<any[]>('/api/attendance/history')
      .subscribe({
        next: (data) => {
          this.timekeepingRecords = data;
        },
        error: (err) => {
          console.error('Failed to load attendance history', err);
        }
      });
  }
}
