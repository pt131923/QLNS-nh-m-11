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
        public IActionResult GetLeaveList()
        {
            var leaves = _leaveRepository.GetAllLeavesAsync();
            return Ok(leaves);
        }

        [HttpGet("by id")]
        public IActionResult GetLeaveById(int id)
        {
            var leave = _leaveRepository.GetLeaveByUserId(id);
            return Ok(leave);
        }

        [HttpPost]
        public IActionResult CreateLeave([FromBody] Leave leave)
        {
            if (leave == null)
            {
                return BadRequest("Leave data is null");
            }
            _leaveRepository.AddLeave(leave);
            return CreatedAtAction(nameof(GetLeaveById), new { id = leave.LeaveId }, leave);
        }
    }
}