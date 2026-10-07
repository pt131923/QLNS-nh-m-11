import { Component, OnInit } from '@angular/core';
import { GridApi, GridReadyEvent, GridOptions, ColDef, ValueFormatterParams } from 'ag-grid-community';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Salary } from 'src/app/_model/salary';
import { SalaryService } from 'src/app/_services/salary.service';

@Component({
  selector: 'app-salary-list',
  templateUrl: './salary-list.component.html',
  styleUrls: ['./salary-list.component.css']
})
export class SalaryListComponent implements OnInit {
  private gridApi!: GridApi<Salary>;
  salaries: Salary[] = [];
  rowData: Salary[] = [];
  searchText: string = '';
  payrollPeriod = this.currentPeriod();
  generating = false;

  public rowSelection: 'single' | 'multiple' = 'multiple';

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
    columnDefs: this.columnDefs,
  };

  defaultColDef = {
    flex: 1,
    minWidth: 100,
    sortable: true,
    resizable: true,
  };

  pagination = true;
  paginationPageSize = 10;
  paginationPageSizeSelector = [5, 10, 15, 20, 25, 30];

  constructor(
    private salaryService: SalaryService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadSalaries();
  }

  onGridReady(event: GridReadyEvent<Salary>) {
    this.gridApi = event.api;
  }

  onBtExport() {
    if (this.gridApi) {
      this.gridApi.exportDataAsCsv({
        fileName: 'Bang-luong.csv'
      });
    } else {
      this.toastr.error('Grid API is not initialized');
    }
  }

  loadSalaries() {
    this.salaryService.getSalaries().subscribe({
      next: (salaries: Salary[]) => {
        this.salaries = salaries;
        this.rowData = [...salaries];
      },
      error: () => {
        this.toastr.error('Failed to load salary data');
      }
    });
  }

  generatePayroll(): void {
    const [year, month] = this.parsePeriod(this.payrollPeriod);
    if (!year || !month) {
      this.toastr.warning('Vui lòng chọn kỳ lương.');
      return;
    }

    this.generating = true;
    this.salaryService.generateMonth(year, month).subscribe({
      next: (res) => {
        this.generating = false;
        this.toastr.success(`Đã lập ${res.generatedCount || 0} phiếu lương kỳ ${res.period}`);
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
        alert('Selected salary record does not have a valid ID.');
      }
    } else {
      alert('Please select one salary record to edit.');
    }
  }

  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const id = selectedRows[0].SalaryId;
      this.salaryService.DeleteSalary(id).subscribe({
        next: () => {
          this.toastr.success('Salary deleted successfully');
          this.loadSalaries();
        },
        error: () => {
          this.toastr.error('Failed to delete salary');
        }
      });
    } else {
      alert('Please select one salary record to delete.');
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
