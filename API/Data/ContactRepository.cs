using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using API.Services;
using MongoDB.Driver;

namespace API.Data
{
    public class ContactRepository : IContactRepository
    {
        private readonly IMongoCollection<Contact> _contacts;
        private readonly IDashboardService _dashboardService;
        private readonly IMongoIdGenerator _idGenerator;

        public ContactRepository(
            IMongoDatabase database,
            IDashboardService dashboardService,
            IMongoIdGenerator idGenerator)
        {
            _contacts = database.GetCollection<Contact>("Contacts");
            _dashboardService = dashboardService;
            _idGenerator = idGenerator;
        }

        // Legacy interface methods (string-based) - giữ để không break compile
        public Task<IEnumerable<string>> GetContactsAsync() =>
            Task.FromResult<IEnumerable<string>>(new List<string>());

        public Task<string> GetContactByIdAsync(int id) =>
            Task.FromResult<string>(null);

        public async Task<bool> SaveContactAsync(string contact)
        {
            var c = new Contact { ContactId = await _idGenerator.NextAsync("Contacts"), Name = contact };
            await _contacts.InsertOneAsync(c);
            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }

        public async Task<bool> ContactExistsAsync(string contactName)
        {
            if (string.IsNullOrWhiteSpace(contactName)) return false;
            var filter = Builders<Contact>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(contactName.Trim())}$", "i"));
            var count = await _contacts.CountDocumentsAsync(filter);
            return count > 0;
        }

        public async Task<bool> AddContactAsync(Contact contact)
        {
            if (contact.ContactId == 0)
                contact.ContactId = await _idGenerator.NextAsync("Contacts");
            if (string.IsNullOrWhiteSpace(contact.Status))
                contact.Status = "Pending";

            await _contacts.InsertOneAsync(contact);
            return true;
        }

        public async Task<bool> SaveAllAsync()
        {
            // Insert/Update/Delete đã thực hiện trực tiếp; ở đây chỉ trigger dashboard refresh
            await _dashboardService.NotifyDataChangedWithCheckAsync();
            return true;
        }

        public async Task<IEnumerable<Contact>> GetAllAsync()
        {
            return await _contacts.Find(_ => true).ToListAsync();
        }
    }
}