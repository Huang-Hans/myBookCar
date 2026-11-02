<script setup lang="ts">
import { computed, ref } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { ApiRequestError, apiErrorMessage } from '../../api/http'
import { lms, type GuestProfileView, type PropertyView, type UpdateGuestRequest } from '../../api/lms'
import { currentUser } from '../../api/session'

const props = defineProps<{ member: GuestProfileView; properties: PropertyView[] }>()
const emit = defineEmits<{ close: []; saved: [] }>()
const { t } = useLocale()
const tiers = ['Silver', 'Gold', 'Platinum', 'Diamond', 'VIP'] as const
const previous = { name: props.member.name, tier: props.member.tier ?? '', roomNumber: props.member.roomNumber ?? '', propertyId: props.member.propertyId ?? '' }
const draft = ref({ ...previous })
const isSaving = ref(false)
const formError = ref('')
const fieldErrors = ref<Record<string, string>>({})
const canClearProperty = computed(() => currentUser.value?.hasAllPropertyAccess ?? false)
const changes = computed<UpdateGuestRequest>(() => {
  const result: UpdateGuestRequest = {}
  if (draft.value.name.trim() !== previous.name) result.name = draft.value.name.trim()
  if (draft.value.tier !== previous.tier) result.tier = draft.value.tier as UpdateGuestRequest['tier']
  if (draft.value.roomNumber.trim() !== previous.roomNumber) result.roomNumber = draft.value.roomNumber.trim() || null
  if (draft.value.propertyId !== previous.propertyId) result.propertyId = draft.value.propertyId || null
  return result
})
const hasChanges = computed(() => Object.keys(changes.value).length > 0)

function validate(): boolean {
  fieldErrors.value = {}
  if (!draft.value.name.trim()) fieldErrors.value.name = 'Enter the member name.'
  if (!tiers.some(tier => tier.toUpperCase() === draft.value.tier)) fieldErrors.value.tier = 'Select a member tier.'
  return Object.keys(fieldErrors.value).length === 0
}
async function save(): Promise<void> {
  if (isSaving.value || !hasChanges.value || !validate()) return
  isSaving.value = true
  formError.value = ''
  try { await lms.updateGuest(props.member.guestId, changes.value); emit('saved') }
  catch (cause) {
    formError.value = apiErrorMessage(cause)
    if (cause instanceof ApiRequestError) fieldErrors.value = Object.fromEntries(cause.errors.map(issue => [issue.field, issue.message]))
  }
  finally { isSaving.value = false }
}
function close(): void { if (!isSaving.value) emit('close') }
</script>

<template>
  <div class="modal-backdrop" @click.self="close" @keydown.esc="close">
    <form class="dialog member-edit-dialog" role="dialog" aria-modal="true" :aria-label="t('Edit member')" @submit.prevent="save">
      <header class="dialog-header"><h2>{{ t('Edit member') }}</h2><button type="button" class="icon-button" :disabled="isSaving" :aria-label="t('Close')" @click="close"><X :size="18" /></button></header>
      <div class="dialog-body">
        <div class="member-edit-identity"><span>{{ t('Member ID') }}<strong>{{ member.memberId || '—' }}</strong></span><span>{{ t('Phone') }}<strong>{{ member.phoneMasked || '—' }}</strong></span></div>
        <fieldset class="member-edit-fields" :disabled="isSaving">
          <label class="field-label">{{ t('Name') }}<input v-model="draft.name" required maxlength="100" :aria-invalid="Boolean(fieldErrors.name)" /><small v-if="fieldErrors.name" class="form-error">{{ t(fieldErrors.name) }}</small></label>
          <label class="field-label">{{ t('TIER') }}<select v-model="draft.tier" required :aria-invalid="Boolean(fieldErrors.tier)"><option v-for="tier in tiers" :key="tier" :value="tier.toUpperCase()">{{ t(tier) }}</option></select><small v-if="fieldErrors.tier" class="form-error">{{ t(fieldErrors.tier) }}</small></label>
          <label class="field-label">{{ t('Property') }}<select v-model="draft.propertyId" :aria-invalid="Boolean(fieldErrors.propertyId)"><option v-if="canClearProperty || !previous.propertyId" value="" :disabled="!canClearProperty">{{ t('No property') }}</option><option v-if="previous.propertyId && !properties.some(item => item.propertyId === previous.propertyId)" :value="previous.propertyId">{{ previous.propertyId }}</option><option v-for="property in properties" :key="property.propertyId" :value="property.propertyId">{{ property.name }}</option></select><small v-if="fieldErrors.propertyId" class="form-error">{{ t(fieldErrors.propertyId) }}</small></label>
          <label class="field-label">{{ t('Room') }}<input v-model="draft.roomNumber" maxlength="50" :aria-invalid="Boolean(fieldErrors.roomNumber)" /><small v-if="fieldErrors.roomNumber" class="form-error">{{ t(fieldErrors.roomNumber) }}</small></label>
        </fieldset>
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
      </div>
      <footer class="dialog-footer"><button type="button" class="secondary-button" :disabled="isSaving" @click="close">{{ t('Cancel') }}</button><button type="submit" class="primary-button" :disabled="isSaving || !hasChanges">{{ t(isSaving ? 'Saving' : 'Save') }}</button></footer>
    </form>
  </div>
</template>

<style scoped>
.member-edit-dialog { max-width: 640px; }
.member-edit-identity { display: flex; flex-wrap: wrap; gap: 16px 40px; padding-bottom: 16px; margin-bottom: 16px; border-bottom: 1px solid var(--line, #e4e7ef); }
.member-edit-identity span { display: grid; gap: 4px; color: var(--muted, #73809c); font-size: 12px; }
.member-edit-identity strong { color: var(--ink, #142343); font-weight: 500; }
.member-edit-fields { border: 0; padding: 0; margin: 0; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
@media (max-width: 640px) { .member-edit-fields { grid-template-columns: 1fr; } }
</style>
