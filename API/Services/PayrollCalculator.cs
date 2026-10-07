namespace API.Services
{
    /// <summary>
    /// Công thức lương Gross → Net theo thực tế HR/kế toán Việt Nam
    /// (BHXH–BHYT–BHTN, giảm trừ gia cảnh, thuế TNCN lũy tiến từng phần).
    /// </summary>
    public static class PayrollCalculator
    {
        public const decimal BhxhEmployeeRate = 0.08m;
        public const decimal BhytEmployeeRate = 0.015m;
        public const decimal BhtnEmployeeRate = 0.01m;
        public const decimal UnionEmployeeRate = 0.01m;

        public const decimal BhxhEmployerRate = 0.175m;
        public const decimal BhytEmployerRate = 0.03m;
        public const decimal BhtnEmployerRate = 0.01m;
        public const decimal UnionEmployerRate = 0.02m;

        public const decimal BaseWage = 2_340_000m;
        public const decimal RegionalMinimumWageRegion1 = 4_960_000m;
        public static decimal BhxhBhytCeiling => BaseWage * 20;
        public static decimal BhtnCeiling => RegionalMinimumWageRegion1 * 20;

        public const decimal PersonalDeduction = 11_000_000m;
        public const decimal DependentDeduction = 4_400_000m;
        public const decimal StandardHoursPerDay = 8m;
        public const decimal OvertimeWeekdayMultiplier = 1.5m;

        public static PayrollResult Calculate(PayrollInput input)
        {
            var standardDays = input.StandardDays > 0 ? input.StandardDays : CountWeekdays(input.Year, input.Month);
            if (standardDays <= 0) standardDays = 26;

            var workedDays = input.WorkedDays.HasValue
                ? Math.Max(0, input.WorkedDays.Value)
                : Math.Max(0, standardDays - Math.Max(0, input.UnpaidLeaveDays));

            if (workedDays > standardDays) workedDays = standardDays;

            var basic = Round(input.BasicSalary);
            var allowance = Round(input.Allowance);
            var bonus = Round(Math.Max(0, input.Bonus));
            var otherDeductions = Round(Math.Max(0, input.OtherDeductions));

            var attendanceRatio = standardDays == 0 ? 0 : workedDays / standardDays;
            var salaryByAttendance = Round((basic + allowance) * attendanceRatio);

            var dailyRate = standardDays == 0 ? 0 : (basic + allowance) / standardDays;
            var hourlyRate = dailyRate / StandardHoursPerDay;
            var overtimePay = Round(Math.Max(0, input.OvertimeHours) * hourlyRate * OvertimeWeekdayMultiplier);

            var gross = Round(salaryByAttendance + overtimePay + bonus);

            var declaredInsurance = input.InsuranceSalary > 0 ? input.InsuranceSalary : basic;
            var insuranceBase = workedDays <= 0 ? 0 : declaredInsurance;
            var bhxhBhytBase = Math.Min(insuranceBase, BhxhBhytCeiling);
            var bhtnBase = Math.Min(insuranceBase, BhtnCeiling);

            var bhxhEmployee = Round(bhxhBhytBase * BhxhEmployeeRate);
            var bhytEmployee = Round(bhxhBhytBase * BhytEmployeeRate);
            var bhtnEmployee = Round(bhtnBase * BhtnEmployeeRate);
            var unionEmployee = input.DeductUnionFee ? Round(bhxhBhytBase * UnionEmployeeRate) : 0;
            var totalInsuranceEmployee = bhxhEmployee + bhytEmployee + bhtnEmployee;

            var dependents = Math.Max(0, input.DependentCount);
            var dependentDeduction = Round(dependents * DependentDeduction);
            var taxable = Round(gross - totalInsuranceEmployee - PersonalDeduction - dependentDeduction);
            if (taxable < 0) taxable = 0;
            var pit = CalculatePersonalIncomeTax(taxable);

            var net = Round(gross - totalInsuranceEmployee - unionEmployee - pit - otherDeductions);
            if (net < 0) net = 0;

            var employerBhxh = Round(bhxhBhytBase * BhxhEmployerRate);
            var employerBhyt = Round(bhxhBhytBase * BhytEmployerRate);
            var employerBhtn = Round(bhtnBase * BhtnEmployerRate);
            var employerUnion = Round(bhxhBhytBase * UnionEmployerRate);
            var employerInsurance = employerBhxh + employerBhyt + employerBhtn;
            var companyCost = Round(gross + employerInsurance + employerUnion);

            return new PayrollResult
            {
                EmployeeId = input.EmployeeId,
                EmployeeName = input.EmployeeName,
                Year = input.Year,
                Month = input.Month,
                Date = new DateTime(input.Year, input.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                BasicSalary = basic,
                Allowance = allowance,
                InsuranceSalary = Round(insuranceBase),
                StandardDays = standardDays,
                WorkedDays = workedDays,
                UnpaidLeaveDays = Round(Math.Max(0, standardDays - workedDays)),
                OvertimeHours = Round(Math.Max(0, input.OvertimeHours)),
                OvertimePay = overtimePay,
                Bonus = bonus,
                OtherDeductions = otherDeductions,
                DependentCount = dependents,
                DeductUnionFee = input.DeductUnionFee,
                GrossSalary = gross,
                BhxhEmployee = bhxhEmployee,
                BhytEmployee = bhytEmployee,
                BhtnEmployee = bhtnEmployee,
                UnionFeeEmployee = unionEmployee,
                TotalInsuranceEmployee = totalInsuranceEmployee,
                PersonalDeductionAmount = PersonalDeduction,
                DependentDeduction = dependentDeduction,
                TaxableIncome = taxable,
                PersonalIncomeTax = pit,
                EmployerBhxh = employerBhxh,
                EmployerBhyt = employerBhyt,
                EmployerBhtn = employerBhtn,
                EmployerUnion = employerUnion,
                TotalEmployerInsurance = employerInsurance,
                CompanyCost = companyCost,
                NetSalary = net,
                FormulaNotes =
                    "Gross = (Lương CB + phụ cấp) × ngày công/ngày chuẩn + OT 150% + thưởng. " +
                    "NLĐ đóng BHXH 8% + BHYT 1.5% + BHTN 1% trên mức đóng BH (trần BHXH/BHYT = 20 × lương cơ sở, trần BHTN = 20 × lương tối thiểu vùng I). " +
                    "TNCN = thuế lũy tiến trên (Gross − BH NLĐ − 11.000.000 − 4.400.000 × NPT). " +
                    "Thực lĩnh = Gross − BH NLĐ − KPCĐ (nếu có) − TNCN − khấu trừ khác. " +
                    "Công ty đóng BHXH 17.5% + BHYT 3% + BHTN 1% + KPCĐ 2%."
            };
        }

        public static decimal CalculatePersonalIncomeTax(decimal taxableIncome)
        {
            if (taxableIncome <= 0) return 0;

            var brackets = new (decimal UpTo, decimal Rate)[]
            {
                (5_000_000m, 0.05m),
                (10_000_000m, 0.10m),
                (18_000_000m, 0.15m),
                (32_000_000m, 0.20m),
                (52_000_000m, 0.25m),
                (80_000_000m, 0.30m),
                (decimal.MaxValue, 0.35m)
            };

            decimal tax = 0;
            decimal remaining = taxableIncome;
            decimal previous = 0;

            foreach (var (upTo, rate) in brackets)
            {
                var slice = Math.Min(remaining, upTo - previous);
                if (slice <= 0) break;
                tax += slice * rate;
                remaining -= slice;
                previous = upTo;
                if (remaining <= 0) break;
            }

            return Round(tax);
        }

        public static int CountWeekdays(int year, int month)
        {
            var days = DateTime.DaysInMonth(year, month);
            var count = 0;
            for (var d = 1; d <= days; d++)
            {
                var dow = new DateTime(year, month, d).DayOfWeek;
                if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                    count++;
            }
            return count;
        }

        public static decimal Round(decimal value) =>
            Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    public class PayrollInput
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal InsuranceSalary { get; set; }
        public decimal? WorkedDays { get; set; }
        public decimal StandardDays { get; set; }
        public decimal UnpaidLeaveDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal Bonus { get; set; }
        public decimal OtherDeductions { get; set; }
        public int DependentCount { get; set; }
        public bool DeductUnionFee { get; set; }
    }

    public class PayrollResult
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime Date { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal InsuranceSalary { get; set; }
        public decimal StandardDays { get; set; }
        public decimal WorkedDays { get; set; }
        public decimal UnpaidLeaveDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Bonus { get; set; }
        public decimal OtherDeductions { get; set; }
        public int DependentCount { get; set; }
        public bool DeductUnionFee { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal BhxhEmployee { get; set; }
        public decimal BhytEmployee { get; set; }
        public decimal BhtnEmployee { get; set; }
        public decimal UnionFeeEmployee { get; set; }
        public decimal TotalInsuranceEmployee { get; set; }
        public decimal PersonalDeductionAmount { get; set; }
        public decimal DependentDeduction { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal PersonalIncomeTax { get; set; }
        public decimal EmployerBhxh { get; set; }
        public decimal EmployerBhyt { get; set; }
        public decimal EmployerBhtn { get; set; }
        public decimal EmployerUnion { get; set; }
        public decimal TotalEmployerInsurance { get; set; }
        public decimal CompanyCost { get; set; }
        public decimal NetSalary { get; set; }
        public string FormulaNotes { get; set; }
    }
}
