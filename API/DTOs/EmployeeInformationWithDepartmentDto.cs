using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class EmployeeInformationWithDepartmentDto
    {
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
    }
}