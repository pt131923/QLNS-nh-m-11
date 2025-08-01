export interface Timekeeping {
  TimeKeepingId: number;
  EmployeeId: string;
  CheckInTime: Date;
  CheckOutTime: Date;
  Status: string;
  Note: string;
}
