using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Interfaces;
using API.Entities;
using System.Diagnostics.Contracts;

namespace API.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController(ILeaveRepository _leaveRepository, IContractRepository _contractRepository) : BaseApiController
        {

        [HttpGet("list")]
        public async Task<IActionResult> GetLeaveList()
        {
            var leaves = await _leaveRepository.GetAllLeavesAsync();
            return Ok(leaves);
        }

        [HttpGet("by-id")]
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

        [HttpPost("convert-expired-contracts")]
        public async Task<IActionResult> ConvertExpiredContracts()
        {
            var contracts = await _contractRepository.GetContractAsync();
            var allLeaves = await _leaveRepository.GetAllLeavesAsync();
            var createList = new List<Leave>();
            var today = DateTime.Now.Date;

            //check xem hop dong co ngay het hạn co k 
            foreach(var contract in contracts)
            {

           // ✅ Bỏ qua nếu chưa hết hạn — VẪN BÊN TRONG foreach
            if (contract.EndDate.Date >= today)
                    continue;

            bool exits = false;
            foreach( var lv in allLeaves)
            {
                if(lv.EmployeeId == contract.ContractId && lv.ContractEndDate == contract.EndDate)
                {
                    exits = true;
                    break;
                }

                if(exits) continue;

                var newLeave = new Leave
                {
                    EmployeeId = contract.ContractId,
                    EmployeeName = contract.EmployeeName,
                    LeaveType = "Het hop dong",
                    StartDate = contract.EndDate.AddDays(1),
                    ContractEndDate = contract.EndDate,
                    Status = "Da xac nhan",
                    Note = $"Tu chuyen hop dong : {contract.ContractName}"
                };

                await _leaveRepository.AddLeave(newLeave);
                createList.Add(newLeave);
            }

            if(createList.Count > 0)
            {
                if(await _leaveRepository.SaveAllAsync())
                {
                    return Ok(new
                    {
                        Message = $"Đã chuyển {createList.Count} hợp đồng hết hạn",
                        Data = createList
                    });
                }
                    return BadRequest("Lỗi lưu bản ghi nghỉ phép");
                }
		   }
           return Ok(new { Message = "Không có hợp đồng hết hạn cần chuyển" });
	    }
    }
}