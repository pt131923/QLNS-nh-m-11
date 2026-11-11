namespace API.DTOs
{
    public class BatchDepartmentUpdateDto
    {
        public List<int> EmployeeIds { get; set; } = new();
        public int NewDepartmentId { get; set; }
    }
}
