import { Component, OnInit } from '@angular/core';
import { GridApi, GridReadyEvent, GridOptions, ColDef } from 'ag-grid-community';
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

  public rowSelection: 'single' | 'multiple' = 'multiple';

  public columnDefs: ColDef<Salary>[] = [
    { field: 'SalaryId', headerName: 'Salary ID' },
    { field: 'EmployeeId', headerName: 'Employee Id' },
    { field: 'EmployeeName', headerName: 'Employee Name', filter: true },
    { field: 'Date', headerName: 'Date' },
    { field: 'MonthlySalary', headerName: 'Monthly Salary' },
    { field: 'Bonus', headerName: 'Bonus' },
    { field: 'TotalSalary', headerName: 'Total Salary' },
    { field: 'SalaryNotes', headerName: 'Salary Notes' }
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
  paginationPageSize = 5;
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
    this.gridApi.exportDataAsCsv();
  }

  loadSalaries() {
    this.salaryService.getSalaries().subscribe({
      next: (salaries: Salary[]) => {
        this.salaries = salaries;
        this.rowData = [...salaries]; // clone để dùng cho search
        console.log('Salaries loaded: ', this.salaries);
      },
      error: (err) => {
        console.error('Error loading salaries:', err);
        this.toastr.error('Failed to load salary data');
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
        error: (err) => {
          console.error('Error deleting salary:', err);
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
}
