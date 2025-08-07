using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
    public interface IContactRepository
    {
        Task<IEnumerable<string>> GetContactsAsync();
        Task<string> GetContactByIdAsync(int id);
        Task<bool> SaveContactAsync(string contact);
        Task<bool> ContactExistsAsync(string contactName);
        Task<bool> AddContactAsync(Contact contact);
        Task<bool> SaveAllAsync();
        Task<IEnumerable<Contact>> GetAllAsync();
    }
}