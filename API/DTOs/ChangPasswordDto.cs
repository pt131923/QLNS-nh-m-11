using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    // DTO: Data Transfer Object dùng để nhận dữ liệu từ Client cho thao tác đổi mật khẩu
    public class ChangePasswordDto
    {
        // Yêu cầu mật khẩu hiện tại để xác thực người dùng
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        public string CurrentPassword { get; set; }

        // Mật khẩu mới mà người dùng muốn đặt
        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, 
            ErrorMessage = "Mật khẩu mới phải dài từ 6 đến 100 ký tự.")]
        public string NewPassword { get; set; }

        // Trường xác nhận mật khẩu mới (nên được client gửi lên)
        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        [Compare(nameof(NewPassword), 
            ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string ConfirmNewPassword { get; set; }
    }
}