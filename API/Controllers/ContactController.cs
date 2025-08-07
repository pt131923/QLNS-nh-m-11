using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController(IContactRepository _contactRepository, AutoMapper.IMapper _mapper) : ControllerBase
    {

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

        // GET api/contact/error
        [HttpGet("error")]
        public IActionResult Error()
        {
            return Problem("An error occurred");
        }
    }
}
