using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
    public class Recuiment
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set;}
        public string CustomerName { get; set; }
        public decimal Amount { get; set; } = 0;
        public int OpeningsCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdatedAt { get; set;} = DateTime.Now;
    }
}