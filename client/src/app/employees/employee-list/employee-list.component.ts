import { Component, OnInit, ViewChild} from '@angular/core';
import { ColDef, GridApi, GridReadyEvent, GridOptions } from 'ag-grid-community';
import { EmployeeService } from 'src/app/_services/employee.service';
import { ModalDirective } from 'ngx-bootstrap/modal'
import { Router } from '@angular/router';
import { Employee } from 'src/app/_model/employee';
import { ToastrService } from 'ngx-toastr';
@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.css']
})

export class EmployeeListComponent implements OnInit{
  @ViewChild('loadListEmployee', {static: true}) modal: ModalDirective | undefined;

  private gridApi!: GridApi<Employee>; // Sử dụng kiểu Employee cho gridApi
  employees: Employee[] = []; // Sử dụng kiểu Employee[] cho employees
  employee: Employee | null = null;
  public rowSelection: 'single' | 'multiple' = 'multiple';

  public columnDefs: ColDef<Employee>[] = [
    { field: 'EmployeeId', headerName: 'EmployeeId' },
    { field: 'EmployeeName', headerName: 'EmployeeName', filter: true },
    { field: 'DepartmentId', headerName: 'DepartmentId' }
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
    // Không cần set rowData ở đây nếu bạn đã binding [rowData] trong template
    // this.gridApi.setGridOption('rowData', this.rowData);
  }

  onBtExport(){
    this.gridApi.exportDataAsCsv();
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
      const id = employeeData.EmployeeId; // Lấy ID từ hàng được chọn
      this.empService.deleteEmployee(id).subscribe({
        next: _ => {
          this.toastr.success('Employee deleted successfully');
          this.loadEmployee(); // Tải lại danh sách nhân viên để cập nhật
        },
        error: (err: any) => {
          console.error(err);
          this.toastr.error('Failed to delete employee');
        }
      });
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
}
