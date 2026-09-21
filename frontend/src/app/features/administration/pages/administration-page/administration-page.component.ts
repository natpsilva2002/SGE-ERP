import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin, take } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { AdminRole, AdminUser } from '../../models/administration.models';
import { AdministrationService } from '../../services/administration.service';

type AdministrationTab = 'users' | 'roles';

interface PermissionRow {
  action: string;
  roles: string[];
}

@Component({
  selector: 'app-administration-page',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule],
  templateUrl: './administration-page.component.html',
  styleUrl: './administration-page.component.css'
})
export class AdministrationPageComponent implements OnInit {
  private readonly service = inject(AdministrationService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly users = signal<AdminUser[]>([]);
  readonly roles = signal<AdminRole[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly activeTab = signal<AdministrationTab>('users');
  readonly formOpen = signal(false);
  readonly editingUserId = signal<string | null>(null);

  readonly roleOrder: string[] = [
    AppRoles.Admin,
    AppRoles.Warehouse,
    AppRoles.Buyer,
    AppRoles.Finance,
  ];

  readonly permissionRows: PermissionRow[] = [
    { action: 'Criar solicitacao', roles: [AppRoles.Warehouse, AppRoles.Buyer, AppRoles.Admin] },
    { action: 'Criar cotacao', roles: [AppRoles.Buyer, AppRoles.Admin] },
    { action: 'Cadastrar orcamento', roles: [AppRoles.Buyer, AppRoles.Admin] },
    { action: 'Enviar cotacao para aprovacao', roles: [AppRoles.Buyer, AppRoles.Admin] },
    { action: 'Escolher vencedor', roles: [AppRoles.Admin] },
    { action: 'Aprovar cotacao', roles: [AppRoles.Admin] },
    { action: 'Receber material', roles: [AppRoles.Warehouse, AppRoles.Admin] },
    { action: 'Registrar pagamento', roles: [AppRoles.Finance, AppRoles.Admin] }
  ];

  readonly userForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    password: [''],
    roleId: ['', [Validators.required]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      users: this.service.getUsers(),
      roles: this.service.getRoles()
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ users, roles }) => {
          this.users.set(users);
          this.roles.set(roles);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  setTab(tab: AdministrationTab): void {
    this.activeTab.set(tab);
  }

  openCreate(): void {
    this.editingUserId.set(null);
    this.formOpen.set(true);
    this.userForm.reset({
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      password: '',
      roleId: '',
      isActive: true
    });
  }

  openEdit(user: AdminUser): void {
    this.editingUserId.set(user.id);
    this.formOpen.set(true);
    this.userForm.reset({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: user.phoneNumber ?? '',
      password: '',
      roleId: user.roleId,
      isActive: user.isActive
    });
  }

  cancelForm(): void {
    this.formOpen.set(false);
    this.editingUserId.set(null);
    this.userForm.reset();
  }

  saveUser(): void {
    if (this.userForm.invalid || this.saving()) {
      this.userForm.markAllAsTouched();
      return;
    }

    const value = this.userForm.getRawValue();
    const editingId = this.editingUserId();

    if (!editingId && !value.password.trim()) {
      this.toast.error('Informe a senha inicial.');
      return;
    }

    this.saving.set(true);

    const request = editingId
      ? this.service.updateUser(editingId, {
          firstName: value.firstName,
          lastName: value.lastName,
          email: value.email,
          phoneNumber: value.phoneNumber || null,
          roleId: value.roleId,
          isActive: value.isActive
        })
      : this.service.createUser({
          firstName: value.firstName,
          lastName: value.lastName,
          email: value.email,
          password: value.password,
          phoneNumber: value.phoneNumber || null,
          roleId: value.roleId,
          isActive: value.isActive
        });

    request.pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success(editingId ? 'Usuario atualizado.' : 'Usuario criado.');
          this.cancelForm();
          this.load();
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  deactivate(user: AdminUser): void {
    this.confirm.confirm({
      title: 'Inativar usuario',
      message: `Inativar ${user.firstName} ${user.lastName}?`,
      confirmLabel: 'Inativar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.service.deactivateUser(user.id).subscribe({
        next: () => {
          this.toast.success('Usuario inativado.');
          this.load();
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
    });
  }

  roleDescription(roleName: string): string {
    const descriptions: Record<string, string> = {
      [AppRoles.Warehouse]: 'Cria solicitacoes e registra recebimentos.',
      [AppRoles.Buyer]: 'Gerencia cotacoes e orcamentos.',
      [AppRoles.Admin]: 'Acesso total e aprovacoes operacionais.',
      [AppRoles.Finance]: 'Responsavel por pagamentos.',
    };

    return descriptions[roleName] ?? 'Perfil do sistema.';
  }

  roleName(roleId: string): string {
    return this.roles().find((role) => role.id === roleId)?.name ?? '';
  }

  sortedRoles(): AdminRole[] {
    return [...this.roles()].sort((a, b) =>
      this.roleOrder.indexOf(a.name) - this.roleOrder.indexOf(b.name));
  }
}
