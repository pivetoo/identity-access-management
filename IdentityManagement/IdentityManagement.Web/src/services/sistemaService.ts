import { httpClient } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from '../types/pagination'
import type { Sistema, CreateSistemaRequest, UpdateSistemaRequest } from '../types/sistema'
import { queryCollection } from './serviceUtils'

interface SystemApplicationApiResponse {
  id: number
  name: string
  description: string
  redirectUris: string
  isActive: boolean
  audience: string
  type: number
  createdAt?: string
  updatedAt?: string
}

function mapSystemApplication(systemApplication: SystemApplicationApiResponse): Sistema {
  return {
    id: systemApplication.id,
    name: systemApplication.name,
    description: systemApplication.description,
    redirectUris: systemApplication.redirectUris,
    isActive: systemApplication.isActive,
    audience: systemApplication.audience,
    criadoEm: systemApplication.createdAt ?? '',
    ultimaAlteracao: systemApplication.updatedAt ?? '',
  }
}

export class SistemaService {
  private static baseUrl = '/systemapplications'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Sistema>> {
    const response = await httpClient.get<SystemApplicationApiResponse[]>(this.baseUrl)
    const sistemas = (response.data ?? []).map(mapSystemApplication)

    return queryCollection(sistemas, params, ['name', 'description', 'audience'])
  }

  static async getById(id: number): Promise<Sistema> {
    const sistemas = await this.getActive()
    const sistema = sistemas.find((item) => item.id === id)

    if (!sistema) {
      throw new Error('Sistema não encontrado.')
    }

    return sistema
  }

  static async getActive(): Promise<Sistema[]> {
    const response = await httpClient.get<SystemApplicationApiResponse[]>(this.baseUrl)
    return (response.data ?? []).map(mapSystemApplication)
  }

  static async create(sistema: CreateSistemaRequest): Promise<Sistema> {
    const response = await httpClient.post<SystemApplicationApiResponse>(this.baseUrl, {
      name: sistema.name,
      description: sistema.description ?? '',
      redirectUris: sistema.redirectUris,
      audience: sistema.audience,
      type: 2,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao criar sistema.')
    }

    return mapSystemApplication(response.data)
  }

  static async update(id: number, sistema: UpdateSistemaRequest): Promise<Sistema> {
    const response = await httpClient.put<SystemApplicationApiResponse>(`${this.baseUrl}/${id}`, {
      id,
      name: sistema.name,
      description: sistema.description ?? '',
      redirectUris: sistema.redirectUris,
      audience: sistema.audience,
      isActive: sistema.isActive,
      type: 2,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar sistema.')
    }

    return mapSystemApplication(response.data)
  }

  static async delete(id: number): Promise<void> {
    const sistema = await this.getById(id)

    await this.update(id, {
      id,
      name: sistema.name,
      description: sistema.description,
      redirectUris: sistema.redirectUris,
      isActive: false,
      audience: sistema.audience,
    })
  }
}
