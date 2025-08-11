using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository(DataContext _context, AutoMapper.IMapper _mapper) : IUserRepository
    {

        Task<IEnumerable<UserDto>> IUserRepository.GetUsersAsync()
        {
            return _context.User
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync()
                .ContinueWith(task => task.Result.AsEnumerable());
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

        public Task<bool> UserExistsAsync(string userName)
        {
            return _context.User
                .AnyAsync(x => x.UserName == userName);
        }

        public Task<bool> SaveUserAsync(UserDto userDto)
        {
            throw new NotImplementedException();
        }

        async Task<bool> IUserRepository.DeleteUserAsync(int userId)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;

            _context.User.Remove(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateUserAsync(UserDto userDto)
        {
            var user = await _context.User.FindAsync(userDto.UserId);
            if (user == null) return false;

            _mapper.Map(userDto, user);
            return await _context.SaveChangesAsync() > 0;
        }

        async Task<bool> IUserRepository.AddUserAsync(UserDto userDto)
        {
            var user = _mapper.Map<User>(userDto);
            _context.User.Add(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}