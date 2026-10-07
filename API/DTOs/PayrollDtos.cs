namespace API.DTOs
{
    public class PayrollCalculateRequest
    {
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal? BasicSalary { get; set; }
        public decimal? Allowance { get; set; }
        public decimal? InsuranceSalary { get; set; }
        public decimal? WorkedDays { get; set; }
        public decimal? StandardDays { get; set; }
        public decimal? UnpaidLeaveDays { get; set; }
        public decimal? OvertimeHours { get; set; }
        public decimal? Bonus { get; set; }
        public decimal? OtherDeductions { get; set; }
        public int? DependentCount { get; set; }
        public bool DeductUnionFee { get; set; }
        public bool Save { get; set; }
    }

    public class PayrollGenerateMonthRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int DependentCount { get; set; }
        public bool DeductUnionFee { get; set; }
    }
}
