import { Component, OnInit } from '@angular/core';
import { GridApi, GridReadyEvent, GridOptions, ColDef, ValueFormatterParams } from 'ag-grid-community';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Salary } from 'src/app/_model/salary';
import { SalaryService } from 'src/app/_services/salary.service';
import { Department } from 'src/app/_model/department';
import { DepartmentService } from 'src/app/_services/department.service';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-salary-list',
  templateUrl: './salary-list.component.html',
  styleUrls: ['./salary-list.component.css']
})
export class SalaryListComponent implements OnInit {
  private gridApi!: GridApi<Salary>;
  salaries: Salary[] = [];
  rowData: Salary[] = [];
  departments: Department[] = [];
  selectedDepartmentId: number | null = null;
  searchText: string = '';
  payrollPeriod = this.currentPeriod();
  generating = false;

  showStats = false;
  statsData: any = null;

  public columnDefs: ColDef<Salary>[] = [
    {
      headerName: '',
      checkboxSelection: true,
      width: 40,
      headerCheckboxSelection: true,
      headerCheckboxSelectionFilteredOnly: true,
      pinned: 'left'
    },
    { field: 'EmployeeName', headerName: 'Nhân viên', filter: true, minWidth: 160, pinned: 'left' },
    {
      headerName: 'Kỳ lương',
      minWidth: 100,
      valueGetter: (p) => this.periodLabel(p.data)
    },
    { field: 'WorkedDays', headerName: 'Ngày công', minWidth: 110 },
    { field: 'BasicSalary', headerName: 'Lương CB', minWidth: 130, valueFormatter: this.vnd },
    { field: 'Allowance', headerName: 'Phụ cấp', minWidth: 120, valueFormatter: this.vnd },
    { field: 'OvertimePay', headerName: 'OT', minWidth: 110, valueFormatter: this.vnd },
    { field: 'Bonus', headerName: 'Thưởng', minWidth: 120, valueFormatter: this.vnd },
    { field: 'GrossSalary', headerName: 'Tổng thu nhập', minWidth: 140, valueFormatter: this.vnd },
    { field: 'BhxhEmployee', headerName: 'BHXH NLĐ', minWidth: 120, valueFormatter: this.vnd },
    { field: 'BhytEmployee', headerName: 'BHYT NLĐ', minWidth: 120, valueFormatter: this.vnd },
    { field: 'BhtnEmployee', headerName: 'BHTN NLĐ', minWidth: 120, valueFormatter: this.vnd },
    { field: 'PersonalIncomeTax', headerName: 'Thuế TNCN', minWidth: 130, valueFormatter: this.vnd },
    { field: 'NetSalary', headerName: 'Thực lĩnh', minWidth: 140, valueFormatter: this.vnd },
    { field: 'CompanyCost', headerName: 'Chi phí CTY', minWidth: 140, valueFormatter: this.vnd },
    { field: 'SalaryNotes', headerName: 'Ghi chú', minWidth: 140 }
  ];

