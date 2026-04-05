import { httpClient, queryCollection } from 'archon-ui'
import type { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../types/company'
import type { PaginationParams, PaginatedResult } from 'archon-ui'

export class CompanyService {
  private static baseUrl = '/companies'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Company>> {
    const response = await httpClient.get<Company[]>(this.baseUrl)
    const companies = response.data ?? []

    return queryCollection(companies, params, ['legalName', 'tradeName', 'document', 'email'])
  }

  static async getById(id: number): Promise<Company> {
    const companies = await this.getActive()
    const company = companies.find((item) => item.id === id)

    if (!company) {
      throw new Error('Company não encontrada.')
    }

    return company
  }

  static async getActive(): Promise<Company[]> {
    const response = await httpClient.get<Company[]>(this.baseUrl)
    return response.data ?? []
  }

  static async create(company: CreateCompanyRequest): Promise<Company> {
    const response = await httpClient.post<Company>(this.baseUrl, company)

    if (!response.data) {
      throw new Error('Resposta vazia ao criar empresa.')
    }

    return response.data
  }

  static async update(id: number, company: UpdateCompanyRequest): Promise<Company> {
    const response = await httpClient.put<Company>(`${this.baseUrl}/${id}`, company)

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar empresa.')
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
