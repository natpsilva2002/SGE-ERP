import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PurchaseRequest, Work } from '../../purchase-requests/models/purchase-request.models';
import { Supplier } from '../../quotations/models/quotation.models';
import {
  CreateServiceOrder,
  CreateServiceMeasurement,
  CreateServiceAdvancePaymentRequest,
  RejectServiceMeasurement,
  RejectServiceAdvancePaymentRequest,
  PayServiceOrder,
  ServiceMeasurement,
  ServiceOrder,
  ServiceOrderCreateContext,
  UpdateServiceMeasurement
} from '../models/service-order.models';

@Injectable({
  providedIn: 'root'
})
export class ServiceOrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAll(): Observable<ServiceOrder[]> {
    return this.http.get<ServiceOrder[]>(`${this.apiUrl}/ServiceOrder`);
  }

  getById(id: string): Observable<ServiceOrder> {
    return this.http.get<ServiceOrder>(`${this.apiUrl}/ServiceOrder/${id}`);
  }

  create(dto: CreateServiceOrder): Observable<ServiceOrder> {
    return this.http.post<ServiceOrder>(`${this.apiUrl}/ServiceOrder`, dto);
  }

  uploadContract(id: string, file: File): Observable<ServiceOrder> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ServiceOrder>(`${this.apiUrl}/ServiceOrder/${id}/contract`, formData);
  }

  downloadContract(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ServiceOrder/${id}/contract`, {
      responseType: 'blob'
    });
  }

  release(id: string): Observable<ServiceOrder> {
    return this.http.post<ServiceOrder>(`${this.apiUrl}/ServiceOrder/${id}/release`, {});
  }

  pay(id: string, payload: PayServiceOrder): Observable<ServiceOrder> {
    return this.http.post<ServiceOrder>(`${this.apiUrl}/ServiceOrder/${id}/pay`, payload);
  }

  requestAdvancePayment(
    id: string,
    payload: CreateServiceAdvancePaymentRequest
  ): Observable<ServiceOrder> {
    return this.http.post<ServiceOrder>(`${this.apiUrl}/ServiceOrder/${id}/advance-payments`, payload);
  }

  approveAdvancePayment(id: string, advancePaymentRequestId: string): Observable<ServiceOrder> {
    return this.http.post<ServiceOrder>(
      `${this.apiUrl}/ServiceOrder/${id}/advance-payments/${advancePaymentRequestId}/approve`,
      {});
  }

  rejectAdvancePayment(
    id: string,
    advancePaymentRequestId: string,
    payload: RejectServiceAdvancePaymentRequest
  ): Observable<ServiceOrder> {
    return this.http.post<ServiceOrder>(
      `${this.apiUrl}/ServiceOrder/${id}/advance-payments/${advancePaymentRequestId}/reject`,
      payload);
  }

  uploadAttachments(id: string, type: number, files: File[]): Observable<ServiceOrder> {
    const formData = new FormData();
    formData.append('type', String(type));
    files.forEach((file) => formData.append('files', file));

    return this.http.post<ServiceOrder>(`${this.apiUrl}/ServiceOrder/${id}/attachments`, formData);
  }

  downloadAttachment(id: string, attachmentId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ServiceOrder/${id}/attachments/${attachmentId}`, {
      responseType: 'blob'
    });
  }

  deleteAttachment(id: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/ServiceOrder/${id}/attachments/${attachmentId}`);
  }

  getMeasurements(id: string): Observable<ServiceMeasurement[]> {
    return this.http.get<ServiceMeasurement[]>(`${this.apiUrl}/ServiceOrder/${id}/measurements`);
  }

  createMeasurement(id: string, payload: CreateServiceMeasurement): Observable<ServiceMeasurement> {
    return this.http.post<ServiceMeasurement>(`${this.apiUrl}/ServiceOrder/${id}/measurements`, payload);
  }

  updateMeasurement(
    id: string,
    measurementId: string,
    payload: UpdateServiceMeasurement
  ): Observable<ServiceMeasurement> {
    return this.http.put<ServiceMeasurement>(
      `${this.apiUrl}/ServiceOrder/${id}/measurements/${measurementId}`,
      payload);
  }

  deleteMeasurement(id: string, measurementId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/ServiceOrder/${id}/measurements/${measurementId}`);
  }

  submitMeasurement(id: string, measurementId: string): Observable<ServiceMeasurement> {
    return this.http.post<ServiceMeasurement>(
      `${this.apiUrl}/ServiceOrder/${id}/measurements/${measurementId}/submit`,
      {});
  }

  approveMeasurement(id: string, measurementId: string): Observable<ServiceMeasurement> {
    return this.http.post<ServiceMeasurement>(
      `${this.apiUrl}/ServiceOrder/${id}/measurements/${measurementId}/approve`,
      {});
  }

  rejectMeasurement(
    id: string,
    measurementId: string,
    payload: RejectServiceMeasurement
  ): Observable<ServiceMeasurement> {
    return this.http.post<ServiceMeasurement>(
      `${this.apiUrl}/ServiceOrder/${id}/measurements/${measurementId}/reject`,
      payload);
  }

  getCreateContext(): Observable<ServiceOrderCreateContext> {
    return forkJoin({
      purchaseRequests: this.http.get<PurchaseRequest[]>(`${this.apiUrl}/PurchaseRequest`),
      suppliers: this.http.get<Supplier[]>(`${this.apiUrl}/Supplier`),
      works: this.http.get<Work[]>(`${this.apiUrl}/Work`),
      serviceOrders: this.getAll()
    });
  }
}
