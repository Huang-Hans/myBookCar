import { describe, expect, it } from 'vitest'
import specJson from '../../openapi/lms-all-services.json'
import { portalField, portalSourcePath, portalSources } from './portalSources'

interface Schema { $ref?: string; type?: string; items?: Schema; properties?: Record<string, Schema> }
interface Operation { responses?: Record<string, { content?: Record<string, { schema?: Schema }> }> }
const spec = specJson as unknown as { paths: Record<string, { get?: Operation }>; components: { schemas: Record<string, Schema> } }
function resolve(schema: Schema): Schema { return schema.$ref ? resolve(spec.components.schemas[schema.$ref.split('/').at(-1) ?? ''] ?? {}) : schema }

describe('Portal menu API contract', () => {
  it('maps every live Portal tab to a documented GET path and response fields', () => {
    for (const [section, group] of Object.entries(portalSources)) {
      expect(group.tabs[group.defaultTab], section).toBeDefined()
      for (const [tab, source] of Object.entries(group.tabs)) {
        const operation = spec.paths[source.path]?.get
        expect(operation, `${section}/${tab}: ${source.path}`).toBeDefined()
        const envelope = resolve(operation?.responses?.['200']?.content?.['application/json']?.schema ?? {})
        const data = resolve(envelope.properties?.data ?? {})
        const item = resolve(data.type === 'array' ? data.items ?? {} : data)
        for (const path of [...source.fields, ...(source.idField ? [source.idField] : [])]) {
          let current = item
          for (const part of path.split('.')) {
            const next: Schema | undefined = resolve(current).properties?.[part]
            expect(next, `${section}/${tab}: response data.${path}`).toBeDefined()
            current = resolve(next ?? {})
          }
        }
        if (source.hasDetails) expect(Object.keys(spec.paths).some((path) => path.startsWith(`${source.path}/{`) && spec.paths[path]?.get), `${section}/${tab}: detail GET`).toBe(true)
      }
    }
  })

  it('builds only supported pagination, search and fixed source query parameters', () => {
    expect(portalSourcePath(portalSources.blacklist!.tabs.drivers!, 2, 20, 'Macau')).toBe('/api/dispatch/staff/v1/drivers?view=BLACKLIST&page=2&pageSize=20&search=Macau')
    expect(portalSourcePath(portalSources.travel!.tabs.navigation!, 1, 10, 'ignored')).toBe('/api/platform/staff/v1/integration-references?group=NAVIGATION')
    expect(portalField({ guest: { name: 'Example' } }, 'guest.name')).toBe('Example')
  })
})
