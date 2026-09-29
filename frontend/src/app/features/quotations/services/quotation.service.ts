import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ApprovalDecision,
  CreateQuotation,
  CreateQuotationItem,
  PurchaseOrder,
  Quotation,
  QuotationApprovalResult,
  QuotationItem,
  Supplier,
  QuotationSupplierOffer,
  UpdateQuotation,
  UpdateQuotationItem
} from '../models/quotation.models';
import {
  CatalogItem,
  PurchaseRequest,
  PurchaseRequestItem,
  Work
} from '../../purchase-requests/models/purchase-request.models';

@Injectable({
  providedIn: 'root'
})
export class QuotationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAll(): Observable<Quotation[]> {
    return this.http.get<Quotation[]>(`${this.apiUrl}/Quotation`);
  }

  getById(id: string): Observable<Quotation> {
    return this.http.get<Quotation>(`${this.apiUrl}/Quotation/${id}`);
  }

  create(dto: CreateQuotation): Observable<Quotation> {
    return this.http.post<Quotation>(`${this.apiUrl}/Quotation`, dto);
  }

  update(id: string, dto: UpdateQuotation): Observable<Quotation> {
    return this.http.put<Quotation>(`${this.apiUrl}/Quotation/${id}`, dto);
  }

  setSupplierOfferFreight(quotationId: string, supplierId: string, freightValue: number): Observable<Quotation> {
    return this.http.put<Quotation>(
      `${this.apiUrl}/Quotation/${quotationId}/supplier-offers/${supplierId}`,
      { freightValue }
    );
  }

  deleteSupplierOffer(quotationId: string, supplierId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/Quotation/${quotationId}/supplier-offers/${supplierId}`);
  }

  submitForApproval(id: string): Observable<Quotation> {
    return this.http.post<Quotation>(`${this.apiUrl}/Quotation/${id}/submit-for-approval`, {});
  }

  selectSupplier(quotationId: string, supplierId: string): Observable<Quotation> {
    return this.http.patch<Quotation>(
      `${this.apiUrl}/Quotation/${quotationId}/select-supplier/${supplierId}`,
      {}
    );
  }

  approve(id: string, dto: ApprovalDecision): Observable<QuotationApprovalResult> {
    return this.http.post<QuotationApprovalResult>(`${this.apiUrl}/Quotation/${id}/approve`, dto);
  }

  reject(id: string, dto: ApprovalDecision): Observable<Quotation> {
    return this.http.post<Quotation>(`${this.apiUrl}/Quotation/${id}/reject`, dto);
  }

  getQuotationItems(): Observable<QuotationItem[]> {
    return this.http.get<QuotationItem[]>(`${this.apiUrl}/QuotationItem`);
  }

  createQuotationItem(dto: CreateQuotationItem): Observable<QuotationItem> {
    return this.http.post<QuotationItem>(`${this.apiUrl}/QuotationItem`, dto);
  }

  updateQuotationItem(id: string, dto: UpdateQuotationItem): Observable<QuotationItem> {
    return this.http.put<QuotationItem>(`${this.apiUrl}/QuotationItem/${id}`, dto);
  }

  deleteQuotationItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/QuotationItem/${id}`);
  }

  getPurchaseRequests(): Observable<PurchaseRequest[]> {
    return this.http.get<PurchaseRequest[]>(`${this.apiUrl}/PurchaseRequest`);
  }

  getWorks(): Observable<Work[]> {
    return this.http.get<Work[]>(`${this.apiUrl}/Work`);
  }

  getPurchaseRequestItems(): Observable<PurchaseRequestItem[]> {
    return this.http.get<PurchaseRequestItem[]>(`${this.apiUrl}/PurchaseRequestItem`);
  }

  getCatalogItems(): Observable<CatalogItem[]> {
    return this.http.get<CatalogItem[]>(`${this.apiUrl}/Item`);
  }

  getSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.apiUrl}/Supplier`);
  }

  uploadQuotationAttachments(quotationId: string, supplierId: string, files: File[]): Observable<Quotation> {
    const formData = new FormData();
    formData.append('supplierId', supplierId);
    files.forEach((file) => formData.append('files', file));
    return this.http.post<Quotation>(`${this.apiUrl}/Quotation/${quotationId}/attachments`, formData);
  }

  downloadQuotationAttachment(quotationId: string, attachmentId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/Quotation/${quotationId}/attachments/${attachmentId}`, { responseType: 'blob' });
  }

  deleteQuotationAttachment(quotationId: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/Quotation/${quotationId}/attachments/${attachmentId}`);
  }

  getPurchaseOrders(): Observable<PurchaseOrder[]> {
    return this.http.get<PurchaseOrder[]>(`${this.apiUrl}/PurchaseOrder`);
  }
}
