using API.DTOs;
using API.Entities;
using API.Services;

namespace API.Interfaces
{
    public interface IPayrollService
    {
        Task<PayrollResult> BuildAsync(PayrollCalculateRequest request, CancellationToken ct = default);
        Task<Salary> UpsertFromResultAsync(PayrollResult result, string notes, CancellationToken ct = default);
        Task<IReadOnlyList<Salary>> GenerateMonthAsync(PayrollGenerateMonthRequest request, CancellationToken ct = default);
    }
}
