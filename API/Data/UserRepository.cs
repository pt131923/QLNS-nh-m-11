using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace API.Data
{
    public class UserRepository(DataContext _context, AutoMapper.IMapper _mapper) : IUserRepository
    {
        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            return await _context.User
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public Task<UserDto> GetUserByNameAsync(string userName)
        {
            return _context.User
                .Where(x => x.UserName == userName)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public Task<UserDto> GetUserByIdAsync(int userId)
        {
            return _context.User
                .Where(x => x.UserId == userId)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        // 🔥 Hàm quan trọng — dùng cho Login
        public async Task<User> GetUserEntityByUsernameAsync(string username)
        {
            return await _context.User.SingleOrDefaultAsync(x => x.UserName == username);
        }

        public Task<bool> UserExistsAsync(string userName)
        {
            return _context.User.AnyAsync(x => x.UserName == userName);
        }

        public async Task<bool> AddUserAsync(UserDto userDto)
        {
            var user = _mapper.Map<User>(userDto);
            _context.User.Add(user);
            return await SaveChangesAsync();
        }

        public async Task<bool> UpdateUserAsync(UserDto userDto)
        {
            var user = await _context.User.FindAsync(userDto.UserId);
            if (user == null) return false;

            _mapper.Map(userDto, user);
            return await SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;

            _context.User.Remove(user);
            return await SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
