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
  public rowSelection: 'single' | 'multiple' = 'multiple';

  public columnDefs: ColDef<Department>[] = [
    {
    headerName: '',
    checkboxSelection: true,
    width: 40,
    headerCheckboxSelection: true, // chọn tất cả
    headerCheckboxSelectionFilteredOnly: true,
    pinned: 'left'
  },
    { field: 'DepartmentId', headerName: 'DepartmentId' },
    { field: 'Name', headerName: 'Name', filter: true },
    { field: 'Description', headerName: 'Description' },
    { field: 'SlNhanVien', headerName: 'SlNhanVien' },
    { field: 'Addresses', headerName: 'Addresses' },
    { field: 'Notes', headerName: 'Notes' }
  ];

  public gridOptions: GridOptions<Department> = {
    rowSelection: 'multiple',
    columnDefs: this.columnDefs
  };

  defaultColDef = {
    flex: 1,
    minWidth: 100,
    sortable: true,
    resizable: true
  };

  pagination = true;
  paginationPageSize = 5;
  paginationPageSizeSelector = [5, 10, 15, 20, 25, 30, 35, 40];
  searchText: string = '';
  rowData: Department[] = [];

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
        fileName: 'Danh sách phòng banban.csv',
        columnKeys: ['DepartmentId', 'Name', 'Description', 'SlNhanVien', 'Addresses', 'Notes']
      });
    } else {
      alert('Grid API is not ready yet.');
    }
  }

  loadDepartments() {
    this.deptService.getDepartments().subscribe({
      next: (departments: Department[]) => {
        this.departments = departments;
        this.rowData = departments;
        console.log('Departments loaded:', this.departments);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
      }
    });
  }

  onEditRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const departmentData = selectedRows[0];
      this.deptService.setDepartmentData(departmentData);

      if (departmentData.DepartmentId) {
        this.router.navigate(['/department-edit', departmentData.DepartmentId]);
      } else {
        alert('Selected department does not have a valid ID.');
      }
    } else {
      alert('Please select one department to edit.');
    }
  }

  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const departmentData = selectedRows[0];
      const id = departmentData.DepartmentId;
      this.deptService.DeleteDepartment(id).subscribe({
        next: _ => {
          this.toastr.success('Department deleted successfully');
          this.loadDepartments();
        },
        error: (err: any) => {
          console.error(err);
          this.toastr.error('Failed to delete department');
        }
      });
    } else {
      alert('Please select one department to delete.');
    }
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
    this.gridApi.setGridOption('rowData', this.rowData);
  }

  onRowClicked(event: any): void {
  const department = event.data;
  if (department && department.DepartmentId) {
    this.router.navigate(['/department', department.DepartmentId]);
  } else {
    console.error('Invalid department data:', department);
    this.toastr.error('Unable to redirect due to missing department information.');
  }
}
}
