using API.DTOs;
using API.Entities;
using API.Interfaces;
using MongoDB.Driver;

namespace API.Services
{
    public class PayrollService(
        IMongoDatabase database,
        ISalaryRepository salaryRepository,
        IMongoIdGenerator idGenerator) : IPayrollService
    {
        private readonly IMongoCollection<Employee> _employees = database.GetCollection<Employee>("Employees");
        private readonly IMongoCollection<Contract> _contracts = database.GetCollection<Contract>("Contracts");
        private readonly IMongoCollection<TimeKeeping> _timekeeping = database.GetCollection<TimeKeeping>("TimeKeeping");
        private readonly IMongoCollection<Salary> _salaries = database.GetCollection<Salary>("Salaries");
        private readonly ISalaryRepository _salaryRepository = salaryRepository;
        private readonly IMongoIdGenerator _idGenerator = idGenerator;

        public async Task<PayrollResult> BuildAsync(PayrollCalculateRequest request, CancellationToken ct = default)
        {
            if (request.EmployeeId <= 0)
                throw new ArgumentException("EmployeeId is required.");

            var year = request.Year > 0 ? request.Year : DateTime.UtcNow.Year;
            var month = request.Month > 0 ? request.Month : DateTime.UtcNow.Month;
            if (month is < 1 or > 12)
                throw new ArgumentException("Month must be between 1 and 12.");

            var employee = await _employees.Find(e => e.EmployeeId == request.EmployeeId).FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Employee does not exist.");

            var contract = await FindActiveContractAsync(employee, year, month, ct);
            var (workedDays, standardDays) = await ResolveAttendanceAsync(employee.EmployeeId, year, month, request, ct);

            var basic = request.BasicSalary ?? contract?.BasicSalary ?? 0;
            var allowance = request.Allowance ?? contract?.Allowance ?? 0;
            if (basic <= 0)
                throw new InvalidOperationException("Không tìm thấy lương cơ bản trên hợp đồng. Vui lòng nhập lương cơ bản.");

            var input = new PayrollInput
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.EmployeeName,
                Year = year,
                Month = month,
                BasicSalary = basic,
                Allowance = allowance,
                InsuranceSalary = request.InsuranceSalary ?? basic,
                WorkedDays = workedDays,
                StandardDays = request.StandardDays ?? standardDays,
                UnpaidLeaveDays = request.UnpaidLeaveDays ?? 0,
                OvertimeHours = request.OvertimeHours ?? 0,
                Bonus = request.Bonus ?? 0,
                OtherDeductions = request.OtherDeductions ?? 0,
                DependentCount = request.DependentCount ?? 0,
                DeductUnionFee = request.DeductUnionFee
            };

            return PayrollCalculator.Calculate(input);
        }

        public async Task<Salary> UpsertFromResultAsync(PayrollResult result, string notes, CancellationToken ct = default)
        {
            var existing = await _salaries.Find(s => s.EmployeeId == result.EmployeeId).ToListAsync(ct);
            var match = existing.FirstOrDefault(s =>
                (s.PeriodYear == result.Year && s.PeriodMonth == result.Month) ||
                ((s.PeriodYear == 0 || s.PeriodMonth == 0) && s.Date.Year == result.Year && s.Date.Month == result.Month));

            var salary = match ?? new Salary();
            Apply(salary, result, notes);

            if (match == null)
            {
                salary.SalaryId = await _idGenerator.NextAsync("Salaries", ct);
                _salaryRepository.Add(salary);
            }
            else
            {
                _salaryRepository.Update(salary);
            }

            await _salaryRepository.SaveAllAsync();
            return salary;
        }

        public async Task<IReadOnlyList<Salary>> GenerateMonthAsync(PayrollGenerateMonthRequest request, CancellationToken ct = default)
        {
            var employees = await _employees.Find(_ => true).ToListAsync(ct);
            var saved = new List<Salary>();

            foreach (var employee in employees)
            {
                try
                {
                    var result = await BuildAsync(new PayrollCalculateRequest
                    {
                        EmployeeId = employee.EmployeeId,
                        Year = request.Year,
                        Month = request.Month,
                        DependentCount = request.DependentCount,
                        DeductUnionFee = request.DeductUnionFee
                    }, ct);

                    saved.Add(await UpsertFromResultAsync(result, $"Bảng lương {request.Month:00}/{request.Year}", ct));
                }
                catch (InvalidOperationException)
                {
                    // Bỏ qua nhân viên chưa có hợp đồng/lương cơ bản
                }
            }

            return saved;
        }

