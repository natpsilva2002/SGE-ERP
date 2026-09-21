import { Supplier } from '../../quotations/models/quotation.models';
import { PurchaseRequest, Work } from '../../purchase-requests/models/purchase-request.models';

export enum ServiceOrderExecutionStatus {
  WaitingContract = 1,
  Released = 2,
  InProgress = 3,
  Completed = 4,
  Cancelled = 5
}

export enum ServiceOrderPaymentStatus {
  Unpaid = 1,
  PartiallyPaid = 2,
  Paid = 3
}

export enum ServiceMeasurementStatus {
  Draft = 1,
  WaitingApproval = 2,
  Approved = 3,
  Rejected = 4
}

export enum PaymentMethod {
  Pix = 1,
  BankTransfer = 2,
  Boleto = 3,
  CreditCard = 4,
  DebitCard = 5,
  Cash = 6,
  Other = 7
}

export enum ServiceAdvancePaymentStatus {
  WaitingApproval = 1,
  Approved = 2,
  Rejected = 3
}

export enum ServiceOrderAttachmentType {
  Invoice = 1,
  PaymentReceipt = 2,
  Other = 3
}

export interface ServiceOrder {
  id: string;
  number: string;
  purchaseRequestId: string;
  purchaseRequestNumber: string;
  requestedByUserName?: string | null;
  workId: string;
  workName: string;
  supplierId: string;
  supplierName: string;
  supplierDocument: string;
  serviceDescription: string;
  serviceSpecification?: string | null;
  estimatedQuantity?: number | null;
  unit?: string | null;
  contractedValue: number;
  paymentCondition?: string | null;
  installmentCount?: number | null;
  executionStatus: ServiceOrderExecutionStatus;
  paymentStatus: ServiceOrderPaymentStatus;
  amountPaid: number;
  amountPending: number;
  availableToPay: number;
  availableMeasuredToPay: number;
  availableAdvanceToPay: number;
  unmeasuredBalance: number;
  contractFileName?: string | null;
  contractUploadedAt?: string | null;
  contractUploadedByUserId?: string | null;
  contractUploadedByUserName?: string | null;
  approvedMeasuredAmount: number;
  committedMeasuredAmount: number;
  remainingToMeasure: number;
  executionPercentage: number;
  measuredQuantity: number;
  remainingQuantity: number;
  physicalPercentage: number;
  financialPercentage: number;
  measurements: ServiceMeasurement[];
  payments: ServiceOrderPayment[];
  lastPaymentId?: string | null;
  advancePaymentRequests: ServiceAdvancePaymentRequest[];
  attachments: ServiceOrderAttachment[];
}

export interface CreateServiceOrder {
  purchaseRequestId: string;
  supplierId: string;
  contractedValue: number;
  paymentCondition?: string | null;
  installmentCount?: number | null;
}

export interface ServiceOrderCreateContext {
  purchaseRequests: PurchaseRequest[];
  suppliers: Supplier[];
  works: Work[];
  serviceOrders: ServiceOrder[];
}

export interface ServiceMeasurement {
  id: string;
  serviceOrderId: string;
  measurementNumber: string;
  measurementDate: string;
  description: string;
  quantityMeasured: number;
  unit: string;
  amount: number;
  observation?: string | null;
  status: ServiceMeasurementStatus;
  createdByUserId: string;
  createdByUserName?: string | null;
  createdAt: string;
  approvedAt?: string | null;
  approvedByUserId?: string | null;
  approvedByUserName?: string | null;
  rejectedAt?: string | null;
  rejectedByUserId?: string | null;
  rejectedByUserName?: string | null;
  rejectionReason?: string | null;
  attachments: ServiceMeasurementAttachment[];
}

export interface ServiceMeasurementAttachment {
  id: string;
  serviceMeasurementId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  uploadedAt: string;
}

export interface ServiceOrderPayment {
  id: string;
  serviceOrderId: string;
  paidByUserId: string;
  advancePaymentRequestId?: string | null;
  paidByUserName?: string | null;
  paymentDate: string;
  amount: number;
  paymentMethod: PaymentMethod;
  observation?: string | null;
  status: number;
  attachments: ServiceOrderPaymentAttachment[];
}

export interface ServiceOrderPaymentAttachment {
  id: string;
  serviceOrderPaymentId: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  uploadedAt: string;
}

