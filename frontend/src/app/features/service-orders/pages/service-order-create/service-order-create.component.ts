import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { PurchaseRequest } from '../../../purchase-requests/models/purchase-request.models';
import { PurchaseRequestStatus } from '../../../purchase-requests/models/purchase-request-status';
import { PurchaseRequestType } from '../../../purchase-requests/models/purchase-request-type';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { Supplier } from '../../../quotations/models/quotation.models';
import { UnitOfMeasure } from '../../../../shared/models/unit-of-measure.models';
import { formatCurrency } from '../../services/formatters';
import { ServiceOrderService } from '../../services/service-order.service';
import { ServiceOrder } from '../../models/service-order.models';

@Component({
  selector: 'app-service-order-create',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './service-order-create.component.html',
  styleUrl: './service-order-create.component.css'
})
export class ServiceOrderCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ServiceOrderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly purchaseRequests = signal<PurchaseRequest[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly units = signal<UnitOfMeasure[]>([]);
  readonly serviceOrders = signal<ServiceOrder[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly selectedPurchaseRequestId = signal('');
  readonly paymentConditionOptions = [
    'Pix',
    'Cartao de credito',
    'Cartao de debito',
    'Boleto',
    'Transferencia bancaria',
    'Dinheiro',
    'Outro'
  ];

  readonly availableRequests = computed(() =>
    this.purchaseRequests().filter((request) =>
      request.type === PurchaseRequestType.Service &&
      request.status === PurchaseRequestStatus.Approved &&
      !this.existingOrderForRequest(request.id))
  );

  readonly selectedRequest = computed(() =>
    this.purchaseRequests().find((request) => request.id === this.selectedPurchaseRequestId()) ?? null
  );

  readonly selectedExistingOrder = computed(() =>
    this.existingOrderForRequest(this.selectedPurchaseRequestId())
  );

  readonly form = this.fb.nonNullable.group({
    purchaseRequestId: ['', Validators.required],
    supplierId: ['', Validators.required],
    contractedValue: [0, [Validators.required, Validators.min(0.01)]],
    contractedQuantity: [0, [Validators.required, Validators.min(0.0001)]],
    unit: ['', Validators.required],
    paymentCondition: ['', Validators.required],
    paymentConditionOther: [''],
    installmentCount: [1, [Validators.required, Validators.min(1), Validators.pattern(/^[1-9]\d*$/)]]
  });

  ngOnInit(): void {
    this.loadContext();
    this.form.controls.purchaseRequestId.valueChanges.subscribe((value) =>
      this.selectedPurchaseRequestId.set(value));
  }

  loadContext(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.getCreateContext()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (context) => {
          this.purchaseRequests.set(context.purchaseRequests);
          this.suppliers.set(context.suppliers);
          this.serviceOrders.set(context.serviceOrders);
          this.units.set(context.units);

          const purchaseRequestId = this.route.snapshot.queryParamMap.get('purchaseRequestId');

          if (purchaseRequestId) {
            this.form.controls.purchaseRequestId.setValue(purchaseRequestId);
            this.selectedPurchaseRequestId.set(purchaseRequestId);
          }
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  submit(): void {
    if (this.selectedExistingOrder()) {
      this.toast.error('Ja existe uma ordem de servico para esta solicitacao.');
      return;
    }

    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');

    const value = this.form.getRawValue();
    const paymentCondition = value.paymentCondition === 'Outro'
      ? value.paymentConditionOther.trim()
      : value.paymentCondition;

    if (!paymentCondition) {
      this.form.controls.paymentConditionOther.markAsTouched();
      this.saving.set(false);
      this.toast.error('Informe a condicao de pagamento.');
      return;
    }

    this.service.create({
      purchaseRequestId: value.purchaseRequestId,
      supplierId: value.supplierId,
      contractedValue: value.contractedValue,
      contractedQuantity: value.contractedQuantity,
      unit: value.unit,
      paymentCondition,
      installmentCount: value.installmentCount
    }).pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (serviceOrder) => {
          this.toast.success('Ordem de servico gerada com sucesso.');
          void this.router.navigate(['/app/ordens-servico', serviceOrder.id]);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  supplierLabel(supplier: Supplier): string {
    return `${supplier.tradeName || supplier.corporateName} - ${supplier.document}`;
  }

  updatePaymentCondition(): void {
    const condition = this.form.controls.paymentCondition.value;

    if (condition !== 'Outro') {
      this.form.controls.paymentConditionOther.setValue('');
    }

    if (['Pix', 'Cartao de debito', 'Transferencia bancaria', 'Dinheiro'].includes(condition)) {
      this.form.controls.installmentCount.setValue(1);
    }
  }

  existingOrderForRequest(purchaseRequestId: string | null): ServiceOrder | null {
    if (!purchaseRequestId) {
      return null;
    }

    return this.serviceOrders().find((order) => order.purchaseRequestId === purchaseRequestId) ?? null;
  }

  money(value: number): string {
    return formatCurrency(value);
  }
}
