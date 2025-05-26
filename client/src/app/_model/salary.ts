
export interface Salary {
  SalaryId: number;
  EmployeeId: number;
  EmployeeName?: string;
  Date: string; // định dạng: 'YYYY-MM-DD'
  MonthlySalary: number;
  Bonus: number;
  TotalSalary: number;
  SalaryNotes?: string;
}
