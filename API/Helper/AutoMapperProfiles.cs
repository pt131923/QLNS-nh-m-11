using System;
using AutoMapper;
using API.DTOs;
using API.Entities;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // ===== EMPLOYEE =====
            CreateMap<EmployeeUpdateDto, Employee>();
            //CreateMap<Employee, EmployeeDto>().ReverseMap();
            CreateMap<Employee, EmployeeDto>();
            CreateMap<EmployeeDto, Employee>();

            // ===== DEPARTMENT =====
            CreateMap<DepartmentDto, AppDepartment>();
            CreateMap<AppDepartment, DepartmentDto>();
            CreateMap<DepartmentUpdateDto, AppDepartment>();
            CreateMap<AppDepartment, DepartmentUpdateDto>();

            // ===== CONTRACT =====
            CreateMap<Contract, ContractDto>()
                .ForMember(dest => dest.ContractId, opt => opt.MapFrom(src => src.ContractId))
                .ForMember(dest => dest.ContractName, opt => opt.MapFrom(src => src.ContractName))
                .ForMember(dest => dest.ContractType, opt => opt.MapFrom(src => src.ContractType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.BasicSalary, opt => opt.MapFrom(src => src.BasicSalary))
                .ForMember(dest => dest.Allowance, opt => opt.MapFrom(src => src.Allowance))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt))
                .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.JobDescription))
                .ForMember(dest => dest.ContractTerm, opt => opt.MapFrom(src => src.ContractTerm))
                .ForMember(dest => dest.WorkLocation, opt => opt.MapFrom(src => src.WorkLocation))
                .ForMember(dest => dest.Leaveofabsence, opt => opt.MapFrom(src => src.Leaveofabsence));

            CreateMap<ContractUpdateDto, Contract>()
                .ForMember(dest => dest.ContractType, opt => opt.MapFrom(src => src.ContractType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.BasicSalary, opt => opt.MapFrom(src => src.BasicSalary))
    .ForMember(dest => dest.Allowance, opt => opt.MapFrom(src => src.Allowance))
    .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt))
    .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt))
    .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.JobDescription))
    .ForMember(dest => dest.ContractTerm, opt => opt.MapFrom(src => src.ContractTerm))
    .ForMember(dest => dest.WorkLocation, opt => opt.MapFrom(src => src.WorkLocation))
    .ForMember(dest => dest.Leaveofabsence, opt => opt.MapFrom(src => src.Leaveofabsence));

            CreateMap<Contract, ContractWithEmployeeDto>()
                .ForMember(dest => dest.ContractId, opt => opt.MapFrom(src => src.ContractId))
                .ForMember(dest => dest.ContractName, opt => opt.MapFrom(src => src.ContractName))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.EmployeeName))
                .ForMember(dest => dest.ContractType, opt => opt.MapFrom(src => src.ContractType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate)).ForMember(dest => dest.BasicSalary, opt => opt.MapFrom(src => src.BasicSalary))
    .ForMember(dest => dest.Allowance, opt => opt.MapFrom(src => src.Allowance))
    .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt))
    .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt))
    .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.JobDescription))
    .ForMember(dest => dest.ContractTerm, opt => opt.MapFrom(src => src.ContractTerm))
    .ForMember(dest => dest.WorkLocation, opt => opt.MapFrom(src => src.WorkLocation))
    .ForMember(dest => dest.Leaveofabsence, opt => opt.MapFrom(src => src.Leaveofabsence));


            // ===== SALARY =====
            CreateMap<Salary, SalaryDto>()
                .ForMember(dest => dest.SalaryId, opt => opt.MapFrom(src => src.SalaryId))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.EmployeeName))
                .ForMember(dest => dest.MonthlySalary, opt => opt.MapFrom(src => src.MonthlySalary))
                .ForMember(dest => dest.Bonus, opt => opt.MapFrom(src => src.Bonus))
                .ForMember(dest => dest.TotalSalary, opt => opt.MapFrom(src => src.TotalSalary))
                .ForMember(dest => dest.SalaryNotes, opt => opt.MapFrom(src => src.SalaryNotes))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date))
                ;

            CreateMap<SalaryDto, Salary>()
                .ForMember(dest => dest.SalaryId, opt => opt.MapFrom(src => src.SalaryId))
                .ForMember(dest => dest.MonthlySalary, opt => opt.MapFrom(src => src.MonthlySalary))
                .ForMember(dest => dest.Bonus, opt => opt.MapFrom(src => src.Bonus))
                .ForMember(dest => dest.TotalSalary, opt => opt.MapFrom(src => src.TotalSalary))
                .ForMember(dest => dest.SalaryNotes, opt => opt.MapFrom(src => src.SalaryNotes))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
                ; // Sẽ gán thủ công trong controller nếu cần

            CreateMap<SalaryUpdateDto, Salary>()
                .ForMember(dest => dest.MonthlySalary, opt => opt.MapFrom(src => src.MonthlySalary))
                .ForMember(dest => dest.Bonus, opt => opt.MapFrom(src => src.Bonus))
                .ForMember(dest => dest.TotalSalary, opt => opt.MapFrom(src => src.TotalSalary))
                .ForMember(dest => dest.SalaryNotes, opt => opt.MapFrom(src => src.SalaryNotes))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date));
        }
    }
}
