using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;

namespace API.Data
{
    public class UserRepository
    {
        // This class would typically contain methods to interact with the User data,
        // such as getting users, adding new users, updating user information, etc.
        // For now, we can leave it empty or implement basic methods as needed.
        public Task<UserDto> GetUserByIdAsync(int userId)
        {
            throw new NotImplementedException();
        }
        public Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            throw new NotImplementedException();
        }
        public Task AddUserAsync(UserDto user)
        {
            throw new NotImplementedException();
        }
        public Task UpdateUserAsync(UserDto user)
        {
            throw new NotImplementedException();
        }
        public Task DeleteUserAsync(int userId)
        {
            throw new NotImplementedException();
        }
    }
}