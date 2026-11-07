using System;
using System.Collections.Generic;

namespace API.DTOs
{
    public class TimeKeepingDto
    {
        public int TimeKeepingId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public TimeSpan TotalHoursWorked { get; set; }
        public decimal TotalHours { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }

        public EmployeeDto Employee { get; set; }
        public int DepartmentId { get; set; }
        public ICollection<TimeKeepingDto> TimeKeepings { get; set; }
    }
}
