using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class Recuiment
    {
        public int Id { get; set;}
        public string CustomerName { get; set; }
        public decimal Amount { get; set; } = 0;
        public int OpeningsCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdatedAt { get; set;} = DateTime.Now;
    }
}