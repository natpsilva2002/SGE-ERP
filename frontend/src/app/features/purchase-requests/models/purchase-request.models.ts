import { PurchaseRequestStatus } from './purchase-request-status';
import { PurchaseRequestType } from './purchase-request-type';

export interface PurchaseRequest {
  id: string;
  companyId: string;
  workId: string;
  requestedByUserId: string;
  requestedByUserName?: string | null;
  number: string;
  description: string;
  createdAt: string;
  type: PurchaseRequestType;
  serviceSpecification?: string | null;
  serviceQuantity?: number | null;
  serviceUnit?: string | null;
  serviceUnitOfMeasureId?: string | null;
  status: PurchaseRequestStatus;
  workflowStatus?: string | null;
  hasQuotation: boolean;
}

export interface CreatePurchaseRequest {
  workId: string;
  requestedByUserId?: string | null;
  number?: string | null;
  description: string;
  type: PurchaseRequestType;
  serviceSpecification?: string | null;
  serviceQuantity?: number | null;
  serviceUnit?: string | null;
  serviceUnitOfMeasureId?: string | null;
  items?: CreatePurchaseRequestMaterialItem[];
}

export interface CreatePurchaseRequestMaterialItem {
  itemId: string;
  quantity: number;
  observation?: string | null;
}

export interface UpdatePurchaseRequest {
  number: string;
  workId?: string | null;
  description: string;
  serviceSpecification?: string | null;
  serviceQuantity?: number | null;
  serviceUnit?: string | null;
  serviceUnitOfMeasureId?: string | null;
}

export interface PurchaseRequestItem {
  id: string;
  purchaseRequestId: string;
  itemId: string;
  quantity: number;
  unit: string;
  unitOfMeasureId?: string | null;
  observation?: string | null;
}

export interface CreatePurchaseRequestItem {
  purchaseRequestId: string;
  itemId: string;
  quantity: number;
  unit: string;
  unitOfMeasureId?: string | null;
  observation?: string | null;
}

export interface UpdatePurchaseRequestItem {
  quantity: number;
  unit: string;
  unitOfMeasureId?: string | null;
  observation?: string | null;
}

export interface ApprovalDecision {
  observation?: string | null;
}

export interface CatalogItem {
  id: string;
  categoryId?: string | null;
  code: string;
  description: string;
  unit: string;
  unitOfMeasureId?: string | null;
  isActive: boolean;
}

export interface CreateCatalogItem {
  categoryId?: string | null;
  code: string;
  description: string;
  unit: string;
  unitOfMeasureId?: string | null;
  isActive: boolean;
}

export interface UpdateCatalogItem {
  code: string;
  description: string;
  unit: string;
  unitOfMeasureId?: string | null;
  isActive: boolean;
}

export interface Category {
  id: string;
  name: string;
  description?: string | null;
}

export interface Company {
  id: string;
  corporateName: string;
  tradeName: string;
  document: string;
  email: string;
  phone: string;
}

export interface Work {
  id: string;
  companyId?: string | null;
  code: string;
  name: string;
  description?: string | null;
  startDate: string;
  endDate?: string | null;
  isActive: boolean;
}
