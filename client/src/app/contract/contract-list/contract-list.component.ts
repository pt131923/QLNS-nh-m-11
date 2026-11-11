import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { ColDef, GridApi, GridReadyEvent, GridOptions, ValueFormatterParams } from 'ag-grid-community';
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

  // ==== BIẾN BỘ LỌC ====
  filter = {
    employeeName: '',
    contractType: undefined as string | undefined,
    workLocation: undefined as string | undefined
  };

  // ==== DANH SÁCH LỰA CHỌN ====
  contractTypeOptions: string[] = ['Chính thức', 'Thời vụ', 'Thử việc', 'Hợp đồng dịch vụ'];
  workLocationOptions: string[] = ['Hà Nội', 'Hồ Chí Minh', 'Đà Nẵng', 'Nghỉ T7, CN', 'Làm việc từ xa'];

  // ==== THỐNG KÊ ====
  showStats = false;
  statsData: any = null;

  // ==== CẤU HÌNH CỘT GRID ====
  public columnDefs: ColDef<Contract>[] = [
    {
      headerName: '',
      checkboxSelection: true,
      width: 40,
      headerCheckboxSelection: true,
      headerCheckboxSelectionFilteredOnly: true,
      pinned: 'left'
    },
    { field: 'ContractId', headerName: 'Mã HĐ', width: 90 },
    { field: 'ContractName', headerName: 'Tên hợp đồng', width: 180 },
    { field: 'EmployeeName', headerName: 'Tên nhân viên', width: 160 },
    { field: 'ContractType', headerName: 'Loại HĐ', width: 130 },
    {
      field: 'StartDate',
      headerName: 'Ngày bắt đầu',
      width: 130,
      valueFormatter: (params: ValueFormatterParams<Contract, Date>) => {
        return params.value ? new Date(params.value).toLocaleDateString('vi-VN') : '';
      }
    },
    {
      field: 'EndDate',
      headerName: 'Ngày kết thúc',
      width: 130,
      valueFormatter: (params: ValueFormatterParams<Contract, Date>) => {
        return params.value ? new Date(params.value).toLocaleDateString('vi-VN') : '';
      }
    },
    {
      field: 'BasicSalary',
      headerName: 'Lương cơ bản',
      width: 140,
      valueFormatter: (params: ValueFormatterParams<Contract, number>) => {
        return params.value != null ? Number(params.value).toLocaleString('vi-VN') + ' đ' : '';
      }
    },
    {
      field: 'Allowance',
      headerName: 'Phụ cấp',
      width: 120,
      valueFormatter: (params: ValueFormatterParams<Contract, number>) => {
        return params.value != null ? Number(params.value).toLocaleString('vi-VN') + ' đ' : '';
      }
    },
    {
      field: 'CreateAt',
      headerName: 'Ngày tạo',
      width: 150,
      valueFormatter: (params: ValueFormatterParams<Contract, Date>) => {
        return params.value ? new Date(params.value).toLocaleDateString('vi-VN') : '';
      }
    },
    {
      field: 'UpdateAt',
      headerName: 'Cập nhật lúc',
      width: 150,
      valueFormatter: (params: ValueFormatterParams<Contract, Date>) => {
        return params.value ? new Date(params.value).toLocaleDateString('vi-VN') : '';
      }
    },
    { field: 'JobDescription', headerName: 'Chức vụ', width: 150 },
    { field: 'ContractTerm', headerName: 'Thời hạn', width: 110 },
    { field: 'WorkLocation', headerName: 'Địa điểm', width: 130 },
    { field: 'Leaveofabsence', headerName: 'Phép/năm', width: 110 }
  ];

  public gridOptions: GridOptions<Contract> = {
    rowSelection: 'multiple',
    columnDefs: this.columnDefs,
  };

  public defaultColDef = {
    resizable: true,
    sortable: true,
    filter: true,
    flex: 1,
    minWidth: 80
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

  // ==== XUẤT EXCEL ====
  onBtExport() {
    if (this.gridApi) {
      this.gridApi.exportDataAsCsv({
        fileName: 'Danh sách hợp đồng_' + new Date().toLocaleDateString('vi-VN') + '.csv',
        columnKeys: ['ContractId', 'ContractName', 'EmployeeName', 'ContractType', 'StartDate', 'EndDate', 'BasicSalary', 'Allowance', 'CreateAt', 'UpdateAt', 'JobDescription', 'ContractTerm', 'WorkLocation', 'Leaveofabsence'],
      });
    } else {
      alert('Không thể xuất dữ liệu, vui lòng thử lại sau.');
    }
  }

  // ==== TẢI DANH SÁCH ====
  loadContracts() {
    this.contractService.getContracts().subscribe({
      next: (contracts: Contract[]) => {
        console.log('Contracts:', contracts);
        this.contracts = contracts;
        this.rowData = contracts;
        this.updateStats();
      },
      error: (err) => {
        console.error('Error loading contracts:', err);
        this.toastr.error('Tải danh sách hợp đồng thất bại');
      }
    });
  }

  // ==== TÌM KIẾM / LỌC ====
  onSearch() {
    let result = [...this.contracts];

    // Lọc theo tên
    if (this.filter.employeeName) {
      const keyword = this.filter.employeeName.toLowerCase();
      result = result.filter(c =>
        c.EmployeeName?.toLowerCase().includes(keyword)
      );
    }

    // Lọc theo loại hợp đồng
    if (this.filter.contractType) {
      result = result.filter(c => c.ContractType === this.filter.contractType);
    }

    // Lọc theo địa điểm
    if (this.filter.workLocation) {
      result = result.filter(c => c.WorkLocation === this.filter.workLocation);
    }

    this.rowData = result;
    this.gridApi?.setGridOption('rowData', this.rowData);
    this.updateStats();
  }

  // ==== XÓA BỘ LỌC ====
  onClearFilter() {
    this.filter = {
      employeeName: '',
      contractType: undefined,
      workLocation: undefined
    };
    this.rowData = [...this.contracts];
    this.gridApi?.setGridOption('rowData', this.rowData);
    this.updateStats();
  }

  // ==== CẬP NHẬT THỐNG KÊ ====
  updateStats() {
    const total = this.rowData.length;
    const byContractType: any = {};
    const byWorkLocation: any = {};

    this.rowData.forEach(c => {
      if (c.ContractType) {
        byContractType[c.ContractType] = (byContractType[c.ContractType] || 0) + 1;
      }
      if (c.WorkLocation) {
        byWorkLocation[c.WorkLocation] = (byWorkLocation[c.WorkLocation] || 0) + 1;
      }
    });

    this.statsData = {
      total,
      byContractType: Object.entries(byContractType).map(([type, count]) => ({ typeName: type, count })),
      byWorkLocation: Object.entries(byWorkLocation).map(([loc, count]) => ({ location: loc, count }))
    };
  }

  getStatPercent(count: number): number {
    const total = this.statsData?.total || 0;
    return total > 0 ? Math.round((count / total) * 100) : 0;
  }

  toggleStats() {
    this.showStats = !this.showStats;
  }

  // ==== XUẤT PDF ====
  onExportPDF() {
    this.toastr.info('Chức năng xuất PDF đang được phát triển');
  }

  // ==== SỬA ====
  onEditRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const contractData = selectedRows[0];
      this.contractService.setContractData(contractData);

      if (contractData.ContractId) {
        this.router.navigate(['/contract-edit', contractData.ContractId]);
      } else {
        alert('Không tìm thấy mã hợp đồng.');
      }
    } else {
      alert('Vui lòng chọn một hợp đồng để sửa.');
    }
  }

  // ==== XÓA ====
  onDeleteRow() {
    const selectedRows = this.gridApi.getSelectedRows();
    if (selectedRows.length === 1) {
      const contractData = selectedRows[0];
      const id = contractData.ContractId;
      if (confirm('Bạn có chắc chắn muốn xóa hợp đồng này?')) {
        this.contractService.DeleteContract(id).subscribe({
          next: _ => {
            this.toastr.success('Xóa hợp đồng thành công');
            this.loadContracts();
          },
          error: (err: any) => {
            console.error(err);
            this.toastr.error('Xóa hợp đồng thất bại');
          }
        });
      }
    } else {
      alert('Vui lòng chọn một hợp đồng để xóa.');
    }
  }

  onRowClicked(event: any): void {
    const contract = event.data;
    if (contract && contract.ContractId) {
      this.router.navigate(['/contract', contract.ContractId]);
    } else {
      console.error('Invalid contract data:', contract);
      this.toastr.error('Không thể chuyển trang do thiếu thông tin hợp đồng.');
    }
  }

  onRowSelected(contractId: number) {
    this.selectedContractId = contractId;
  }

  // ==== CHUYỂN SANG BẢNG PHÉP ====
  moveToLeave(): void {
    const selectedRows = this.gridApi.getSelectedRows();

    if (selectedRows.length === 1) {
      const selected = selectedRows[0];

      this.contractService.addLeave(selected).subscribe({
        next: () => {
          this.contractService.DeleteContract(selected.ContractId).subscribe({
            next: () => {
              this.toastr.success('Chuyển sang bảng Phép thành công');
              this.contracts = this.contracts.filter(c => c.ContractId !== selected.ContractId);
              this.rowData = [...this.contracts];
              this.updateStats();

              this.router.navigate(['/leave'], {
                queryParams: { contractId: selected.ContractId }
              });
            },
            error: () => this.toastr.error('Xóa hợp đồng thất bại')
          });
        },
        error: () => this.toastr.error('Lưu thông tin phép thất bại')
      });
    } else {
      alert('Vui lòng chọn một hợp đồng để chuyển sang bảng Phép.');
    }
  }
}