  public gridOptions: GridOptions<Salary> = {
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
  paginationPageSize = 10;
  paginationPageSizeSelector = [5, 10, 15, 20, 25, 30];

  constructor(
    private salaryService: SalaryService,
    private departmentService: DepartmentService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadDepartments();
    this.loadSalaries();
  }

  loadDepartments(): void {
    this.departmentService.getDepartments().subscribe({
      next: (depts) => {
        this.departments = depts;
      },
      error: () => {}
    });
  }

  onGridReady(event: GridReadyEvent<Salary>) {
    this.gridApi = event.api;
  }

  exportExcel(): void {
    const [year, month] = this.parsePeriod(this.payrollPeriod);
    this.salaryService.exportExcel(this.searchText, month || undefined, year || undefined).subscribe({
      next: (blob) => {
        saveAs(blob, `BangLuong_${this.payrollPeriod || 'all'}.xlsx`);
        this.toastr.success('Xuất file Excel bảng lương thành công');
      },
      error: () => {
        this.toastr.error('Xuất file Excel thất bại');
      }
    });
  }

  exportPdf(): void {
    const rows = this.rowData;
    if (!rows || rows.length === 0) {
      this.toastr.warning('Không có dữ liệu để in PDF');
      return;
    }

    const tableRows = rows.map((s, idx) => `
      <tr>
        <td style="text-align: center;">${idx + 1}</td>
        <td>${s.EmployeeId}</td>
        <td><strong>${s.EmployeeName || ''}</strong></td>
        <td style="text-align: center;">${this.periodLabel(s)}</td>
        <td style="text-align: center;">${s.WorkedDays ?? '-'}</td>
        <td style="text-align: right;">${(s.BasicSalary || 0).toLocaleString('vi-VN')}</td>
        <td style="text-align: right;">${(s.Allowance || 0).toLocaleString('vi-VN')}</td>
        <td style="text-align: right;">${(s.OvertimePay || 0).toLocaleString('vi-VN')}</td>
        <td style="text-align: right;">${(s.GrossSalary || 0).toLocaleString('vi-VN')}</td>
        <td style="text-align: right;">${((s.BhxhEmployee || 0) + (s.BhytEmployee || 0) + (s.BhtnEmployee || 0)).toLocaleString('vi-VN')}</td>
        <td style="text-align: right;">${(s.PersonalIncomeTax || 0).toLocaleString('vi-VN')}</td>
        <td style="text-align: right; font-weight: bold; color: #1f497d;">${(s.NetSalary || 0).toLocaleString('vi-VN')}</td>
      </tr>
    `).join('');

    const html = `
      <html>
      <head>
        <meta charset="utf-8">
        <title>Bảng lương nhân viên - ${this.payrollPeriod}</title>
        <style>
          body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; font-size: 11px; }
          h2 { text-align: center; color: #1f497d; margin-bottom: 5px; }
          .sub { text-align: center; color: #666; margin-bottom: 20px; }
          table { width: 100%; border-collapse: collapse; margin-top: 10px; }
          th { background: #1f497d; color: white; padding: 6px; border: 1px solid #ccc; font-size: 11px; }
          td { padding: 5px; border: 1px solid #ddd; font-size: 10px; }
          tr:nth-child(even) { background-color: #f9f9f9; }
          .summary { margin-top: 15px; font-size: 12px; }
        </style>
      </head>
      <body>
        <h2>BẢNG LƯƠNG NHÂN VIÊN DOANH NGHIỆP</h2>
        <div class="sub">Kỳ lương: ${this.payrollPeriod} | Ngày xuất: ${new Date().toLocaleDateString('vi-VN')}</div>
        <table>
          <thead>
            <tr>
              <th>STT</th>
              <th>Mã NV</th>
              <th>Họ và tên</th>
              <th>Kỳ</th>
              <th>Công</th>
              <th>Lương CB</th>
              <th>Phụ cấp</th>
              <th>Lương OT</th>
              <th>Tổng thu nhập (Gross)</th>
              <th>BHXH, YT, TN</th>
              <th>Thuế TNCN</th>
              <th>Thực lĩnh (Net)</th>
            </tr>
          </thead>
          <tbody>
            ${tableRows}
          </tbody>
        </table>
        <div class="summary">
          <strong>Tổng số nhân viên:</strong> ${rows.length} | 
          <strong>Tổng chi trả thực lĩnh:</strong> ${rows.reduce((sum, r) => sum + (r.NetSalary || 0), 0).toLocaleString('vi-VN')} đ
        </div>
      </body>
      </html>
    `;

    const win = window.open('', '_blank');
    if (win) {
      win.document.write(html);
      win.document.close();
      win.focus();
      setTimeout(() => {
        win.print();
        win.close();
      }, 500);
    }
  }

  onBtExport() {
    this.exportExcel();
  }

  loadSalaries() {
    this.salaryService.getSalaries().subscribe({
      next: (salaries: Salary[]) => {
        this.salaries = salaries;
        this.rowData = [...salaries];
        this.updateStats();
      },
      error: () => {
        this.toastr.error('Tải dữ liệu bảng lương thất bại');
      }
    });
  }

  generatePayroll(): void {
    const [year, month] = this.parsePeriod(this.payrollPeriod);
    if (!year || !month) {
      this.toastr.warning('Vui lòng chọn kỳ lương.');
      return;
    }

    const scopeText = this.selectedDepartmentId 
      ? `phòng ban đã chọn` 
      : `toàn bộ nhân viên công ty`;

    this.generating = true;
    this.salaryService.generateMonth(year, month, this.selectedDepartmentId).subscribe({
      next: (res) => {
        this.generating = false;
        this.toastr.success(`Đã tự động tính & lập ${res.generatedCount || 0} phiếu lương cho ${scopeText} kỳ ${res.period}!`);
        this.loadSalaries();
      },
      error: (err) => {
        this.generating = false;
        this.toastr.error(err?.error?.message || 'Không lập được bảng lương tháng');
      }
    });
  }

  onEditRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const salary = selectedRows[0];
      if (salary.SalaryId) {
        this.router.navigate(['/salary-edit', salary.SalaryId]);
      } else {
        alert('Bản ghi lương không có mã hợp lệ.');
      }
    } else {
      alert('Vui lòng chọn một bản ghi để sửa.');
    }
  }

  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const id = selectedRows[0].SalaryId;
      if (confirm('Bạn có chắc chắn muốn xóa bản ghi này?')) {
        this.salaryService.DeleteSalary(id).subscribe({
          next: () => {
            this.toastr.success('Xóa bản ghi lương thành công');
            this.loadSalaries();
          },
          error: () => {
            this.toastr.error('Xóa bản ghi lương thất bại');
          }
        });
      }
    } else {
      alert('Vui lòng chọn một bản ghi để xóa.');
    }
  }

  onSearch() {
    if (this.searchText.trim()) {
      const lower = this.searchText.toLowerCase();
      const filtered = this.salaries.filter(row =>
        Object.values(row).some(val =>
          typeof val === 'string' && val.toLowerCase().includes(lower)
        )
      );
      this.rowData = filtered;
    } else {
      this.rowData = [...this.salaries];
    }
    this.gridApi?.setGridOption('rowData', this.rowData);
    this.updateStats();
  }

  onClearFilter() {
    this.searchText = '';
    this.rowData = [...this.salaries];
    this.gridApi?.setGridOption('rowData', this.rowData);
    this.updateStats();
  }

  toggleStats() {
    this.showStats = !this.showStats;
  }

  updateStats() {
    const totalRecords = this.rowData.length;
    const totalBasic = this.rowData.reduce((sum, r) => sum + (r.BasicSalary || 0), 0);
    const totalGross = this.rowData.reduce((sum, r) => sum + (r.GrossSalary || 0), 0);
    const totalNet = this.rowData.reduce((sum, r) => sum + (r.NetSalary || 0), 0);
    const totalTax = this.rowData.reduce((sum, r) => sum + (r.PersonalIncomeTax || 0), 0);
    const totalInsurance = this.rowData.reduce((sum, r) =>
      sum + (r.BhxhEmployee || 0) + (r.BhytEmployee || 0) + (r.BhtnEmployee || 0), 0
    );

    this.statsData = {
      totalRecords,
      totalBasic,
      totalGross,
      totalNet,
      totalTax,
      totalInsurance
    };
  }

  private vnd(params: ValueFormatterParams<Salary>): string {
    if (params.value === null || params.value === undefined || params.value === '') {
      return '';
    }
    return Number(params.value).toLocaleString('vi-VN') + ' ₫';
  }

  private periodLabel(row?: Salary): string {
    if (!row) return '';
    const year = row.PeriodYear || (row.Date ? new Date(row.Date).getFullYear() : 0);
    const month = row.PeriodMonth || (row.Date ? new Date(row.Date).getMonth() + 1 : 0);
    if (!year || !month) return '';
    return `${month.toString().padStart(2, '0')}/${year}`;
  }

  private currentPeriod(): string {
    const now = new Date();
    return `${now.getFullYear()}-${(now.getMonth() + 1).toString().padStart(2, '0')}`;
  }

  private parsePeriod(value: string): [number, number] {
    const parts = (value || '').split('-');
    return [Number(parts[0]), Number(parts[1])];
  }
}
