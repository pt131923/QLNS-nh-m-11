using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class ContactRepository(DataContext _context) : IContactRepository
    {
        public async Task<IEnumerable<Contact>> GetAllContactsAsync()
        {
            return await _context.Contact.ToListAsync();
        }

        public async Task<Contact> GetContactByIdAsync(int id)
        {
            return await _context.Contact.FindAsync(id);
        }

        public async Task UpdateContactAsync(Contact contact)
        {
            _context.Contact.Update(contact);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(int id)
        {
            var contact = await _context.Contact.FindAsync(id);
            if (contact != null)
            {
                _context.Contact.Remove(contact);
                await _context.SaveChangesAsync();
            }
        }

        public Task<IEnumerable<string>> GetContactsAsync()
        {
            throw new NotImplementedException();
        }

        Task<string> IContactRepository.GetContactByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContactExistsAsync(string contactName)
        {
            throw new NotImplementedException();
        }

        public void AddContact(string contact)
        {
            var newContact = new Contact { Name = contact };
            _context.Contact.Add(newContact);
            _context.SaveChanges();
        }
        public async Task<bool> AddContactAsync(Contact contact)
        {
            await _context.Contact.AddAsync(contact);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveContactAsync(string contactName)
        {
            var newContact = new Contact { Name = contactName };
            await _context.Contact.AddAsync(newContact);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Contact>> GetAllAsync()
        {
            return await _context.Contact.ToListAsync();
        }
    }
}