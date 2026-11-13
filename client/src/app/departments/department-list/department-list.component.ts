import { Component, OnInit, ViewChild } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent, GridOptions } from 'ag-grid-community';
import { DepartmentFilter, DepartmentService } from 'src/app/_services/department.service';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Router } from '@angular/router';
import { Department } from 'src/app/_model/department';
import { ToastrService } from 'ngx-toastr';
import saveAs from 'file-saver';

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

  filter: DepartmentFilter = {
    name: '',
    description: '',
    slNhanVien: undefined,
    addresses: '',
    notes: ''
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

  // ===== XUẤT EXCEL =====
  onExportExcel(): void {
  this.deptService.exportExcel(this.filter).subscribe({
    next: (blob: Blob) => {
      const date = new Date().toISOString().slice(0, 10).replace(/-/g, '');
      saveAs(blob, `DanhSachPhongBan_${date}.xlsx`);
      this.toastr.success('Xuất Excel thành công!');
    },
    error: () => this.toastr.error('Xuất Excel thất bại!')
  });
}

// ===== XUẤT PDF =====
onExportPDF(): void {
  const rows = this.rowData;
  const headers = ['STT', 'Mã PB', 'Tên phòng ban', 'Mô tả', 'Số NV', 'Địa điểm', 'Ghi chú'];
  
  const tableRows = rows.map((dept: { DepartmentId: any; Name: any; Description: any; SlNhanVien: any; Addresses: any; Notes: any; }, i: number) => [
    i + 1,
    dept.DepartmentId,
    dept.Name || '',
    dept.Description || '',
    dept.SlNhanVien,
    dept.Addresses || '',
    dept.Notes || ''
  ]);

  const html = this.buildPDFHtml(headers, tableRows, rows.length);
  this.openAndPrintPDF(html);
}

private buildPDFHtml(headers: string[], rows: (string | number)[][], total: number): string {
  const headerHtml = headers.map(h => `<th>${h}</th>`).join('');
  const rowHtml = rows.map(r => r.map(c => `<td>${c}</td>`).join('')).map(tr => `<tr>${tr}</tr>`).join('');
  const exportDate = new Date().toLocaleDateString('vi-VN');

  return `
<html>
<head>
  <meta charset="UTF-8">
  <title>Danh sách phòng ban</title>
  <style>
    body{font-family:Arial,sans-serif;font-size:12px;padding:20px}
    h2{text-align:center;color:#1f497d;margin-bottom:10px}
    .info{text-align:right;font-size:11px;color:#555;margin-bottom:8px}
    table{width:100%;border-collapse:collapse}
    th{background:#1f497d;color:#fff;padding:6px;text-align:left;border:1px solid #ddd}
    td{padding:6px;border:1px solid #ddd}
    tr:nth-child(even){background:#f2f2f2}
  </style>
</head>
<body>
  <h2>DANH SÁCH PHÒNG BAN</h2>
  <div class="info">Ngày xuất: ${exportDate} | Tổng: ${total} phòng ban</div>
  <table><thead><tr>${headerHtml}</tr></thead><tbody>${rowHtml}</tbody></table>
</body>
</html>`;
}

private openAndPrintPDF(html: string): void {
  const win = window.open('', '_blank');
  if (!win) {
    this.toastr.warning('Vui lòng cho phép pop-up để in');
    return;
  }
  win.document.write(html);
  win.document.close();
  win.focus();
  setTimeout(() => { win.print(); win.close(); }, 400);
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
