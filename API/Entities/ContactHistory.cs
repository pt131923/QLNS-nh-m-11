using System;

namespace API.Entities
{
    public class ContactHistory
    {
        public int Id { get; set; }

        public int ContactId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
