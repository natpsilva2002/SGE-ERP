import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, of, take } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { PurchaseRequest } from '../../models/purchase-request.models';
import {
  PurchaseRequestStatus,
  getPurchaseRequestStatusLabel,
  purchaseRequestStatusOptions
} from '../../models/purchase-request-status';
import {
  PurchaseRequestType,
  getPurchaseRequestTypeLabel
} from '../../models/purchase-request-type';
import { getApiErrorMessage } from '../../services/api-error';
import { PurchaseRequestService } from '../../services/purchase-request.service';
import { QuotationService } from '../../../quotations/services/quotation.service';
import { ServiceOrder } from '../../../service-orders/models/service-order.models';
import { ServiceOrderService } from '../../../service-orders/services/service-order.service';

@Component({
  selector: 'app-purchase-request-list',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './purchase-request-list.component.html',
  styleUrl: './purchase-request-list.component.css'
})
export class PurchaseRequestListComponent implements OnInit {
  private readonly service = inject(PurchaseRequestService);
  private readonly quotationService = inject(QuotationService);
  private readonly serviceOrderService = inject(ServiceOrderService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl<number | 'all'>('all', { nonNullable: true });
  readonly startDateControl = new FormControl('', { nonNullable: true });
  readonly endDateControl = new FormControl('', { nonNullable: true });
  readonly statusOptions = purchaseRequestStatusOptions;

  readonly loading = signal(false);
  readonly error = signal('');
  readonly requests = signal<PurchaseRequest[]>([]);
  readonly serviceOrders = signal<ServiceOrder[]>([]);

  readonly filteredRequests = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();
    const status = this.statusControl.value;

    return this.requests().filter((request) => {
      const matchesSearch = !search ||
        request.number.toLowerCase().includes(search) ||
        request.description.toLowerCase().includes(search);
      const matchesStatus = status === 'all' || request.status === status;
      const matchesDate = this.matchesDateRange(
        request.createdAt,
        this.startDateControl.value,
        this.endDateControl.value);

      return matchesSearch && matchesStatus && matchesDate;
    });
  });

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() => this.requests.update((items) => [...items]));
    this.statusControl.valueChanges.subscribe(() => this.requests.update((items) => [...items]));
    this.startDateControl.valueChanges.subscribe(() => this.requests.update((items) => [...items]));
    this.endDateControl.valueChanges.subscribe(() => this.requests.update((items) => [...items]));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    const canReadServiceOrders = this.authService.hasRole([
      AppRoles.Approver,
      AppRoles.Finance,
      AppRoles.Admin
    ]);

    forkJoin({
      requests: this.service.getAll(),
      serviceOrders: canReadServiceOrders ? this.serviceOrderService.getAll() : of([])
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ requests, serviceOrders }) => {
          this.requests.set(requests);
          this.serviceOrders.set(serviceOrders);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  canCreate(): boolean {
    return this.authService.hasRole([
      AppRoles.Warehouse,
      AppRoles.Approver,
      AppRoles.Admin
    ]);
  }

  canSubmit(request: PurchaseRequest): boolean {
    if (request.status !== PurchaseRequestStatus.Draft) {
      return false;
    }

    if (request.type === PurchaseRequestType.Material) {
      return false;
    }

    if (request.type === PurchaseRequestType.Service) {
      return this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
    }

    return false;
  }

  canApprove(request: PurchaseRequest): boolean {
    return request.type === PurchaseRequestType.Service &&
      request.status === PurchaseRequestStatus.WaitingApproval &&
      this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
  }

  canSendToQuotation(request: PurchaseRequest): boolean {
    return false;
  }

  canCreateQuotation(request: PurchaseRequest): boolean {
    return (request.status === PurchaseRequestStatus.WaitingQuotation ||
      request.status === PurchaseRequestStatus.QuotationInProgress) &&
      request.type === PurchaseRequestType.Material &&
      this.authService.hasRole([AppRoles.Buyer, AppRoles.Approver, AppRoles.Admin]);
  }

  canCreateServiceOrder(request: PurchaseRequest): boolean {
    return request.status === PurchaseRequestStatus.Approved &&
      request.type === PurchaseRequestType.Service &&
      !this.serviceOrderForRequest(request) &&
      this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
  }

  serviceOrderForRequest(request: PurchaseRequest): ServiceOrder | null {
    if (request.type !== PurchaseRequestType.Service) {
      return null;
    }

    return this.serviceOrders().find((order) => order.purchaseRequestId === request.id) ?? null;
  }

  submitForApproval(request: PurchaseRequest): void {
    this.confirm.confirm({
      title: 'Enviar para aprovacao',
      message: `Enviar a solicitacao ${request.number} para aprovacao?`,
      confirmLabel: 'Enviar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.submitForApproval(request.id).subscribe({
          next: (updated) => this.replaceRequest(updated, 'Solicitacao enviada para aprovacao.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  approve(request: PurchaseRequest): void {
    this.confirm.confirm({
      title: 'Aprovar solicitacao',
      message: `Aprovar a solicitacao ${request.number}?`,
      confirmLabel: 'Aprovar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.approve(request.id, { observation: 'Aprovado pelo frontend.' }).subscribe({
          next: (updated) => this.replaceRequest(updated, 'Solicitacao aprovada.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  reject(request: PurchaseRequest): void {
    this.confirm.confirm({
      title: 'Rejeitar solicitacao',
      message: `Rejeitar a solicitacao ${request.number}?`,
      confirmLabel: 'Rejeitar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.reject(request.id, { observation: 'Rejeitado pelo frontend.' }).subscribe({
          next: (updated) => this.replaceRequest(updated, 'Solicitacao rejeitada.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  sendToQuotation(request: PurchaseRequest): void {
    this.confirm.confirm({
      title: 'Enviar para cotacao',
      message: `Enviar a solicitacao ${request.number} para cotacao?`,
      confirmLabel: 'Enviar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.sendToQuotation(request.id).subscribe({
          next: (updated) => this.replaceRequest(updated, 'Solicitacao enviada para cotacao.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  createQuotation(request: PurchaseRequest): void {
    this.quotationService.create({ purchaseRequestId: request.id }).subscribe({
      next: (quotation) => {
        this.toast.success('Cotacao criada com sucesso.');
        void this.router.navigate(['/app/cotacoes', quotation.id]);
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  statusLabel(status: PurchaseRequestStatus): string {
    return getPurchaseRequestStatusLabel(status);
  }

  typeLabel(type: PurchaseRequestType): string {
    return getPurchaseRequestTypeLabel(type);
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusControl.setValue('all');
    this.startDateControl.setValue('');
    this.endDateControl.setValue('');
  }

  requesterLabel(request: PurchaseRequest): string {
    const user = this.authService.getCurrentUser();

    if (user?.id === request.requestedByUserId) {
      return user.name;
    }

    return 'Nao informado';
  }

  private replaceRequest(updated: PurchaseRequest, message: string): void {
    this.requests.update((items) => items.map((item) => item.id === updated.id ? updated : item));
    this.toast.success(message);
  }

  private matchesDateRange(value: string, start: string, end: string): boolean {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return true;
    }

    if (start) {
      const startDate = new Date(`${start}T00:00:00`);

      if (date < startDate) {
        return false;
      }
    }

    if (end) {
      const endDate = new Date(`${end}T23:59:59.999`);

      if (date > endDate) {
        return false;
      }
    }

    return true;
  }
}
