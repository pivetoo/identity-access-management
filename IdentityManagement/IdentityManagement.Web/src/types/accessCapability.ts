// Permissao de produto ("financeiro.aprovar") que agrupa varios endpoints. O catalogo e sincronizado
// pelo proprio sistema consumidor na subida; aqui ele so e lido para montar os templates de perfil.
export interface AccessCapability {
  key: string
  module: string
  moduleLabel: string
  moduleOrder: number
  label: string
  description: string
  order: number
  isBaseline: boolean
  resourceCount: number
}
