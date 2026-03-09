using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
    public class FileHistory
    {
        [Key]
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
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