import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { ColDef, GridApi, GridReadyEvent, GridOptions } from 'ag-grid-community';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { Contract } from 'src/app/_model/contract';
import { ContractService } from 'src/app/_services/contract.service';

@Component({
  selector: 'app-contract-list',
  templateUrl: './contract-list.component.html',
  styleUrls: ['./contract-list.component.css'],
})
export class ContractListComponent implements OnInit {
  @ViewChild('loadListContract', { static: true }) modal: ModalDirective | undefined;

  private gridApi!: GridApi<Contract>;
  contracts: Contract[] = [];
  contract: Contract | null = null;
  searchText: string = '';
  rowData: Contract[] = [];
  selectedContractId: number | null = null;

  public columnDefs: ColDef<Contract>[] = [
  {
    headerName: '',
    checkboxSelection: true,
    width: 40,
    headerCheckboxSelection: true,
    headerCheckboxSelectionFilteredOnly: true,
    pinned: 'left'
  },
    { field: 'ContractId', headerName: 'ContractID' },
    { field: 'ContractName', headerName: 'ContractName' },
    { field: 'EmployeeName', headerName: 'EmployeeName' },
    { field: 'ContractType', headerName: 'ContractType' },
    { field: 'StartDate', headerName: 'StartDate' },
    { field: 'EndDate', headerName: 'EndDate' },
    { field: 'BasicSalary', headerName:'BasicSalary'},
      { field: 'Allowance', headerName: 'Allowance' },
  { field: 'CreateAt', headerName: 'Created At' },
  { field: 'UpdateAt', headerName: 'Updated At' },
  { field: 'JobDescription', headerName: 'Job Description' },
  { field: 'ContractTerm', headerName: 'Contract Term' },
  { field: 'WorkLocation', headerName: 'Work Location' },
  { field: 'Leaveofabsence', headerName: 'Leave of Absence' }
  ];

  public gridOptions: GridOptions<Contract> = {
    rowSelection: 'multiple',
    columnDefs: this.columnDefs,
  };

  public defaultColDef = {
    flex: 1,
    minWidth: 100,
    sortable: true,
    resizable: true,
    filter: true
  };

  public pagination = true;
  public paginationPageSize = 5;
  public paginationPageSizeSelector = [5, 10, 15, 20, 25, 30, 35, 40];

  constructor(
    private contractService: ContractService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadContracts();
    this.contract = this.contractService.getContractData();
  }

  onGridReady(event: GridReadyEvent<Contract>) {
    this.gridApi = event.api;
  }

  onBtExport() {
    if (this.gridApi) {
      this.gridApi.exportDataAsCsv({
        fileName: 'Danh sách hợp đồng.csv',
        columnKeys: ['ContractId', 'ContractName', 'EmployeeId', 'ContractType', 'StartDate', 'EndDate'],
      });
    } else {
      alert('Grid API is not initialized.');
    }
  }

  loadContracts() {
    this.contractService.getContracts().subscribe({
      next: (contracts: Contract[]) => {
        console.log('Contracts:', contracts);
        this.contracts = contracts;
        this.rowData = contracts;
      },
      error: (err) => {
        console.error('Error loading contracts:', err);
        this.toastr.error('Failed to load contracts');
      }
    });
  }

  onEditRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const contractData = selectedRows[0];
      this.contractService.setContractData(contractData);

      if (contractData.ContractId) {
        this.router.navigate(['/contract-edit', contractData.ContractId]);
      } else {
        alert('Selected contract does not have a valid ID.');
      }
    } else {
      alert('Please select one contract to edit.');
    }
  }

  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const contractData = selectedRows[0];
      const id = contractData.ContractId;
      this.contractService.DeleteContract(id).subscribe({
        next: _ => {
          this.toastr.success('Contract deleted successfully');
          this.loadContracts();
        },
        error: (err: any) => {
          console.error(err);
          this.toastr.error('Failed to delete contract');
        }
      });
    } else {
      alert('Please select one contract to delete.');
    }
  }

  onSearch() {
    if (this.searchText) {
      const searchTextLower = this.searchText.toLowerCase();
      this.rowData = this.contracts.filter(row =>
        Object.values(row).some(value =>
          typeof value === 'string' && value.toLowerCase().includes(searchTextLower)
        )
      );
    } else {
      this.rowData = [...this.contracts];
    }

    this.gridApi.setGridOption('rowData', this.rowData);
  }

  onRowClicked(event: any): void {
  const contract = event.data;
  if (contract && contract.ContractId) {
    this.router.navigate(['/contract', contract.ContractId]);
  } else {
    console.error('Invalid department data:', contract);
    this.toastr.error('Unable to redirect due to missing contract information.');
  }
}
  onRowSelected(contractId: number) {
  this.selectedContractId = contractId;
}

moveToLeave(): void {
  const selectedRows = this.gridApi.getSelectedRows();

  if (selectedRows.length === 1) {
    const selected = selectedRows[0];

    // Gửi sang Leave API trước
    this.contractService.addLeave(selected).subscribe({
      next: () => {
        // Sau khi lưu vào bảng Leave, tiến hành xoá Contract
        this.contractService.DeleteContract(selected.ContractId).subscribe({
          next: () => {
            this.toastr.success('Contract moved to Leave successfully');
            this.contracts = this.contracts.filter(c => c.ContractId !== selected.ContractId);
            this.rowData = [...this.contracts];

            // Điều hướng sang trang Leave để xem thông tin đã lưu
            this.router.navigate(['/leave'], {
              queryParams: { contractId: selected.ContractId }
            });
          },
          error: () => this.toastr.error('Failed to delete contract')
        });
      },
      error: () => this.toastr.error('Failed to save leave record')
    });
  } else {
    alert('Please select a contract to move to Leave.');
  }
}
}
