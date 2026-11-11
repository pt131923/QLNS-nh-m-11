import { Component, OnInit, ViewChild } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent, GridOptions } from 'ag-grid-community';
import { DepartmentService } from 'src/app/_services/department.service';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Router } from '@angular/router';
import { Department } from 'src/app/_model/department';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-department-list',
  templateUrl: './department-list.component.html',
  styleUrls: ['./department-list.component.css']
})
export class DepartmentListComponent implements OnInit {
  @ViewChild('loadListDepartment', { static: true }) modal: ModalDirective | undefined;

  private gridApi!: GridApi<Department>;
  departments: Department[] = [];
  department: Department | null = null;
  searchText: string = '';
  rowData: Department[] = [];

  showStats = false;
  statsData: any = null;

  public columnDefs: ColDef<Department>[] = [
    {
      headerName: '',
      checkboxSelection: true,
      width: 40,
      headerCheckboxSelection: true,
      headerCheckboxSelectionFilteredOnly: true,
      pinned: 'left'
    },
    { field: 'DepartmentId', headerName: 'Mã PB', width: 90 },
    { field: 'Name', headerName: 'Tên phòng ban', filter: true, width: 180 },
    { field: 'Description', headerName: 'Mô tả', width: 250 },
    { field: 'SlNhanVien', headerName: 'Số nhân viên', width: 130 },
    { field: 'Addresses', headerName: 'Địa điểm', width: 200 },
    { field: 'Notes', headerName: 'Ghi chú', width: 200 }
  ];

  public gridOptions: GridOptions<Department> = {
    rowSelection: 'multiple',
    columnDefs: this.columnDefs
  };

  defaultColDef = {
    flex: 1,
    minWidth: 100,
    sortable: true,
    resizable: true,
    filter: true
  };

  pagination = true;
  paginationPageSize = 5;
  paginationPageSizeSelector = [5, 10, 15, 20, 25, 30, 35, 40];

  constructor(
    private deptService: DepartmentService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadDepartments();
    this.deptService.getDepartments().subscribe({
      next: (departments: Department[]) => {
        this.department = departments.length > 0 ? departments[0] : null;
      },
      error: (err) => {
        console.error('Error loading departments:', err);
        this.department = null;
      }
    });
  }

  onGridReady(event: GridReadyEvent<Department>) {
    this.gridApi = event.api;
  }

  onBtExport() {
    if (this.gridApi) {
      this.gridApi.exportDataAsCsv({
        fileName: 'Danh sách phòng ban_' + new Date().toLocaleDateString('vi-VN') + '.csv',
        columnKeys: ['DepartmentId', 'Name', 'Description', 'SlNhanVien', 'Addresses', 'Notes']
      });
    } else {
      alert('Grid API chưa sẵn sàng.');
    }
  }

  loadDepartments() {
    this.deptService.getDepartments().subscribe({
      next: (departments: Department[]) => {
        this.departments = departments;
        this.rowData = departments;
        this.updateStats();
        console.log('Departments loaded:', this.departments);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
        this.toastr.error('Tải danh sách phòng ban thất bại');
      }
    });
  }

  onSearch() {
    if (this.searchText) {
      const searchTextLower = this.searchText.toLowerCase();
      this.rowData = this.departments.filter(row => {
        return Object.values(row).some(value => {
          if (typeof value === 'string') {
            return value.toLowerCase().includes(searchTextLower);
          }
          return false;
        });
      });
    } else {
      this.rowData = [...this.departments];
    }
    this.gridApi?.setGridOption('rowData', this.rowData);
    this.updateStats();
  }

  onClearFilter() {
    this.searchText = '';
    this.rowData = [...this.departments];
    this.gridApi?.setGridOption('rowData', this.rowData);
    this.updateStats();
  }

  updateStats() {
    const total = this.rowData.length;
    const totalEmployees = this.rowData.reduce((sum, d) => sum + (d.SlNhanVien || 0), 0);
    this.statsData = {
      total,
      totalEmployees,
      avgEmployeesPerDept: total > 0 ? Math.round(totalEmployees / total) : 0
    };
  }

  toggleStats() {
    this.showStats = !this.showStats;
  }

  onEditRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const departmentData = selectedRows[0];
      this.deptService.setDepartmentData(departmentData);

      if (departmentData.DepartmentId) {
        this.router.navigate(['/department-edit', departmentData.DepartmentId]);
      } else {
        alert('Không tìm thấy mã phòng ban hợp lệ.');
      }
    } else {
      alert('Vui lòng chọn một phòng ban để sửa.');
    }
  }

  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const departmentData = selectedRows[0];
      const id = departmentData.DepartmentId;
      if (confirm('Bạn có chắc chắn muốn xóa phòng ban này?')) {
        this.deptService.DeleteDepartment(id).subscribe({
          next: _ => {
            this.toastr.success('Xóa phòng ban thành công');
            this.loadDepartments();
          },
          error: (err: any) => {
            console.error(err);
            this.toastr.error('Xóa phòng ban thất bại');
          }
        });
      }
    } else {
      alert('Vui lòng chọn một phòng ban để xóa.');
    }
  }

  onRowClicked(event: any): void {
    const department = event.data;
    if (department && department.DepartmentId) {
      this.router.navigate(['/department', department.DepartmentId]);
    } else {
      console.error('Invalid department data:', department);
      this.toastr.error('Không thể chuyển trang do thiếu thông tin phòng ban.');
    }
  }
}
