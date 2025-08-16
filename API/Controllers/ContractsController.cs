using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractsController(DataContext _context, IContractRepository _contractRepository, AutoMapper.IMapper _mapper) : BaseApiController
    {
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
            if (await ContractExists(contractDto.ContractName))
                return BadRequest("Contract name already exists");

            var employeeExists = await _context.Employee.AnyAsync(e => e.EmployeeName == contractDto.EmployeeName);
            if (!employeeExists)
                return BadRequest("Employee does not exist");

            var contract = _mapper.Map<Contract>(contractDto);

            _context.Contract.Add(contract);
            await _context.SaveChangesAsync();

            return Ok(contract);
        }

        private async Task<bool> ContractExists(string contractName)
        {
            return await _context.Contract.AnyAsync(c => c.ContractName.Equals(contractName, StringComparison.CurrentCultureIgnoreCase));
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
            var contracts = await _context.Contract
                .Include(c => c.Employee)
                .Select(c => _mapper.Map<ContractWithEmployeeDto>(c))
                .ToListAsync();

            return Ok(contracts);
        }

        private async Task<bool> EmployeeExists(string employeeName)
        {
            return await _context.Employee
                .AnyAsync(e => e.EmployeeName == employeeName);
        }
    }
}
