using API.Data;
using API.Entities;
using API.Interfaces;
using API.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Data
{
    public class ContractRepository(DataContext _context, IDashboardService _dashboardService) : IContractRepository
    {

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
            var result = await _context.SaveChangesAsync() > 0;

            // Trigger cập nhật dashboard nếu có thay đổi dữ liệu hợp đồng
            if (result)
            {
                await _dashboardService.NotifyDataChangedWithCheckAsync();
            }

            return result;
        }
    }
}
