<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { api, type ExpenseReportSummary, ReceiptCategory } from '@/services/api'
import { Camera, CameraResultType, CameraSource } from '@capacitor/camera'
import { Capacitor } from '@capacitor/core'
import { Camera as CameraIcon, Check, X } from 'lucide-vue-next'

const router = useRouter()

const reports = ref<ExpenseReportSummary[]>([])
const selectedReportId = ref<number | null>(null)
const receiptDate = ref(new Date().toISOString().slice(0, 10))
const category = ref<ReceiptCategory>(ReceiptCategory.Toll)
const merchant = ref('')
const amountTtc = ref<number>(24.50)
const vatRate = ref<number>(20.0)
const notes = ref('')

const photoPreview = ref<string | null>(null)
const photoFile = ref<File | null>(null)
const isSubmitting = ref(false)

const categories = [
  { id: ReceiptCategory.Toll, label: 'Péage', icon: '🛣️' },
  { id: ReceiptCategory.Parking, label: 'Stationnement', icon: '🅿️' },
  { id: ReceiptCategory.Meal, label: 'Repas', icon: '🍽️' },
  { id: ReceiptCategory.Fuel, label: 'Carburant', icon: '⛽' },
  { id: ReceiptCategory.Hotel, label: 'Hôtel', icon: '🏨' },
  { id: ReceiptCategory.Other, label: 'Autre', icon: '🧾' },
]

onMounted(async () => {
  try {
    const data = await api.getReports()
    reports.value = data
    if (data.length > 0) selectedReportId.value = data[0].id
  } catch (err) {
    console.error(err)
  }
})

// Prise de photo via Capacitor Camera (ou fallback Web)
async function takePhoto() {
  if (Capacitor.isNativePlatform()) {
    try {
      const image = await Camera.getPhoto({
        quality: 85,
        allowEditing: false,
        resultType: CameraResultType.Uri,
        source: CameraSource.Camera
      })

      if (image.webPath) {
        photoPreview.value = image.webPath
        const response = await fetch(image.webPath)
        const blob = await response.blob()
        photoFile.value = new File([blob], `receipt_${Date.now()}.jpg`, { type: 'image/jpeg' })
      }
      return
    } catch {
      // Annulé par l'utilisateur
      return
    }
  }

  // Fallback Web : trigger file input
  const input = document.getElementById('web-file-input') as HTMLInputElement
  input?.click()
}

function onFileSelected(e: Event) {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (file) {
    photoFile.value = file
    photoPreview.value = URL.createObjectURL(file)
  }
}

function clearPhoto() {
  photoPreview.value = null
  photoFile.value = null
}

