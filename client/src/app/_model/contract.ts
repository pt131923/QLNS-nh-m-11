export interface Contract {
  ContractId: number;
  ContractName: string;
  ContractType: string;
  EmployeeName: string;
  StartDate: Date | undefined;
  EndDate: Date | undefined;
  BasicSalary: number;
  Allowance: number;
  CreateAt: Date | undefined;
  UpdateAt: Date | undefined;
  JobDescription: string;
  ContractTerm: string;
  WorkLocation: string;
  Leaveofabsence: string
}
