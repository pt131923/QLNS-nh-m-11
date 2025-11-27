import { Component, OnInit, ViewChild} from '@angular/core';
import { ColDef, GridApi, GridReadyEvent, GridOptions, RowClickedEvent } from 'ag-grid-community';
import { EmployeeService } from 'src/app/_services/employee.service';
import { ModalDirective } from 'ngx-bootstrap/modal'
import { Router } from '@angular/router';
import { Employee } from 'src/app/_model/employee';
import { ToastrService } from 'ngx-toastr';
import { Department } from 'src/app/_model/department';
@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.css']
})

export class EmployeeListComponent implements OnInit{
  @ViewChild('loadListEmployee', {static: true}) modal: ModalDirective | undefined;

  private gridApi!: GridApi<Employee>;
  employees: Employee[] = [];
  employee: Employee | null = null;
  public rowSelection: 'single' | 'multiple' = 'multiple';
  departments: Department[] = [];
  public columnDefs: ColDef<Employee>[] = [
    {
    headerName: '',
    checkboxSelection: true,
    width: 40,
    headerCheckboxSelection: true,
    headerCheckboxSelectionFilteredOnly: true,
    pinned: 'left'
  },
  { field: 'EmployeeId', headerName: 'EmployeeId' },
  { field: 'EmployeeName', headerName: 'EmployeeName', filter: true },
  {
    field: 'DepartmentId',
    headerName: 'DepartmentId',
    filter: true
  },
  { field: 'EmployeeEmail', headerName: 'EmployeeEmail', filter: true },
  { field: 'EmployeePhone', headerName: 'EmployeePhone', filter: true },
  { field: 'EmployeeAddress', headerName: 'EmployeeAddress', filter: true },
  { field: 'BirthDate', headerName: 'BirthDate', filter: true },
  { field: 'PlaceOfBirth', headerName: 'PlaceOfBirth', filter: true },
  { field: 'Gender', headerName: 'Gender', filter: true },
  { field: 'MaritalStatus', headerName: 'MaritalStatus', filter: true },
  { field: 'IdentityNumber', headerName: 'IdentityNumber', filter: true },
  { field: 'IdentityIssuedDate', headerName: 'IdentityIssuedDate', filter: true },
  { field: 'IdentityIssuedPlace', headerName: 'IdentityIssuedPlace', filter: true },
  { field: 'Religion', headerName: 'Religion', filter: true },
  { field: 'Ethnicity', headerName: 'Ethnicity', filter: true },
  { field: 'Nationality', headerName: 'Nationality', filter: true },
  { field: 'EducationLevel', headerName: 'EducationLevel', filter: true },
  { field: 'Specialization', headerName: 'Specialization', filter: true }
];


  public gridOptions: GridOptions<Employee> = {
    rowSelection: 'multiple',
    columnDefs: this.columnDefs,
  };

  defaultColDef = {
    flex: 1,
    minWidth: 100,
    sortable: true,
    resizable: true,
  }

  pagination = true;
  paginationPageSize = 5;
  paginationPageSizeSelector = [5, 10, 15,20, 25, 30, 35, 40];
  searchText: string = ''; // Khởi tạo searchText là string rỗng
  rowData: Employee[] = []; // Sử dụng kiểu Employee[] cho rowData và khởi tạo là mảng rỗng

  constructor(private empService: EmployeeService, private router: Router, private toastr: ToastrService) {  }

  ngOnInit(): void {
    this.loadEmployee();
    this.empService.getEmployees().subscribe({
      next: (employees: Employee[]) => {
        this.employee = employees.length > 0 ? employees[0] : null;
      },
      error: (err) => {
        console.error('Error loading employees:', err);
        this.employee = null;
      }
    });
  }

  onGridReady(event: GridReadyEvent<Employee>) {
    this.gridApi = event.api;
  }

  onBtExport() {
  // Đảm bảo map chứa dữ liệu đúng
  const departmentMap = new Map<number, string>();
  this.departments.forEach(dep => {
    // Kiểm tra xem DepartmentId có hợp lệ không
    if (dep.DepartmentId === undefined || dep.Name === undefined) {
      console.warn('Invalid department data:', dep);
      return;
    }
    // Thêm vào map với DepartmentId là key và Name là value
    if (typeof dep.DepartmentId !== 'number' || typeof dep.Name !== 'string') {
      console.warn('DepartmentId must be a number and Name must be a string:', dep);
      return;
    }
  departmentMap.set(dep.DepartmentId, dep.Name);
   });

  this.gridApi.exportDataAsCsv({
    processCellCallback: (params) => {
      const colId = params.column.getColId();

      if (colId === 'DepartmentId') {
        console.log('DepartmentId raw:', params.value);
        const name = departmentMap.get(params.value);
        return name || 'Không xác định';
      }

      return params.value;
    },
    fileName: 'Danh_sach_nhan_vien.csv'
  });
}

  loadEmployee() {
    this.empService.getEmployees().subscribe({
      next: (employees: Employee[]) => {
        this.employees = employees;
        this.rowData = employees; // Gán dữ liệu nhận được vào rowData
        console.log('Employees loaded:', this.employees);
      },
      error: (err) => {
        console.error('Error loading employees:', err);
      }
    });
  }

  onEditRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const employeeData = selectedRows[0];
      this.empService.setEmployeeData(employeeData);

      // Kiểm tra ID trước khi điều hướng
      if (employeeData.EmployeeId) {
        this.router.navigate(['/employee-edit', employeeData.EmployeeId]);
      } else {
        alert('Selected employee does not have a valid ID.');
      }

    } else {
      alert('Please select one employee to edit.');
    }
  }


  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const employeeData = selectedRows[0];
      const id = employeeData.EmployeeId;
      if (!id) {
        this.toastr.error('EmployeeId is required');
        return;
      }
      //thêm thông báo cho người dùng
      this.toastr.warning('Are you sure you want to delete this employee?');
      if (confirm('Are you sure you want to delete this employee?')) {
        this.empService.DeleteEmployee(id).subscribe({
          next: _ => {
            this.toastr.success('Employee deleted successfully');
            this.loadEmployee(); // Tải lại danh sách nhân viên để cập nhật
          },
          error: (err: any) => {
            console.error(err);
            this.toastr.error('Failed to delete employee');
          }
        });
      }
    } else {
      alert('Please select one employee to delete.');
    }
  }

  onSearch() {
    if (this.searchText) {
      const searchTextLower = this.searchText.toLowerCase();
      this.rowData = this.employees.filter(row => {
        return Object.values(row).some(value => {
          if (typeof value === 'string') {
            return value.toLowerCase().includes(searchTextLower);
          }
          return false;
        });
      });
    } else {
      this.rowData = [...this.employees]; // Nếu search text trống, hiển thị lại toàn bộ dữ liệu
    }
    this.gridApi.setGridOption('rowData', this.rowData);
  }
  onRowClicked(event: any): void {
  const employee = event.data;
  if (employee && employee.EmployeeId) {
    this.router.navigate(['/employee', employee.EmployeeId]);
  } else {
    console.error('Invalid employee data:', employee);
    this.toastr.error('Not found');
  }
}
}

