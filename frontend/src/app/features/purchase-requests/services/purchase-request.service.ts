import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ApprovalDecision,
  CatalogItem,
  Company,
  CreatePurchaseRequest,
  CreatePurchaseRequestItem,
  PurchaseRequest,
  PurchaseRequestItem,
  UpdatePurchaseRequest,
  UpdatePurchaseRequestItem,
  Work
} from '../models/purchase-request.models';

@Injectable({
  providedIn: 'root'
})
export class PurchaseRequestService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAll(): Observable<PurchaseRequest[]> {
    return this.http.get<PurchaseRequest[]>(`${this.apiUrl}/PurchaseRequest`);
  }

  getById(id: string): Observable<PurchaseRequest> {
    return this.http.get<PurchaseRequest>(`${this.apiUrl}/PurchaseRequest/${id}`);
  }

  create(dto: CreatePurchaseRequest): Observable<PurchaseRequest> {
    return this.http.post<PurchaseRequest>(`${this.apiUrl}/PurchaseRequest`, dto);
  }

  update(id: string, dto: UpdatePurchaseRequest): Observable<PurchaseRequest> {
    return this.http.put<PurchaseRequest>(`${this.apiUrl}/PurchaseRequest/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/PurchaseRequest/${id}`);
  }

  submitForApproval(id: string): Observable<PurchaseRequest> {
    return this.http.post<PurchaseRequest>(
      `${this.apiUrl}/PurchaseRequest/${id}/submit-for-approval`,
      {}
    );
  }

  approve(id: string, dto: ApprovalDecision): Observable<PurchaseRequest> {
    return this.http.post<PurchaseRequest>(
      `${this.apiUrl}/PurchaseRequest/${id}/approve`,
      dto
    );
  }

  reject(id: string, dto: ApprovalDecision): Observable<PurchaseRequest> {
    return this.http.post<PurchaseRequest>(
      `${this.apiUrl}/PurchaseRequest/${id}/reject`,
      dto
    );
  }

  sendToQuotation(id: string): Observable<PurchaseRequest> {
    return this.http.post<PurchaseRequest>(
      `${this.apiUrl}/PurchaseRequest/${id}/send-to-quotation`,
      {}
    );
  }

  getAllItems(): Observable<PurchaseRequestItem[]> {
    return this.http.get<PurchaseRequestItem[]>(`${this.apiUrl}/PurchaseRequestItem`);
  }

  createItem(dto: CreatePurchaseRequestItem): Observable<PurchaseRequestItem> {
    return this.http.post<PurchaseRequestItem>(`${this.apiUrl}/PurchaseRequestItem`, dto);
  }

  updateItem(id: string, dto: UpdatePurchaseRequestItem): Observable<PurchaseRequestItem> {
    return this.http.put<PurchaseRequestItem>(`${this.apiUrl}/PurchaseRequestItem/${id}`, dto);
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/PurchaseRequestItem/${id}`);
  }

  getCatalogItems(): Observable<CatalogItem[]> {
    return this.http.get<CatalogItem[]>(`${this.apiUrl}/Item`);
  }

  getCompanies(): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.apiUrl}/Company`);
  }

  getWorks(): Observable<Work[]> {
    return this.http.get<Work[]>(`${this.apiUrl}/Work`);
  }
}
