using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class WorkLog
    {
        public int WorkLogId { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public int HoursWorked { get; set; }
        public string Description { get; set; }
    }
}