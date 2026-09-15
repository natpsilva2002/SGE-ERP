import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { CatalogItem, PurchaseRequest, PurchaseRequestItem } from '../../../purchase-requests/models/purchase-request.models';
import { PurchaseRequestStatus } from '../../../purchase-requests/models/purchase-request-status';
import { PurchaseRequestType } from '../../../purchase-requests/models/purchase-request-type';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { QuotationService } from '../../services/quotation.service';

@Component({
  selector: 'app-quotation-create',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './quotation-create.component.html',
  styleUrl: './quotation-create.component.css'
})
export class QuotationCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(QuotationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly purchaseRequests = signal<PurchaseRequest[]>([]);
  readonly purchaseRequestItems = signal<PurchaseRequestItem[]>([]);
  readonly catalogItems = signal<CatalogItem[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');

  readonly availableRequests = computed(() =>
    this.purchaseRequests().filter((request) =>
      request.status === PurchaseRequestStatus.WaitingQuotation &&
      request.type === PurchaseRequestType.Material)
  );

  readonly form = this.fb.nonNullable.group({
    purchaseRequestId: ['', Validators.required]
  });

  readonly selectedRequest = computed(() =>
    this.purchaseRequests().find((request) => request.id === this.form.controls.purchaseRequestId.value) ?? null
  );

  readonly selectedRequestItems = computed(() =>
    this.purchaseRequestItems().filter((item) => item.purchaseRequestId === this.form.controls.purchaseRequestId.value)
  );

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      purchaseRequests: this.service.getPurchaseRequests(),
      purchaseRequestItems: this.service.getPurchaseRequestItems(),
      catalogItems: this.service.getCatalogItems()
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ purchaseRequests, purchaseRequestItems, catalogItems }) => {
          this.purchaseRequests.set(purchaseRequests);
          this.purchaseRequestItems.set(purchaseRequestItems);
          this.catalogItems.set(catalogItems);

          const purchaseRequestId = this.route.snapshot.queryParamMap.get('purchaseRequestId');

          if (purchaseRequestId) {
            this.form.controls.purchaseRequestId.setValue(purchaseRequestId);
            return;
          }

          this.error.set('Crie uma cotacao a partir do detalhe da solicitacao.');
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');

    this.service.create(this.form.getRawValue())
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (quotation) => {
          this.toast.success('Cotacao criada com sucesso.');
          void this.router.navigate(['/app/cotacoes', quotation.id]);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  itemLabel(requestItem: PurchaseRequestItem): string {
    const item = this.catalogItems().find((candidate) => candidate.id === requestItem.itemId);

    return item ? `${requestItem.quantity} ${requestItem.unit} - ${item.description}` : `${requestItem.quantity} ${requestItem.unit}`;
  }
}
