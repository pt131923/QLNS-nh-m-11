using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class TimeKeepingController(ITimeKeepingRepository _timekeepingRepository) : ControllerBase
    {

        [HttpGet("{employeeId}")]
        public async Task<IActionResult> GetTimeEntriesForUser(string employeeId)
        {
            var timeEntries = await _timekeepingRepository.GetTimeEntriesForUser(employeeId);
            return Ok(timeEntries);
        }

        [HttpPost("api/timekeeping")]
        public async Task<IActionResult> CreateTimeEntry([FromBody] TimeKeepingDto timeKeepingDto)
        {
            var createdEntry = await _timekeepingRepository.CreateTimeEntry(timeKeepingDto);
            return CreatedAtAction(nameof(GetTimeEntriesForUser), new { employeeId = timeKeepingDto.EmployeeId }, createdEntry);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTimeEntry(int id, [FromBody] TimeKeepingDto timeKeepingDto)
        {
            if (id != timeKeepingDto.TimeKeepingId)
            {
                return BadRequest("ID mismatch");
            }

            var updatedEntry = await _timekeepingRepository.UpdateTimeEntry(timeKeepingDto);
            if (updatedEntry == null)
            {
                return NotFound();
            }

            return Ok(updatedEntry);
        }
    }
}