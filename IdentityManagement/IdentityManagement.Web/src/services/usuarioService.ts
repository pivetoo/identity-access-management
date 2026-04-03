import { httpClient } from 'd-rts'
import type { PaginationParams, PaginatedResult } from 'd-rts'
import type { Usuario, CreateUsuarioRequest, UpdateUsuarioRequest } from '../types/usuario'
import { queryCollection } from './serviceUtils'

interface UserApiResponse {
  id: number
  username: string
  email: string
  name: string
  avatarUrl?: string
  isActive: boolean
  lastLoginAt?: string
  createdAt?: string
  updatedAt?: string
}

function mapUser(user: UserApiResponse): Usuario {
  return {
    id: user.id,
    username: user.username,
    email: user.email,
    name: user.name,
    isActive: user.isActive,
    lastLoginAt: user.lastLoginAt,
    createdAt: user.createdAt ?? '',
    updatedAt: user.updatedAt,
  }
}

export class UsuarioService {
  private static baseUrl = '/users'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<Usuario>> {
    const response = await httpClient.get<UserApiResponse[]>(this.baseUrl)
    const usuarios = (response.data ?? []).map(mapUser)

    return queryCollection(usuarios, params, ['username', 'email', 'name'])
  }

  static async getById(id: number): Promise<Usuario> {
    const response = await httpClient.get<UserApiResponse>(`${this.baseUrl}/${id}`)

    if (!response.data) {
      throw new Error('Usuário não encontrado.')
    }

    return mapUser(response.data)
  }

  static async getActive(): Promise<Usuario[]> {
    const response = await httpClient.get<UserApiResponse[]>(this.baseUrl)
    return (response.data ?? []).map(mapUser)
  }

  static async create(usuario: CreateUsuarioRequest): Promise<Usuario> {
    const response = await httpClient.post<UserApiResponse>(this.baseUrl, {
      username: usuario.username,
      email: usuario.email,
      password: usuario.password,
      name: usuario.name,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao criar usuário.')
    }

    return mapUser(response.data)
  }

  static async update(id: number, usuario: UpdateUsuarioRequest): Promise<Usuario> {
    const response = await httpClient.put<UserApiResponse>(`${this.baseUrl}/${id}`, {
      id,
      username: usuario.username,
      email: usuario.email,
      password: usuario.password,
      name: usuario.name,
      isActive: usuario.isActive,
    })

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar usuário.')
    }

    return mapUser(response.data)
  }

  static async delete(id: number): Promise<void> {
    const usuario = await this.getById(id)

    await this.update(id, {
      id,
      username: usuario.username,
      email: usuario.email,
      name: usuario.name,
      isActive: false,
    })
  }
}
