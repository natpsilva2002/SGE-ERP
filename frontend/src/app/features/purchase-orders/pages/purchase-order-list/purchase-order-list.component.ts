import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { formatCurrency } from '../../../quotations/services/formatters';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import {
  PurchaseOrder,
  PurchaseOrderPaymentStatus,
  PurchaseOrderStatus,
  getPurchaseOrderFinancialStatusLabel,
  getPurchaseOrderPaymentStatusLabel,
  getPurchaseOrderReceivingStatusLabel,
  getPurchaseOrderStatusLabel,
  purchaseOrderStatusOptions
} from '../../models/purchase-order.models';
import { PurchaseOrderService } from '../../services/purchase-order.service';

type ReceivingTab = 'all' | 'approval' | 'pending' | 'completed';

@Component({
  selector: 'app-purchase-order-list',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './purchase-order-list.component.html',
  styleUrl: './purchase-order-list.component.css'
})
export class PurchaseOrderListComponent implements OnInit {
  private readonly service = inject(PurchaseOrderService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl<number | 'all'>('all', { nonNullable: true });
  readonly startDateControl = new FormControl('', { nonNullable: true });
  readonly endDateControl = new FormControl('', { nonNullable: true });
  readonly statusOptions = purchaseOrderStatusOptions;

  readonly loading = signal(false);
  readonly error = signal('');
  readonly purchaseOrders = signal<PurchaseOrder[]>([]);
  readonly receivingTab = signal<ReceivingTab>('all');

  readonly allCount = computed(() => this.purchaseOrders().length);
  readonly awaitingApprovalCount = computed(() =>
    this.purchaseOrders().filter((order) => this.isAwaitingApproval(order)).length);
  readonly pendingReceivingCount = computed(() =>
    this.purchaseOrders().filter((order) => this.isAwaitingReceiving(order)).length);
  readonly completedReceivingCount = computed(() =>
    this.purchaseOrders().filter((order) => this.isReceivingCompleted(order)).length);

  readonly filteredOrders = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();
    const status = this.statusControl.value;
    const receivingTab = this.receivingTab();

    return this.purchaseOrders().filter((order) => {
      const matchesSearch = !search ||
        order.number.toLowerCase().includes(search) ||
        (order.supplierName ?? '').toLowerCase().includes(search) ||
        (order.workName ?? '').toLowerCase().includes(search);
      const matchesStatus = status === 'all' || order.status === status;
      const matchesDate = this.matchesDateRange(
        order.issueDate,
        this.startDateControl.value,
        this.endDateControl.value);
      const matchesReceivingTab = receivingTab === 'all' ||
        (receivingTab === 'approval' && this.isAwaitingApproval(order)) ||
        (receivingTab === 'pending' && this.isAwaitingReceiving(order)) ||
        (receivingTab === 'completed' && this.isReceivingCompleted(order));

      return matchesSearch && matchesStatus && matchesDate && matchesReceivingTab;
    });
  });

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
    this.statusControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
    this.startDateControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
    this.endDateControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (orders) => this.purchaseOrders.set(orders),
        error: (error) => this.error.set(getApiErrorMessage(error))
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

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusControl.setValue('all');
    this.startDateControl.setValue('');
    this.endDateControl.setValue('');
  }

  selectReceivingTab(tab: ReceivingTab): void {
    this.receivingTab.set(tab);
  }

  money(value: number): string {
    return formatCurrency(value);
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

  private isReceivingCompleted(order: PurchaseOrder): boolean {
    if (order.status === PurchaseOrderStatus.Received ||
      order.status === PurchaseOrderStatus.Completed) {
      return true;
    }

    return order.items.length > 0 &&
      order.items.every((item) => item.quantityPending <= 0);
  }

  private isAwaitingReceiving(order: PurchaseOrder): boolean {
    return !this.isReceivingCompleted(order);
  }

  private isAwaitingApproval(order: PurchaseOrder): boolean {
    return order.paymentStatus === PurchaseOrderPaymentStatus.Unpaid &&
      !order.isPaymentApproved &&
      !order.paymentApprovedAt;
  }
}
