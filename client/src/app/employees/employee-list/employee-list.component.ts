import { Component, OnInit, inject, signal } from '@angular/core';
import { ColDef, GridApi, GridReadyEvent, GridOptions, RowClickedEvent } from 'ag-grid-community';
import { EmployeeService } from 'src/app/_services/employee.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Employee } from 'src/app/_model/employee';
import { Department } from 'src/app/_model/department';

@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.css']
})
export class EmployeeListComponent implements OnInit {

  gridOptions: GridOptions = {};

  // Inject services
  private empService = inject(EmployeeService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  private gridApi!: GridApi<Employee>;

  // Signals
  departments = signal<Department[]>([]);
  employees = signal<Employee[]>([]);
  rowData = signal<Employee[]>([]);

  // Search text
  searchText: string = '';

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
    { field: 'EmployeeId', headerName: 'Employee ID', maxWidth: 100 },
    { field: 'EmployeeName', headerName: 'Employee Name', filter: true, minWidth: 150 },
    {
      field: 'DepartmentId',
      headerName: 'Department',
      filter: true,
      valueFormatter: (params) => {
        const dep = this.departments().find(d => d.DepartmentId === params.value);
        return dep ? dep.Name : 'N/A';
      },
      minWidth: 150,
    },
    { field: 'EmployeeEmail', headerName: 'Email', filter: true, minWidth: 150 },
    { field: 'EmployeePhone', headerName: 'Phone', filter: true, minWidth: 120 },
    { field: 'EmployeeAddress', headerName: 'Address', filter: true, minWidth: 200 },
    { field: 'BirthDate', headerName: 'Birth Date', filter: true, minWidth: 120 },
    { field: 'Gender', headerName: 'Gender', filter: true, minWidth: 100 },
    { field: 'IdentityNumber', headerName: 'ID Number', filter: true },
    { field: 'EducationLevel', headerName: 'Education Level', filter: true }
  ];

  defaultColDef: ColDef = {
    flex: 1,
    minWidth: 80,
    sortable: true,
    resizable: true,
    filter: true,
  };

  // ------------ Lifecycle ------------
  ngOnInit(): void {
    this.loadDepartments();
    this.loadEmployee();
  }

  onGridReady(event: GridReadyEvent<Employee>) {
    this.gridApi = event.api;
  }

  // ------------ Load Data ------------

  loadDepartments() {
    this.empService.getDepartment().subscribe({
      next: (dep) => {
        this.departments.set(dep);
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Unable to load departments.');
      }
    });
  }

  loadEmployee() {
    this.empService.getEmployees().subscribe({
      next: (employees) => {
        this.employees.set(employees);
        this.rowData.set(employees);
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Unable to load employees.');
      }
    });
  }

  // ------------ Export CSV ------------

  onBtExport() {
    const depMap = new Map<number, string>();
    this.departments().forEach(d => depMap.set(d.DepartmentId, d.Name));

    this.gridApi.exportDataAsCsv({
      fileName: 'Employee_List_Export.csv',
      processCellCallback: (params) => {
        const colId = params.column.getColId();

        if (colId === 'DepartmentId') {
          return depMap.get(params.value) || 'Unknown';
        }

        if (colId === 'BirthDate' || colId === 'IdentityIssuedDate') {
          if (params.value instanceof Date) {
            return params.value.toLocaleDateString('en-US');
          }
        }

        return params.value;
      },
      prependContent: [
        [{ data: 'EMPLOYEE LIST' as any }],
        [{ data: 'Export Date:' }, { data: new Date().toLocaleDateString('en-US') }],
        []
      ],

      columnKeys: this.columnDefs.map(c => c.field).filter(f => f) as string[],
      skipColumnHeaders: false,
      suppressQuotes: true
    });

    this.toastr.success('CSV export completed successfully!');
  }

  // ------------ Edit ------------

  onEditRow() {
    const rows = this.gridApi.getSelectedRows();
    if (rows.length !== 1) {
      this.toastr.warning('Please select exactly one employee to edit.');
      return;
    }

    const emp = rows[0];
    if (!emp.EmployeeId) {
      this.toastr.error('Invalid Employee ID.');
      return;
    }

    this.empService.setEmployeeData(emp);
    this.router.navigate(['/employee-edit', emp.EmployeeId]);
  }

  // ------------ Delete ------------

  handleDeleteConfirmation() {
    const rows = this.gridApi.getSelectedRows();
    if (rows.length !== 1) {
      this.toastr.warning('Please select exactly one employee to delete.');
      return;
    }

    const emp = rows[0];
    if (!emp.EmployeeId) {
      this.toastr.error('Invalid Employee ID.');
      return;
    }

    this.toastr.info(`Deleting employee with ID: ${emp.EmployeeId}...`);

    this.deleteEmployee(emp.EmployeeId);
  }

  private deleteEmployee(id: number) {
    this.empService.DeleteEmployee(id).subscribe({
      next: () => {
        this.toastr.success('Employee deleted successfully!');
        this.loadEmployee();
      },
      error: (err) => {
        console.error(err);
        this.toastr.error('Failed to delete employee.');
      }
    });
  }

  // ------------ Search ------------

  onSearch() {
    const text = this.searchText.toLowerCase().trim();

    if (!text) {
      this.rowData.set([...this.employees()]);
      return;
    }

    const filtered = this.employees().filter(emp =>
      Object.values(emp).some(v =>
        typeof v === 'string' && v.toLowerCase().includes(text)
      )
    );

    this.rowData.set(filtered);
  }

  // ------------ Row Click ------------

  onRowClicked(event: RowClickedEvent<Employee>) {
    if (!event.data?.EmployeeId) {
      this.toastr.error('Employee details not found.');
      return;
    }

    this.router.navigate(['/employee', event.data.EmployeeId]);
  }

}
