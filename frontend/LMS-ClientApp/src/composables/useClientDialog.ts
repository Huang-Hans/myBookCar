import { ref } from 'vue'

interface DialogState {
  title: string
  message: string
  confirmLabel: string
  secondaryLabel?: string
  onConfirm?: () => void | Promise<void>
}

export function useClientDialog() {
  const dialog = ref<DialogState | null>(null)

  function alert(title: string, message: string, confirmLabel: string): void {
    dialog.value = { title, message, confirmLabel }
  }

  function confirm(title: string, message: string, confirmLabel: string, secondaryLabel: string, onConfirm: () => void | Promise<void>): void {
    dialog.value = { title, message, confirmLabel, secondaryLabel, onConfirm }
  }

  function close(): void { dialog.value = null }

  async function submit(): Promise<void> {
    const action = dialog.value?.onConfirm
    close()
    await action?.()
  }

  return { dialog, alert, confirm, close, submit }
}
