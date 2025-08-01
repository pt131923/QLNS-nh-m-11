using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class TimeKeeping
    {
        public int TimeKeepingId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public TimeSpan TotalHoursWorked { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }

        // Navigation property
        public Employee Employee { get; set; }

        public int DepartmentId { get; set; }

        public ICollection<TimeKeeping> TimeKeepings { get; set; }
    }
}