using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    public class TimeKeepingController : ControllerBase
    {
        private readonly ITimeKeepingRepository _timeKeepingService;

        public TimeKeepingController(ITimeKeepingRepository timeKeepingService)
        {
            _timeKeepingService = timeKeepingService;
        }

        [HttpGet("api/timekeeping/{employeeId}")]
        public async Task<IActionResult> GetTimeEntriesForUser(string employeeId)
        {
            var timeEntries = await _timeKeepingService.GetTimeEntriesForUser(employeeId);
            return Ok(timeEntries);
        }

        [HttpPost("api/timekeeping")]
        public async Task<IActionResult> CreateTimeEntry([FromBody] TimeKeepingDto timeKeepingDto)
        {
            var createdEntry = await _timeKeepingService.CreateTimeEntry(timeKeepingDto);
            return CreatedAtAction(nameof(GetTimeEntriesForUser), new { employeeId = timeKeepingDto.EmployeeId }, createdEntry);
        }
    }
}