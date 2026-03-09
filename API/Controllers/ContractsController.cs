using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Linq;

namespace API.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController(IContractRepository _contractRepository, AutoMapper.IMapper _mapper, IMongoDatabase _db) : BaseApiController
    {
        private readonly IMongoCollection<Contract> _contracts = _db.GetCollection<Contract>("Contracts");
        private readonly IMongoCollection<Employee> _employees = _db.GetCollection<Employee>("Employees");

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractDto>>> GetContracts()
        {
            var contracts = await _contractRepository.GetContractAsync();
            return Ok(contracts);
        }

        [HttpGet("{id}", Name = "GetContractById")]
        public async Task<ActionResult<ContractDto>> GetContractById(int id)
        {
            var contract = await _contractRepository.GetContractByIdAsync(id);

            if (contract == null)
            {
                return NotFound();
            }

            return Ok(contract);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateContract(ContractUpdateDto contractUpdateDto, int id)
        {
            var contract = await _contractRepository.GetContractByIdAsync(id);

            if (contract == null)
            {
                return NotFound();
            }

            _mapper.Map(contractUpdateDto, contract);

            _contractRepository.Update(contract);

            if(await _contractRepository.SaveAllAsync())
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

            _contractRepository.Add(contract);
            await _contractRepository.SaveAllAsync();

            // Trả về contract vừa tạo
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

        [HttpGet("contracts-with-employees")]
        public async Task<ActionResult<IEnumerable<ContractWithEmployeeDto>>> GetContractsWithEmployees()
        {
            var contracts = await _contractRepository.GetContractAsync();
            var dtos = contracts.Select(c => _mapper.Map<ContractWithEmployeeDto>(c)).ToList();
            return Ok(dtos);
        }
    }
}
