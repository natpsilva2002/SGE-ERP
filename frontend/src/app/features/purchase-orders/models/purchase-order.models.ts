export enum PurchaseOrderStatus {
  Open = 1,
  Approved = 2,
  Sent = 3,
  PartiallyReceived = 4,
  Received = 5,
  PartiallyCompleted = 6,
  Completed = 7,
  Cancelled = 8,
  WaitingSecondApproval = 9
}

export enum PurchaseOrderPaymentStatus {
  Unpaid = 1,
  PartiallyPaid = 2,
  Paid = 3
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
  status: PurchaseOrderStatus;
  totalValue: number;
  amountPaid: number;
  amountPending: number;
  paymentStatus: PurchaseOrderPaymentStatus;
  deliveryDays?: number | null;
  paymentCondition?: string | null;
  installmentCount?: number | null;
  approvedAt?: string | null;
  approvedByUserId?: string | null;
  firstApprovedAt?: string | null;
  firstApprovedByUserId?: string | null;
  firstApprovedByUserName?: string | null;
  secondApprovedAt?: string | null;
  secondApprovedByUserId?: string | null;
  secondApprovedByUserName?: string | null;
  sentAt?: string | null;
  sentByUserId?: string | null;
  paymentApprovedAt?: string | null;
  paymentApprovedByUserId?: string | null;
  paymentApprovedByUserName?: string | null;
  isPaymentApproved: boolean;
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

export interface PayPurchaseOrder {
  amount: number;
  paymentDate: string;
  paymentMethod: PaymentMethod;
  observation?: string | null;
}

export interface Payment {
  id: string;
  purchaseOrderId: string;
  paidByUserId: string;
  paymentDate: string;
  amount: number;
  paymentMethod: PaymentMethod;
  observation?: string | null;
  status: number;
}

export interface ReceivePurchaseOrderItem {
  purchaseOrderItemId: string;
  quantityReceived: number;
  observation?: string | null;
}

export interface ReceivePurchaseOrder {
  observation?: string | null;
  items: ReceivePurchaseOrderItem[];
}

export interface Receipt {
  id: string;
  purchaseOrderId: string;
  receivedByUserId: string;
  receivedByUserName?: string | null;
  receiptDate: string;
  observation?: string | null;
  invoiceNumber?: string | null;
  invoiceFileName?: string | null;
  invoiceFilePath?: string | null;
  status: ReceiptStatus;
  items: ReceiptItem[];
}

export interface ReceiptItem {
  id: string;
  receiptId: string;
  purchaseOrderItemId: string;
  itemDescription: string;
  unit: string;
  quantityReceived: number;
  observation?: string | null;
  hasDivergence: boolean;
  divergenceQuantity: number;
}

export enum ReceiptStatus {
  Registered = 1
}

export function getPurchaseOrderStatusLabel(status: PurchaseOrderStatus): string {
  const labels: Record<PurchaseOrderStatus, string> = {
    [PurchaseOrderStatus.Open]: 'Aberta',
    [PurchaseOrderStatus.Approved]: 'Aprovada',
    [PurchaseOrderStatus.Sent]: 'Enviada',
    [PurchaseOrderStatus.PartiallyReceived]: 'Parcialmente recebida',
    [PurchaseOrderStatus.Received]: 'Recebida',
    [PurchaseOrderStatus.PartiallyCompleted]: 'Encerrada parcialmente',
    [PurchaseOrderStatus.Completed]: 'Concluida',
    [PurchaseOrderStatus.Cancelled]: 'Cancelada',
    [PurchaseOrderStatus.WaitingSecondApproval]: 'Aguardando segundo aprovador'
  };

  return labels[status] ?? 'Desconhecido';
}

export function getPurchaseOrderPaymentStatusLabel(status: PurchaseOrderPaymentStatus): string {
  const labels: Record<PurchaseOrderPaymentStatus, string> = {
    [PurchaseOrderPaymentStatus.Unpaid]: 'Nao pago',
    [PurchaseOrderPaymentStatus.PartiallyPaid]: 'Parcialmente pago',
    [PurchaseOrderPaymentStatus.Paid]: 'Pago'
  };

  return labels[status] ?? 'Desconhecido';
}

export function getPaymentMethodLabel(method: PaymentMethod): string {
  const labels: Record<PaymentMethod, string> = {
    [PaymentMethod.Pix]: 'Pix',
    [PaymentMethod.BankTransfer]: 'Transferencia bancaria',
    [PaymentMethod.Boleto]: 'Boleto',
    [PaymentMethod.CreditCard]: 'Cartao de credito',
    [PaymentMethod.DebitCard]: 'Cartao de debito',
    [PaymentMethod.Cash]: 'Dinheiro',
    [PaymentMethod.Other]: 'Outro'
  };

  return labels[method] ?? 'Desconhecido';
}

export function getPurchaseOrderFinancialStatusLabel(order: PurchaseOrder): string {
  if (order.paymentStatus === PurchaseOrderPaymentStatus.Paid) {
    return 'Pago';
  }

  if (order.paymentStatus === PurchaseOrderPaymentStatus.PartiallyPaid) {
    return 'Parcialmente pago';
  }

  if (order.isPaymentApproved || order.paymentApprovedAt) {
    return 'Pagamento autorizado';
  }

  return 'Aguardando aprovacao';
}

export function getPurchaseOrderReceivingStatusLabel(order: PurchaseOrder): string {
  if (order.status === PurchaseOrderStatus.Received) {
    return 'Recebida';
  }

  if (order.status === PurchaseOrderStatus.PartiallyReceived ||
    order.items.some((item) => item.quantityReceived > 0)) {
    return 'Parcialmente recebida';
  }

  return 'Nao recebida';
}

export function getReceiptStatusLabel(status: ReceiptStatus): string {
  const labels: Record<ReceiptStatus, string> = {
    [ReceiptStatus.Registered]: 'Registrado'
  };

  return labels[status] ?? 'Desconhecido';
}

export const purchaseOrderStatusOptions = [
  PurchaseOrderStatus.Open,
  PurchaseOrderStatus.Approved,
  PurchaseOrderStatus.Sent,
  PurchaseOrderStatus.PartiallyReceived,
  PurchaseOrderStatus.Received,
  PurchaseOrderStatus.PartiallyCompleted,
  PurchaseOrderStatus.Completed,
  PurchaseOrderStatus.Cancelled,
  PurchaseOrderStatus.WaitingSecondApproval
];
