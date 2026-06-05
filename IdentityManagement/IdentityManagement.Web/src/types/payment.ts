export const PaymentStatus = {
  Pending: 1,
  Confirmed: 2,
  Received: 3,
  Overdue: 4,
  Refunded: 5,
  ChargebackRequested: 6,
  Deleted: 7,
} as const

export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus]

export interface Payment {
  id: number
  externalPaymentId: string
  companyId?: number
  companyName?: string
  externalSubscriptionId?: string
  value: number
  billingType?: string
  status: PaymentStatus
  dueDate?: string
  paidDate?: string
  createdAt: string
}

export interface WebhookEvent {
  id: number
  externalEventId: string
  eventType: string
  externalPaymentId?: string
  outcome?: string
  processedAt: string
  rawPayload?: string
}
