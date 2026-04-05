export function sortByPriority<T extends { isRoot: boolean; isDefault?: boolean; name: string }>(items: T[]): T[] {
  return [...items].sort((left, right) => {
    if (left.isRoot !== right.isRoot) {
      return left.isRoot ? -1 : 1
    }

    if ((left.isDefault ?? false) !== (right.isDefault ?? false)) {
      return left.isDefault ? -1 : 1
    }

    return left.name.localeCompare(right.name, 'pt-BR', { sensitivity: 'base' })
  })
}

export function sortRolesByPriority<T extends { isRoot: boolean; isDefault?: boolean; name: string }>(roles: T[]): T[] {
  return sortByPriority(roles)
}
