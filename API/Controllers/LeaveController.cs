using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Interfaces;
using API.Entities;

namespace API.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveRepository _leaveRepository;
        public LeaveController(ILeaveRepository leaveRepository)
        {
            _leaveRepository = leaveRepository;
        }
        [HttpGet("list")]
        public async Task<IActionResult> GetLeaveList()
        {
            var leaves = await _leaveRepository.GetAllLeavesAsync();
            return Ok(leaves);
        }

        [HttpGet("by id")]
        public async Task<IActionResult> GetLeaveById(int id)
        {
            var leave = await _leaveRepository.GetLeaveByUserId(id);
            return Ok(leave);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLeave([FromBody] Leave leave)
        {
            if (leave == null)
            {
                return BadRequest("Leave data is null");
            }
            await _leaveRepository.AddLeave(leave);
            return CreatedAtAction(nameof(GetLeaveById), new { id = leave.LeaveId }, leave);
        }
    }
}