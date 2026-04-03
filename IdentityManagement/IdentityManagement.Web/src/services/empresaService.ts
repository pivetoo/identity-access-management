import { httpClient } from 'd-rts'
import type { PaginationParams, PaginatedResult } from 'd-rts'
import type { Empresa, CreateEmpresaRequest, UpdateEmpresaRequest } from '../types/empresa'
import { queryCollection } from './serviceUtils'

interface CompanyApiResponse {
  id: number
  legalName: string
  tradeName: string
  document: string
  email: string
  phoneNumber: string
  isActive: boolean
  createdAt?: string
  updatedAt?: string
}

function mapCompany(company: CompanyApiResponse): Empresa {
  return {
    id: company.id,
    nome: company.legalName,
    nomeFantasia: company.tradeName,
    documento: company.document,
    email: company.email,
    telefone: company.phoneNumber,
    isActive: company.isActive,
    criadoEm: company.createdAt ?? '',
    ultimaAlteracao: company.updatedAt ?? '',
  }
}

export class EmpresaService {
  private static baseUrl = '/companies'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Empresa>> {
    const response = await httpClient.get<CompanyApiResponse[]>(this.baseUrl)
    const empresas = (response.data ?? []).map(mapCompany)

    return queryCollection(empresas, params, ['nome', 'nomeFantasia', 'documento', 'email'])
  }

  static async getById(id: number): Promise<Empresa> {
    const empresas = await this.getActive()
    const empresa = empresas.find((item) => item.id === id)

    if (!empresa) {
      throw new Error('Empresa não encontrada.')
    }

    return empresa
  }

  static async getActive(): Promise<Empresa[]> {
    const response = await httpClient.get<CompanyApiResponse[]>(this.baseUrl)
    return (response.data ?? []).map(mapCompany)
  }

  static async create(empresa: CreateEmpresaRequest): Promise<Empresa> {
    const response = await httpClient.post<CompanyApiResponse>(this.baseUrl, {
      legalName: empresa.nome,
      tradeName: empresa.nomeFantasia,
      document: empresa.documento,
      email: empresa.email,
      phoneNumber: empresa.telefone,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao criar empresa.')
    }

    return mapCompany(response.data)
  }

  static async update(id: number, empresa: UpdateEmpresaRequest): Promise<Empresa> {
    const response = await httpClient.put<CompanyApiResponse>(`${this.baseUrl}/${id}`, {
      id,
      legalName: empresa.nome,
      tradeName: empresa.nomeFantasia,
      document: empresa.documento,
      email: empresa.email,
      phoneNumber: empresa.telefone,
      isActive: empresa.isActive,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar empresa.')
    }

    return mapCompany(response.data)
  }

  static async delete(id: number): Promise<void> {
    const empresa = await this.getById(id)

    await this.update(id, {
      id,
      nome: empresa.nome,
      nomeFantasia: empresa.nomeFantasia,
      documento: empresa.documento,
      email: empresa.email,
      telefone: empresa.telefone,
      isActive: false,
    })
  }

  static async existsDocumento(documento: string): Promise<boolean> {
    const empresas = await this.getActive()
    return empresas.some((empresa) => empresa.documento === documento)
  }

  static async existsEmail(email: string): Promise<boolean> {
    const empresas = await this.getActive()
    return empresas.some((empresa) => empresa.email.toLowerCase() === email.toLowerCase())
  }
}
