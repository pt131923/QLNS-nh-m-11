using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly DataContext _context;

        public ContractRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Contract>> GetContractAsync()
        {
            return await _context.Contract.Include(c => c.Employee).ToListAsync();
        }

        public async Task<Contract> GetContractByIdAsync(int id)
        {
            return await _context.Contract.Include(c => c.Employee).FirstOrDefaultAsync(c => c.ContractId == id);
        }

        public void Update(Contract contract)
        {
            _context.Contract.Update(contract);
        }

        public void Delete(Contract contract)
        {
            _context.Contract.Remove(contract);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
