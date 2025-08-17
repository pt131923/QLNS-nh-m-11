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
            CreateMap<Employee, EmployeeDto>().ReverseMap();
            CreateMap<EmployeeUpdateDto, Employee>();

            // ===== DEPARTMENT =====
            CreateMap<AppDepartment, DepartmentDto>().ReverseMap();
            CreateMap<DepartmentUpdateDto, AppDepartment>().ReverseMap();

            // ===== CONTRACT =====
            // Entity -> DTO
            CreateMap<Contract, ContractDto>()
                .ForMember(dest => dest.EmployeeName, 
                           opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : src.EmployeeName));

            // DTO -> Entity
            CreateMap<ContractDto, Contract>()
                .ForMember(dest => dest.Employee, opt => opt.Ignore()); // tránh lỗi map vào navigation

            // UpdateDto -> Entity
            CreateMap<ContractUpdateDto, Contract>();

            // Entity -> ContractWithEmployeeDto
            CreateMap<Contract, ContractWithEmployeeDto>()
                .ForMember(dest => dest.EmployeeName, 
                           opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeName : src.EmployeeName));

            // ===== SALARY =====
            CreateMap<Salary, SalaryDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.EmployeeName));
            CreateMap<SalaryDto, Salary>()
                .ForMember(dest => dest.Employee, opt => opt.Ignore());
            CreateMap<SalaryUpdateDto, Salary>();
        }
    }
}

