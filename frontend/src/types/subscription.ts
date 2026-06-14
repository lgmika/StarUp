export interface UsageQuotaDto {
  resourceKey: string;
  limit: number;
}

export interface SubscriptionPlanDto {
  id: string;
  code: string;
  name: string;
  description: string;
  monthlyPrice: number;
  currency: string;
  quotas: UsageQuotaDto[];
}

export interface SubscriptionDto {
  id: string;
  planId: string;
  planCode: string;
  planName: string;
  status: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEndsAt?: string;
  cancelledAt?: string;
  quotas: UsageQuotaDto[];
}

export interface CheckoutRequest {
  planId: string;
  successUrl?: string;
  cancelUrl?: string;
}

export interface CheckoutResponse {
  transactionId: string;
  provider: string;
  checkoutSessionId: string;
  checkoutUrl: string;
}
