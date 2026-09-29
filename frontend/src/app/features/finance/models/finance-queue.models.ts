export enum FinanceQueueDocumentType {
  Material = 1,
  Service = 2
}

export enum FinanceQueuePaymentStatus {
  AwaitingApproval = 1,
  Authorized = 2,
  PartiallyPaid = 3,
  Paid = 4,
  Unpaid = 5
}

export interface FinanceQueueItem {
  id: string;
  documentType: FinanceQueueDocumentType;
  number: string;
  supplierName: string;
  workName: string;
  totalValue: number;
  amountPaid: number;
  amountPending: number;
  paymentStatus: FinanceQueuePaymentStatus;
  date: string;
}
