export type ContactOption = 'Call Driver' | 'Contact Support' | 'Chat with Operations'

// WINSON-DEVIATION: User-provided default service numbers; deployments may override them without changing code.
export const supportPhoneNumber = import.meta.env.VITE_SUPPORT_PHONE_NUMBER || '+8613700000000'
export const operationsPhoneNumber = import.meta.env.VITE_OPERATIONS_PHONE_NUMBER || '+8613900000000'
// WINSON-DEVIATION: The user requested a temporary demo driver number. Enable it only in development or with an explicit preview flag.
export const mockDriverPhoneNumber = '+12025550100'
export const isMockDriverPhoneEnabled = import.meta.env.DEV || import.meta.env.VITE_USE_MOCK_DRIVER_PHONE === 'true'

export function isDialablePhoneNumber(value: string): boolean {
  return /^\+[1-9]\d{7,14}$/.test(value)
}

export async function resolveContactPhoneNumber(
  option: ContactOption,
  tripId: string,
  getDriverContact: (tripId: string) => Promise<{ data: { phoneNumber: string } }>,
  useMockDriverPhone = isMockDriverPhoneEnabled
): Promise<string> {
  const phoneNumber = option === 'Contact Support'
    ? supportPhoneNumber
    : option === 'Chat with Operations'
      ? operationsPhoneNumber
      : useMockDriverPhone
        ? mockDriverPhoneNumber
        : (await getDriverContact(tripId)).data.phoneNumber

  if (!isDialablePhoneNumber(phoneNumber)) throw new Error('Contact phone number is unavailable')
  return phoneNumber
}