async function submitReceipt() {
  if (!selectedReportId.value) {
    alert('Sélectionnez une note de frais')
    return
  }

  isSubmitting.value = true
  try {
    const created = await api.addReceipt(selectedReportId.value, {
      date: new Date(receiptDate.value).toISOString(),
      category: category.value,
      merchant: merchant.value,
      amountTtc: amountTtc.value,
      vatRate: vatRate.value,
      vatAmount: vatRate.value ? Math.round((amountTtc.value * (vatRate.value / (100 + vatRate.value))) * 100) / 100 : null,
      notes: notes.value
    })

    // Si une photo a été prise, on l'uploade
    if (photoFile.value && created.id) {
      await api.uploadReceiptImage(selectedReportId.value, created.id, photoFile.value)
    }

    router.push(`/reports/${selectedReportId.value}`)
  } catch (err) {
    alert('Erreur lors de l’enregistrement du justificatif')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="max-w-xl mx-auto px-4 pt-6 pb-28">
    <div class="mb-5">
      <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Ajouter un justificatif</h1>
      <p class="text-xs text-slate-500 mt-0.5">Péages, parkings, repas et frais professionnels</p>
    </div>

    <form @submit.prevent="submitReceipt" class="space-y-4">
      <!-- Sélection Note de frais -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">
          Rattacher à la Note de frais
        </label>
        <select
          v-model="selectedReportId"
          required
          class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2.5 text-sm text-slate-900 font-medium outline-none"
        >
          <option v-for="r in reports" :key="r.id" :value="r.id">
            {{ r.title }} ({{ r.totalReimbursableAmount.toFixed(2) }} €)
          </option>
        </select>
      </div>

      <!-- Photo du Justificatif (Scan mobile ou upload web) -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-2">
          Photo du ticket ou reçu
        </label>

        <input
          id="web-file-input"
          type="file"
          accept="image/*"
          capture="environment"
          class="hidden"
          @change="onFileSelected"
        />

        <div v-if="photoPreview" class="relative rounded-xl overflow-hidden border border-slate-200 bg-slate-100">
          <img :src="photoPreview" alt="Reçu" class="w-full h-48 object-cover" />
          <button
            type="button"
            @click="clearPhoto"
            class="absolute top-2 right-2 p-1.5 bg-slate-900/70 hover:bg-slate-900 text-white rounded-full transition-colors"
          >
            <X class="w-4 h-4" />
          </button>
        </div>

        <button
          v-else
          type="button"
          @click="takePhoto"
          class="w-full border-2 border-dashed border-slate-300 hover:border-blue-500 hover:bg-blue-50/40 rounded-xl p-6 flex flex-col items-center justify-center transition-all group"
        >
          <div class="w-12 h-12 rounded-full bg-blue-50 group-hover:bg-blue-100 text-blue-600 flex items-center justify-center mb-2 transition-colors">
            <CameraIcon class="w-6 h-6" />
          </div>
          <span class="text-xs font-bold text-slate-800">Prendre en photo le reçu</span>
          <span class="text-[11px] text-slate-400 mt-0.5">Appareil photo ou fichier justificatif</span>
        </button>
      </div>

      <!-- Catégories de frais -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-2">
          Catégorie de dépense
        </label>
        <div class="grid grid-cols-3 gap-2">
          <button
            v-for="cat in categories"
            :key="cat.id"
            type="button"
            @click="category = cat.id"
            class="p-2.5 rounded-xl border text-center transition-all"
            :class="category === cat.id ? 'border-blue-600 bg-blue-50 text-blue-700 font-bold shadow-xs' : 'border-slate-200 hover:bg-slate-50 text-slate-700'"
          >
            <div class="text-base mb-0.5">{{ cat.icon }}</div>
            <div class="text-xs">{{ cat.label }}</div>
          </button>
        </div>
      </div>

      <!-- Détails Commerçant & Montants -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-3">
        <div>
          <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1">
            Commerçant / Fournisseur
          </label>
          <input
            v-model="merchant"
            type="text"
            required
            placeholder="Ex: APRR, Indigo, Total, Bistrot du coin..."
            class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
          />
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1">
              Date de dépense
            </label>
            <input
              v-model="receiptDate"
              type="date"
              required
              class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1">
              Montant TTC (€)
            </label>
            <div class="relative">
              <input
                v-model.number="amountTtc"
                type="number"
                step="0.01"
                min="0.1"
                required
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold text-slate-900 outline-none"
              />
              <span class="absolute right-3 top-2 text-xs text-slate-400 font-bold">€</span>
            </div>
          </div>
        </div>

        <div>
          <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1">
            Taux de TVA
          </label>
          <div class="flex gap-2">
            <button
              v-for="rate in [20, 10, 5.5, 0]"
              :key="rate"
              type="button"
              @click="vatRate = rate"
              class="flex-1 py-1.5 rounded-lg border text-xs font-semibold transition-all"
              :class="vatRate === rate ? 'bg-blue-50 text-blue-700 border-blue-200' : 'bg-slate-50 text-slate-600 border-slate-200'"
            >
              {{ rate }}%
            </button>
          </div>
        </div>
      </div>

      <button
        type="submit"
        :disabled="isSubmitting"
        class="w-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 text-white font-bold py-3 px-4 rounded-xl shadow-md shadow-blue-500/20 transition-all flex items-center justify-center gap-2"
      >
        <Check class="w-5 h-5" />
        {{ isSubmitting ? 'Enregistrement...' : 'Ajouter le reçu' }}
      </button>
    </form>
  </div>
</template>
