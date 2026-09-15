export const AppRoles = {
  Requester: 'Requester',
  Approver: 'Approver',
  Buyer: 'Buyer',
  Warehouse: 'Warehouse',
  Finance: 'Finance',
  Admin: 'Admin'
} as const;

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles];
