export const BillingPeriod = {
  Monthly: 1,
  Yearly: 2,
} as const

export type BillingPeriod = (typeof BillingPeriod)[keyof typeof BillingPeriod]

export interface Plan {
  id: number
  name: string
  description?: string
  priceAmount: number
  currency: string
  billingPeriod: BillingPeriod
  trialDays: number
  isActive: boolean
}

export interface CreatePlanRequest {
  name: string
  priceAmount: number
  billingPeriod: BillingPeriod
  currency?: string
  trialDays?: number
  description?: string
}

export interface UpdatePlanRequest {
  name: string
  priceAmount: number
  billingPeriod: BillingPeriod
  currency: string
  trialDays: number
  description?: string
}
