using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserDto>> GetUsersAsync();
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> GetUserByNameAsync(string userName);

        // 🔥 Thêm mới — dùng cho login
        Task<User> GetUserEntityByUsernameAsync(string username);

        Task<bool> UserExistsAsync(string userName);
        Task<bool> AddUserAsync(UserDto userDto);
        Task<bool> UpdateUserAsync(UserDto userDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> SaveChangesAsync();
    }
}
