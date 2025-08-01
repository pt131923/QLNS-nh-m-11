namespace API.DTOs
{
    public class EmployeeSeedDto
    {
        public string EmployeeName { get; set; }
        public string EmployeeEmail { get; set; }
        public string EmployeePhone { get; set; }
        public string EmployeeAddress { get; set; }

        public string EmployeeInformation { get; set; }
        public DateTime? BirthDate { get; set; }
        public string PlaceOfBirth { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string IdentityNumber { get; set; }
        public DateTime? IdentityIssuedDate { get; set; }
        public string IdentityIssuedPlace { get; set; }
        public string Religion { get; set; }
        public string Ethnicity { get; set; }
        public string Nationality { get; set; }
        public string EducationLevel { get; set; }
        public string Specialization { get; set; }

        public DepartmentDto Department { get; set; } // tên phòng ban (ví dụ: "Phòng Kế toán")

        public int DepartmentId { get; set; } // ID của phòng ban (ví dụ: "1", "2", "3")
    }
}
