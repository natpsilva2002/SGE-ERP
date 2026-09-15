import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { PurchaseRequest, Work } from '../../../purchase-requests/models/purchase-request.models';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { Quotation } from '../../models/quotation.models';
import { QuotationStatus, getQuotationStatusLabel, quotationStatusOptions } from '../../models/quotation-status';
import { QuotationService } from '../../services/quotation.service';

@Component({
  selector: 'app-quotation-list',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './quotation-list.component.html',
  styleUrl: './quotation-list.component.css'
})
export class QuotationListComponent implements OnInit {
  private readonly service = inject(QuotationService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl<number | 'all'>('all', { nonNullable: true });
  readonly startDateControl = new FormControl('', { nonNullable: true });
  readonly endDateControl = new FormControl('', { nonNullable: true });
  readonly statusOptions = quotationStatusOptions;

  readonly quotations = signal<Quotation[]>([]);
  readonly purchaseRequests = signal<PurchaseRequest[]>([]);
  readonly works = signal<Work[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  readonly filteredQuotations = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();
    const status = this.statusControl.value;

    return this.quotations().filter((quotation) => {
      const request = this.findPurchaseRequest(quotation.purchaseRequestId);
      const text = [
        quotation.id,
        quotation.number,
        request?.number,
        request?.description,
        request ? this.workLabel(request.workId) : '',
        quotation.observation
      ].join(' ').toLowerCase();

      const matchesDate = this.matchesDateRange(
        quotation.quotationDate,
        this.startDateControl.value,
        this.endDateControl.value);

      return (!search || text.includes(search)) &&
        (status === 'all' || quotation.status === status) &&
        matchesDate;
    });
  });

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() => this.quotations.update((items) => [...items]));
    this.statusControl.valueChanges.subscribe(() => this.quotations.update((items) => [...items]));
    this.startDateControl.valueChanges.subscribe(() => this.quotations.update((items) => [...items]));
    this.endDateControl.valueChanges.subscribe(() => this.quotations.update((items) => [...items]));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      quotations: this.service.getAll(),
      purchaseRequests: this.service.getPurchaseRequests(),
      works: this.service.getWorks()
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ quotations, purchaseRequests, works }) => {
          this.quotations.set(quotations);
          this.purchaseRequests.set(purchaseRequests);
          this.works.set(works);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  requestLabel(purchaseRequestId: string): string {
    const request = this.findPurchaseRequest(purchaseRequestId);
    return request ? request.number : 'Solicitacao nao encontrada';
  }

  workLabel(workId: string): string {
    const work = this.works().find((candidate) => candidate.id === workId);
    return work ? `${work.code} - ${work.name}` : 'Nao informado';
  }

  quotationNumber(quotation: Quotation): string {
    return quotation.number || quotation.id;
  }

  statusLabel(status: QuotationStatus): string {
    return getQuotationStatusLabel(status);
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusControl.setValue('all');
    this.startDateControl.setValue('');
    this.endDateControl.setValue('');
  }

  findPurchaseRequest(id: string): PurchaseRequest | undefined {
    return this.purchaseRequests().find((request) => request.id === id);
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
