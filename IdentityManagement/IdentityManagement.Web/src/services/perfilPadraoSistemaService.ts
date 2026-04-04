import { httpClient } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from '../types/pagination'
import type {
  CreatePerfilPadraoSistemaRequest,
  PerfilPadraoSistema,
  UpdatePerfilPadraoSistemaRequest,
} from '../types/perfilPadraoSistema'
import { queryCollection } from './serviceUtils'

interface SystemRoleTemplateApiResponse {
  id: number
  systemApplicationId: number
  name: string
  description: string
  isRoot: boolean
  isDefault: boolean
  isActive: boolean
  accessResourceIds?: number[]
  createdAt?: string
  updatedAt?: string
}

function mapSystemRoleTemplate(template: SystemRoleTemplateApiResponse): PerfilPadraoSistema {
  return {
    id: template.id,
    systemApplicationId: template.systemApplicationId,
    name: template.name,
    description: template.description,
    isRoot: template.isRoot,
    isDefault: template.isDefault,
    isActive: template.isActive,
    accessResourceIds: template.accessResourceIds ?? [],
    criadoEm: template.createdAt,
    ultimaAlteracao: template.updatedAt,
  }
}

export class PerfilPadraoSistemaService {
  private static baseUrl = '/systemroletemplates'

  static async getBySystemApplicationId(
    systemApplicationId: number,
    params?: PaginationParams
  ): Promise<PaginatedResult<PerfilPadraoSistema>> {
    const response = await httpClient.get<SystemRoleTemplateApiResponse[]>(
      `${this.baseUrl}/system-application/${systemApplicationId}`
    )

    const templates = (response.data ?? []).map(mapSystemRoleTemplate)
    return queryCollection(templates, params, ['name', 'description'])
  }

  static async getById(id: number): Promise<PerfilPadraoSistema> {
    const response = await httpClient.get<SystemRoleTemplateApiResponse>(`${this.baseUrl}/${id}`)

    if (!response.data) {
      throw new Error('Perfil padrão não encontrado.')
    }

    return mapSystemRoleTemplate(response.data)
  }

  static async create(request: CreatePerfilPadraoSistemaRequest): Promise<PerfilPadraoSistema> {
    const response = await httpClient.post<SystemRoleTemplateApiResponse>(this.baseUrl, request)

    if (!response.data) {
      throw new Error('Resposta vazia ao criar perfil padrão.')
    }

    return mapSystemRoleTemplate(response.data)
  }

  static async update(id: number, request: UpdatePerfilPadraoSistemaRequest): Promise<PerfilPadraoSistema> {
    const response = await httpClient.put<SystemRoleTemplateApiResponse>(`${this.baseUrl}/${id}`, request)

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar perfil padrão.')
    }

    return mapSystemRoleTemplate(response.data)
  }
}
