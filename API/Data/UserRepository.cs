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
    public class UserRepository(DataContext _context, AutoMapper.IMapper _mapper) : IUserRepossitory
    {
        public Task<UserDto> GetUserByIdAsync(int userId)
        {
            return _context.User
                .Where(x => x.UserId == userId)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }
        public Task<List<UserDto>> GetAllUsersAsync()
        {
            return _context.User
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        public Task AddUserAsync(UserDto user)
        {
            var newUser = _mapper.Map<User>(user);
            _context.User.Add(newUser);
            return _context.SaveChangesAsync();
        }
        public Task UpdateUserAsync(UserDto user)
        {
            var existingUser = _context.User.Find(user.UserId);
            if (existingUser != null)
            {
                _mapper.Map(user, existingUser);
                return _context.SaveChangesAsync();
            }
            return Task.CompletedTask;
        }
        public Task DeleteUserAsync(int userId)
        {
            var user = _context.User.Find(userId);
            if (user != null)
            {
                _context.User.Remove(user);
                return _context.SaveChangesAsync();
            }
            return Task.CompletedTask;
        }

        public Task<List<UserDto>> GetUsersAsync()
        {
            return _context.User
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}