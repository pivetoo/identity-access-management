import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../types/company'

export class CompanyService {
  private static baseUrl = '/companies'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Company>> {
    const response = await httpClient.get<Company[]>(`${this.baseUrl}/getactive`)
    const companies = response.data ?? []

    return queryCollection(companies, params, ['legalName', 'tradeName', 'document', 'email'])
  }

  static async getById(id: number): Promise<Company> {
    const companies = await this.getActive()
    const company = companies.find((item) => item.id === id)

    if (!company) {
      throw new Error(translate('company.notFound'))
    }

    return company
  }

  static async getActive(): Promise<Company[]> {
    const response = await httpClient.get<Company[]>(`${this.baseUrl}/getactive`)
    return response.data ?? []
  }

  static async create(company: CreateCompanyRequest): Promise<Company> {
    const response = await httpClient.post<Company>(`${this.baseUrl}/create`, company)

    if (!response.data) {
      throw new Error(translate('company.service.create.emptyResponse'))
    }

    return response.data
  }

  static async update(id: number, company: UpdateCompanyRequest): Promise<Company> {
    const response = await httpClient.put<Company>(`${this.baseUrl}/update/${id}`, company)

    if (!response.data) {
      throw new Error(translate('company.service.update.emptyResponse'))
    }

    return response.data
  }

  static async delete(id: number): Promise<void> {
    const company = await this.getById(id)

    await this.update(id, {
      id,
      legalName: company.legalName,
      tradeName: company.tradeName,
      document: company.document,
      email: company.email,
      phoneNumber: company.phoneNumber,
      isActive: false,
    })
  }

  static async existsDocument(document: string): Promise<boolean> {
    const companies = await this.getActive()
    return companies.some((company) => company.document === document)
  }

  static async existsEmail(email: string): Promise<boolean> {
    const companies = await this.getActive()
    return companies.some((company) => company.email.toLowerCase() === email.toLowerCase())
  }
}
