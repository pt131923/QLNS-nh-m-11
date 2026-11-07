export interface Timekeeping {
  TimeKeepingId: number;
  EmployeeId: number | string;
  Date?: string | Date;
  CheckInTime?: any;
  CheckOutTime?: any;
  TotalHoursWorked?: any;
  TotalHours?: number;
  Status: string;
  Note?: string;
  DepartmentId?: number;
}
