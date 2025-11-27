using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using AutoMapper; // Cần thêm using này nếu IMapper không được resolve

namespace API.Data
{
    // Sử dụng primary constructor cho DataContext và IMapper
    public class UserRepository(DataContext _context, AutoMapper.IMapper _mapper) : IUserRepository
    {
        // GET: Lấy tất cả người dùng và ánh xạ sang UserDto
        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            return await _context.User
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        // GET: Lấy người dùng theo UserName
        public Task<UserDto> GetUserByNameAsync(string userName)
        {
            return _context.User
                .Where(x => x.UserName == userName)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        // GET: Lấy người dùng theo UserId
        public Task<UserDto> GetUserByIdAsync(int userId)
        {
            return _context.User
                .Where(x => x.UserId == userId)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        // CHECK: Kiểm tra sự tồn tại của người dùng
        public Task<bool> UserExistsAsync(string userName)
        {
            return _context.User
                .AnyAsync(x => x.UserName == userName);
        }
        
        // CREATE: Thêm người dùng mới
        public async Task<bool> AddUserAsync(UserDto userDto)
        {
            var user = _mapper.Map<User>(userDto);
            _context.User.Add(user);
            return await SaveChangesAsync(); // Tái sử dụng SaveChangesAsync
        }

        // UPDATE: Cập nhật thông tin người dùng
        public async Task<bool> UpdateUserAsync(UserDto userDto)
        {
            var user = await _context.User.FindAsync(userDto.UserId);
            if (user == null) return false;

            // Dùng AutoMapper để ánh xạ các thuộc tính từ DTO sang Entity đã được tracking
            _mapper.Map(userDto, user); 
            
            // EF Core sẽ tự động đánh dấu entity là Modified khi SaveChanges được gọi
            return await SaveChangesAsync();
        }

        // DELETE: Xóa người dùng
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;

            _context.User.Remove(user);
            return await SaveChangesAsync(); // Tái sử dụng SaveChangesAsync
        }

        // Lưu các thay đổi vào DB
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}