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
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

            CreateMap<ContractUpdateDto, Contract>()
                .ForMember(dest => dest.ContractType, opt => opt.MapFrom(src => src.ContractType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

            CreateMap<Contract, ContractWithEmployeeDto>()
                .ForMember(dest => dest.ContractId, opt => opt.MapFrom(src => src.ContractId))
                .ForMember(dest => dest.ContractName, opt => opt.MapFrom(src => src.ContractName))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Employee.EmployeeId))
                .ForMember(dest => dest.ContractType, opt => opt.MapFrom(src => src.ContractType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

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
