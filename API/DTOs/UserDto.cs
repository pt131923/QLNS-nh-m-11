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

        public string Role { get; set; }
        public string Image { get; set; }

        // FE dùng trực tiếp để hiển thị (có thể là /assets/... hoặc http://host/uploads/...)
        public string AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Gán giá trị mặc định
    }
}