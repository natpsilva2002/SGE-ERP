import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  PayPurchaseOrder,
  Payment,
  PurchaseOrder,
  Receipt,
  ReceivePurchaseOrder
} from '../models/purchase-order.models';

@Injectable({
  providedIn: 'root'
})
export class PurchaseOrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAll(): Observable<PurchaseOrder[]> {
    return this.http.get<PurchaseOrder[]>(`${this.apiUrl}/PurchaseOrder`);
  }

  getById(id: string): Observable<PurchaseOrder> {
    return this.http.get<PurchaseOrder>(`${this.apiUrl}/PurchaseOrder/${id}`);
  }

  approve(id: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiUrl}/PurchaseOrder/${id}/approve`, {});
  }

  approvePayment(id: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiUrl}/PurchaseOrder/${id}/approve-payment`, {});
  }

  markAsSent(id: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.apiUrl}/PurchaseOrder/${id}/mark-as-sent`, {});
  }

  pay(id: string, payload: PayPurchaseOrder): Observable<Payment> {
    return this.http.post<Payment>(`${this.apiUrl}/PurchaseOrder/${id}/pay`, payload);
  }

  receive(id: string, payload: ReceivePurchaseOrder): Observable<Receipt> {
    return this.http.post<Receipt>(`${this.apiUrl}/PurchaseOrder/${id}/receive`, payload);
  }

  getReceiptsByPurchaseOrder(id: string): Observable<Receipt[]> {
    return this.http.get<Receipt[]>(`${this.apiUrl}/Receipt/by-purchase-order/${id}`);
  }

  uploadReceiptInvoice(
    receiptId: string,
    invoiceNumber: string | null,
    file: File | null
  ): Observable<Receipt> {
    const formData = new FormData();

    if (invoiceNumber) {
      formData.append('invoiceNumber', invoiceNumber);
    }

    if (file) {
      formData.append('file', file);
    }

    return this.http.post<Receipt>(`${this.apiUrl}/Receipt/${receiptId}/invoice`, formData);
  }

  downloadReceiptInvoice(receiptId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/Receipt/${receiptId}/invoice`, {
      responseType: 'blob'
    });
  }

  downloadPdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/PurchaseOrder/${id}/pdf`, {
      responseType: 'blob'
    });
  }
}