        private async Task<Contract> FindActiveContractAsync(Employee employee, int year, int month, CancellationToken ct)
        {
            var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var name = employee.EmployeeName ?? string.Empty;

            var contracts = await _contracts.Find(c => c.EmployeeName == name).ToListAsync(ct);
            if (contracts.Count == 0)
            {
                contracts = await _contracts.Find(_ => true).ToListAsync(ct);
                contracts = contracts
                    .Where(c => string.Equals(c.EmployeeName, name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return contracts
                .Where(c =>
                    (c.StartDate == default || c.StartDate <= periodEnd) &&
                    (c.EndDate == default || c.EndDate >= periodStart))
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault()
                ?? contracts.OrderByDescending(c => c.UpdateAt).FirstOrDefault();
        }

        private async Task<(decimal? WorkedDays, decimal StandardDays)> ResolveAttendanceAsync(
            int employeeId, int year, int month, PayrollCalculateRequest request, CancellationToken ct)
        {
            var standardDays = request.StandardDays ?? PayrollCalculator.CountWeekdays(year, month);
            if (request.WorkedDays.HasValue)
                return (request.WorkedDays.Value, standardDays);

            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            var records = await _timekeeping.Find(t =>
                    t.EmployeeId == employeeId &&
                    t.Date >= start &&
                    t.Date < end)
                .ToListAsync(ct);

            if (records.Count == 0)
                return (null, standardDays);

            var present = records
                .Where(IsPresent)
                .Select(t => t.Date.Date)
                .Distinct()
                .Count();

            return (present, standardDays);
        }

        private static bool IsPresent(TimeKeeping record)
        {
            var status = record.Status?.Trim() ?? string.Empty;
            if (status.Equals("Absent", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Vắng", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Nghỉ không lương", StringComparison.OrdinalIgnoreCase))
                return false;

            if (record.TotalHours > 0 || record.TotalHoursWorked > TimeSpan.Zero)
                return true;

            return status.Equals("Present", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Late", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Có mặt", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Đi trễ", StringComparison.OrdinalIgnoreCase);
        }

        private static void Apply(Salary salary, PayrollResult result, string notes)
        {
            salary.EmployeeId = result.EmployeeId;
            salary.EmployeeName = result.EmployeeName;
            salary.PeriodYear = result.Year;
            salary.PeriodMonth = result.Month;
            salary.Date = result.Date;
            salary.BasicSalary = result.BasicSalary;
            salary.Allowance = result.Allowance;
            salary.InsuranceSalary = result.InsuranceSalary;
            salary.StandardDays = result.StandardDays;
            salary.WorkedDays = result.WorkedDays;
            salary.UnpaidLeaveDays = result.UnpaidLeaveDays;
            salary.OvertimeHours = result.OvertimeHours;
            salary.OvertimePay = result.OvertimePay;
            salary.Bonus = result.Bonus;
            salary.OtherDeductions = result.OtherDeductions;
            salary.DependentCount = result.DependentCount;
            salary.DeductUnionFee = result.DeductUnionFee;
            salary.GrossSalary = result.GrossSalary;
            salary.BhxhEmployee = result.BhxhEmployee;
            salary.BhytEmployee = result.BhytEmployee;
            salary.BhtnEmployee = result.BhtnEmployee;
            salary.UnionFeeEmployee = result.UnionFeeEmployee;
            salary.TotalInsuranceEmployee = result.TotalInsuranceEmployee;
            salary.PersonalDeductionAmount = result.PersonalDeductionAmount;
            salary.DependentDeduction = result.DependentDeduction;
            salary.TaxableIncome = result.TaxableIncome;
            salary.PersonalIncomeTax = result.PersonalIncomeTax;
            salary.EmployerBhxh = result.EmployerBhxh;
            salary.EmployerBhyt = result.EmployerBhyt;
            salary.EmployerBhtn = result.EmployerBhtn;
            salary.EmployerUnion = result.EmployerUnion;
            salary.TotalEmployerInsurance = result.TotalEmployerInsurance;
            salary.CompanyCost = result.CompanyCost;
            salary.NetSalary = result.NetSalary;
            salary.MonthlySalary = result.GrossSalary;
            salary.TotalSalary = result.NetSalary;
            salary.Amount = (long)result.NetSalary;
            salary.FormulaNotes = result.FormulaNotes;
            if (!string.IsNullOrWhiteSpace(notes))
                salary.SalaryNotes = notes;
        }
    }
}
