import { httpClient, queryCollection, translate } from 'archon-ui'
import type { PaginationParams, PaginatedResult } from 'archon-ui'
import type { User, CreateUserRequest, UpdateUserRequest } from '../types/user'

export class UserService {
  private static baseUrl = '/users'

  static async getAll(params?: PaginationParams): Promise<PaginatedResult<User>> {
    const response = await httpClient.get<User[]>(`${this.baseUrl}/getactive`)
    const usuarios = response.data ?? []

    return queryCollection(usuarios, params, ['username', 'email', 'name'])
  }

  static async getById(id: number): Promise<User> {
    const response = await httpClient.get<User>(`${this.baseUrl}/getbyid/${id}`)

    if (!response.data) {
      throw new Error(translate('user.notFound'))
    }

    return response.data
  }

  static async getActive(): Promise<User[]> {
    const response = await httpClient.get<User[]>(`${this.baseUrl}/getactive`)
    return response.data ?? []
  }

  static async create(usuario: CreateUserRequest): Promise<User> {
    const response = await httpClient.post<User>(`${this.baseUrl}/create`, usuario)

    if (!response.data) {
      throw new Error(translate('user.service.create.emptyResponse'))
    }

    return response.data
  }

  static async update(id: number, usuario: UpdateUserRequest): Promise<User> {
    const response = await httpClient.put<User>(`${this.baseUrl}/update/${id}`, usuario)

    if (!response.data) {
      throw new Error(translate('user.service.update.emptyResponse'))
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
