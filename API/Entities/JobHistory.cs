using System;

namespace API.Entities
{
    // Lịch sử ứng tuyển công việc
    public class ApplicationHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; } // ID người dùng (Ứng viên/Nhân viên)
        public int JobId { get; set; } // ID công việc đã ứng tuyển
        public string JobTitle { get; set; }
        public DateTime ApplicationDate { get; set; } = DateTime.Now;
        public string Status { get; set; } 
        
        // Navigation Property
        public User User { get; set; }
    }
}