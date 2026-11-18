import { Component, OnInit } from '@angular/core';
import { UserService } from '../_services/user.service';
import { EmployeeService } from '../_services/employee.service';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-users-admin',
  templateUrl: './users-admin.component.html',
  styleUrls: ['./users-admin.component.css']
})
export class UsersAdminComponent implements OnInit {
  users: any[] = [];
  allUsers: any[] = [];
  employees: any[] = [];

  isLoading = false;
  isSubmitting = false;
  showCreateCard = false;

  // Search & Filter
  q = '';
  roleFilter = 'all';
  statusFilter = 'all';

  // Pagination
  page = 1;
  pageSize = 10;
  total = 0;

  // Create form
  createForm = {
    UserName: '',
    Password: '',
    Email: '',
    PhoneNumber: '',
    Address: '',
    Role: 'user',
    IsActive: true,
    EmployeeId: null as number | null
  };

  // Password reset modal
  showPasswordModal = false;
  selectedUserForReset: any = null;
  newPassword = '';

  constructor(
    private userService: UserService,
    private employeeService: EmployeeService,
    private toastr: ToastrService,
    public translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.employeeService.getEmployees().subscribe({
      next: (data) => {
        this.employees = Array.isArray(data) ? data : [];
      },
      error: (err) => {
        console.error('Failed to load employees for user linking:', err);
      }
    });
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getUsers(this.page, this.pageSize, this.q).subscribe({
      next: (res: any) => {
        if (Array.isArray(res)) {
          // Backend returned full list
          this.allUsers = res;
          this.applyClientSideFilterAndPagination();
        } else if (res && Array.isArray(res.items)) {
          // Backend returned paginated object
          this.users = res.items;
          this.total = res.total ?? res.items.length;
          this.allUsers = res.items;
        } else {
          this.users = [];
          this.total = 0;
          this.allUsers = [];
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Failed to load users:', err);
        this.toastr.error('Không thể tải danh sách người dùng');
      }
    });
  }

  private applyClientSideFilterAndPagination(): void {
    let filtered = [...this.allUsers];

    // Search query
    if (this.q.trim()) {
      const term = this.q.trim().toLowerCase();
      filtered = filtered.filter(u =>
        (u.UserName && u.UserName.toLowerCase().includes(term)) ||
        (u.Email && u.Email.toLowerCase().includes(term)) ||
        (u.PhoneNumber && u.PhoneNumber.includes(term)) ||
        (u.Role && u.Role.toLowerCase().includes(term)) ||
        (u.UserId && String(u.UserId).includes(term))
      );
    }

    // Role filter
    if (this.roleFilter !== 'all') {
      filtered = filtered.filter(u => (u.Role || 'user').toLowerCase() === this.roleFilter.toLowerCase());
    }

    // Status filter
    if (this.statusFilter === 'active') {
      filtered = filtered.filter(u => u.IsActive !== false);
    } else if (this.statusFilter === 'inactive') {
      filtered = filtered.filter(u => u.IsActive === false);
    }

    this.total = filtered.length;

    // Slice for current page
    const start = (this.page - 1) * this.pageSize;
    this.users = filtered.slice(start, start + this.pageSize);
  }

  search(): void {
    this.page = 1;
    this.loadUsers();
  }

  resetFilters(): void {
    this.q = '';
    this.roleFilter = 'all';
    this.statusFilter = 'all';
    this.page = 1;
    this.loadUsers();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  nextPage(): void {
    if (this.page >= this.totalPages) return;
    this.page += 1;
    this.loadUsers();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.loadUsers();
  }

  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages) {
      this.page = p;
      this.loadUsers();
    }
  }

  toggleCreateCard(): void {
    this.showCreateCard = !this.showCreateCard;
  }

  createUser(): void {
    if (!this.createForm.UserName || !this.createForm.UserName.trim()) {
      this.toastr.warning('Vui lòng nhập tên đăng nhập');
      return;
    }
    if (!this.createForm.Password || this.createForm.Password.length < 6) {
      this.toastr.warning('Mật khẩu phải có ít nhất 6 ký tự');
      return;
    }

    this.isSubmitting = true;
    this.userService.CreateUser(this.createForm).subscribe({
      next: (res: any) => {
        this.isSubmitting = false;
        const msg = res?.message || 'Tạo tài khoản thành công!';
        this.toastr.success(msg);
        this.createForm = {
          UserName: '',
          Password: '',
          Email: '',
          PhoneNumber: '',
          Address: '',
          Role: 'user',
          IsActive: true,
          EmployeeId: null
        };
        this.showCreateCard = false;
        this.loadUsers();
      },
      error: (err) => {
        this.isSubmitting = false;
        const msg = err?.error?.message || err?.message || 'Không thể tạo tài khoản';
        this.toastr.error(msg);
      }
    });
  }

