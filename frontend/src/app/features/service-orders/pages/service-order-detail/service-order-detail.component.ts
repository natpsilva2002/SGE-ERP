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
  ServiceOrderPayment,
  ServiceOrderPaymentAttachment,
  ServiceAdvancePaymentRequest,
  ServiceAdvancePaymentStatus,
  ServiceMeasurement,
  ServiceMeasurementAttachment,
  ServiceMeasurementStatus,
  ServiceOrderExecutionStatus,
  ServiceOrderAttachment,
  ServiceOrderAttachmentType,
  ServiceOrderAmendment,
  ServiceOrderAmendmentStatus,
  getServiceOrderAmendmentStatusLabel,
  PaymentMethod,
  UpdateServiceOrderContract,
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
  readonly selectedPaymentAttachmentFiles = signal<File[]>([]);
  readonly selectedMeasurementAttachmentFiles = signal<File[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly downloadingPdf = signal(false);
  readonly editingContractTerms = signal(false);
  readonly showAmendmentDialog = signal(false);
  readonly editingAmendmentId = signal<string | null>(null);
  readonly selectedAmendmentFiles = signal<File[]>([]);
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
  readonly ServiceOrderAmendmentStatus = ServiceOrderAmendmentStatus;
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
    quantityMeasured: [0, [Validators.required, Validators.min(0.01)]],
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

  readonly contractTermsForm = this.fb.nonNullable.group({
    contractedValue: [0, [Validators.required, Validators.min(0.01)]],
    contractedQuantity: [0, [Validators.required, Validators.min(0.0001)]],
    unit: ['', Validators.required],
    paymentCondition: [''],
    installmentCount: [1, [Validators.required, Validators.min(1)]]
  });

  readonly amendmentForm = this.fb.group({
    reason: ['', Validators.required],
    valueAdjustment: [null as number | null],
    quantityAdjustment: [null as number | null],
    observation: ['']
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

  downloadPdf(order: ServiceOrder): void {
    if (this.downloadingPdf()) return;
    this.downloadingPdf.set(true);
    this.service.downloadPdf(order.id)
      .pipe(finalize(() => this.downloadingPdf.set(false)))
      .subscribe({
        next: (blob) => this.downloadBlob(blob, `OS-${order.number}.pdf`),
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
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

  downloadPaymentAttachment(
    order: ServiceOrder,
    payment: ServiceOrderPayment,
    attachment: ServiceOrderPaymentAttachment): void {
    this.service.downloadPaymentAttachment(order.id, payment.id, attachment.id).subscribe({
      next: (blob) => this.downloadBlob(blob, attachment.originalFileName),
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  downloadMeasurementAttachment(
    order: ServiceOrder,
    measurement: ServiceMeasurement,
    attachment: ServiceMeasurementAttachment): void {
    this.service.downloadMeasurementAttachment(order.id, measurement.id, attachment.id).subscribe({
      next: (blob) => this.downloadBlob(blob, attachment.originalFileName),
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

  canUploadContract(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      order.executionStatus === ServiceOrderExecutionStatus.WaitingContract;
  }

  canDownloadContract(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canRelease(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      order.executionStatus === ServiceOrderExecutionStatus.WaitingContract &&
      !!order.contractFileName;
  }

  canRegisterPayment(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      order.isReleasedForExecution &&
      order.availableMeasuredToPay > 0 &&
      order.paymentStatus !== 3;
  }

  openPaymentDialog(order: ServiceOrder): void {
    this.paymentAdvanceRequest.set(null);
    this.selectedPaymentAttachmentFiles.set([]);
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
    this.selectedPaymentAttachmentFiles.set([]);
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
    this.selectedPaymentAttachmentFiles.set([]);
  }

  onPaymentAttachmentFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (this.validateDocumentFiles(files)) {
      this.selectedPaymentAttachmentFiles.set(files);
    }
  }

  removePaymentAttachmentFile(index: number): void {
    this.selectedPaymentAttachmentFiles.update((files) => files.filter((_, i) => i !== index));
  }

  onMeasurementAttachmentFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (this.validateDocumentFiles(files)) {
      this.selectedMeasurementAttachmentFiles.set(files);
    }
  }

  removeMeasurementAttachmentFile(index: number): void {
    this.selectedMeasurementAttachmentFiles.update((files) => files.filter((_, i) => i !== index));
  }

  submitPayment(order: ServiceOrder): void {
    if (this.paymentForm.invalid || this.saving()) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const value = this.paymentForm.getRawValue();
    const selectedFiles = this.selectedPaymentAttachmentFiles();

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
    }).subscribe({
        next: (updated) => {
          const payment = updated.lastPaymentId
            ? updated.payments.find((item) => item.id === updated.lastPaymentId)
            : null;
          if (!payment || selectedFiles.length === 0) {
            this.serviceOrder.set(updated);
            this.closePaymentDialog();
            this.saving.set(false);
            this.toast.success('Pagamento registrado.');
            return;
          }

          this.service.uploadPaymentAttachments(order.id, payment.id, selectedFiles)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
              next: (withAttachments) => {
                this.serviceOrder.set(withAttachments);
                this.closePaymentDialog();
                this.toast.success('Pagamento e anexos registrados.');
              },
              error: (error) => this.toast.error(getApiErrorMessage(error))
            });
        },
        error: (error) => {
          this.saving.set(false);
          this.toast.error(getApiErrorMessage(error));
        }
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
      order.isReleasedForExecution &&
      order.amountPending > 0;
  }

  canApproveAdvance(): boolean {
    return this.authService.hasRole([AppRoles.Admin]);
  }

  hasPendingAdvance(order: ServiceOrder): boolean {
    return order.advancePaymentRequests.some((request) => request.status === ServiceAdvancePaymentStatus.WaitingApproval);
  }

  canPayAdvance(advance: ServiceAdvancePaymentRequest): boolean {
    const order = this.serviceOrder();
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]) &&
      !!order?.isReleasedForExecution &&
      advance.status === ServiceAdvancePaymentStatus.Approved &&
      advance.amountPending > 0;
  }

  canEditContractTerms(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Admin]) &&
      order.executionStatus === ServiceOrderExecutionStatus.WaitingContract;
  }

  startEditingContractTerms(order: ServiceOrder): void {
    this.contractTermsForm.reset({
      contractedValue: order.contractedValue,
      contractedQuantity: order.estimatedQuantity ?? 0,
      unit: order.unit ?? '',
      paymentCondition: order.paymentCondition ?? '',
      installmentCount: order.installmentCount ?? 1
    });
    this.editingContractTerms.set(true);
  }

  saveContractTerms(order: ServiceOrder): void {
    if (this.contractTermsForm.invalid || this.saving()) {
      this.contractTermsForm.markAllAsTouched();
      return;
    }
    const terms = this.contractTermsForm.getRawValue();
    this.saving.set(true);
    this.service.updateContractTerms(order.id, {
      supplierId: order.supplierId,
      ...terms
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: updated => {
        this.serviceOrder.set(updated);
        this.editingContractTerms.set(false);
        this.toast.success('Termos contratuais atualizados.');
      },
      error: error => this.toast.error(getApiErrorMessage(error))
    });
  }

  canCreateAmendment(order: ServiceOrder): boolean {
    return this.authService.hasRole([AppRoles.Admin]) && order.isReleasedForExecution;
  }

  canDecideAmendment(amendment: ServiceOrderAmendment): boolean {
    return this.authService.hasRole([AppRoles.Admin]) && amendment.status === ServiceOrderAmendmentStatus.WaitingApproval;
  }

  canEditAmendment(amendment: ServiceOrderAmendment): boolean {
    return this.authService.hasRole([AppRoles.Admin]) &&
      (amendment.status === ServiceOrderAmendmentStatus.Draft || amendment.status === ServiceOrderAmendmentStatus.Rejected);
  }

  canSubmitAmendment(amendment: ServiceOrderAmendment): boolean {
    return this.canEditAmendment(amendment);
  }

  openAmendmentDialog(): void {
    this.editingAmendmentId.set(null);
    this.amendmentForm.reset({ reason: '', valueAdjustment: null, quantityAdjustment: null, observation: '' });
    this.selectedAmendmentFiles.set([]);
    this.showAmendmentDialog.set(true);
  }

  editAmendment(amendment: ServiceOrderAmendment): void {
    this.editingAmendmentId.set(amendment.id);
    this.amendmentForm.reset({
      reason: amendment.reason,
      valueAdjustment: amendment.valueAdjustment ?? null,
      quantityAdjustment: amendment.quantityAdjustment ?? null,
      observation: amendment.observation ?? ''
    });
    this.selectedAmendmentFiles.set([]);
    this.showAmendmentDialog.set(true);
  }

  closeAmendmentDialog(): void {
    this.showAmendmentDialog.set(false);
    this.selectedAmendmentFiles.set([]);
  }

  onAmendmentFilesSelected(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    if (this.validateDocumentFiles(files)) this.selectedAmendmentFiles.set(files);
  }

  createAmendment(order: ServiceOrder): void {
    if (this.amendmentForm.invalid || this.saving()) {
      this.amendmentForm.markAllAsTouched();
      return;
    }
    const formValue = this.amendmentForm.getRawValue();
    if (!formValue.valueAdjustment && !formValue.quantityAdjustment) {
      this.toast.error('Informe uma alteração de valor ou quantidade.');
      return;
    }
    this.saving.set(true);
    const amendmentId = this.editingAmendmentId();
    const payload = {
      reason: formValue.reason?.trim() ?? '',
      valueAdjustment: formValue.valueAdjustment,
      quantityAdjustment: formValue.quantityAdjustment,
      observation: formValue.observation?.trim() || null
    };
    const saveRequest = amendmentId
      ? this.service.updateAmendment(order.id, amendmentId, payload)
      : this.service.createAmendment(order.id, payload);
    saveRequest.subscribe({
      next: amendment => {
        const files = this.selectedAmendmentFiles();
        const request = files.length
          ? this.service.uploadAmendmentAttachments(order.id, amendment.id, files)
          : null;
        if (!request) {
          this.finishAmendmentSave(order.id);
          return;
        }
        request.pipe(finalize(() => this.saving.set(false))).subscribe({
          next: () => this.finishAmendmentSave(order.id),
          error: error => {
            this.saving.set(false);
            this.closeAmendmentDialog();
            this.load();
            this.toast.error(`Adendo salvo, mas não foi possível enviar os anexos: ${getApiErrorMessage(error)}`);
          }
        });
      },
      error: error => {
        this.saving.set(false);
        this.toast.error(getApiErrorMessage(error));
      }
    });
  }

  submitAmendment(order: ServiceOrder, amendment: ServiceOrderAmendment): void {
    this.saving.set(true);
    this.service.submitAmendment(order.id, amendment.id).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.toast.success('Adendo enviado para aprovação.'); this.load(); },
      error: error => this.toast.error(getApiErrorMessage(error))
    });
  }

  decideAmendment(order: ServiceOrder, amendment: ServiceOrderAmendment, approve: boolean): void {
    const perform = () => {
      this.saving.set(true);
      this.service.decideAmendment(order.id, amendment.id, approve).pipe(finalize(() => this.saving.set(false))).subscribe({
        next: () => { this.toast.success(approve ? 'Adendo aprovado.' : 'Adendo rejeitado.'); this.load(); },
        error: error => this.toast.error(getApiErrorMessage(error))
      });
    };
    if (!approve) {
      this.confirm.confirm({ title: 'Rejeitar adendo', message: `Rejeitar o adendo “${amendment.reason}”?`, confirmLabel: 'Rejeitar' })
        .pipe(take(1)).subscribe(confirmed => { if (confirmed) perform(); });
      return;
    }
    this.confirm.confirm({ title: 'Aprovar adendo', message: `Aprovar o adendo “${amendment.reason}” e atualizar os limites contratuais?`, confirmLabel: 'Aprovar' })
      .pipe(take(1)).subscribe(confirmed => { if (confirmed) perform(); });
  }

  uploadAmendmentAttachments(order: ServiceOrder, amendment: ServiceOrderAmendment, event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    if (!files.length || !this.validateDocumentFiles(files)) return;
    this.saving.set(true);
    this.service.uploadAmendmentAttachments(order.id, amendment.id, files).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.toast.success('Anexos adicionados.'); this.load(); },
      error: error => this.toast.error(getApiErrorMessage(error))
    });
  }

  deleteAmendmentAttachment(order: ServiceOrder, amendment: ServiceOrderAmendment, attachmentId: string): void {
    this.confirm.confirm({ title: 'Remover anexo', message: 'Remover este arquivo do adendo em edição?', confirmLabel: 'Remover' })
      .pipe(take(1)).subscribe(confirmed => {
        if (!confirmed) return;
        this.service.deleteAmendmentAttachment(order.id, amendment.id, attachmentId).subscribe({
          next: () => { this.toast.success('Anexo removido.'); this.load(); },
          error: error => this.toast.error(getApiErrorMessage(error))
        });
      });
  }

  downloadAmendmentAttachment(order: ServiceOrder, amendment: ServiceOrderAmendment, attachment: { id: string; originalFileName: string }): void {
    this.service.downloadAmendmentAttachment(order.id, amendment.id, attachment.id).subscribe({
      next: blob => this.downloadBlob(blob, attachment.originalFileName),
      error: error => this.toast.error(getApiErrorMessage(error))
    });
  }

  amendmentStatusLabel(status: ServiceOrderAmendmentStatus): string {
    return getServiceOrderAmendmentStatusLabel(status);
  }

  private finishAmendmentSave(orderId: string): void {
    this.saving.set(false);
    this.closeAmendmentDialog();
    this.service.getById(orderId).subscribe({ next: updated => this.serviceOrder.set(updated) });
    this.toast.success(this.editingAmendmentId() ? 'Rascunho do adendo atualizado.' : 'Adendo criado em rascunho.');
    this.editingAmendmentId.set(null);
  }

  canManageAttachments(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canDownloadAttachment(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canManageMeasurements(): boolean {
    return this.authService.hasRole([AppRoles.Finance, AppRoles.Admin]);
  }

  canCreateMeasurement(order: ServiceOrder): boolean {
    return this.canManageMeasurements() &&
      (order.executionStatus === ServiceOrderExecutionStatus.Released ||
        order.executionStatus === ServiceOrderExecutionStatus.InProgress);
  }

  openCreateMeasurement(): void {
    this.editingMeasurement.set(null);
    this.selectedMeasurementAttachmentFiles.set([]);
    this.measurementForm.reset({
      measurementDate: this.todayAsInputValue(),
      description: '',
      quantityMeasured: 0,
      amount: 0,
      observation: ''
    });
    this.showMeasurementDialog.set(true);
  }

  openEditMeasurement(measurement: ServiceMeasurement): void {
    this.editingMeasurement.set(measurement);
    this.selectedMeasurementAttachmentFiles.set([]);
    this.measurementForm.reset({
      measurementDate: this.asDateInputValue(measurement.measurementDate),
      description: measurement.description,
      quantityMeasured: measurement.quantityMeasured,
      amount: measurement.amount,
      observation: measurement.observation ?? ''
    });
    this.showMeasurementDialog.set(true);
  }

  closeMeasurementDialog(): void {
    this.showMeasurementDialog.set(false);
    this.editingMeasurement.set(null);
    this.selectedMeasurementAttachmentFiles.set([]);
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

    if (value.amount <= 0 || value.amount > available ||
      (order.estimatedQuantity !== null && order.estimatedQuantity !== undefined &&
        value.quantityMeasured > order.remainingQuantity + (editing?.quantityMeasured ?? 0))) {
      this.toast.error('Quantidade e valor da medicao devem respeitar os saldos disponiveis.');
      return;
    }

    const request = editing
      ? this.service.updateMeasurement(order.id, editing.id, {
          measurementDate: value.measurementDate,
          description: value.description.trim(),
          quantityMeasured: value.quantityMeasured,
          unit: order.unit,
          amount: value.amount,
          observation: value.observation || null
        })
      : this.service.createMeasurement(order.id, {
          measurementDate: value.measurementDate,
          description: value.description.trim(),
          quantityMeasured: value.quantityMeasured,
          unit: order.unit,
          amount: value.amount,
          observation: value.observation || null
        });

    this.saving.set(true);
    request
      .subscribe({
        next: (measurement) => {
          const selectedFiles = this.selectedMeasurementAttachmentFiles();
          if (selectedFiles.length === 0) {
            this.saving.set(false);
            this.toast.success(editing ? 'Medicao atualizada.' : 'Medicao criada.');
            this.closeMeasurementDialog();
            this.load();
            return;
          }

          this.service.uploadMeasurementAttachments(order.id, measurement.id, selectedFiles)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
              next: () => {
                this.toast.success(editing ? 'Medicao e anexos atualizados.' : 'Medicao e anexos registrados.');
                this.closeMeasurementDialog();
                this.load();
              },
              error: (error) => this.toast.error(getApiErrorMessage(error))
            });
        },
        error: (error) => {
          this.saving.set(false);
          this.toast.error(getApiErrorMessage(error));
        }
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
    })
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

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private validateDocumentFiles(files: File[]): boolean {
    const allowed = ['pdf', 'jpg', 'jpeg', 'png', 'doc', 'docx', 'xls', 'xlsx'];
    const invalid = files.find((file) => {
      const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
      return !allowed.includes(extension) || file.size > 10 * 1024 * 1024;
    });

    if (invalid) {
      this.toast.error('Anexos devem ser PDF, JPG, JPEG, PNG, DOC, DOCX, XLS ou XLSX, com ate 10 MB cada.');
      return false;
    }

    return files.length > 0;
  }
}
