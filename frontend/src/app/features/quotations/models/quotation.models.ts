import { PurchaseRequest, PurchaseRequestItem, CatalogItem } from '../../purchase-requests/models/purchase-request.models';
import { QuotationStatus } from './quotation-status';

export interface Quotation {
  id: string;
  purchaseRequestId: string;
  number: string;
  quotationDate: string;
  observation?: string | null;
  status: QuotationStatus;
  firstApprovedAt?: string | null;
  firstApprovedByUserId?: string | null;
  firstApprovedByUserName?: string | null;
  secondApprovedAt?: string | null;
  secondApprovedByUserId?: string | null;
  secondApprovedByUserName?: string | null;
}

export interface CreateQuotation {
  purchaseRequestId: string;
}

export interface UpdateQuotation {
  observation?: string | null;
}

export interface QuotationItem {
  id: string;
  quotationId: string;
  purchaseRequestItemId: string;
  supplierId: string;
  unitPrice: number;
  totalPrice: number;
  deliveryDays: number;
  proposalNumber?: string | null;
  paymentCondition?: string | null;
  installmentCount?: number | null;
  observation?: string | null;
  selected: boolean;
}

export interface CreateQuotationItem {
  quotationId: string;
  purchaseRequestItemId: string;
  supplierId: string;
  unitPrice?: number;
  totalPrice: number;
  deliveryDays: number;
  proposalNumber?: string | null;
  paymentCondition?: string | null;
  installmentCount?: number | null;
  observation?: string | null;
}

export interface UpdateQuotationItem {
  unitPrice?: number;
  totalPrice: number;
  deliveryDays: number;
  proposalNumber?: string | null;
  paymentCondition?: string | null;
  installmentCount?: number | null;
  observation?: string | null;
}

export interface Supplier {
  id: string;
  companyId?: string | null;
  corporateName: string;
  tradeName: string;
  document: string;
  stateRegistration?: string | null;
  email: string;
  phone: string;
  contactName: string;
  address: string;
  number: string;
  complement?: string | null;
  district: string;
  city: string;
  state: string;
  zipCode: string;
  pixKey?: string | null;
  bank?: string | null;
  agency?: string | null;
  account?: string | null;
  isActive: boolean;
}

export interface CreateSupplier {
  companyId?: string | null;
  corporateName: string;
  tradeName: string;
  document: string;
  stateRegistration?: string | null;
  email: string;
  phone: string;
  contactName: string;
  address: string;
  number: string;
  complement?: string | null;
  district: string;
  city: string;
  state: string;
  zipCode: string;
  pixKey?: string | null;
  bank?: string | null;
  agency?: string | null;
  account?: string | null;
  isActive: boolean;
}

export interface UpdateSupplier {
  corporateName: string;
  tradeName: string;
  document: string;
  stateRegistration?: string | null;
  email: string;
  phone: string;
  contactName: string;
  address: string;
  number: string;
  complement?: string | null;
  district: string;
  city: string;
  state: string;
  zipCode: string;
  pixKey?: string | null;
  bank?: string | null;
  agency?: string | null;
  account?: string | null;
  isActive: boolean;
}

export interface PurchaseOrder {
  id: string;
  quotationId: string;
  supplierId: string;
  supplierName: string;
  supplierDocument: string;
  purchaseRequestId: string;
  purchaseRequestNumber: string;
  workId: string;
  workName: string;
  quotationNumber: string;
  number: string;
  issueDate: string;
  expectedDeliveryDate?: string | null;
  status: number;
  totalValue: number;
  amountPaid: number;
  amountPending: number;
  paymentStatus: number;
  deliveryDays?: number | null;
  paymentCondition?: string | null;
  installmentCount?: number | null;
  approvedAt?: string | null;
  approvedByUserId?: string | null;
  sentAt?: string | null;
  sentByUserId?: string | null;
  items: PurchaseOrderItem[];
}

export interface PurchaseOrderItem {
  id: string;
  purchaseOrderId: string;
  itemId: string;
  itemDescription: string;
  quantityOrdered: number;
  quantityReceived: number;
  quantityPending: number;
  unit: string;
  unitPrice: number;
  negotiatedTotalValue?: number | null;
  totalValue: number;
  observation?: string | null;
}

export interface QuotationApprovalResult {
  quotation: Quotation;
  purchaseOrders: PurchaseOrder[];
}

export interface ApprovalDecision {
  observation?: string | null;
}

export interface QuotationRequestContext {
  quotation: Quotation;
  purchaseRequest: PurchaseRequest;
  requestItems: PurchaseRequestItem[];
  catalogItems: CatalogItem[];
  quotationItems: QuotationItem[];
  suppliers: Supplier[];
}
