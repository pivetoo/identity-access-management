import { httpClient } from 'archon-ui'
import type { Contrato, CreateContratoRequest, UpdateContratoRequest, ContratoSecrets } from '../types/contrato'
import type { PaginationParams, PaginatedResult } from '../types/pagination'
import { queryCollection } from './serviceUtils'

interface ContractApiResponse {
  id: number
  companyId: number
  systemApplicationId?: number
  companyName: string
  systemApplicationName: string
  startDate: string
  endDate?: string
  isActive: boolean
  clientId?: string
  accessTokenLifetime: number
  refreshTokenLifetime: number
  createdAt?: string
  updatedAt?: string
  isValid?: boolean
}

function mapContract(contract: ContractApiResponse): Contrato {
  return {
    id: contract.id,
    empresa: {
      id: contract.companyId,
      nome: contract.companyName,
    },
    empresaName: contract.companyName,
    sistema: {
      id: contract.systemApplicationId ?? 0,
      name: contract.systemApplicationName,
    },
    sistemaName: contract.systemApplicationName,
    startDate: contract.startDate,
    endDate: contract.endDate ?? '',
    isActive: contract.isActive,
    isValid: contract.isValid,
    clientId: contract.clientId,
    accessTokenLifetime: contract.accessTokenLifetime,
    refreshTokenLifetime: contract.refreshTokenLifetime,
    criadoEm: contract.createdAt,
    ultimaAlteracao: contract.updatedAt,
  }
}

export class ContratoService {
  private static baseUrl = '/contracts'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Contrato>> {
    const response = await httpClient.get<ContractApiResponse[]>(this.baseUrl)
    const contratos = (response.data ?? []).map(mapContract)

    return queryCollection(contratos, params, ['empresaName', 'sistemaName', 'clientId'])
  }

  static async getById(id: number): Promise<Contrato> {
    const contratos = await this.getActive()
    const contrato = contratos.find((item) => item.id === id)

    if (!contrato) {
      throw new Error('Contrato não encontrado.')
    }

    return contrato
  }

  static async getActive(): Promise<Contrato[]> {
    const response = await httpClient.get<ContractApiResponse[]>(this.baseUrl)
    return (response.data ?? []).map(mapContract)
  }

  static async getByEmpresaId(empresaId: number): Promise<Contrato[]> {
    const response = await httpClient.get<ContractApiResponse[]>(`${this.baseUrl}/company/${empresaId}`)
    return (response.data ?? []).map(mapContract)
  }

  static async getBySistemaId(sistemaId: number): Promise<Contrato[]> {
    const response = await httpClient.get<ContractApiResponse[]>(`${this.baseUrl}/system-application/${sistemaId}`)
    return (response.data ?? []).map(mapContract)
  }

  static async create(contrato: CreateContratoRequest): Promise<Contrato> {
    const response = await httpClient.post<ContractApiResponse>(this.baseUrl, {
      companyId: contrato.empresaId,
      systemApplicationId: contrato.sistemaId,
      startDate: contrato.startDate,
      endDate: contrato.endDate,
      accessTokenLifetime: contrato.accessTokenLifetime ?? 60,
      refreshTokenLifetime: contrato.refreshTokenLifetime ?? 1440,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao criar contrato.')
    }

    return mapContract(response.data)
  }

  static async update(id: number, contrato: UpdateContratoRequest): Promise<Contrato> {
    const response = await httpClient.put<ContractApiResponse>(`${this.baseUrl}/${id}`, {
      id,
      companyId: contrato.empresaId,
      systemApplicationId: contrato.sistemaId,
      startDate: contrato.startDate,
      endDate: contrato.endDate,
      isActive: contrato.isActive,
      accessTokenLifetime: contrato.accessTokenLifetime,
      refreshTokenLifetime: contrato.refreshTokenLifetime,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar contrato.')
    }

    return mapContract(response.data)
  }

  static async toggleActive(id: number, contrato: Contrato): Promise<Contrato> {
    return this.update(id, {
      id,
      empresaId: contrato.empresa?.id ?? 0,
      sistemaId: contrato.sistema?.id ?? 0,
      startDate: contrato.startDate,
      endDate: contrato.endDate,
      isActive: !contrato.isActive,
      accessTokenLifetime: contrato.accessTokenLifetime,
      refreshTokenLifetime: contrato.refreshTokenLifetime,
    })
  }

  static async getSecrets(id: number): Promise<ContratoSecrets> {
    const response = await httpClient.get<ContratoSecrets>(`${this.baseUrl}/${id}/secrets`)

    if (!response.data) {
      throw new Error('Contrato não encontrado.')
    }

    return response.data
  }
}
