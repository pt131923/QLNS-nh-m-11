using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;

namespace API.Interfaces
{
    public interface IUserRepossitory
    {
        Task<IEnumerable<UserDto>> GetUsersAsync();
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> GetUserByNameAsync(string userName);
        Task<bool> UserExistsAsync(string userName);
        Task<bool> SaveUserAsync(UserDto userDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UpdateUserAsync(UserDto userDto);
        Task<bool> AddUserAsync(UserDto userDto);
        Task<bool> SaveChangesAsync();
    }
}