export interface Salary {
  SalaryId: number;
  EmployeeId: number;
  EmployeeName?: string;
  Date: string;
  PeriodYear?: number;
  PeriodMonth?: number;
  MonthlySalary: number;
  BasicSalary?: number;
  Allowance?: number;
  InsuranceSalary?: number;
  StandardDays?: number;
  WorkedDays?: number;
  UnpaidLeaveDays?: number;
  OvertimeHours?: number;
  OvertimePay?: number;
  Bonus: number;
  OtherDeductions?: number;
  DependentCount?: number;
  DeductUnionFee?: boolean;
  GrossSalary?: number;
  BhxhEmployee?: number;
  BhytEmployee?: number;
  BhtnEmployee?: number;
  UnionFeeEmployee?: number;
  TotalInsuranceEmployee?: number;
  PersonalDeductionAmount?: number;
  DependentDeduction?: number;
  TaxableIncome?: number;
  PersonalIncomeTax?: number;
  EmployerBhxh?: number;
  EmployerBhyt?: number;
  EmployerBhtn?: number;
  EmployerUnion?: number;
  TotalEmployerInsurance?: number;
  CompanyCost?: number;
  NetSalary?: number;
  TotalSalary: number;
  SalaryNotes?: string;
  FormulaNotes?: string;
}

export interface PayrollCalculateRequest {
  EmployeeId: number;
  Year: number;
  Month: number;
  BasicSalary?: number | null;
  Allowance?: number | null;
  InsuranceSalary?: number | null;
  WorkedDays?: number | null;
  StandardDays?: number | null;
  UnpaidLeaveDays?: number | null;
  OvertimeHours?: number | null;
  Bonus?: number | null;
  OtherDeductions?: number | null;
  DependentCount?: number | null;
  DeductUnionFee?: boolean;
  Save?: boolean;
}
