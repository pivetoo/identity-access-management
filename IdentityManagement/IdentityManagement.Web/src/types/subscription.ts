export const SubscriptionStatus = {
  Trialing: 1,
  Active: 2,
  PastDue: 3,
  Suspended: 4,
  Canceled: 5,
} as const

export type SubscriptionStatus = (typeof SubscriptionStatus)[keyof typeof SubscriptionStatus]

export interface Subscription {
  id: number
  companyId: number
  planId: number
  planName: string
  status: SubscriptionStatus
  startedAt: string
  trialEndsAt?: string
  currentPeriodStart: string
  currentPeriodEnd: string
  canceledAt?: string
  providerName?: string
}
