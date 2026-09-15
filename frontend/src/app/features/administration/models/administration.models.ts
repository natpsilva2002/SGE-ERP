export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
  roleId: string;
  role: string;
}

export interface AdminRole {
  id: string;
  name: string;
  description: string;
}

export interface CreateAdminUser {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber?: string | null;
  roleId: string;
  isActive: boolean;
}

export interface UpdateAdminUser {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
  isActive: boolean;
}
