import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { finalize, take } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import {
  ServiceOrder,
  ServiceAdvancePaymentRequest,
  ServiceAdvancePaymentStatus,
  ServiceMeasurement,
  ServiceMeasurementStatus,
  ServiceOrderExecutionStatus,
  ServiceOrderAttachment,
  ServiceOrderAttachmentType,
  PaymentMethod,
  getAdvancePaymentStatusLabel,
  getAttachmentTypeLabel,
  getExecutionStatusLabel,
  getMeasurementStatusLabel,
  getPaymentMethodLabel,
  getPaymentStatusLabel
} from '../../models/service-order.models';
import { formatCurrency, formatDateTime } from '../../services/formatters';
import { ServiceOrderService } from '../../services/service-order.service';

@Component({
  selector: 'app-service-order-detail',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './service-order-detail.component.html',
  styleUrl: './service-order-detail.component.css'
})
export class ServiceOrderDetailComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(ServiceOrderService);
  private readonly authService = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly serviceOrder = signal<ServiceOrder | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly selectedAttachmentFiles = signal<File[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly showMeasurementDialog = signal(false);
  readonly editingMeasurement = signal<ServiceMeasurement | null>(null);
  readonly rejectingMeasurement = signal<ServiceMeasurement | null>(null);
  readonly showPaymentDialog = signal(false);
  readonly paymentAdvanceRequest = signal<ServiceAdvancePaymentRequest | null>(null);
  readonly showAdvanceDialog = signal(false);
  readonly rejectingAdvance = signal<ServiceAdvancePaymentRequest | null>(null);
  readonly ServiceMeasurementStatus = ServiceMeasurementStatus;
  readonly ServiceAdvancePaymentStatus = ServiceAdvancePaymentStatus;
  readonly attachmentTypes = [
    ServiceOrderAttachmentType.Invoice,
    ServiceOrderAttachmentType.PaymentReceipt,
    ServiceOrderAttachmentType.Other
  ];
  readonly paymentMethods = [
    PaymentMethod.Pix,
    PaymentMethod.BankTransfer,
    PaymentMethod.Boleto,
    PaymentMethod.CreditCard,
    PaymentMethod.DebitCard,
    PaymentMethod.Cash,
    PaymentMethod.Other
  ];

  readonly measurementForm = this.fb.nonNullable.group({
    measurementDate: ['', Validators.required],
    description: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    observation: ['']
  });

  readonly rejectForm = this.fb.nonNullable.group({
    rejectionReason: ['', Validators.required]
  });

  readonly paymentForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    paymentDate: ['', Validators.required],
    paymentMethod: [PaymentMethod.Pix, Validators.required],
    observation: ['']
  });

  readonly advanceForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    observation: ['']
  });

  readonly rejectAdvanceForm = this.fb.nonNullable.group({
    rejectionReason: ['', Validators.required]
  });

  readonly attachmentForm = this.fb.nonNullable.group({
    type: [ServiceOrderAttachmentType.Invoice, Validators.required]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Ordem de servico nao encontrada.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.service.getById(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (order) => this.serviceOrder.set(order),
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
  }

  uploadContract(): void {
    const order = this.serviceOrder();
    const file = this.selectedFile();

    if (!order || !file || this.saving()) {
      return;
    }

    this.saving.set(true);

    this.service.uploadContract(order.id, file)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.serviceOrder.set(updated);
          this.selectedFile.set(null);
          this.toast.success('Contrato anexado com sucesso.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  onAttachmentFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedAttachmentFiles.set(Array.from(input.files ?? []));
  }

  uploadAttachments(order: ServiceOrder): void {
    const files = this.selectedAttachmentFiles();

    if (!files.length || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.service.uploadAttachments(order.id, this.attachmentForm.controls.type.value, files)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.serviceOrder.set(updated);
          this.selectedAttachmentFiles.set([]);
          this.toast.success('Anexos enviados.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  downloadAttachment(order: ServiceOrder, attachment: ServiceOrderAttachment): void {
    this.service.downloadAttachment(order.id, attachment.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = attachment.originalFileName;
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  deleteAttachment(order: ServiceOrder, attachment: ServiceOrderAttachment): void {
    this.confirm.confirm({
      title: 'Excluir anexo',
      message: `Excluir o anexo ${attachment.originalFileName}?`,
      confirmLabel: 'Excluir'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.deleteAttachment(order.id, attachment.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('Anexo excluido.');
            this.load();
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  downloadContract(): void {
    const order = this.serviceOrder();

    if (!order) {
      return;
    }

    this.service.downloadContract(order.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = order.contractFileName || 'contrato';
        anchor.click();
        URL.revokeObjectURL(url);
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  release(): void {
    const order = this.serviceOrder();

    if (!order || this.saving()) {
      return;
    }

    this.confirm.confirm({
      title: 'Liberar execucao',
      message: `Liberar a OS ${order.number} para execucao?`,
      confirmLabel: 'Liberar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.release(order.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: (updated) => {
            this.serviceOrder.set(updated);
            this.toast.success('Ordem de servico liberada para execucao.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  canUploadContract(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canDownloadContract(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canRelease(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]) &&
      order.executionStatus === ServiceOrderExecutionStatus.WaitingContract &&
      !!order.contractFileName;
  }

  canRegisterPayment(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      order.availableMeasuredToPay > 0 &&
      order.paymentStatus !== 3;
  }

  openPaymentDialog(order: ServiceOrder): void {
    this.paymentAdvanceRequest.set(null);
    this.paymentForm.reset({
      amount: order.availableMeasuredToPay,
      paymentDate: this.todayAsInputValue(),
      paymentMethod: PaymentMethod.Pix,
      observation: ''
    });
    this.showPaymentDialog.set(true);
  }

  openAdvancePaymentDialog(advance: ServiceAdvancePaymentRequest): void {
    this.paymentAdvanceRequest.set(advance);
    this.paymentForm.reset({
      amount: advance.amountPending,
      paymentDate: this.todayAsInputValue(),
      paymentMethod: PaymentMethod.Pix,
      observation: ''
    });
    this.showPaymentDialog.set(true);
  }

  closePaymentDialog(): void {
    this.showPaymentDialog.set(false);
    this.paymentAdvanceRequest.set(null);
  }

  submitPayment(order: ServiceOrder): void {
    if (this.paymentForm.invalid || this.saving()) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const value = this.paymentForm.getRawValue();

    const advance = this.paymentAdvanceRequest();
    const available = advance ? advance.amountPending : order.availableMeasuredToPay;

    if (value.amount <= 0 || value.amount > available) {
      this.toast.error('O pagamento deve respeitar o valor disponivel para pagamento.');
      return;
    }

    this.saving.set(true);
    this.service.pay(order.id, {
      advancePaymentRequestId: advance?.id ?? null,
      amount: value.amount,
      paymentDate: value.paymentDate,
      paymentMethod: value.paymentMethod,
      observation: value.observation || null
    }).pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.serviceOrder.set(updated);
          this.closePaymentDialog();
          this.toast.success('Pagamento registrado.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  openAdvanceDialog(order: ServiceOrder): void {
    this.advanceForm.reset({
      amount: Math.max(order.contractedValue - order.amountPaid, 0),
      observation: ''
    });
    this.showAdvanceDialog.set(true);
  }

  closeAdvanceDialog(): void {
    this.showAdvanceDialog.set(false);
  }

  requestAdvancePayment(order: ServiceOrder): void {
    if (this.advanceForm.invalid || this.saving()) {
      this.advanceForm.markAllAsTouched();
      return;
    }

    const value = this.advanceForm.getRawValue();

    if (value.amount <= 0 || value.amount > order.amountPending) {
      this.toast.error('A antecipacao deve respeitar o saldo contratado pendente.');
      return;
    }

    this.saving.set(true);
    this.service.requestAdvancePayment(order.id, {
      amount: value.amount,
      observation: value.observation || null
    }).pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.serviceOrder.set(updated);
          this.closeAdvanceDialog();
          this.toast.success('Antecipacao enviada para aprovacao.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  approveAdvancePayment(order: ServiceOrder, advance: ServiceAdvancePaymentRequest): void {
    this.confirm.confirm({
      title: 'Aprovar antecipacao',
      message: `Aprovar antecipacao de ${this.money(advance.amount)}?`,
      confirmLabel: 'Aprovar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.approveAdvancePayment(order.id, advance.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: (updated) => {
            this.serviceOrder.set(updated);
            this.toast.success('Antecipacao aprovada.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  openRejectAdvance(advance: ServiceAdvancePaymentRequest): void {
    this.rejectingAdvance.set(advance);
    this.rejectAdvanceForm.reset({ rejectionReason: '' });
  }

  closeRejectAdvance(): void {
    this.rejectingAdvance.set(null);
  }

  rejectAdvancePayment(): void {
    const order = this.serviceOrder();
    const advance = this.rejectingAdvance();

    if (!order || !advance || this.rejectAdvanceForm.invalid || this.saving()) {
      this.rejectAdvanceForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.service.rejectAdvancePayment(order.id, advance.id, {
      rejectionReason: this.rejectAdvanceForm.controls.rejectionReason.value.trim()
    }).pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.serviceOrder.set(updated);
          this.closeRejectAdvance();
          this.toast.success('Antecipacao rejeitada.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  canRequestAdvance(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      order.amountPending > 0;
  }

  canApproveAdvance(): boolean {
    return this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
  }

  canPayAdvance(advance: ServiceAdvancePaymentRequest): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      advance.status === ServiceAdvancePaymentStatus.Approved &&
      advance.amountPending > 0;
  }

  canManageAttachments(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canDownloadAttachment(): boolean {
    return this.authService.hasRole([AppRoles.Approver, AppRoles.Finance, AppRoles.Admin]);
  }

  canManageMeasurements(): boolean {
    return this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
  }

  canCreateMeasurement(order: ServiceOrder): boolean {
    return this.canManageMeasurements() &&
      (order.executionStatus === ServiceOrderExecutionStatus.Released ||
        order.executionStatus === ServiceOrderExecutionStatus.InProgress);
  }

  openCreateMeasurement(): void {
    this.editingMeasurement.set(null);
    this.measurementForm.reset({
      measurementDate: this.todayAsInputValue(),
      description: '',
      amount: 0,
      observation: ''
    });
    this.showMeasurementDialog.set(true);
  }

  openEditMeasurement(measurement: ServiceMeasurement): void {
    this.editingMeasurement.set(measurement);
    this.measurementForm.reset({
      measurementDate: this.asDateInputValue(measurement.measurementDate),
      description: measurement.description,
      amount: measurement.amount,
      observation: measurement.observation ?? ''
    });
    this.showMeasurementDialog.set(true);
  }

  closeMeasurementDialog(): void {
    this.showMeasurementDialog.set(false);
    this.editingMeasurement.set(null);
  }

  submitMeasurementForm(): void {
    const order = this.serviceOrder();

    if (!order || this.measurementForm.invalid || this.saving()) {
      this.measurementForm.markAllAsTouched();
      return;
    }

    const value = this.measurementForm.getRawValue();
    const editing = this.editingMeasurement();
    const available = this.availableBalanceForForm(order, editing);

    if (value.amount <= 0 || value.amount > available) {
      this.toast.error('O valor da medicao deve respeitar o saldo disponivel.');
      return;
    }

    const request = editing
      ? this.service.updateMeasurement(order.id, editing.id, {
          measurementDate: value.measurementDate,
          description: value.description.trim(),
          amount: value.amount,
          observation: value.observation || null
        })
      : this.service.createMeasurement(order.id, {
          measurementDate: value.measurementDate,
          description: value.description.trim(),
          amount: value.amount,
          observation: value.observation || null
        });

    this.saving.set(true);
    request.pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success(editing ? 'Medicao atualizada.' : 'Medicao criada.');
          this.closeMeasurementDialog();
          this.load();
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  submitMeasurement(measurement: ServiceMeasurement): void {
    const order = this.serviceOrder();

    if (!order || this.saving()) {
      return;
    }

    this.confirm.confirm({
      title: 'Enviar medicao',
      message: `Enviar a medicao ${measurement.measurementNumber} para aprovacao?`,
      confirmLabel: 'Enviar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.submitMeasurement(order.id, measurement.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('Medicao enviada para aprovacao.');
            this.load();
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  approveMeasurement(measurement: ServiceMeasurement): void {
    const order = this.serviceOrder();

    if (!order || this.saving()) {
      return;
    }

    this.confirm.confirm({
      title: 'Aprovar medicao',
      message: `Aprovar a medicao ${measurement.measurementNumber}?`,
      confirmLabel: 'Aprovar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.approveMeasurement(order.id, measurement.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('Medicao aprovada.');
            this.load();
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  openRejectMeasurement(measurement: ServiceMeasurement): void {
    this.rejectingMeasurement.set(measurement);
    this.rejectForm.reset({ rejectionReason: '' });
  }

  closeRejectMeasurement(): void {
    this.rejectingMeasurement.set(null);
  }

  rejectMeasurement(): void {
    const order = this.serviceOrder();
    const measurement = this.rejectingMeasurement();

    if (!order || !measurement || this.rejectForm.invalid || this.saving()) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.service.rejectMeasurement(order.id, measurement.id, {
      rejectionReason: this.rejectForm.controls.rejectionReason.value.trim()
    }).pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Medicao rejeitada.');
          this.closeRejectMeasurement();
          this.load();
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  deleteMeasurement(measurement: ServiceMeasurement): void {
    const order = this.serviceOrder();

    if (!order || this.saving()) {
      return;
    }

    this.confirm.confirm({
      title: 'Excluir medicao',
      message: `Excluir a medicao ${measurement.measurementNumber}?`,
      confirmLabel: 'Excluir'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      this.service.deleteMeasurement(order.id, measurement.id)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('Medicao excluida.');
            this.load();
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  executionLabel(order: ServiceOrder): string {
    return getExecutionStatusLabel(order.executionStatus);
  }

  paymentLabel(order: ServiceOrder): string {
    return getPaymentStatusLabel(order.paymentStatus);
  }

  paymentMethodLabel(method: PaymentMethod): string {
    return getPaymentMethodLabel(method);
  }

  advanceStatusLabel(status: ServiceAdvancePaymentStatus): string {
    return getAdvancePaymentStatusLabel(status);
  }

  attachmentTypeLabel(type: ServiceOrderAttachmentType): string {
    return getAttachmentTypeLabel(type);
  }

  money(value: number): string {
    return formatCurrency(value);
  }

  date(value?: string | null): string {
    return formatDateTime(value);
  }

  measurementStatusLabel(status: ServiceMeasurementStatus): string {
    return getMeasurementStatusLabel(status);
  }

  availableBalanceForForm(order: ServiceOrder, editing: ServiceMeasurement | null): number {
    return order.remainingToMeasure + (editing?.amount ?? 0);
  }

  private todayAsInputValue(): string {
    return this.asDateInputValue(new Date().toISOString());
  }

  private asDateInputValue(value: string): string {
    return value.slice(0, 10);
  }
}
