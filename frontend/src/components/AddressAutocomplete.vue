<script setup lang="ts">
import { ref, watch } from 'vue'
import { api, type AddressResult } from '@/services/api'
import { MapPin, Loader2, X } from 'lucide-vue-next'
import { useI18n } from '@/i18n'

const { t } = useI18n()

const props = defineProps<{
  modelValue: string
  placeholder?: string
  label?: string
  country?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'select', item: AddressResult): void
}>()

const query = ref(props.modelValue || '')
const results = ref<AddressResult[]>([])
const isLoading = ref(false)
const isOpen = ref(false)
let debounceTimeout: ReturnType<typeof setTimeout> | undefined = undefined

watch(() => props.modelValue, (newVal) => {
  query.value = newVal
})

function onInput(e: Event) {
  const val = (e.target as HTMLInputElement).value
  query.value = val
  emit('update:modelValue', val)

  clearTimeout(debounceTimeout)
  if (val.trim().length < 3) {
    results.value = []
    isOpen.value = false
    return
  }

  isLoading.value = true
  debounceTimeout = setTimeout(async () => {
    try {
      results.value = await api.autocompleteAddress(val, props.country)
      isOpen.value = results.value.length > 0
    } catch {
      results.value = []
    } finally {
      isLoading.value = false
    }
  }, 300)
}

function selectResult(item: AddressResult) {
  query.value = item.label
  emit('update:modelValue', item.label)
  emit('select', item)
  isOpen.value = false
}

function clear() {
  query.value = ''
  emit('update:modelValue', '')
  results.value = []
  isOpen.value = false
}
</script>

<template>
  <div class="relative w-full">
    <label v-if="label" class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">
      {{ label }}
    </label>

    <div class="relative flex items-center">
      <div class="absolute left-3 text-slate-400 pointer-events-none">
        <MapPin class="w-4 h-4" />
      </div>

      <input
        type="text"
        :value="query"
        @input="onInput"
        @focus="isOpen = results.length > 0"
        :placeholder="placeholder || t('address.placeholder')"
        class="w-full bg-white border border-slate-200 rounded-xl pl-9 pr-9 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-600/20 focus:border-blue-600 transition-all shadow-xs"
      />

      <div class="absolute right-3 flex items-center gap-1.5">
        <Loader2 v-if="isLoading" class="w-4 h-4 text-blue-600 animate-spin" />
        <button
          v-else-if="query"
          type="button"
          @click="clear"
          class="text-slate-400 hover:text-slate-600 transition-colors"
        >
          <X class="w-4 h-4" />
        </button>
      </div>
    </div>

    <!-- Dropdown des suggestions (BAN France + Photon OpenStreetMap International) -->
    <div
      v-if="isOpen && results.length > 0"
      class="absolute z-50 left-0 right-0 mt-1 bg-white border border-slate-200 rounded-xl shadow-xl max-h-60 overflow-y-auto divide-y divide-slate-100"
    >
      <button
        v-for="(item, idx) in results"
        :key="idx"
        type="button"
        @click="selectResult(item)"
        class="w-full text-left px-3.5 py-2.5 hover:bg-blue-50/70 transition-colors flex items-start gap-2.5"
      >
        <MapPin class="w-4 h-4 text-blue-600 mt-0.5 shrink-0" />
        <div>
          <div class="text-sm font-medium text-slate-800">{{ item.label }}</div>
          <div class="text-xs text-slate-400">
            {{ item.postcode }} {{ item.city }}<span v-if="item.country"> · {{ item.country }}</span>
          </div>
        </div>
      </button>
    </div>
  </div>
</template>
