<script setup lang="ts">
defineProps<{ message: string; kind?: 'empty' | 'error' }>()
</script>
<template><view class="state"><text class="symbol">{{ kind === 'error' ? '!' : '◇' }}</text><text class="message">{{ message }}</text><slot /></view></template>
<style scoped>.state { display:flex; align-items:center; flex-direction:column; gap:12px; padding:36px 20px; border:1px dashed #d7dbe5; border-radius:18px; background:#fff; color:#7b8497; text-align:center; }.symbol { display:flex; align-items:center; justify-content:center; width:48px; height:48px; border-radius:50%; background:#f1f2fa; color:#5964a1; font-size:27px; }.message { font-size:13px; line-height:1.6; word-break:break-word; }</style>
