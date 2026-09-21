import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin, take } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import {
  CatalogItem,
  PurchaseRequest,
  PurchaseRequestItem,
  Work
} from '../../../purchase-requests/models/purchase-request.models';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import {
  PurchaseOrder,
  Quotation,
  QuotationAttachment,
  QuotationItem,
  Supplier
} from '../../models/quotation.models';
import { QuotationStatus, getQuotationStatusLabel } from '../../models/quotation-status';
import { formatCurrency, formatDeliveryDays } from '../../services/formatters';
import { QuotationService } from '../../services/quotation.service';

interface RequestItemGroup {
  requestItem: PurchaseRequestItem;
  catalogItem?: CatalogItem;
  proposals: QuotationItem[];
}

interface BudgetGroup {
  supplierId: string;
  supplierName: string;
  supplierDocument: string;
  items: QuotationItem[];
  total: number;
  deliveryDays: number;
  paymentCondition: string;
  installmentCount?: number | null;
}

interface BudgetDraft {
  supplierId: string;
  includedItems: Record<string, boolean>;
  totalPriceTexts: Record<string, string>;
  totalPrices: Record<string, number>;
  observations: Record<string, string>;
  deliveryDays: number;
  paymentCondition: string;
  paymentConditionOther: string;
  installmentCount: number | null;
}

