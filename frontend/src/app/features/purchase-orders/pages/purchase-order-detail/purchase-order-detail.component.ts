import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { formatCurrency, formatDeliveryDays } from '../../../quotations/services/formatters';
import {
  PaymentMethod,
  PurchaseOrder,
  PurchaseOrderPaymentStatus,
  PurchaseOrderStatus,
  Receipt,
  getPaymentMethodLabel,
  getPurchaseOrderFinancialStatusLabel,
  getPurchaseOrderPaymentStatusLabel,
  getPurchaseOrderReceivingStatusLabel,
  getReceiptStatusLabel,
  getPurchaseOrderStatusLabel
} from '../../models/purchase-order.models';
import { PurchaseOrderService } from '../../services/purchase-order.service';

type ReceiptItemFormGroup = FormGroup<{
  purchaseOrderItemId: FormControl<string>;
  itemDescription: FormControl<string>;
  unit: FormControl<string>;
  quantityOrdered: FormControl<number>;
  quantityReceived: FormControl<number>;
  quantityPending: FormControl<number>;
  quantityReceivedNow: FormControl<number | null>;
  observation: FormControl<string>;
}>;

@Component({
  selector: 'app-purchase-order-detail',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './purchase-order-detail.component.html',
  styleUrl: './purchase-order-detail.component.css'
})
export class PurchaseOrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(PurchaseOrderService);
  private readonly authService = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly purchaseOrder = signal<PurchaseOrder | null>(null);
  readonly loading = signal(false);
  readonly downloading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly showPaymentDialog = signal(false);
  readonly showReceiptDialog = signal(false);
  readonly receipts = signal<Receipt[]>([]);
  readonly selectedInvoiceFile = signal<File | null>(null);

  readonly paymentMethods = [
    PaymentMethod.Pix,
    PaymentMethod.BankTransfer,
    PaymentMethod.Boleto,
    PaymentMethod.CreditCard,
    PaymentMethod.DebitCard,
    PaymentMethod.Cash,
    PaymentMethod.Other
  ];

  readonly paymentForm = new FormGroup({
    amount: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0.01)
    ]),
    paymentDate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    paymentMethod: new FormControl<PaymentMethod | null>(null, [
      Validators.required
    ]),
    observation: new FormControl('', { nonNullable: true })
  });

  readonly receiptForm = new FormGroup({
    observation: new FormControl('', { nonNullable: true }),
    invoiceNumber: new FormControl('', { nonNullable: true }),
    items: new FormArray<ReceiptItemFormGroup>([])
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Ordem de compra nao encontrada.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.service.getById(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (order) => {
          this.purchaseOrder.set(order);
          this.loadReceipts(order.id);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  loadReceipts(purchaseOrderId: string): void {
    this.service.getReceiptsByPurchaseOrder(purchaseOrderId)
      .subscribe({
        next: (receipts) => this.receipts.set(receipts),
        error: () => this.receipts.set([])
      });
  }

  canFirstApprove(order: PurchaseOrder): boolean {
    return false;
  }

  canSecondApprove(order: PurchaseOrder): boolean {
    return false;
  }

  waitingForOtherApprover(order: PurchaseOrder): boolean {
    return false;
  }

  canMarkAsSent(order: PurchaseOrder): boolean {
    return order.status === PurchaseOrderStatus.Approved &&
      this.authService.hasRole([AppRoles.Buyer, AppRoles.Admin]);
  }

  canDownloadPdf(order: PurchaseOrder): boolean {
    return order.status !== PurchaseOrderStatus.Open &&
      order.status !== PurchaseOrderStatus.WaitingSecondApproval &&
      order.status !== PurchaseOrderStatus.Cancelled;
  }

  canReceive(order: PurchaseOrder): boolean {
    return this.authService.hasRole([AppRoles.Warehouse, AppRoles.Admin]) &&
      (order.status === PurchaseOrderStatus.Approved ||
        order.status === PurchaseOrderStatus.Sent ||
        order.status === PurchaseOrderStatus.PartiallyReceived) &&
      order.items.some((item) => item.quantityPending > 0);
  }

  canApprovePayment(order: PurchaseOrder): boolean {
    return this.hasFinanceRole() &&
      this.isPaymentCompatibleStatus(order) &&
      !this.isPaymentApproved(order) &&
      order.amountPending > 0 &&
      order.paymentStatus !== PurchaseOrderPaymentStatus.Paid;
  }

  canRegisterPayment(order: PurchaseOrder): boolean {
    return this.hasFinanceRole() &&
      this.isPaymentCompatibleStatus(order) &&
      this.isPaymentApproved(order) &&
      order.amountPending > 0 &&
      order.paymentStatus !== PurchaseOrderPaymentStatus.Paid;
  }

  approve(order: PurchaseOrder, approvalStep: 1 | 2): void {
    this.confirm.confirm({
      title: 'Aprovar Ordem de Compra',
      message: approvalStep === 1
        ? `Realizar a primeira aprovacao da ordem ${order.number}?`
        : `Realizar a segunda aprovacao da ordem ${order.number} e liberar a OC?`,
      confirmLabel: approvalStep === 1 ? 'Realizar 1ª aprovacao' : 'Realizar 2ª aprovacao'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.approve(order.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: (updated) => {
            this.purchaseOrder.set(updated);
            this.toast.success(approvalStep === 1
              ? 'Primeira aprovacao registrada.'
              : 'Ordem de compra aprovada.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  markAsSent(order: PurchaseOrder): void {
    this.confirm.confirm({
      title: 'Marcar como enviada',
      message: `Marcar a ordem ${order.number} como enviada ao fornecedor?`,
      confirmLabel: 'Marcar enviada'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.markAsSent(order.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: (updated) => {
            this.purchaseOrder.set(updated);
            this.toast.success('Ordem de compra marcada como enviada.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  approvePayment(order: PurchaseOrder): void {
    this.confirm.confirm({
      title: 'Aprovar pagamento',
      message: `Confirma a autorizacao do pagamento da ordem ${order.number}?`,
      confirmLabel: 'Aprovar pagamento'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.approvePayment(order.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: (updated) => {
            this.purchaseOrder.set(updated);
            this.toast.success('Pagamento autorizado.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  openPaymentDialog(order: PurchaseOrder): void {
    this.paymentForm.reset({
      amount: order.amountPending,
      paymentDate: this.todayAsInputValue(),
      paymentMethod: PaymentMethod.Pix,
      observation: ''
    });
    this.showPaymentDialog.set(true);
  }

  closePaymentDialog(): void {
    if (this.saving()) {
      return;
    }

    this.showPaymentDialog.set(false);
  }

  submitPayment(order: PurchaseOrder): void {
    this.paymentForm.markAllAsTouched();

    const amount = this.paymentForm.controls.amount.value;
    const paymentMethod = this.paymentForm.controls.paymentMethod.value;

    if (this.paymentForm.invalid ||
      amount === null ||
      amount <= 0 ||
      amount > order.amountPending ||
      paymentMethod === null) {
      this.toast.error('Informe um pagamento valido dentro do saldo pendente.');
      return;
    }

    this.saving.set(true);
    this.service.pay(order.id, {
      amount,
      paymentDate: this.paymentForm.controls.paymentDate.value,
      paymentMethod,
      observation: this.paymentForm.controls.observation.value || null
    }).pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Pagamento registrado.');
          this.showPaymentDialog.set(false);
          this.load();
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  openReceiptDialog(order: PurchaseOrder): void {
    this.receiptItems.clear();
    this.selectedInvoiceFile.set(null);
    this.receiptForm.controls.observation.setValue('');
    this.receiptForm.controls.invoiceNumber.setValue('');

    for (const item of order.items.filter((orderItem) => orderItem.quantityPending > 0)) {
      this.receiptItems.push(new FormGroup({
        purchaseOrderItemId: new FormControl(item.id, { nonNullable: true }),
        itemDescription: new FormControl(item.itemDescription, { nonNullable: true }),
        unit: new FormControl(item.unit, { nonNullable: true }),
        quantityOrdered: new FormControl(item.quantityOrdered, { nonNullable: true }),
        quantityReceived: new FormControl(item.quantityReceived, { nonNullable: true }),
        quantityPending: new FormControl(item.quantityPending, { nonNullable: true }),
        quantityReceivedNow: new FormControl<number | null>(null, [
          Validators.min(0)
        ]),
        observation: new FormControl('', { nonNullable: true })
      }));
    }

    this.showReceiptDialog.set(true);
  }

  closeReceiptDialog(): void {
    if (this.saving()) {
      return;
    }

    this.showReceiptDialog.set(false);
    this.selectedInvoiceFile.set(null);
  }

  onInvoiceFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedInvoiceFile.set(input.files?.[0] ?? null);
  }

  submitReceipt(order: PurchaseOrder): void {
    this.receiptForm.markAllAsTouched();

    const globalObservation = this.receiptForm.controls.observation.value.trim();
    const items = this.receiptItems.controls
      .map((control) => ({
        purchaseOrderItemId: control.controls.purchaseOrderItemId.value,
        quantityReceived: control.controls.quantityReceivedNow.value ?? 0,
        quantityPending: control.controls.quantityPending.value,
        observation: control.controls.observation.value.trim()
      }))
      .filter((item) => item.quantityReceived > 0);

    if (!items.length) {
      this.toast.error('Informe ao menos um item recebido.');
      return;
    }

    if (items.some((item) => item.quantityReceived > item.quantityPending &&
      !item.observation &&
      !globalObservation)) {
      this.toast.error('Recebimento acima do pendente exige justificativa.');
      return;
    }

    const invalidFileMessage = this.validateInvoiceFile(this.selectedInvoiceFile());

    if (invalidFileMessage) {
      this.toast.error(invalidFileMessage);
      return;
    }

    this.saving.set(true);
    this.service.receive(order.id, {
      observation: globalObservation || null,
      items: items.map((item) => ({
        purchaseOrderItemId: item.purchaseOrderItemId,
        quantityReceived: item.quantityReceived,
        observation: item.observation || null
      }))
    }).subscribe({
      next: (receipt) => {
        const invoiceNumber = this.receiptForm.controls.invoiceNumber.value.trim();
        const file = this.selectedInvoiceFile();

        if (invoiceNumber || file) {
          this.service.uploadReceiptInvoice(receipt.id, invoiceNumber || null, file)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
              next: () => this.finishReceipt(order.id),
              error: (error) => this.toast.error(getApiErrorMessage(error))
            });
          return;
        }

        this.saving.set(false);
        this.finishReceipt(order.id);
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(getApiErrorMessage(error));
      }
    });
  }

  downloadInvoice(receipt: Receipt): void {
    this.service.downloadReceiptInvoice(receipt.id)
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = receipt.invoiceFileName || `nota-fiscal-${receipt.receiptDate}.pdf`;
          link.click();
          window.URL.revokeObjectURL(url);
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  downloadPdf(order: PurchaseOrder): void {
    this.downloading.set(true);
    this.service.downloadPdf(order.id)
      .pipe(finalize(() => this.downloading.set(false)))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `${order.number}.pdf`;
          link.click();
          window.URL.revokeObjectURL(url);
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  statusLabel(status: PurchaseOrderStatus): string {
    return getPurchaseOrderStatusLabel(status);
  }

  paymentStatusLabel(status: PurchaseOrderPaymentStatus): string {
    return getPurchaseOrderPaymentStatusLabel(status);
  }

  financialStatusLabel(order: PurchaseOrder): string {
    return getPurchaseOrderFinancialStatusLabel(order);
  }

  receivingStatusLabel(order: PurchaseOrder): string {
    return getPurchaseOrderReceivingStatusLabel(order);
  }

  receiptStatusLabel(status: Receipt['status']): string {
    return getReceiptStatusLabel(status);
  }

  paymentMethodLabel(method: PaymentMethod): string {
    return getPaymentMethodLabel(method);
  }

  money(value: number): string {
    return formatCurrency(value);
  }

  delivery(days?: number | null): string {
    return days === null || days === undefined ? '-' : formatDeliveryDays(days);
  }

  installments(value?: number | null): string {
    return value ? `${value}x` : '-';
  }

  approverName(value?: string | null): string {
    return value && value.trim() ? value : '-';
  }

  formatCnpj(document: string): string {
    const digits = (document || '').replace(/\D/g, '');

    if (digits.length !== 14) {
      return document || '-';
    }

    return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8, 12)}-${digits.slice(12)}`;
  }

  totalOrdered(order: PurchaseOrder): number {
    return order.items.reduce((sum, item) => sum + item.quantityOrdered, 0);
  }

  totalReceived(order: PurchaseOrder): number {
    return order.items.reduce((sum, item) => sum + item.quantityReceived, 0);
  }

  totalPending(order: PurchaseOrder): number {
    return order.items.reduce((sum, item) => sum + item.quantityPending, 0);
  }

  get receiptItems(): FormArray<ReceiptItemFormGroup> {
    return this.receiptForm.controls.items;
  }

  get receiptItemControls(): ReceiptItemFormGroup[] {
    return this.receiptForm.controls.items.controls;
  }

  private hasFinanceRole(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  private isPaymentApproved(order: PurchaseOrder): boolean {
    return order.isPaymentApproved || !!order.paymentApprovedAt;
  }

  private isPaymentCompatibleStatus(order: PurchaseOrder): boolean {
    return order.status === PurchaseOrderStatus.Approved ||
      order.status === PurchaseOrderStatus.Sent ||
      order.status === PurchaseOrderStatus.PartiallyReceived ||
      order.status === PurchaseOrderStatus.Received ||
      order.status === PurchaseOrderStatus.PartiallyCompleted;
  }

  private normalizeId(value?: string | null): string {
    return (value ?? '').trim().toLowerCase();
  }

  private todayAsInputValue(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private finishReceipt(purchaseOrderId: string): void {
    this.toast.success('Recebimento registrado.');
    this.showReceiptDialog.set(false);
    this.selectedInvoiceFile.set(null);
    this.load();
    this.loadReceipts(purchaseOrderId);
  }

  private validateInvoiceFile(file: File | null): string {
    if (!file) {
      return '';
    }

    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png'];
    const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png'];
    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();

    if (file.size > 10 * 1024 * 1024) {
      return 'A nota fiscal deve ter no maximo 10 MB.';
    }

    if (!allowedExtensions.includes(extension) || !allowedTypes.includes(file.type)) {
      return 'Apenas arquivos PDF, JPG, JPEG ou PNG sao permitidos para nota fiscal.';
    }

    return '';
  }
}
