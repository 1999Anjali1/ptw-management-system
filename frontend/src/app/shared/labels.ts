import { PermitAction, PermitStatus, PermitType, UserRole } from '../core/models';

export const TYPE_LABELS: Record<PermitType, string> = {
  ColdWork: 'Cold Work',
  HotWork: 'Hot Work',
  ConfinedSpaceEntry: 'Confined Space Entry',
  Electrical: 'Electrical',
  WorkAtHeight: 'Work at Height',
  Excavation: 'Excavation',
};

export const ROLE_LABELS: Record<UserRole, string> = {
  Requester: 'Requester',
  AreaAuthority: 'Area Authority',
  Issuer: 'Permit Issuer',
  Admin: 'Administrator',
};

export const STATUS_LABELS: Record<PermitStatus, string> = {
  Draft: 'Draft',
  Submitted: 'Pending approval',
  Approved: 'Approved',
  Active: 'Active',
  Suspended: 'Suspended',
  Closed: 'Closed',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
};

/** Button text, style and whether a comment is mandatory for each workflow action. */
export const ACTION_CONFIG: Record<PermitAction, { label: string; style: 'primary' | 'danger' | 'neutral'; needsComment: boolean }> = {
  Submit: { label: 'Submit for approval', style: 'primary', needsComment: false },
  Approve: { label: 'Approve', style: 'primary', needsComment: false },
  Reject: { label: 'Reject', style: 'danger', needsComment: true },
  Issue: { label: 'Issue permit', style: 'primary', needsComment: false },
  Suspend: { label: 'Suspend work', style: 'danger', needsComment: true },
  Resume: { label: 'Resume work', style: 'primary', needsComment: false },
  Close: { label: 'Close permit', style: 'neutral', needsComment: false },
  Cancel: { label: 'Cancel permit', style: 'neutral', needsComment: false },
};
