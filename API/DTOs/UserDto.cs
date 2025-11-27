namespace API.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        
        // Sửa lỗi kiểu dữ liệu: Địa chỉ phải là chuỗi
        public string Address { get; set; } 

        // Thêm các trường quan trọng khác (Tuỳ chọn)
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Gán giá trị mặc định
    }
}