export interface PayServiceOrder {
  advancePaymentRequestId?: string | null;
  amount: number;
  paymentDate: string;
  paymentMethod: PaymentMethod;
  observation?: string | null;
}

export interface CreateServiceAdvancePaymentRequest {
  amount: number;
  observation?: string | null;
}

export interface RejectServiceAdvancePaymentRequest {
  rejectionReason: string;
}

export interface ServiceAdvancePaymentRequest {
  id: string;
  serviceOrderId: string;
  requestedByUserId: string;
  requestedByUserName?: string | null;
  requestedAt: string;
  amount: number;
  amountPaid: number;
  amountPending: number;
  observation?: string | null;
  status: ServiceAdvancePaymentStatus;
  approvedByUserId?: string | null;
  approvedByUserName?: string | null;
  approvedAt?: string | null;
  rejectedByUserId?: string | null;
  rejectedByUserName?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
}

export interface ServiceOrderAttachment {
  id: string;
  serviceOrderId: string;
  type: ServiceOrderAttachmentType;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  uploadedByUserName?: string | null;
  uploadedAt: string;
}

export interface CreateServiceMeasurement {
  measurementDate: string;
  description: string;
  quantityMeasured: number;
  unit?: string | null;
  amount: number;
  observation?: string | null;
}

export type UpdateServiceMeasurement = CreateServiceMeasurement;

export interface RejectServiceMeasurement {
  rejectionReason: string;
}

export function getExecutionStatusLabel(status: ServiceOrderExecutionStatus): string {
  switch (status) {
    case ServiceOrderExecutionStatus.WaitingContract:
      return 'Aguardando contrato';
    case ServiceOrderExecutionStatus.Released:
      return 'Liberada';
    case ServiceOrderExecutionStatus.InProgress:
      return 'Em execucao';
    case ServiceOrderExecutionStatus.Completed:
      return 'Concluida';
    case ServiceOrderExecutionStatus.Cancelled:
      return 'Cancelada';
    default:
      return 'Nao informado';
  }
}

export function getPaymentStatusLabel(status: ServiceOrderPaymentStatus): string {
  switch (status) {
    case ServiceOrderPaymentStatus.Unpaid:
      return 'Nao pago';
    case ServiceOrderPaymentStatus.PartiallyPaid:
      return 'Parcialmente pago';
    case ServiceOrderPaymentStatus.Paid:
      return 'Pago';
    default:
      return 'Nao informado';
  }
}

export function getMeasurementStatusLabel(status: ServiceMeasurementStatus): string {
  switch (status) {
    case ServiceMeasurementStatus.Draft:
      return 'Rascunho';
    case ServiceMeasurementStatus.WaitingApproval:
      return 'Aguardando aprovacao';
    case ServiceMeasurementStatus.Approved:
      return 'Aprovada';
    case ServiceMeasurementStatus.Rejected:
      return 'Rejeitada';
    default:
      return 'Nao informado';
  }
}

export function getPaymentMethodLabel(method: PaymentMethod): string {
  switch (method) {
    case PaymentMethod.Pix:
      return 'Pix';
    case PaymentMethod.BankTransfer:
      return 'Transferencia bancaria';
    case PaymentMethod.Boleto:
      return 'Boleto';
    case PaymentMethod.CreditCard:
      return 'Cartao de credito';
    case PaymentMethod.DebitCard:
      return 'Cartao de debito';
    case PaymentMethod.Cash:
      return 'Dinheiro';
    case PaymentMethod.Other:
      return 'Outro';
    default:
      return 'Nao informado';
  }
}

export function getAdvancePaymentStatusLabel(status: ServiceAdvancePaymentStatus): string {
  switch (status) {
    case ServiceAdvancePaymentStatus.WaitingApproval:
      return 'Aguardando aprovacao';
    case ServiceAdvancePaymentStatus.Approved:
      return 'Aprovada';
    case ServiceAdvancePaymentStatus.Rejected:
      return 'Rejeitada';
    default:
      return 'Nao informado';
  }
}

export function getAttachmentTypeLabel(type: ServiceOrderAttachmentType): string {
  switch (type) {
    case ServiceOrderAttachmentType.Invoice:
      return 'Nota fiscal';
    case ServiceOrderAttachmentType.PaymentReceipt:
      return 'Comprovante de pagamento';
    case ServiceOrderAttachmentType.Other:
      return 'Outro';
    default:
      return 'Nao informado';
  }
}
