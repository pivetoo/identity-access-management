import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { Contract, CreateContractRequest, UpdateContractRequest } from '../types/contract'

export class ContractService {
  private static baseUrl = '/contracts'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Contract>> {
    const response = await httpClient.get<Contract[]>(`${this.baseUrl}/getactive`)
    const contracts = response.data ?? []

    return queryCollection(contracts, params, ['companyName', 'systemApplicationName'])
  }

  static async getById(id: number): Promise<Contract> {
    const contracts = await this.getActive()
    const contract = contracts.find((item) => item.id === id)

    if (!contract) {
      throw new Error(translate('contract.notFound'))
    }

    return contract
  }

  static async getActive(): Promise<Contract[]> {
    const response = await httpClient.get<Contract[]>(`${this.baseUrl}/getactive`)
    return response.data ?? []
  }

  static async getByCompanyId(companyId: number): Promise<Contract[]> {
    const response = await httpClient.get<Contract[]>(`${this.baseUrl}/getbycompanyid/${companyId}`)
    return response.data ?? []
  }

  static async getBySystemApplicationId(systemApplicationId: number): Promise<Contract[]> {
    const response = await httpClient.get<Contract[]>(`${this.baseUrl}/getbysystemapplicationid/${systemApplicationId}`)
    return response.data ?? []
  }

  static async create(contract: CreateContractRequest): Promise<Contract> {
    const response = await httpClient.post<Contract>(`${this.baseUrl}/create`, contract)

    if (!response.data) {
      throw new Error(translate('contract.service.create.emptyResponse'))
    }

    return response.data
  }

  static async update(id: number, contract: UpdateContractRequest): Promise<Contract> {
    const response = await httpClient.put<Contract>(`${this.baseUrl}/update/${id}`, contract)

    if (!response.data) {
      throw new Error(translate('contract.service.update.emptyResponse'))
    }

    return response.data
  }

  static async toggleActive(id: number, contract: Contract): Promise<Contract> {
    return this.update(id, {
      id,
      companyId: contract.companyId,
      systemApplicationId: contract.systemApplicationId,
      startDate: contract.startDate,
      endDate: contract.endDate,
      isActive: !contract.isActive,
    })
  }

}
