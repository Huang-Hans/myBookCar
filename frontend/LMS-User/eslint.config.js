import tsParser from '@typescript-eslint/parser'
import tsPlugin from '@typescript-eslint/eslint-plugin'
import vueParser from 'vue-eslint-parser'

const namingConvention = [
  'error',
  { selector: 'typeLike', format: ['PascalCase'] },
  { selector: 'property', format: ['camelCase'], filter: { regex: '^(Content-Type|Retry-After)$', match: false } },
]

export default [
  {
    files: ['src/**/*.ts'],
    languageOptions: { parser: tsParser, parserOptions: { ecmaVersion: 'latest', sourceType: 'module' } },
    plugins: { '@typescript-eslint': tsPlugin },
    rules: { '@typescript-eslint/naming-convention': namingConvention },
  },
  {
    files: ['src/**/*.vue'],
    languageOptions: { parser: vueParser, parserOptions: { parser: tsParser, ecmaVersion: 'latest', sourceType: 'module' } },
    plugins: { '@typescript-eslint': tsPlugin },
    rules: { '@typescript-eslint/naming-convention': namingConvention },
  },
]
