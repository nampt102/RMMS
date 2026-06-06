/** Mirrors `Rmms.Application.Approvals.ApprovalDto` (M09, camelCase over the wire). */
export type ApprovalStatus = "pending" | "approved" | "rejected" | "overridden";

export type Approval = {
  id: string;
  entityType: string;
  entityId: string;
  requesterId: string;
  requesterName: string;
  approverId: string;
  approverRole: string;
  status: ApprovalStatus;
  decisionReason: string | null;
  decidedAt: string | null;
  decidedVia: string | null;
  overriddenBy: string | null;
  overrideReason: string | null;
  createdAt: string;
};

/** Mirrors `EmailActionPreviewDto` — drives the public BUH landing page (AC-18). */
export type EmailActionPreview = {
  valid: boolean;
  expired: boolean;
  used: boolean;
  alreadyDecided: boolean;
  status: string | null;
  entityType: string | null;
  actionOptions: string[];
};

/** Mirrors `EmailActionResultDto`. */
export type EmailActionResult = {
  status: string;
  entityType: string;
};
