export enum QuotationStatus {
  Draft = 1,
  WaitingApproval = 2,
  Approved = 3,
  Rejected = 4,
  Completed = 5,
  WaitingSecondApproval = 6
}

const statusLabels: Record<QuotationStatus, string> = {
  [QuotationStatus.Draft]: 'Rascunho',
  [QuotationStatus.WaitingApproval]: 'Aguardando aprovacao',
  [QuotationStatus.Approved]: 'Aprovada',
  [QuotationStatus.Rejected]: 'Rejeitada',
  [QuotationStatus.Completed]: 'Concluida',
  [QuotationStatus.WaitingSecondApproval]: 'Aguardando segunda aprovacao'
};

export const quotationStatusOptions = Object.entries(statusLabels).map(
  ([value, label]) => ({
    value: Number(value),
    label
  })
);

export function getQuotationStatusLabel(status: QuotationStatus): string {
  return statusLabels[status] ?? 'Status desconhecido';
}
