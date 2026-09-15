import { AsyncPipe, NgFor, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { AppRole, AppRoles } from '../../core/auth/app-roles';
import { AuthService } from '../../core/auth/auth.service';
import { AuthUser } from '../../core/auth/auth.models';
import { ConfirmDialogComponent } from '../../shared/feedback/confirm-dialog.component';
import { ToastComponent } from '../../shared/feedback/toast.component';

interface MenuItem {
  label: string;
  route: string;
  roles: AppRole[];
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    AsyncPipe,
    ConfirmDialogComponent,
    NgFor,
    NgIf,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    ToastComponent
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css'
})
export class MainLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly user$ = this.authService.currentUser$;
  drawerOpen = false;

  readonly menuItems: MenuItem[] = [
    {
      label: 'Inicio',
      route: '/app',
      roles: [
        AppRoles.Requester,
        AppRoles.Approver,
        AppRoles.Buyer,
        AppRoles.Warehouse,
        AppRoles.Finance,
        AppRoles.Admin
      ]
    },
    {
      label: 'Solicitacoes',
      route: '/app/solicitacoes',
      roles: [AppRoles.Requester, AppRoles.Approver, AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Admin]
    },
    {
      label: 'Cotacoes',
      route: '/app/cotacoes',
      roles: [AppRoles.Approver, AppRoles.Buyer, AppRoles.Admin]
    },
    {
      label: 'Ordens de Compra',
      route: '/app/ordens-compra',
      roles: [AppRoles.Approver, AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Finance, AppRoles.Admin]
    },
    {
      label: 'Ordens de Servico',
      route: '/app/ordens-servico',
      roles: [AppRoles.Approver, AppRoles.Finance, AppRoles.Admin]
    },
    {
      label: 'Recebimentos',
      route: '/app/recebimentos',
      roles: [AppRoles.Warehouse, AppRoles.Admin]
    },
    {
      label: 'Financeiro',
      route: '/app/financeiro',
      roles: [AppRoles.Finance, AppRoles.Admin]
    },
    {
      label: 'Cadastros',
      route: '/app/cadastros',
      roles: [AppRoles.Admin]
    },
    {
      label: 'Administracao',
      route: '/app/administracao',
      roles: [AppRoles.Admin]
    }
  ];

  readonly visibleMenuItems$ = this.user$.pipe(
    map((user) => this.getVisibleMenuItems(user))
  );

  logout(): void {
    this.closeDrawer();
    this.authService.logout(false);
    void this.router.navigate(['/login']);
  }

  openDrawer(): void {
    this.drawerOpen = true;
  }

  closeDrawer(): void {
    this.drawerOpen = false;
  }

  private getVisibleMenuItems(user: AuthUser | null): MenuItem[] {
    if (!user) {
      return [];
    }

    if (user.role === AppRoles.Admin) {
      return this.menuItems;
    }

    return this.menuItems.filter((item) => item.roles.includes(user.role as AppRole));
  }
}
