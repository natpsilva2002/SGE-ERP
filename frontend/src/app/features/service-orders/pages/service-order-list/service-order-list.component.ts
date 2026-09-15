import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import {
  ServiceOrder,
  getExecutionStatusLabel,
  getPaymentStatusLabel
} from '../../models/service-order.models';
import { formatCurrency } from '../../services/formatters';
import { ServiceOrderService } from '../../services/service-order.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-service-order-list',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './service-order-list.component.html',
  styleUrl: './service-order-list.component.css'
})
export class ServiceOrderListComponent implements OnInit {
  private readonly service = inject(ServiceOrderService);
  private readonly authService = inject(AuthService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly loading = signal(false);
  readonly error = signal('');
  readonly serviceOrders = signal<ServiceOrder[]>([]);

  readonly filteredOrders = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();

    return this.serviceOrders().filter((order) => !search ||
      order.number.toLowerCase().includes(search) ||
      order.serviceDescription.toLowerCase().includes(search) ||
      order.supplierName.toLowerCase().includes(search));
  });

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() =>
      this.serviceOrders.update((orders) => [...orders]));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (orders) => this.serviceOrders.set(orders),
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  canCreate(): boolean {
    return this.authService.hasRole([AppRoles.Approver, AppRoles.Admin]);
  }

  executionLabel(order: ServiceOrder): string {
    return getExecutionStatusLabel(order.executionStatus);
  }

  paymentLabel(order: ServiceOrder): string {
    return getPaymentStatusLabel(order.paymentStatus);
  }

  money(value: number): string {
    return formatCurrency(value);
  }
}
