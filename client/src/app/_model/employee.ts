export interface Employee {
    EmployeeId: number;
    EmployeeName: string;
    DepartmentId: number;
    EmployeeEmail: string;
    EmployeePhone: string;
    EmployeeAddress: string;
    EmployeeInformation: string;
    BirthDate?: Date;
    PlaceOfBirth?: string;
    Gender?: string;
    MaritalStatus?: string;
    IdentityNumber?: string;
    IdentityIssuedDate?: Date;
    IdentityIssuedPlace?: string;
    Religion?: string;
    Ethnicity?: string;
    Nationality?: string;
    EducationLevel?: string;
    Specialization?: string;
}
