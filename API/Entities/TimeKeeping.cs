using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
    public class TimeKeeping
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int TimeKeepingId { get; set; }
        public int EmployeeId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }
        public TimeSpan CheckIn { get; set; }
        public TimeSpan CheckOut { get; set; }
        public TimeSpan TotalHoursWorked { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }
        public decimal TotalHours { get; set; }

        // Navigation property
        public Employee Employee { get; set; }

        public int DepartmentId { get; set; }
    }
}
