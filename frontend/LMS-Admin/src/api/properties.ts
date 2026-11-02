import { createPropertyService } from '../../../shared/properties/api'
import { api } from './session'

export const propertiesApi = createPropertyService(api)
