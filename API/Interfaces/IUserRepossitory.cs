using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        // Lấy tất cả người dùng, ánh xạ sang DTO
        Task<IEnumerable<UserDto>> GetUsersAsync();
        
        // Lấy người dùng theo ID, ánh xạ sang DTO
        Task<UserDto> GetUserByIdAsync(int userId);
        
        // Lấy người dùng theo UserName, ánh xạ sang DTO
        Task<UserDto> GetUserByNameAsync(string userName);
        
        // Kiểm tra sự tồn tại của người dùng
        Task<bool> UserExistsAsync(string userName);
        
        // Thêm người dùng mới
        Task<bool> AddUserAsync(UserDto userDto);
        
        // Cập nhật thông tin người dùng
        Task<bool> UpdateUserAsync(UserDto userDto);
        
        // Xoá người dùng
        Task<bool> DeleteUserAsync(int userId);
        
        // Lưu các thay đổi vào cơ sở dữ liệu
        Task<bool> SaveChangesAsync();
    }
}