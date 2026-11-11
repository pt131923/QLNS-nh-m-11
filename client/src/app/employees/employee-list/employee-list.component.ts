import { Component, OnInit, inject, signal } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent, GridOptions, RowClickedEvent } from 'ag-grid-community';
import { EmployeeService, EmployeeFilter, EmployeeStats } from 'src/app/_services/employee.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Employee } from 'src/app/_model/employee';
import { Department } from 'src/app/_model/department';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.css']
})
export class EmployeeListComponent implements OnInit {

  gridOptions: GridOptions = {};

  private empService = inject(EmployeeService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  public gridApi!: GridApi<Employee>;

  // Signals
  departments = signal<Department[]>([]);
  employees = signal<Employee[]>([]);
  rowData = signal<Employee[]>([]);

  // Statistics
  stats = signal<EmployeeStats | null>(null);
  showStats = false;

  // Filter state
  filter: EmployeeFilter = {
    name: '',
    departmentId: undefined,
    gender: '',
    educationLevel: '',
    contractStatus: ''
  };

  // Batch department transfer
  showBatchModal = false;
  batchTargetDeptId: number | null = null;
  batchLoading = false;

  // Pagination
  rowSelection: 'single' | 'multiple' = 'multiple';
  pagination = true;
  paginationPageSize = 10;
  paginationPageSizeSelector = [5, 10, 15, 20, 25, 30, 35, 40];

  // Column definitions
  columnDefs: ColDef<Employee>[] = [
    {
      headerName: '',
      checkboxSelection: true,
      width: 40,
      headerCheckboxSelection: true,
      headerCheckboxSelectionFilteredOnly: true,
      pinned: 'left'
    },
    { field: 'EmployeeId', headerName: 'Mã NV', maxWidth: 100 },
    { field: 'EmployeeName', headerName: 'Họ và tên', filter: true, minWidth: 150 },
    {
      field: 'DepartmentId',
      headerName: 'Phòng ban',
      filter: true,
      valueFormatter: (params) => {
        const dep = this.departments().find(d => d.DepartmentId === params.value);
        return dep ? dep.Name : 'N/A';
      },
      minWidth: 150,
    },
    { field: 'EmployeeEmail', headerName: 'Email', filter: true, minWidth: 150 },
    { field: 'EmployeePhone', headerName: 'Điện thoại', filter: true, minWidth: 120 },
    { field: 'Gender', headerName: 'Giới tính', filter: true, minWidth: 100 },
    { field: 'BirthDate', headerName: 'Ngày sinh', filter: true, minWidth: 120,
      valueFormatter: p => p.value ? new Date(p.value).toLocaleDateString('vi-VN') : '' },
    { field: 'EmployeeAddress', headerName: 'Địa chỉ', filter: true, minWidth: 200 },
    { field: 'IdentityNumber', headerName: 'CCCD', filter: true },
    { field: 'EducationLevel', headerName: 'Trình độ', filter: true }
  ];

  defaultColDef: ColDef = {
    flex: 1,
    minWidth: 80,
    sortable: true,
    resizable: true,
    filter: true,
  };

  genderOptions = ['', 'Nam', 'Nữ', 'Khác'];
  educationOptions = ['', 'Trung học', 'Trung cấp', 'Cao đẳng', 'Đại học', 'Thạc sĩ', 'Tiến sĩ'];
  contractStatusOptions = [
    { value: '', label: 'Tất cả' },
    { value: 'active', label: 'Đang có hợp đồng' },
    { value: 'inactive', label: 'Hết hạn hợp đồng' },
    { value: 'none', label: 'Chưa có hợp đồng' }
  ];

  ngOnInit(): void {
    this.loadDepartments();
    this.loadEmployee();
    this.loadStats();
  }

  onGridReady(event: GridReadyEvent<Employee>) {
    this.gridApi = event.api;
  }

  loadDepartments() {
    this.empService.getDepartment().subscribe({
      next: (dep) => this.departments.set(dep),
      error: () => this.toastr.error('Không thể tải danh sách phòng ban.')
    });
  }

  loadEmployee() {
    this.empService.getEmployees().subscribe({
      next: (employees) => {
        this.employees.set(employees);
        this.rowData.set(employees);
      },
      error: () => this.toastr.error('Không thể tải danh sách nhân viên.')
    });
  }

  loadStats() {
    this.empService.getStatistics().subscribe({
      next: (s) => this.stats.set(s),
      error: () => {}
    });
  }

  onSearch() {
    const hasFilter = this.filter.name || this.filter.departmentId ||
      this.filter.gender || this.filter.educationLevel;

    if (!hasFilter) {
      this.rowData.set([...this.employees()]);
      return;
    }

    this.empService.searchEmployees(this.filter).subscribe({
      next: (result) => this.rowData.set(result),
      error: () => {
        this.toastr.warning('Không tìm thấy nhân viên phù hợp.');
        this.rowData.set([]);
      }
    });
  }

  onClearFilter() {
    this.filter = { name: '', departmentId: undefined, gender: '', educationLevel: '', contractStatus: '' };
    this.rowData.set([...this.employees()]);
  }

  // ───────────────────── Export Excel (server-side) ─────────────────────

  onExportExcel() {
    this.empService.exportExcel(this.filter).subscribe({
      next: (blob) => {
        const date = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        saveAs(blob, `DanhSachNhanVien_${date}.xlsx`);
        this.toastr.success('Xuất Excel thành công!');
      },
      error: () => this.toastr.error('Xuất Excel thất bại.')
    });
  }

  // ───────────────────── Export PDF (client-side print) ─────────────────────

  onExportPDF() {
    const depMap = new Map<number, string>();
    this.departments().forEach(d => depMap.set(d.DepartmentId, d.Name));

    const rows = this.rowData();
    const tableRows = rows.map((emp, i) => `
      <tr>
        <td>${i + 1}</td>
        <td>${emp.EmployeeId}</td>
        <td>${emp.EmployeeName || ''}</td>
        <td>${depMap.get(emp.DepartmentId) || emp.DepartmentId}</td>
        <td>${emp.EmployeeEmail || ''}</td>
        <td>${emp.EmployeePhone || ''}</td>
        <td>${emp.Gender || ''}</td>
        <td>${emp.BirthDate ? new Date(emp.BirthDate).toLocaleDateString('vi-VN') : ''}</td>
        <td>${emp.EducationLevel || ''}</td>
      </tr>`).join('');

    const html = `
      <html>
      <head>
        <meta charset="UTF-8">
        <title>Danh sách nhân viên</title>
        <style>
          body { font-family: Arial, sans-serif; font-size: 12px; }
          h2 { text-align: center; }
          table { width: 100%; border-collapse: collapse; }
          th { background: #1f497d; color: white; padding: 6px; text-align: left; }
          td { padding: 5px; border-bottom: 1px solid #ddd; }
          tr:nth-child(even) { background: #f2f2f2; }
          .export-info { text-align: right; font-size: 11px; margin-bottom: 8px; }
        </style>
      </head>
      <body>
        <h2>DANH SÁCH NHÂN VIÊN</h2>
        <div class="export-info">Ngày xuất: ${new Date().toLocaleDateString('vi-VN')} | Tổng: ${rows.length} nhân viên</div>
        <table>
          <thead>
            <tr>
              <th>STT</th><th>Mã NV</th><th>Họ và tên</th><th>Phòng ban</th>
              <th>Email</th><th>ĐT</th><th>Giới tính</th><th>Ngày sinh</th><th>Trình độ</th>
            </tr>
          </thead>
          <tbody>${tableRows}</tbody>
        </table>
      </body>
      </html>`;

    const win = window.open('', '_blank');
    if (win) {
      win.document.write(html);
      win.document.close();
      win.focus();
      setTimeout(() => { win.print(); win.close(); }, 500);
    }
  }

  // ───────────────────── Batch Department Transfer ─────────────────────

  openBatchModal() {
    const rows = this.gridApi.getSelectedRows();
    if (rows.length < 2) {
      this.toastr.warning('Vui lòng chọn ít nhất 2 nhân viên để chuyển phòng ban hàng loạt.');
      return;
    }
    this.batchTargetDeptId = null;
    this.showBatchModal = true;
  }

  closeBatchModal() {
    this.showBatchModal = false;
    this.batchTargetDeptId = null;
  }

  confirmBatchTransfer() {
    if (!this.batchTargetDeptId) {
      this.toastr.warning('Vui lòng chọn phòng ban đích.');
      return;
    }

    const rows = this.gridApi.getSelectedRows();
    const ids = rows.map(r => r.EmployeeId);

    this.batchLoading = true;
    this.empService.batchUpdateDepartment({ employeeIds: ids, newDepartmentId: this.batchTargetDeptId }).subscribe({
      next: (res) => {
        this.toastr.success(`Đã chuyển ${res.updated} nhân viên thành công!`);
        this.showBatchModal = false;
        this.batchLoading = false;
        this.loadEmployee();
        this.loadStats();
      },
      error: () => {
        this.toastr.error('Chuyển phòng ban thất bại.');
        this.batchLoading = false;
      }
    });
  }

  // ───────────────────── Statistics ─────────────────────

  toggleStats() {
    this.showStats = !this.showStats;
    if (this.showStats) this.loadStats();
  }

  getStatPercent(count: number): number {
    const total = this.stats()?.total || 1;
    return Math.round((count / total) * 100);
  }

  // ───────────────────── Edit ─────────────────────

  onEditRow() {
    const rows = this.gridApi.getSelectedRows();
    if (rows.length !== 1) {
      this.toastr.warning('Vui lòng chọn đúng 1 nhân viên để sửa.');
      return;
    }
    const emp = rows[0];
    this.empService.setEmployeeData(emp);
    this.router.navigate(['/employee-edit', emp.EmployeeId]);
  }

  // ───────────────────── Delete ─────────────────────

  handleDeleteConfirmation() {
    const rows = this.gridApi.getSelectedRows();
    if (rows.length !== 1) {
      this.toastr.warning('Vui lòng chọn đúng 1 nhân viên để xóa.');
      return;
    }
    const emp = rows[0];
    this.empService.DeleteEmployee(emp.EmployeeId).subscribe({
      next: () => {
        this.toastr.success('Đã xóa nhân viên thành công!');
        this.loadEmployee();
        this.loadStats();
      },
      error: () => this.toastr.error('Xóa nhân viên thất bại.')
    });
  }

  // ───────────────────── Row Click ─────────────────────

  onRowClicked(event: RowClickedEvent<Employee>) {
    if (!event.data?.EmployeeId) return;
    this.router.navigate(['/employee', event.data.EmployeeId]);
  }
}
