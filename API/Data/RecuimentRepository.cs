using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class RecuimentRepository
    {
        private readonly DataContext _context;

        public RecuimentRepository(DataContext context)
        {
            _context = context;
        }

        // Lấy danh sách Recuiment
        public async Task<IEnumerable<Recuiment>> GetRecuimentsAsync()
        {
            return await _context.Recuiment
                .ToListAsync();
        }

        // Lấy Recuiment theo ID
        public async Task<Recuiment> GetRecuimentByIdAsync(int id)
        {
            return await _context.Recuiment
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        // Thêm
        public void Add(Recuiment recuiment)
        {
            _context.Recuiment.Add(recuiment);
        }

        // Cập nhật
        public void Update(Recuiment recuiment)
        {
            _context.Entry(recuiment).State = EntityState.Modified;
        }

        // Xóa
        public void Delete(Recuiment recuiment)
        {
            _context.Recuiment.Remove(recuiment);
        }

        // Lưu thay đổi
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
