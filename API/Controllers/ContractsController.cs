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
    public class ContractsController(DataContext context, IContractRepository ContractRepository, AutoMapper.IMapper mapper) : BaseApiController
    {
        private readonly DataContext _context = context;
        private readonly IContractRepository _contractRepository = ContractRepository;
        private readonly AutoMapper.IMapper _mapper = mapper;

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

            if(await _contractRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update contract");
        }

        [HttpPost("add-contract")]
        public async Task<ActionResult<ContractDto>> AddContract(ContractDto contractDto)
        {
            if (await ContractExists(contractDto.ContractName))
                return BadRequest("Contract name already exists");

            var employeeExists = await _context.Employee.AnyAsync(e => e.EmployeeName == contractDto.EmployeeName);
            if (!employeeExists)
                return BadRequest("Employee does not exist");

            var contract = new Contract
            {
                ContractName = contractDto.ContractName.ToLower(),
                ContractType = contractDto.ContractType,
                EmployeeName = contractDto.EmployeeName,
                StartDate = contractDto.StartDate,
                EndDate = contractDto.EndDate,
                BasicSalary = contractDto.BasicSalary,
                Allowance = contractDto.Allowance,
                CreateAt = contractDto.CreateAt,
                UpdateAt = contractDto.UpdateAt,
                JobDescription = contractDto.JobDescription,
                ContractTerm = contractDto.ContractTerm,
                WorkLocation = contractDto.WorkLocation,
                Leaveofabsence = contractDto.Leaveofabsence
            };

            _context.Contract.Add(contract);
            await _context.SaveChangesAsync();

            return new ContractDto
            {
                ContractId = contract.ContractId,
                ContractName = contract.ContractName,
                EmployeeName = contract.EmployeeName,
                StartDate = contract.StartDate,
                EndDate = (DateTime)contract.EndDate,
                ContractType = contract.ContractType,
                BasicSalary = contract.BasicSalary,
                Allowance = contract.Allowance,
                CreateAt = contract.CreateAt,
                UpdateAt = contract.UpdateAt,
                JobDescription = contract.JobDescription,
                ContractTerm = contract.ContractTerm,
                WorkLocation = contract.WorkLocation,
                Leaveofabsence = contract.Leaveofabsence
            };
        }

        private async Task<bool> ContractExists(string contractName)
        {
            return await _context.Contract.AnyAsync(x => x.ContractName == contractName.ToLower());
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
                .Select(c => new ContractWithEmployeeDto
                {
                    ContractId = c.ContractId,
                    ContractName = c.ContractName,
                    EmployeeName = c.Employee.EmployeeName,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    ContractType = c.ContractType,
                    BasicSalary = c.BasicSalary,
                    Allowance = c.Allowance,
                    CreateAt = c.CreateAt,
                    UpdateAt = c.UpdateAt,
                    JobDescription = c.JobDescription,
                    ContractTerm = c.ContractTerm,
                    WorkLocation = c.WorkLocation,
                    Leaveofabsence = c.Leaveofabsence
                })
                .ToListAsync();

            return Ok(contracts);
        }
    }
}
