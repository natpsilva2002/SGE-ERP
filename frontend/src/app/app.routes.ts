import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { loginGuard } from './core/guards/login.guard';
import { roleGuard } from './core/guards/role.guard';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { AppRoles } from './core/auth/app-roles';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [loginGuard],
    loadComponent: () => import('./features/auth/login/login.component')
      .then((m) => m.LoginComponent)
  },
  {
    path: 'forbidden',
    canActivate: [authGuard],
    loadComponent: () => import('./features/errors/forbidden/forbidden.component')
      .then((m) => m.ForbiddenComponent)
  },
  {
    path: 'app',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [roleGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/placeholders/home-page.component')
          .then((m) => m.HomePageComponent)
      },
      {
        path: 'solicitacoes',
        data: { roles: [AppRoles.Warehouse, AppRoles.Buyer, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/purchase-requests/pages/purchase-request-list/purchase-request-list.component')
          .then((m) => m.PurchaseRequestListComponent)
      },
      {
        path: 'solicitacoes/nova',
        data: { roles: [AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Admin] },
        loadComponent: () => import('./features/purchase-requests/pages/purchase-request-create/purchase-request-create.component')
          .then((m) => m.PurchaseRequestCreateComponent)
      },
      {
        path: 'solicitacoes/:id',
        data: { roles: [AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/purchase-requests/pages/purchase-request-detail/purchase-request-detail.component')
          .then((m) => m.PurchaseRequestDetailComponent)
      },
      {
        path: 'cotacoes',
        data: { roles: [AppRoles.Buyer, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/quotations/pages/quotation-list/quotation-list.component')
          .then((m) => m.QuotationListComponent)
      },
      {
        path: 'cotacoes/nova',
        data: { roles: [AppRoles.Buyer, AppRoles.Admin] },
        loadComponent: () => import('./features/quotations/pages/quotation-create/quotation-create.component')
          .then((m) => m.QuotationCreateComponent)
      },
      {
        path: 'cotacoes/:id',
        data: { roles: [AppRoles.Buyer, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/quotations/pages/quotation-detail/quotation-detail.component')
          .then((m) => m.QuotationDetailComponent)
      },
      {
        path: 'ordens-compra',
        data: { roles: [AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/purchase-orders/pages/purchase-order-list/purchase-order-list.component')
          .then((m) => m.PurchaseOrderListComponent)
      },
      {
        path: 'ordens-compra/:id',
        data: { roles: [AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/purchase-orders/pages/purchase-order-detail/purchase-order-detail.component')
          .then((m) => m.PurchaseOrderDetailComponent)
      },
      {
        path: 'ordens-de-compra',
        pathMatch: 'full',
        redirectTo: 'ordens-compra'
      },
      {
        path: 'ordens-servico',
        data: { roles: [AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/service-orders/pages/service-order-list/service-order-list.component')
          .then((m) => m.ServiceOrderListComponent)
      },
      {
        path: 'ordens-servico/nova',
        data: { roles: [AppRoles.Admin] },
        loadComponent: () => import('./features/service-orders/pages/service-order-create/service-order-create.component')
          .then((m) => m.ServiceOrderCreateComponent)
      },
      {
        path: 'ordens-servico/:id',
        data: { roles: [AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/service-orders/pages/service-order-detail/service-order-detail.component')
          .then((m) => m.ServiceOrderDetailComponent)
      },
      {
        path: 'recebimentos',
        data: { roles: [AppRoles.Buyer, AppRoles.Warehouse, AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/receipts/pages/receipts-page/receipts-page.component')
          .then((m) => m.ReceiptsPageComponent)
      },
      {
        path: 'financeiro',
        data: { roles: [AppRoles.Finance, AppRoles.Admin] },
        loadComponent: () => import('./features/finance/pages/finance-page/finance-page.component')
          .then((m) => m.FinancePageComponent)
      },
      {
        path: 'cadastros',
        data: { roles: [AppRoles.Admin] },
        loadComponent: () => import('./features/placeholders/registrations-page.component')
          .then((m) => m.RegistrationsPageComponent)
      },
      {
        path: 'administracao',
        data: { roles: [AppRoles.Admin] },
        loadComponent: () => import('./features/administration/pages/administration-page/administration-page.component')
          .then((m) => m.AdministrationPageComponent)
      }
    ]
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'app'
  },
  {
    path: '**',
    redirectTo: 'app'
  }
];
