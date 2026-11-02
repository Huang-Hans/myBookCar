import 'vue'

declare module 'vue' {
  interface ComponentCustomProperties {
    $t: (source: string) => string
  }
}
