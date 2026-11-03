// TypeScript mirrors of the API DTOs (enums are sent as strings by the .NET API)

export type UserRole = 'Requester' | 'AreaAuthority' | 'Issuer' | 'Admin';
export type PermitType = 'ColdWork' | 'HotWork' | 'ConfinedSpaceEntry' | 'Electrical' | 'WorkAtHeight' | 'Excavation';
export type PermitStatus = 'Draft' | 'Submitted' | 'Approved' | 'Active' | 'Suspended' | 'Closed' | 'Rejected' | 'Cancelled';
export type PermitAction = 'Submit' | 'Approve' | 'Reject' | 'Issue' | 'Suspend' | 'Resume' | 'Close' | 'Cancel';
export type RiskLevel = 'Low' | 'Medium' | 'High';
export type IsolationType = 'Electrical' | 'Mechanical' | 'Process' | 'Instrument';

export const PERMIT_TYPES: PermitType[] = ['ColdWork', 'HotWork', 'ConfinedSpaceEntry', 'Electrical', 'WorkAtHeight', 'Excavation'];
export const PERMIT_STATUSES: PermitStatus[] = ['Draft', 'Submitted', 'Approved', 'Active', 'Suspended', 'Closed', 'Rejected', 'Cancelled'];
export const RISK_LEVELS: RiskLevel[] = ['Low', 'Medium', 'High'];
export const ISOLATION_TYPES: IsolationType[] = ['Electrical', 'Mechanical', 'Process', 'Instrument'];

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  company: string;
  isActive: boolean;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PermitSummary {
  id: number;
  permitNumber: string;
  title: string;
  type: PermitType;
  status: PermitStatus;
  riskLevel: RiskLevel;
  location: string;
  plannedStart: string;
  plannedEnd: string;
  requestedBy: string;
  company: string;
  isOverdue: boolean;
}

export interface GasTest {
  id: number;
  oxygenPercent: number;
  lelPercent: number;
  h2sPpm: number;
  coPpm: number;
  isPassed: boolean;
  remarks?: string;
  testedBy: string;
  testedAt: string;
  failures: string[];
}

export interface Isolation {
  id: number;
  type: IsolationType;
  isolationPoint: string;
  lockNumber?: string;
  description?: string;
  isRemoved: boolean;
  appliedBy: string;
  appliedAt: string;
  removedBy?: string;
  removedAt?: string;
}

export interface PermitHistory {
  action: string;
  fromStatus?: PermitStatus;
  toStatus: PermitStatus;
  comment?: string;
  performedBy: string;
  timestamp: string;
}

export interface PermitDetail extends PermitSummary {
  workDescription: string;
  equipmentTag?: string;
  workersCount: number;
  hazards: string;
  precautions: string;
  requiredPpe: string;
  requestedById: number;
  approvedBy?: string;
  issuedBy?: string;
  createdAt: string;
  submittedAt?: string;
  approvedAt?: string;
  issuedAt?: string;
  closedAt?: string;
  lastComment?: string;
  requiresGasTest: boolean;
  requiresIsolation: boolean;
  availableActions: PermitAction[];
  canEdit: boolean;
  canRecordSafetyChecks: boolean;
  gasTests: GasTest[];
  isolations: Isolation[];
  history: PermitHistory[];
}

export interface PermitRequest {
  type: PermitType;
  riskLevel: RiskLevel;
  title: string;
  workDescription: string;
  location: string;
  equipmentTag?: string | null;
  workersCount: number;
  plannedStart: string;
  plannedEnd: string;
  hazards: string;
  precautions: string;
  requiredPpe: string;
}

export interface PermitQuery {
  status?: PermitStatus | '';
  type?: PermitType | '';
  search?: string;
  mine?: boolean;
  page: number;
  pageSize: number;
}

export interface PermitConflict {
  id: number;
  permitNumber: string;
  title: string;
  type: PermitType;
  status: PermitStatus;
  plannedStart: string;
  plannedEnd: string;
}

export interface GasTestRequest {
  oxygenPercent: number;
  lelPercent: number;
  h2sPpm: number;
  coPpm: number;
  remarks?: string;
}

export interface IsolationRequest {
  type: IsolationType;
  isolationPoint: string;
  lockNumber?: string;
  description?: string;
}

export interface DashboardSummary {
  totalPermits: number;
  activePermits: number;
  pendingApproval: number;
  readyToIssue: number;
  suspended: number;
  overdue: number;
  openIsolations: number;
  byStatus: Record<string, number>;
  byType: Record<string, number>;
  activeNow: PermitSummary[];
}
