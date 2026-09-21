export const AppRoles = {
  Admin: 'Administrador',
  Warehouse: 'Almoxarife',
  Buyer: 'Compras',
  Finance: 'Financeiro'
} as const;

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles];
