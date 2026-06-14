import { subscriptionService } from "@/services/subscription-service";

export const subscriptionsApi = {
  getPlans: subscriptionService.listPlans,
  getCurrent: subscriptionService.getCurrent,
  checkout: subscriptionService.checkout,
  cancel: subscriptionService.cancel,
  resume: subscriptionService.resume,
};
