using System;
using System.ComponentModel.DataAnnotations;

namespace API.Entities
{
    public class FileHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        // Hash của file để chống trùng lặp
        public string FileHash { get; set; } 

        public string FileName { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Trạng thái xử lý: SUCCESS, FAILED, PARTIAL
        public string Status { get; set; } 
    }
}