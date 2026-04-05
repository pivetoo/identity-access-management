import { httpClient, queryCollection } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { User, CreateUserRequest, UpdateUserRequest } from '../types/user'

export class UserService {
  private static baseUrl = '/users'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<User>> {
    const response = await httpClient.get<User[]>(this.baseUrl)
    const usuarios = response.data ?? []

    return queryCollection(usuarios, params, ['username', 'email', 'name'])
  }

  static async getById(id: number): Promise<User> {
    const response = await httpClient.get<User>(`${this.baseUrl}/${id}`)

    if (!response.data) {
      throw new Error('Usuário não encontrado.')
    }

    return response.data
  }

  static async getActive(): Promise<User[]> {
    const response = await httpClient.get<User[]>(this.baseUrl)
    return response.data ?? []
  }

  static async create(usuario: CreateUserRequest): Promise<User> {
    const response = await httpClient.post<User>(this.baseUrl, usuario)

    if (!response.data) {
      throw new Error('Resposta vazia ao criar usuário.')
    }

    return response.data
  }

  static async update(id: number, usuario: UpdateUserRequest): Promise<User> {
    const response = await httpClient.put<User>(`${this.baseUrl}/${id}`, usuario)

    if (!response.data) {
      throw new Error('Resposta vazia ao atualizar usuário.')
    }

    return response.data
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
