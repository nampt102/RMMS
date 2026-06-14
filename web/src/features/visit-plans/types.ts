/** Mirrors `Rmms.Application.VisitPlans.VisitPlanDto` (camelCase over the wire). */

export type VisitPlanStatus = "pending" | "approved" | "rejected" | "executed";

export type VisitPlanItem = {
  id: string;
  storeId: string;
  storeName: string | null;
  formId: string;
  formName: string | null;
  ordering: number;
  executedAt: string | null;
  formSubmissionId: string | null;
};

export type VisitPlan = {
  id: string;
  leaderUserId: string;
  visitDate: string; // ISO date
  notes: string | null;
  status: VisitPlanStatus;
  approvalId: string | null;
  createdAt: string;
  items: VisitPlanItem[];
  leaderName: string | null;
};

export type ListVisitPlansParams = {
  status?: VisitPlanStatus;
  leaderUserId?: string;
};
