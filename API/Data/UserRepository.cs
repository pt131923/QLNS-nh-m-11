using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using MongoDB.Driver;
using IMapper = AutoMapper.IMapper;

namespace API.Data
{
    /// <summary>
    /// UserRepository sử dụng MongoDB thay cho EF Core.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMapper _mapper;

        public UserRepository(IMongoDatabase database, IMapper mapper)
        {
            _users = database.GetCollection<User>("Users");
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            var users = await _users.Find(_ => true).ToListAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> GetUserByNameAsync(string userName)
        {
            var user = await _users.Find(x => x.UserName == userName).FirstOrDefaultAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _users.Find(x => x.UserId == userId).FirstOrDefaultAsync();
            return _mapper.Map<UserDto>(user);
        }

        // 🔥 Hàm quan trọng — dùng cho Login
        public async Task<User> GetUserEntityByUsernameAsync(string username)
        {
            return await _users.Find(x => x.UserName == username).FirstOrDefaultAsync();
        }

        public async Task<bool> UserExistsAsync(string userName)
        {
            var count = await _users.CountDocumentsAsync(x => x.UserName == userName);
            return count > 0;
        }

        public async Task<bool> AddUserAsync(UserDto userDto)
        {
            var user = _mapper.Map<User>(userDto);

            // Tự tăng UserId đơn giản (nếu bạn vẫn dùng int)
            if (user.UserId == 0)
            {
                var lastUser = await _users.Find(_ => true)
                    .SortByDescending(u => u.UserId)
                    .FirstOrDefaultAsync();
                user.UserId = lastUser != null ? lastUser.UserId + 1 : 1;
            }

            await _users.InsertOneAsync(user);
            return true;
        }

        public async Task<bool> UpdateUserAsync(UserDto userDto)
        {
            var existing = await _users.Find(u => u.UserId == userDto.UserId).FirstOrDefaultAsync();
            if (existing == null) return false;

            _mapper.Map(userDto, existing);
            var result = await _users.ReplaceOneAsync(u => u.UserId == existing.UserId, existing);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var result = await _users.DeleteOneAsync(u => u.UserId == userId);
            return result.DeletedCount > 0;
        }

        public Task<bool> SaveChangesAsync()
        {
            // MongoDB không có SaveChanges, các thao tác đã ghi trực tiếp.
            return Task.FromResult(true);
        }
    }
}
