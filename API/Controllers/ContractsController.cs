using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using AutoMapper;
using System.Linq;

namespace API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ContractsController(IContractRepository _contractRepository, IMapper _mapper, IMongoCollection<Contract> _contracts, IMongoCollection<Employee> _employees) : BaseApiController
{
    private readonly IMongoCollection<Contract> _contracts = _contracts;
    private readonly IMongoCollection<Employee> _employees = _employees;
    private readonly IContractRepository _contractRepository = _contractRepository;
    private readonly IMapper _mapper = _mapper;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractDto>>> GetContract()
    {
        var contracts = await _contractRepository.GetContractAsync();
        return Ok(contracts);
    }

    [HttpGet("{id}", Name = "GetContractById")]
    public async Task<ActionResult<ContractDto>> GetContractById(int id)
    {
        var contract = await _contractRepository.GetContractByIdAsync(id);
        if(contract == null){
            return NotFound();
        }

        return Ok(contract);
    }


    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateContract(ContractUpdateDto contractUpdateDto, int id)
    {
        var contract = await _contractRepository.GetContractByIdAsync(id);
        if(contract == null)
        {
            return NotFound("Contract does not exist");
        }

        _mapper.Map(contractUpdateDto, contract);
        contract.UpdateAt = DateTime.UtcNow;

        _contractRepository.Update(contract);

        if (await _contractRepository.SaveAllAsync())
        {
            return NoContent();
        }

        return BadRequest("Failed to update contract");
    }

    [HttpPost("add-contract")]
    public async Task<ActionResult<ContractDto>> AddContract([FromBody] ContractDto contractDto)
    {
        var contractExists = await _contracts.CountDocumentsAsync(
            Builders<Contract>.Filter.Regex(
                x => x.ContractName,
                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(contractDto.ContractName?.Trim() ?? string.Empty)}$", "i"))) > 0;

        if (contractExists)
        {
            return BadRequest("Contract name already exists");
        }

        // Kiểm tra employee có tồn tại không
        var employeeExists = await _employees.CountDocumentsAsync(
            Builders<Employee>.Filter.Regex(
                x => x.EmployeeName,
                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(contractDto.EmployeeName?.Trim() ?? string.Empty)}$", "i"))) > 0;

        if (!employeeExists)
        {
            return BadRequest("Employee does not exist");
        }

        // Map DTO sang entity
        var contract = _mapper.Map<Contract>(contractDto);
        var emp = await _employees.Find(
            Builders<Employee>.Filter.Regex(
                x => x.EmployeeName,
                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(contractDto.EmployeeName?.Trim() ?? string.Empty)}$", "i")))
            .FirstOrDefaultAsync();

        if (emp != null)
        {
            contract.EmployeeId = emp.EmployeeId;
        }

        if (contract.CreateAt == default) contract.CreateAt = DateTime.UtcNow;
        contract.UpdateAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(contract.Status)) contract.Status = "Hiệu lực";

        _contractRepository.Add(contract);
        await _contractRepository.SaveAllAsync();

        return CreatedAtRoute("GetContractById", new { id = contract.ContractId }, contract);
    }

    [HttpDelete("delete-contract/{id}")]
    public async Task<ActionResult> DeleteContract(int id)
    {
        var contract = await _contractRepository.GetContractByIdAsync(id);

        if (contract == null) return NotFound();

        _contractRepository.Delete(contract);

        if (await _contractRepository.SaveAllAsync()) return Ok();

        return BadRequest("Failed to delete the contract");
    }


    [HttpPost("convert-to-leave/{id}")]
    public async Task<ActionResult> ConvertContractToLeave(int id, [FromServices] ILeaveRepository leaveRepo)
    {
        var contract = await _contractRepository.GetContractByIdAsync(id);
        if (contract == null) return NotFound("Contract does not exist");

        var leave = new Leave
        {
            EmployeeId = contract.EmployeeId ?? contract.ContractId,
            EmployeeName = contract.EmployeeName,
            LeaveType = "Nghỉ việc / Chấm dứt hợp đồng",
            StartDate = contract.EndDate != default ? contract.EndDate.AddDays(1) : DateTime.UtcNow,
            ContractEndDate = contract.EndDate,
            Status = "Đã duyệt",
            Note = $"Chuyển từ hợp đồng {contract.ContractName} ({contract.ContractType})"
        };

        await leaveRepo.AddLeave(leave);
        await leaveRepo.SaveAllAsync();

        contract.Status = "Đã thanh lý";
        contract.UpdateAt = DateTime.UtcNow;
        _contractRepository.Update(contract);
        await _contractRepository.SaveAllAsync();

        return Ok(new { message = "Đã chuyển hợp đồng sang danh sách nghỉ phép / thôi việc thành công!", leave });
    }

    [HttpGet("contracts-with-employees")]
    public async Task<ActionResult<IEnumerable<ContractWithEmployeeDto>>> GetContractsWithEmployees()
    {
        var contracts = await _contractRepository.GetContractAsync();
        var dtos = contracts.Select(_mapper.Map<ContractWithEmployeeDto>).ToList();
        return Ok(dtos);
    }
}
