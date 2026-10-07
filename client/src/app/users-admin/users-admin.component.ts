import { Component, OnInit } from '@angular/core';
import { UserService } from '../_services/user.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-users-admin',
  templateUrl: './users-admin.component.html',
  styleUrls: ['./users-admin.component.css']
})
export class UsersAdminComponent implements OnInit {
  users: any[] = [];
  isLoading = false;
  q = '';
  page = 1;
  pageSize = 20;
  total = 0;

  createForm = {
    UserName: '',
    Email: '',
    PhoneNumber: '',
    Address: '',
    Role: 'employee',
    Password: '',
    IsActive: true,
    EmployeeId: null as number | null
  };

  constructor(
    private userService: UserService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getUsers(this.page, this.pageSize, this.q).subscribe({
      next: (res: any) => {
        this.users = res?.items || [];
        this.total = res?.total || 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load users');
      }
    });
  }

  search(): void {
    this.page = 1;
    this.loadUsers();
  }

  nextPage(): void {
    if (this.page * this.pageSize >= this.total) return;
    this.page += 1;
    this.loadUsers();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.loadUsers();
  }

  createUser(): void {
    if (!this.createForm.UserName || !this.createForm.Password) {
      this.toastr.error('Username and password are required');
      return;
    }
    this.userService.AdminCreateUser(this.createForm).subscribe({
      next: () => {
        this.toastr.success('User created');
        this.createForm = { UserName: '', Email: '', PhoneNumber: '', Address: '', Role: 'employee', Password: '', IsActive: true, EmployeeId: null };
        this.loadUsers();
      },
      error: (err) => {
        const msg = err?.error?.message || 'Failed to create user';
        this.toastr.error(msg);
      }
    });
  }

  updateRole(user: any, role: string): void {
    this.userService.updateRole(user.UserId, role).subscribe({
      next: () => {
        this.toastr.success('Role updated');
        user.Role = role;
      },
      error: (err) => {
        const msg = err?.error?.message || 'Failed to update role';
        this.toastr.error(msg);
      }
    });
  }

  updateActive(user: any, isActive: boolean): void {
    this.userService.updateActive(user.UserId, isActive).subscribe({
      next: () => {
        this.toastr.success('Status updated');
        user.IsActive = isActive;
      },
      error: (err) => {
        const msg = err?.error?.message || 'Failed to update status';
        this.toastr.error(msg);
      }
    });
  }

  updateEmployeeLink(user: any, employeeIdValue: string): void {
    const employeeId = employeeIdValue ? Number(employeeIdValue) : null;
    this.userService.updateEmployeeLink(user.UserId, employeeId).subscribe({
      next: () => {
        this.toastr.success('Employee link updated');
        user.EmployeeId = employeeId;
      },
      error: (err) => {
        const msg = err?.error?.message || 'Failed to update employee link';
        this.toastr.error(msg);
      }
    });
  }

  deleteUser(user: any): void {
    this.userService.DeleteUser(user.UserId).subscribe({
      next: () => {
        this.toastr.success('User deleted');
        this.loadUsers();
      },
      error: (err) => {
        const msg = err?.error?.message || 'Failed to delete user';
        this.toastr.error(msg);
      }
    });
  }
}
