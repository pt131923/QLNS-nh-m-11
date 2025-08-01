using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public string Status { get; set; } // e.g., Present, Absent, Late, etc.
        public string Note { get; set; } // Additional notes for the time entry

        // Navigation property
        public EmployeeDto Employee { get; set; }

        public int DepartmentId { get; set; }

        public ICollection<TimeKeepingDto> TimeKeepings { get; set; }
    }
}