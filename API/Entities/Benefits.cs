using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
    public class Benefits
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set;}
        public string Name { get; set;}
        public decimal Cost { get; set; } = 0;
        public int EmployeeId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdatedAt { get; set;} = DateTime.Now;
    }
}