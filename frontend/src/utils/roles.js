export const ROLES = {
  REQUESTER: 'Requester',
  MANAGER: 'Manager',
  OFFICER: 'ProcurementOfficer',
  ADMIN: 'Admin',
};

export const ROLE_LABELS = {
  Requester: 'Requester',
  Manager: 'Manager',
  ProcurementOfficer: 'Procurement Officer',
  Admin: 'Admin',
};

// Who may open which screen. The API enforces the same rules.
export const ACCESS = {
  approvals: [ROLES.MANAGER, ROLES.ADMIN],
  orders: [ROLES.MANAGER, ROLES.OFFICER, ROLES.ADMIN],
  users: [ROLES.ADMIN],
  auditLogs: [ROLES.ADMIN],
  // May create, edit and delete vendors and products
  masterData: [ROLES.OFFICER, ROLES.ADMIN],
  // May create purchase requests
  requestCreate: [ROLES.REQUESTER, ROLES.ADMIN],
};