  updateRole(user: any, role: string): void {
    this.userService.updateRole(user.UserId, role).subscribe({
      next: () => {
        this.toastr.success(`Đã cập nhật vai trò cho [${user.UserName}] thành ${role}`);
        user.Role = role;
      },
      error: (err: any) => {
        const msg = err?.error?.message || 'Cập nhật vai trò thất bại';
        this.toastr.error(msg);
      }
    });
  }

  updateActive(user: any, isActive: boolean): void {
    this.userService.updateActive(user.UserId, isActive).subscribe({
      next: () => {
        const statusText = isActive ? 'Hoạt động' : 'Tạm khóa';
        this.toastr.success(`Đã chuyển trạng thái tài khoản [${user.UserName}] sang: ${statusText}`);
        user.IsActive = isActive;
      },
      error: (err: any) => {
        const msg = err?.error?.message || 'Cập nhật trạng thái thất bại';
        this.toastr.error(msg);
      }
    });
  }

  updateEmployeeLink(user: any, employeeIdValue: any): void {
    const employeeId = employeeIdValue ? Number(employeeIdValue) : null;
    this.userService.updateEmployeeLink(user.UserId, employeeId).subscribe({
      next: () => {
        this.toastr.success(`Đã cập nhật liên kết nhân viên cho [${user.UserName}]`);
        user.EmployeeId = employeeId;
      },
      error: (err: any) => {
        const msg = err?.error?.message || 'Cập nhật liên kết nhân viên thất bại';
        this.toastr.error(msg);
      }
    });
  }

  deleteUser(user: any): void {
    const confirmMsg = `Bạn có chắc chắn muốn xóa tài khoản [${user.UserName}] (ID: ${user.UserId}) không? Hành động này không thể hoàn tác!`;
    if (!confirm(confirmMsg)) {
      return;
    }

    this.userService.DeleteUser(user.UserId).subscribe({
      next: () => {
        this.toastr.success(`Đã xóa tài khoản [${user.UserName}]`);
        this.loadUsers();
      },
      error: (err) => {
        const msg = err?.error?.message || 'Xóa tài khoản thất bại';
        this.toastr.error(msg);
      }
    });
  }

  // Reset password modal handlers
  openResetPasswordModal(user: any): void {
    this.selectedUserForReset = user;
    this.newPassword = '';
    this.showPasswordModal = true;
  }

  closeResetPasswordModal(): void {
    this.showPasswordModal = false;
    this.selectedUserForReset = null;
    this.newPassword = '';
  }

  confirmResetPassword(): void {
    if (!this.newPassword || this.newPassword.length < 6) {
      this.toastr.warning('Mật khẩu mới phải có ít nhất 6 ký tự');
      return;
    }

    const payload = {
      Password: '',
      NewPassword: this.newPassword
    };

    this.userService.updatePassword(this.selectedUserForReset.UserId, payload).subscribe({
      next: () => {
        this.toastr.success(`Đã đổi mật khẩu cho [${this.selectedUserForReset.UserName}]`);
        this.closeResetPasswordModal();
      },
      error: (err) => {
        const msg = err?.error?.message || 'Không thể đổi mật khẩu';
        this.toastr.error(msg);
      }
    });
  }

  getEmployeeDisplayName(employeeId: number | null): string {
    if (!employeeId) return '--';
    const emp = this.employees.find(e => (e.EmployeeId ?? e.employeeId) === employeeId);
    if (emp) {
      const name = emp.EmployeeName ?? emp.employeeName ?? '';
      return `#${employeeId} - ${name}`;
    }
    return `#${employeeId}`;
  }

  // KPI Stats Getters
  get totalAdminCount(): number {
    return this.allUsers.filter(u => (u.Role || '').toLowerCase() === 'admin').length;
  }

  get totalManagerCount(): number {
    return this.allUsers.filter(u => (u.Role || '').toLowerCase() === 'manager').length;
  }

  get totalUserCount(): number {
    return this.allUsers.filter(u => {
      const r = (u.Role || '').toLowerCase();
      return r === 'user' || r === 'employee';
    }).length;
  }

  get totalActiveCount(): number {
    return this.allUsers.filter(u => u.IsActive !== false).length;
  }

  get totalInactiveCount(): number {
    return this.allUsers.filter(u => u.IsActive === false).length;
  }
}
