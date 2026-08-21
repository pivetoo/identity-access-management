export interface ContactRequest {
  id: number
  name: string
  email: string
  phoneNumber?: string | null
  companyName?: string | null
  message: string
  sourceIp?: string | null
  notificationSent: boolean
  handledAt?: string | null
  createdAt: string
}
