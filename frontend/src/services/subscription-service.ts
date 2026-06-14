import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { CheckoutRequest, CheckoutResponse, SubscriptionDto, SubscriptionPlanDto } from "@/types/subscription";

export const subscriptionService = {
  async listPlans() {
    const { data } = await api.get<ApiResponse<SubscriptionPlanDto[]>>("/subscriptions/plans");
    return data.data;
  },

  async getCurrent() {
    const { data } = await api.get<ApiResponse<SubscriptionDto>>("/subscriptions/me");
    return data.data;
  },

  async checkout(request: CheckoutRequest) {
    const { data } = await api.post<ApiResponse<CheckoutResponse>>("/subscriptions/checkout", request);
    return data.data;
  },

  async cancel() {
    const { data } = await api.post<ApiResponse<SubscriptionDto>>("/subscriptions/cancel");
    return data.data;
  },

  async resume() {
    const { data } = await api.post<ApiResponse<SubscriptionDto>>("/subscriptions/resume");
    return data.data;
  },
};
