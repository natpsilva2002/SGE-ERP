import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, of, take } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import {
  CatalogItem,
  Company,
  PurchaseRequest,
  PurchaseRequestItem,
  Work
} from '../../models/purchase-request.models';
import {
  PurchaseRequestStatus,
  getPurchaseRequestStatusLabel
} from '../../models/purchase-request-status';
import {
  PurchaseRequestType,
  getPurchaseRequestTypeLabel
} from '../../models/purchase-request-type';
import { getApiErrorMessage } from '../../services/api-error';
import { PurchaseRequestService } from '../../services/purchase-request.service';
import { Quotation } from '../../../quotations/models/quotation.models';
import { QuotationService } from '../../../quotations/services/quotation.service';
import { getQuotationStatusLabel } from '../../../quotations/models/quotation-status';
import { ServiceOrder } from '../../../service-orders/models/service-order.models';
import { ServiceOrderService } from '../../../service-orders/services/service-order.service';

@Component({
  selector: 'app-purchase-request-detail',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './purchase-request-detail.component.html',
  styleUrl: './purchase-request-detail.component.css'
})
export class PurchaseRequestDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(PurchaseRequestService);
  private readonly quotationService = inject(QuotationService);
  private readonly serviceOrderService = inject(ServiceOrderService);
  private readonly authService = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly request = signal<PurchaseRequest | null>(null);
  readonly items = signal<PurchaseRequestItem[]>([]);
  readonly catalogItems = signal<CatalogItem[]>([]);
  readonly companies = signal<Company[]>([]);
  readonly works = signal<Work[]>([]);
  readonly quotations = signal<Quotation[]>([]);
  readonly serviceOrder = signal<ServiceOrder | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly editingRequest = signal(false);
  readonly editingItemId = signal<string | null>(null);
  readonly PurchaseRequestType = PurchaseRequestType;

  readonly hasLinkedQuotations = computed(() => this.quotations().length > 0);

  readonly canEditCurrent = computed(() => {
    const request = this.request();

    if (!request || !this.canManageCurrentType()) {
      return false;
    }

    if (request.type === PurchaseRequestType.Material) {
      return request.status === PurchaseRequestStatus.WaitingQuotation &&
        !this.hasLinkedQuotations();
    }

    return request.status === PurchaseRequestStatus.Draft;
  });

  readonly canSubmitForApprovalCurrent = computed(() =>
    this.request()?.type === PurchaseRequestType.Service &&
    this.request()?.status === PurchaseRequestStatus.Draft &&
    this.canManageCurrentType()
  );

  readonly canApproveCurrent = computed(() =>
    this.request()?.type === PurchaseRequestType.Service &&
    this.request()?.status === PurchaseRequestStatus.WaitingApproval &&
    this.authService.hasRole([AppRoles.Approver, AppRoles.Admin])
  );

  readonly canDeleteCurrent = computed(() =>
    this.request()?.type === PurchaseRequestType.Material &&
    this.request()?.status === PurchaseRequestStatus.WaitingQuotation &&
      !this.hasLinkedQuotations() &&
      this.authService.hasRole([AppRoles.Admin])
  );

  readonly canEditRequestFields = computed(() =>
    this.request()?.type === PurchaseRequestType.Service &&
    this.canEditCurrent() &&
    this.editingRequest()
  );

  readonly canCreateServiceOrderCurrent = computed(() =>
    this.request()?.status === PurchaseRequestStatus.Approved &&
    this.request()?.type === PurchaseRequestType.Service &&
    !this.serviceOrder() &&
    this.authService.hasRole([AppRoles.Approver, AppRoles.Admin])
  );

  readonly activeCatalogItems = computed(() =>
    this.catalogItems().filter((item) => item.isActive !== false)
  );

  readonly selectedItemUnit = computed(() => {
    const itemId = this.itemForm.controls.itemId.value;
    return this.catalogItems().find((item) => item.id === itemId)?.unit ?? '';
  });

  readonly editForm = this.fb.nonNullable.group({
    number: ['', Validators.required],
    workId: ['', Validators.required],
    description: ['', Validators.required],
    serviceSpecification: [''],
    serviceQuantity: [null as number | null],
    serviceUnit: ['']
  });

  readonly itemForm = this.fb.nonNullable.group({
    itemId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^[1-9]\d*$/)]],
    unit: ['', Validators.required],
    observation: ['']
  });

  ngOnInit(): void {
    this.load();
    this.itemForm.controls.itemId.valueChanges.subscribe((itemId) => {
      const item = this.catalogItems().find((candidate) => candidate.id === itemId);

      if (item && !this.editingItemId()) {
        this.itemForm.controls.unit.setValue(item.unit);
      }
    });
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Solicitacao nao encontrada.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const canReadServiceOrders = this.authService.hasRole([
      AppRoles.Approver,
      AppRoles.Finance,
      AppRoles.Admin
    ]);

    forkJoin({
      request: this.service.getById(id),
      items: this.service.getAllItems(),
      catalogItems: this.service.getCatalogItems(),
      companies: this.service.getCompanies(),
      works: this.service.getWorks(),
      quotations: this.quotationService.getAll(),
      serviceOrders: canReadServiceOrders ? this.serviceOrderService.getAll() : of([])
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ request, items, catalogItems, companies, works, quotations, serviceOrders }) => {
          this.request.set(request);
          this.items.set(items.filter((item) => item.purchaseRequestId === request.id));
          this.catalogItems.set(catalogItems);
          this.companies.set(companies);
          this.works.set(works);
          this.quotations.set(quotations.filter((quotation) => quotation.purchaseRequestId === request.id));
          this.serviceOrder.set(serviceOrders.find((order) => order.purchaseRequestId === request.id) ?? null);
          this.editForm.setValue({
            number: request.number,
            workId: request.workId,
            description: request.description,
            serviceSpecification: request.serviceSpecification ?? '',
            serviceQuantity: request.serviceQuantity ?? null,
            serviceUnit: request.serviceUnit ?? ''
          });
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  saveRequest(): void {
    const request = this.request();

    if (!request || this.editForm.invalid || this.saving()) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    this.service.update(request.id, this.editForm.getRawValue())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.request.set(updated);
          this.editingRequest.set(false);
          this.toast.success('Solicitacao atualizada com sucesso.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  saveItem(): void {
    const request = this.request();

    if (!request || this.itemForm.invalid || this.saving()) {
      this.itemForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.itemForm.getRawValue();
    const catalogItem = this.catalogItems().find((item) => item.id === value.itemId);

    if (!catalogItem) {
      this.toast.error('Material nao encontrado.');
      this.saving.set(false);
      return;
    }

    const editingId = this.editingItemId();
    const operation = editingId
      ? this.service.updateItem(editingId, {
          quantity: value.quantity,
          unit: catalogItem.unit,
          observation: null
        })
      : this.service.createItem({
          purchaseRequestId: request.id,
          itemId: value.itemId,
          quantity: value.quantity,
          unit: catalogItem.unit,
          observation: null
        });

    operation.pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (item) => {
          this.items.update((items) => editingId
            ? items.map((current) => current.id === item.id ? item : current)
            : [...items, item]);
          this.cancelItemEdit();
          this.toast.success(editingId ? 'Item atualizado.' : 'Item adicionado.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  editItem(item: PurchaseRequestItem): void {
    this.editingItemId.set(item.id);
    this.itemForm.setValue({
      itemId: item.itemId,
      quantity: item.quantity,
      unit: item.unit,
      observation: item.observation ?? ''
    });
  }

  startRequestEdit(): void {
    if (this.canEditCurrent()) {
      this.editingRequest.set(true);
    }
  }

  cancelItemEdit(): void {
    this.editingItemId.set(null);
    this.itemForm.reset({
      itemId: '',
      quantity: 1,
      unit: '',
      observation: ''
    });
  }

  deleteItem(item: PurchaseRequestItem): void {
    this.confirm.confirm({
      title: 'Remover item',
      message: 'Remover este item da solicitacao?',
      confirmLabel: 'Remover'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.deleteItem(item.id).subscribe({
          next: () => {
            this.items.update((items) => items.filter((current) => current.id !== item.id));
            this.toast.success('Item removido.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  submitForApproval(): void {
    const request = this.request();

    if (!request) {
      return;
    }

    this.confirm.confirm({
      title: 'Enviar para aprovacao',
      message: `Enviar a solicitacao ${request.number} para aprovacao?`,
      confirmLabel: 'Enviar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.submitForApproval(request.id).subscribe({
          next: (updated) => this.updateRequest(updated, 'Solicitacao enviada para aprovacao.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  approve(): void {
    const request = this.request();

    if (!request) {
      return;
    }

    this.confirm.confirm({
      title: 'Aprovar solicitacao',
      message: `Aprovar a solicitacao ${request.number}?`,
      confirmLabel: 'Aprovar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.approve(request.id, { observation: 'Aprovado pelo frontend.' }).subscribe({
          next: (updated) => this.updateRequest(updated, 'Solicitacao aprovada.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  reject(): void {
    const request = this.request();

    if (!request) {
      return;
    }

    this.confirm.confirm({
      title: 'Rejeitar solicitacao',
      message: `Rejeitar a solicitacao ${request.number}?`,
      confirmLabel: 'Rejeitar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.reject(request.id, { observation: 'Rejeitado pelo frontend.' }).subscribe({
          next: (updated) => this.updateRequest(updated, 'Solicitacao rejeitada.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  deleteRequest(): void {
    const request = this.request();

    if (!request) {
      return;
    }

    this.confirm.confirm({
      title: 'Excluir solicitacao',
      message: `Excluir a solicitacao ${request.number}?`,
      confirmLabel: 'Excluir'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.delete(request.id).subscribe({
          next: () => {
            this.toast.success('Solicitacao excluida.');
            void this.router.navigate(['/app/solicitacoes']);
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  getItemDescription(itemId: string): string {
    const item = this.catalogItems().find((catalogItem) => catalogItem.id === itemId);

    return item ? `${item.code} - ${item.description}` : 'Item nao encontrado';
  }

  requesterLabel(request: PurchaseRequest): string {
    const user = this.authService.getCurrentUser();

    if (user?.id === request.requestedByUserId) {
      return user.name;
    }

    return 'Nao informado';
  }

  companyLabel(companyId: string): string {
    const company = this.companies().find((candidate) => candidate.id === companyId);

    return company?.tradeName || company?.corporateName || 'Nao informado';
  }

  workLabel(workId: string): string {
    const work = this.works().find((candidate) => candidate.id === workId);

    return work ? `${work.code} - ${work.name}` : 'Nao informado';
  }

  statusLabel(status: PurchaseRequestStatus): string {
    return getPurchaseRequestStatusLabel(status);
  }

  typeLabel(type: PurchaseRequestType): string {
    return getPurchaseRequestTypeLabel(type);
  }

  quotationStatusLabel(status: number): string {
    return getQuotationStatusLabel(status);
  }

  isMaterial(request: PurchaseRequest): boolean {
    return request.type === PurchaseRequestType.Material;
  }

  isService(request: PurchaseRequest): boolean {
    return request.type === PurchaseRequestType.Service;
  }

  private canManageCurrentType(): boolean {
    const request = this.request();

    if (!request) {
      return false;
    }

    if (request.type === PurchaseRequestType.Material) {
      return this.authService.hasRole([AppRoles.Requester, AppRoles.Warehouse, AppRoles.Admin]);
    }

    if (request.type === PurchaseRequestType.Service) {
      return this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
    }

    return false;
  }

  private updateRequest(updated: PurchaseRequest, message: string): void {
    this.request.set(updated);
    this.toast.success(message);
  }
}
