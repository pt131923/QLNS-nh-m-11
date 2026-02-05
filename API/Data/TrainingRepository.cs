using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class TrainingRepository
    {
        private readonly DataContext _context;

        public TrainingRepository(DataContext context)
        {
            _context = context;
        }

        // Lấy danh sách Training
        public async Task<IEnumerable<Training>> GetTrainingsAsync()
        {
            return await _context.Training
                .ToListAsync();
        }

        // Lấy Training theo ID
        public async Task<Training> GetTrainingByIdAsync(int id)
        {
            return await _context.Training
                .SingleOrDefaultAsync(x => x.TrainingId == id);
        }

        // Thêm
        public void Add(Training training)
        {
            _context.Training.Add(training);
        }

        // Cập nhật
        public void Update(Training training)
        {
            _context.Entry(training).State = EntityState.Modified;
        }

        // Xóa
        public void Delete(Training training)
        {
            _context.Training.Remove(training);
        }

        // Lưu thay đổi
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}