@Component({
  selector: 'app-quotation-detail',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, RouterLink],
  templateUrl: './quotation-detail.component.html',
  styleUrl: './quotation-detail.component.css'
})
export class QuotationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(QuotationService);
  private readonly authService = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly quotation = signal<Quotation | null>(null);
  readonly purchaseRequest = signal<PurchaseRequest | null>(null);
  readonly requestItems = signal<PurchaseRequestItem[]>([]);
  readonly catalogItems = signal<CatalogItem[]>([]);
  readonly works = signal<Work[]>([]);
  readonly quotationItems = signal<QuotationItem[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly generatedOrders = signal<PurchaseOrder[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly editingBudgetSupplierId = signal<string | null>(null);
  readonly budgetDraft = signal<BudgetDraft>(this.emptyDraft());
  readonly selectedBudgetFiles = signal<File[]>([]);
  readonly selectedBudgetSupplierId = signal<string | null>(null);
  readonly paymentConditionOptions = [
    'Pix',
    'Cartao de credito',
    'Cartao de debito',
    'Boleto',
    'Transferencia bancaria',
    'Dinheiro',
    'Outro'
  ];

  readonly canManageDraft = computed(() =>
    this.quotation()?.status === QuotationStatus.Draft &&
    this.authService.hasRole([AppRoles.Buyer, AppRoles.Admin])
  );

  readonly canSubmit = computed(() =>
    this.quotation()?.status === QuotationStatus.Draft &&
    this.authService.hasRole([AppRoles.Buyer, AppRoles.Admin])
  );

  readonly canApprove = computed(() =>
    this.quotation()?.status === QuotationStatus.WaitingApproval &&
    this.authService.hasRole([AppRoles.Admin])
  );

  readonly canSecondApprove = computed(() => {
    const quotation = this.quotation();
    const user = this.authService.getCurrentUser();

    return quotation?.status === QuotationStatus.WaitingSecondApproval &&
      this.authService.hasRole([AppRoles.Admin]) &&
      !!user &&
      this.normalizeId(user.id) !== this.normalizeId(quotation.firstApprovedByUserId);
  });

  readonly waitingForOtherApprover = computed(() => {
    const quotation = this.quotation();
    const user = this.authService.getCurrentUser();

    return quotation?.status === QuotationStatus.WaitingSecondApproval &&
      this.authService.hasRole([AppRoles.Admin]) &&
      !!user &&
      this.normalizeId(user.id) === this.normalizeId(quotation.firstApprovedByUserId);
  });

  readonly approvalPendingForBuyer = computed(() =>
    (this.quotation()?.status === QuotationStatus.WaitingApproval ||
      this.quotation()?.status === QuotationStatus.WaitingSecondApproval) &&
      !this.authService.hasRole([AppRoles.Admin])
  );

  readonly canSelectWinner = computed(() =>
    this.quotation()?.status === QuotationStatus.WaitingApproval &&
    this.authService.hasRole([AppRoles.Admin])
  );

  readonly activeSuppliers = computed(() =>
    this.suppliers().filter((supplier) => supplier.isActive !== false)
  );

  readonly itemGroups = computed<RequestItemGroup[]>(() =>
    this.requestItems().map((requestItem) => ({
      requestItem,
      catalogItem: this.catalogItems().find((item) => item.id === requestItem.itemId),
      proposals: this.quotationItems().filter(
        (proposal) => proposal.purchaseRequestItemId === requestItem.id
      )
    }))
  );

  readonly budgetGroups = computed<BudgetGroup[]>(() => {
    const groups = new Map<string, QuotationItem[]>();

    for (const item of this.quotationItems()) {
      groups.set(item.supplierId, [...(groups.get(item.supplierId) ?? []), item]);
    }

    return Array.from(groups.entries())
      .map(([supplierId, items]) => ({
        supplierId,
        supplierName: this.supplierLabel(supplierId),
        supplierDocument: this.supplierDocument(supplierId),
        items: items.sort((a, b) => this.itemLabelByRequestItemId(a.purchaseRequestItemId)
          .localeCompare(this.itemLabelByRequestItemId(b.purchaseRequestItemId))),
        total: items.reduce((sum, item) => sum + item.totalPrice, 0),
        deliveryDays: items[0]?.deliveryDays ?? 0,
        paymentCondition: items[0]?.paymentCondition ?? '',
        installmentCount: items[0]?.installmentCount
      }))
      .sort((a, b) => a.supplierName.localeCompare(b.supplierName));
  });

  attachmentsForSupplier(supplierId: string): QuotationAttachment[] {
    return (this.quotation()?.attachments ?? []).filter((attachment) => this.normalizeId(attachment.supplierId) === this.normalizeId(supplierId));
  }

  onBudgetFilesSelected(supplierId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (files.some((file) => file.size === 0 || file.size > 10 * 1024 * 1024 || !['.pdf', '.jpg', '.jpeg', '.png', '.doc', '.docx', '.xls', '.xlsx'].includes(file.name.slice(file.name.lastIndexOf('.')).toLowerCase()))) {
      this.toast.error('Anexos devem ter extensao permitida e no maximo 10 MB cada.');
      input.value = '';
      this.selectedBudgetFiles.set([]);
      return;
    }
    this.selectedBudgetSupplierId.set(supplierId);
    this.selectedBudgetFiles.set(files);
  }

  removeSelectedBudgetFile(index: number): void {
    this.selectedBudgetFiles.update((files) => files.filter((_, fileIndex) => fileIndex !== index));
  }

  uploadBudgetAttachments(supplierId: string): void {
    const files = this.selectedBudgetFiles();
    if (!files.length) return;
    const quotation = this.quotation();
    if (!quotation) return;
    this.saving.set(true);
    this.service.uploadQuotationAttachments(quotation.id, supplierId, files).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (updated) => { this.quotation.set(updated); this.selectedBudgetFiles.set([]); this.selectedBudgetSupplierId.set(null); this.toast.success('Anexos adicionados.'); },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  downloadQuotationAttachment(attachment: QuotationAttachment): void {
    const quotation = this.quotation();
    if (!quotation) return;
    this.service.downloadQuotationAttachment(quotation.id, attachment.id).subscribe({ next: (blob) => this.downloadBlob(blob, attachment.originalFileName), error: (error) => this.toast.error(getApiErrorMessage(error)) });
  }

  deleteQuotationAttachment(attachment: QuotationAttachment): void {
    const quotation = this.quotation();
    if (!quotation) return;
    this.confirm.confirm({ title: 'Excluir anexo', message: `Excluir ${attachment.originalFileName}?`, confirmLabel: 'Excluir' }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) return;
      this.service.deleteQuotationAttachment(quotation.id, attachment.id).subscribe({ next: () => this.load(), error: (error) => this.toast.error(getApiErrorMessage(error)) });
    });
  }

  private downloadBlob(blob: Blob, fileName: string): void { const url = URL.createObjectURL(blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = fileName; anchor.click(); URL.revokeObjectURL(url); }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Cotacao nao encontrada.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    forkJoin({
      quotation: this.service.getById(id),
      quotationsItems: this.service.getQuotationItems(),
      purchaseRequests: this.service.getPurchaseRequests(),
      purchaseRequestItems: this.service.getPurchaseRequestItems(),
      catalogItems: this.service.getCatalogItems(),
      works: this.service.getWorks(),
      suppliers: this.service.getSuppliers(),
      purchaseOrders: this.service.getPurchaseOrders()
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          const purchaseRequest = result.purchaseRequests.find(
            (request) => request.id === result.quotation.purchaseRequestId
          ) ?? null;

          this.quotation.set(result.quotation);
          this.purchaseRequest.set(purchaseRequest);
          this.requestItems.set(result.purchaseRequestItems.filter(
            (item) => item.purchaseRequestId === result.quotation.purchaseRequestId
          ));
          this.catalogItems.set(result.catalogItems);
          this.works.set(result.works);
          this.quotationItems.set(result.quotationsItems.filter(
            (item) => item.quotationId === result.quotation.id
          ));
          this.suppliers.set(result.suppliers);
          this.generatedOrders.set(result.purchaseOrders.filter(
            (order) => order.quotationId === result.quotation.id
          ));
          this.resetBudgetForm();
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  saveBudget(): void {
    const quotation = this.quotation();
    const draft = this.budgetDraft();
    const paymentCondition = this.paymentConditionValue(draft);
    const selectedItems = this.requestItems().filter((item) => draft.includedItems[item.id]);
    const editingSupplierId = this.editingBudgetSupplierId();

    if (!quotation || this.saving()) {
      return;
    }

    if (!draft.supplierId) {
      this.toast.error('Selecione o fornecedor.');
      return;
    }

    if (!editingSupplierId &&
        this.quotationItems().some((item) => item.supplierId === draft.supplierId)) {
      this.toast.error('Ja existe orcamento para este fornecedor nesta cotacao. Use Editar orcamento.');
      return;
    }

    if (selectedItems.length === 0) {
      this.toast.error('Selecione pelo menos um material cotado.');
      return;
    }

    for (const item of selectedItems) {
      if ((draft.totalPrices[item.id] ?? 0) <= 0) {
        this.toast.error(`Informe o preco total para ${this.itemLabel(item)}.`);
        return;
      }
    }

    if (draft.deliveryDays < 0) {
      this.toast.error('Informe um prazo de entrega valido.');
      return;
    }

    if (!paymentCondition) {
      this.toast.error('Selecione a condicao de pagamento.');
      return;
    }

    if (draft.installmentCount !== null && draft.installmentCount <= 0) {
      this.toast.error('Informe uma quantidade de parcelas maior que zero.');
      return;
    }

    const existingItems = this.quotationItems().filter(
      (item) => item.supplierId === (editingSupplierId ?? draft.supplierId)
    );
    const operations = [];

    for (const requestItem of selectedItems) {
      const existing = existingItems.find(
        (item) => item.purchaseRequestItemId === requestItem.id
      );
      const payload = {
        totalPrice: draft.totalPrices[requestItem.id],
        deliveryDays: draft.deliveryDays,
        proposalNumber: null,
        paymentCondition,
        installmentCount: draft.installmentCount,
        observation: draft.observations[requestItem.id]?.trim() || null
      };

      operations.push(existing
        ? this.service.updateQuotationItem(existing.id, payload)
        : this.service.createQuotationItem({
            quotationId: quotation.id,
            purchaseRequestItemId: requestItem.id,
            supplierId: draft.supplierId,
            ...payload
          }));
    }

    for (const existing of existingItems) {
      if (!draft.includedItems[existing.purchaseRequestItemId]) {
        operations.push(this.service.deleteQuotationItem(existing.id));
      }
    }

    this.saving.set(true);
    forkJoin(operations)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toast.success(editingSupplierId ? 'Orcamento atualizado.' : 'Orcamento adicionado.');
          this.load();
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  editBudget(group: BudgetGroup): void {
    const firstItem = group.items[0];
    const option = this.paymentConditionOptions.includes(group.paymentCondition)
      ? group.paymentCondition
      : 'Outro';
    const draft = this.emptyDraft();

    for (const item of group.items) {
      draft.includedItems[item.purchaseRequestItemId] = true;
      draft.totalPrices[item.purchaseRequestItemId] = item.totalPrice;
      draft.totalPriceTexts[item.purchaseRequestItemId] = this.money(item.totalPrice);
      draft.observations[item.purchaseRequestItemId] = item.observation ?? '';
    }

    draft.supplierId = group.supplierId;
    draft.deliveryDays = firstItem?.deliveryDays ?? 0;
    draft.paymentCondition = option;
    draft.paymentConditionOther = option === 'Outro' ? group.paymentCondition : '';
    draft.installmentCount = group.installmentCount ?? 1;

    this.editingBudgetSupplierId.set(group.supplierId);
    this.budgetDraft.set(draft);
  }

  cancelBudgetEdit(): void {
    this.resetBudgetForm();
  }

  deleteBudget(group: BudgetGroup): void {
    this.confirm.confirm({
      title: 'Excluir orcamento',
      message: `Excluir o orcamento de ${group.supplierName}?`,
      confirmLabel: 'Excluir'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (!confirmed) {
        return;
      }

      this.saving.set(true);
      forkJoin(group.items.map((item) => this.service.deleteQuotationItem(item.id)))
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('Orcamento excluido.');
            this.load();
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
    });
  }

  selectBudget(group: BudgetGroup): void {
    const quotation = this.quotation();

    if (!quotation) {
      return;
    }

    this.service.selectSupplier(quotation.id, group.supplierId).subscribe({
      next: () => {
        this.quotationItems.update((items) => items.map((item) => {
          if (item.quotationId !== quotation.id) {
            return item;
          }

          return {
            ...item,
            selected: item.supplierId === group.supplierId
          };
        }));
        this.toast.success('Orcamento selecionado.');
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  submitForApproval(): void {
    const quotation = this.quotation();

    if (!quotation) {
      return;
    }

    const validationMessage = this.getSubmitValidationMessage();

    if (validationMessage) {
      this.toast.error(validationMessage);
      return;
    }

    this.confirm.confirm({
      title: 'Enviar cotacao',
      message: 'Enviar esta cotacao para aprovacao?',
      confirmLabel: 'Enviar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.submitForApproval(quotation.id).subscribe({
          next: (updated) => this.updateQuotation(updated, 'Cotacao enviada para aprovacao.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  approve(): void {
    const quotation = this.quotation();

    if (!quotation) {
      return;
    }

    const validationMessage = this.getApprovalValidationMessage();

    if (validationMessage) {
      this.toast.error(validationMessage);
      return;
    }

    this.confirm.confirm({
      title: 'Aprovar cotacao',
      message: quotation.status === QuotationStatus.WaitingApproval
        ? 'Registrar a primeira aprovacao desta cotacao?'
        : 'Registrar a segunda aprovacao desta cotacao e gerar a Ordem de Compra?',
      confirmLabel: quotation.status === QuotationStatus.WaitingApproval
        ? 'Aprovar'
        : 'Aprovar como segundo aprovador'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.approve(quotation.id, { observation: 'Aprovado pelo frontend.' }).subscribe({
          next: (result) => {
            this.quotation.set(result.quotation);
            this.generatedOrders.set(result.purchaseOrders);
            if (result.quotation.status === QuotationStatus.WaitingSecondApproval) {
              this.toast.success('Primeira aprovacao registrada.');
              return;
            }

            this.toast.success('Cotacao aprovada.');
            this.toast.info('Ordem de compra gerada com sucesso.');
          },
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  reject(): void {
    const quotation = this.quotation();

    if (!quotation) {
      return;
    }

    this.confirm.confirm({
      title: 'Rejeitar cotacao',
      message: 'Rejeitar esta cotacao?',
      confirmLabel: 'Rejeitar'
    }).pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.service.reject(quotation.id, { observation: 'Rejeitado pelo frontend.' }).subscribe({
          next: (updated) => this.updateQuotation(updated, 'Cotacao rejeitada.'),
          error: (error) => this.toast.error(getApiErrorMessage(error))
        });
      }
    });
  }

  updateBudgetDraft(patch: Partial<BudgetDraft>): void {
    this.budgetDraft.update((draft) => ({
      ...draft,
      ...patch
    }));
  }

  toggleBudgetItem(requestItemId: string, checked: boolean): void {
    this.budgetDraft.update((draft) => ({
      ...draft,
      includedItems: {
        ...draft.includedItems,
        [requestItemId]: checked
      }
    }));
  }

  updateTotalPrice(requestItemId: string, value: string): void {
    this.budgetDraft.update((draft) => ({
      ...draft,
      totalPriceTexts: {
        ...draft.totalPriceTexts,
        [requestItemId]: value
      },
      totalPrices: {
        ...draft.totalPrices,
        [requestItemId]: this.parseCurrency(value)
      }
    }));
  }

  updateItemObservation(requestItemId: string, value: string): void {
    this.budgetDraft.update((draft) => ({
      ...draft,
      observations: {
        ...draft.observations,
        [requestItemId]: value
      }
    }));
  }

  formatTotalPrice(requestItemId: string): void {
    const draft = this.budgetDraft();
    const value = draft.totalPrices[requestItemId] ?? 0;

    this.budgetDraft.update((current) => ({
      ...current,
      totalPriceTexts: {
        ...current.totalPriceTexts,
        [requestItemId]: value > 0 ? this.money(value) : ''
      }
    }));
  }

  draftItemPriceText(requestItemId: string): string {
    return this.budgetDraft().totalPriceTexts[requestItemId] ?? '';
  }

  draftItemObservation(requestItemId: string): string {
    return this.budgetDraft().observations[requestItemId] ?? '';
  }

  draftItemIncluded(requestItemId: string): boolean {
    return this.budgetDraft().includedItems[requestItemId] ?? false;
  }

  budgetTotal(): number {
    const draft = this.budgetDraft();

    return this.requestItems()
      .filter((item) => draft.includedItems[item.id])
      .reduce((sum, item) => sum + (draft.totalPrices[item.id] ?? 0), 0);
  }

  supplierLabel(supplierId: string): string {
    const supplier = this.suppliers().find((item) => item.id === supplierId);
    return supplier?.tradeName || supplier?.corporateName || 'Fornecedor nao encontrado';
  }

  supplierDocument(supplierId: string): string {
    const supplier = this.suppliers().find((item) => item.id === supplierId);
    return supplier ? this.formatCnpj(supplier.document) : '-';
  }

  itemLabel(requestItem: PurchaseRequestItem): string {
    const item = this.catalogItems().find((catalogItem) => catalogItem.id === requestItem.itemId);
    return item ? item.description : 'Item nao encontrado';
  }

  itemLabelByRequestItemId(requestItemId: string): string {
    const requestItem = this.requestItems().find((item) => item.id === requestItemId);
    return requestItem ? this.itemLabel(requestItem) : 'Item nao encontrado';
  }

  requestItemById(requestItemId: string): PurchaseRequestItem | undefined {
    return this.requestItems().find((item) => item.id === requestItemId);
  }

  workLabel(workId: string): string {
    const work = this.works().find((candidate) => candidate.id === workId);
    return work ? `${work.code} - ${work.name}` : 'Nao informado';
  }

  statusLabel(status: QuotationStatus): string {
    return getQuotationStatusLabel(status);
  }

  money(value: number): string {
    return formatCurrency(value);
  }

  delivery(days: number): string {
    return formatDeliveryDays(days);
  }

  itemTotal(proposal: QuotationItem): string {
    return formatCurrency(proposal.totalPrice);
  }

  formatInstallments(value?: number | null): string {
    return value ? `${value}x` : '-';
  }

  approverName(value?: string | null): string {
    return value && value.trim() ? value : '-';
  }

  budgetSelected(group: BudgetGroup): boolean {
    return group.items.length > 0 && group.items.every((item) => item.selected);
  }

  budgetComplete(group: BudgetGroup): boolean {
    return this.requestItems().every((requestItem) =>
      group.items.some((item) => item.purchaseRequestItemId === requestItem.id));
  }

  formatCnpj(document: string): string {
    const digits = (document || '').replace(/\D/g, '');

    if (digits.length !== 14) {
      return document || '-';
    }

    return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8, 12)}-${digits.slice(12)}`;
  }

  orderItemsTotal(order: PurchaseOrder): number {
    return order.items?.length ?? 0;
  }

  private getSubmitValidationMessage(): string {
    if (this.requestItems().length === 0) {
      return 'A solicitacao vinculada nao possui itens.';
    }

    for (const group of this.itemGroups()) {
      if (group.proposals.length === 0) {
        return `${this.itemLabel(group.requestItem)} ainda nao possui orcamento.`;
      }
    }

    return '';
  }

  private getApprovalValidationMessage(): string {
    const submitValidation = this.getSubmitValidationMessage();

    if (submitValidation) {
      return submitValidation;
    }

    const selectedGroups = this.budgetGroups().filter((group) => this.budgetSelected(group));

    if (selectedGroups.length !== 1) {
      return 'A cotacao precisa possuir exatamente um orcamento selecionado.';
    }

    if (!this.budgetComplete(selectedGroups[0])) {
      return 'O orcamento selecionado precisa conter todos os materiais da solicitacao.';
    }

    return '';
  }

  private updateQuotation(updated: Quotation, message: string): void {
    this.quotation.set(updated);
    this.toast.success(message);
  }

  private emptyDraft(): BudgetDraft {
    return {
      supplierId: '',
      includedItems: {},
      totalPriceTexts: {},
      totalPrices: {},
      observations: {},
      deliveryDays: 0,
      paymentCondition: '',
      paymentConditionOther: '',
      installmentCount: 1
    };
  }

  private resetBudgetForm(): void {
    this.editingBudgetSupplierId.set(null);
    this.budgetDraft.set(this.emptyDraft());
  }

  private paymentConditionValue(draft: BudgetDraft): string {
    if (draft.paymentCondition === 'Outro') {
      return draft.paymentConditionOther.trim();
    }

    return draft.paymentCondition;
  }

  private parseCurrency(value: string): number {
    const normalized = value
      .replace(/[^\d,.-]/g, '')
      .replace(/\./g, '')
      .replace(',', '.');

    const parsed = Number(normalized);

    return Number.isFinite(parsed) ? parsed : 0;
  }

  private normalizeId(value?: string | null): string {
    return (value ?? '').trim().toLowerCase();
  }
}
