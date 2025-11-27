using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController(IContactRepository _contactRepository, AutoMapper.IMapper _mapper) : ControllerBase
    {

        // GET api/contact
        [HttpGet]
        public async Task<IActionResult> GetContacts()
        {
            var contacts = await _contactRepository.GetAllAsync();
            return Ok(contacts);
        }

        // GET api/contact/list
        [HttpGet("list")]
        public async Task<IActionResult> GetContact()
        {
            var contacts = await _contactRepository.GetAllAsync();
            return Ok(contacts);
        }

        // POST api/contact
        [HttpPost]
        public async Task<IActionResult> CreateContact(ContactDto contactDto)
        {
            if (contactDto == null)
            {
                return BadRequest("Contact data is null");
            }

            var contact = _mapper.Map<Contact>(contactDto);
            _ = await _contactRepository.AddContactAsync(contact);

            if (await _contactRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Failed to save contact");
        }

        // GET api/contact/history
        [HttpGet("history")]
        public async Task<IActionResult> GetContactHistory()
        {
            var contacts = await _contactRepository.GetAllAsync();
            return Ok(contacts);
        }

        // GET api/contact/error
        [HttpGet("error")]
        public IActionResult Error()
        {
            return Problem("An error occurred");
        }
    }
}

