using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
    public class Leave
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int LeaveId { get; set;}
        public int EmployeeId { get; set; }
        public string EmployeeName{ get; set; }
        public string LeaveType{get; set; }
        public DateTime StartDate{get; set; }
        
        public DateTime ContractEndDate { get; set; }
        public string Note { get; set; }

        public string Status { get; set; }
    }
}