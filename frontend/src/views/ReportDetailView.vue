<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, type ExpenseReportDetail, ReceiptCategory, formatCurrencySymbol } from '@/services/api'
import { ArrowLeft, Download, Trash2, MapPin, Receipt, Car, Plus, Eye } from 'lucide-vue-next'
import { Share } from '@capacitor/share'
import { Capacitor } from '@capacitor/core'

const route = useRoute()
const router = useRouter()
const reportId = Number(route.params.id)

const report = ref<ExpenseReportDetail | null>(null)
const isLoading = ref(true)
const activeTab = ref<'trips' | 'receipts'>('trips')
const selectedPreviewImage = ref<string | null>(null)

const currencySymbol = computed(() => formatCurrencySymbol(report.value?.currency))

async function loadReport() {
  isLoading.value = true
  try {
    report.value = await api.getReport(reportId)
  } catch (err) {
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

onMounted(loadReport)

async function deleteTrip(tripId: number) {
  if (!confirm('Supprimer ce trajet ?')) return
  await api.deleteTrip(reportId, tripId)
  await loadReport()
}

async function deleteReceipt(receiptId: number) {
  if (!confirm('Supprimer ce reçu ?')) return
  await api.deleteReceipt(reportId, receiptId)
  await loadReport()
}

async function shareOrDownloadPdf() {
  if (!report.value) return
  const pdfUrl = api.getPdfDownloadUrl(report.value.id)

  if (Capacitor.isNativePlatform()) {
    try {
      await Share.share({
        title: `Note de frais - ${report.value.title}`,
        text: `Voici le document récapitulatif pour la période.`,
        url: window.location.origin + pdfUrl,
        dialogTitle: 'Partager le PDF'
      })
      return
    } catch {
      // Fallback
    }
  }

  window.open(pdfUrl, '_blank')
}

function getCategoryLabel(cat: ReceiptCategory) {
  switch (cat) {
    case ReceiptCategory.Toll: return 'Péage'
    case ReceiptCategory.Parking: return 'Stationnement'
    case ReceiptCategory.Meal: return 'Repas'
    case ReceiptCategory.Fuel: return 'Carburant'
    case ReceiptCategory.Hotel: return 'Hôtel'
    default: return 'Autre'
  }
}
</script>

<template>
  <div class="max-w-xl mx-auto px-4 pt-6 pb-28">
    <!-- En-tête de navigation -->
    <div class="flex items-center justify-between mb-4">
      <button
        @click="router.push('/')"
        class="inline-flex items-center gap-1 text-xs font-semibold text-slate-600 hover:text-slate-900 bg-white border border-slate-200 px-3 py-1.5 rounded-xl shadow-xs transition-colors"
      >
        <ArrowLeft class="w-4 h-4" />
        Retour
      </button>

      <button
        @click="shareOrDownloadPdf"
        class="inline-flex items-center gap-1.5 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 px-3.5 py-1.5 rounded-xl shadow-xs transition-all"
      >
        <Download class="w-4 h-4" />
        Exporter PDF
      </button>
    </div>

    <div v-if="isLoading" class="space-y-4">
      <div class="h-32 bg-slate-200/60 rounded-2xl animate-pulse"></div>
      <div class="h-64 bg-slate-200/60 rounded-2xl animate-pulse"></div>
    </div>

    <div v-else-if="report" class="space-y-4">
      <!-- Carte Récapitulative -->
      <div class="bg-white border border-slate-200 rounded-2xl p-5 shadow-xs">
        <div class="flex items-start justify-between mb-2">
          <div>
            <span class="text-[11px] font-mono text-slate-400">NDF-{{ String(report.id).padStart(5, '0') }}</span>
            <h1 class="text-xl font-black text-slate-900">{{ report.title }}</h1>
            <p class="text-xs text-slate-500 mt-0.5">
              {{ report.employeeName }} · {{ report.companyName }}
            </p>
          </div>
          <div class="text-right">
            <span class="text-xs font-semibold text-slate-500">Net à rembourser</span>
            <div class="text-2xl font-black text-blue-600">
              {{ report.totalReimbursableAmount.toFixed(2) }} {{ currencySymbol }}
            </div>
            <div v-if="report.reimbursementRatePercentage < 100" class="text-[10px] text-amber-600 font-bold">
              Prise en charge {{ report.reimbursementRatePercentage }}%
            </div>
          </div>
        </div>

        <div class="grid grid-cols-3 gap-2 mt-4 pt-3 border-t border-slate-100 text-center">
          <div class="bg-slate-50 rounded-xl p-2">
            <span class="text-[10px] text-slate-400 font-semibold block uppercase">Distance</span>
            <span class="text-sm font-bold text-slate-800">{{ report.totalDistanceKm.toFixed(0) }} km</span>
          </div>
          <div class="bg-slate-50 rounded-xl p-2">
            <span class="text-[10px] text-slate-400 font-semibold block uppercase">Total IK</span>
            <span class="text-sm font-bold text-slate-800">{{ report.totalMileageAmount.toFixed(2) }} {{ currencySymbol }}</span>
          </div>
          <div class="bg-slate-50 rounded-xl p-2">
            <span class="text-[10px] text-slate-400 font-semibold block uppercase">Frais Réels</span>
            <span class="text-sm font-bold text-slate-800">{{ report.totalReceiptsAmount.toFixed(2) }} {{ currencySymbol }}</span>
          </div>
        </div>
      </div>

      <!-- Onglets Trajets / Justificatifs -->
      <div class="flex bg-slate-200/70 p-1 rounded-xl">
        <button
          @click="activeTab = 'trips'"
          class="flex-1 py-2 text-xs font-bold rounded-lg transition-all"
          :class="activeTab === 'trips' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600 hover:text-slate-900'"
        >
          Trajets & IK ({{ report.trips.length }})
        </button>
        <button
          @click="activeTab = 'receipts'"
          class="flex-1 py-2 text-xs font-bold rounded-lg transition-all"
          :class="activeTab === 'receipts' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-600 hover:text-slate-900'"
        >
          Justificatifs & Reçus ({{ report.receipts.length }})
        </button>
      </div>

      <!-- Contenu Onglet 1 : Trajets -->
      <div v-if="activeTab === 'trips'" class="space-y-3">
        <div v-if="report.trips.length === 0" class="text-center py-8 bg-white border border-slate-200 rounded-2xl p-6">
          <Car class="w-8 h-8 text-slate-300 mx-auto mb-2" />
          <p class="text-xs text-slate-500 mb-3">Aucun trajet enregistré dans cette note.</p>
          <router-link
            to="/add-trip"
            class="inline-flex items-center gap-1 text-xs font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 px-3 py-1.5 rounded-xl transition-colors"
          >
            <Plus class="w-3.5 h-3.5" />
            Ajouter un trajet
          </router-link>
        </div>

        <div
          v-for="trip in report.trips"
          :key="trip.id"
          class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs"
        >
          <div class="flex items-start justify-between gap-2 mb-1.5">
            <div>
              <div class="text-xs text-slate-400 font-medium">
                {{ new Date(trip.date).toLocaleDateString('fr-FR') }} ·
                <span class="text-slate-600 font-semibold">{{ trip.clientOrLocation || 'Client' }}</span>
              </div>
              <h3 class="text-sm font-bold text-slate-900">{{ trip.purpose }}</h3>
            </div>
            <div class="text-right">
              <span class="text-base font-black text-blue-600">{{ trip.calculatedAmount.toFixed(2) }} {{ currencySymbol }}</span>
              <div class="text-[11px] text-slate-400">{{ trip.totalDistanceKm.toFixed(0) }} km</div>
            </div>
          </div>

          <div class="text-xs text-slate-600 mt-2 bg-slate-50 rounded-xl p-2.5 flex items-center justify-between">
            <div class="flex items-center gap-1.5 truncate mr-2">
              <MapPin class="w-3.5 h-3.5 text-blue-600 shrink-0" />
              <span class="truncate">{{ trip.departureAddress }} ➔ {{ trip.arrivalAddress }}</span>
            </div>
            <span class="text-[10px] font-semibold text-slate-400 shrink-0">
              {{ trip.isRoundTrip ? 'A/R' : 'Aller simple' }}
            </span>
          </div>

          <div class="mt-2.5 flex items-center justify-between pt-2 border-t border-slate-100">
            <span class="text-[11px] text-slate-400">
              {{ trip.vehicle?.name || 'Véhicule' }} ({{ trip.vehicle?.fiscalHorsepower || 5 }} CV)
            </span>
            <button
              @click="deleteTrip(trip.id)"
              class="text-slate-400 hover:text-rose-600 p-1 transition-colors"
              title="Supprimer"
            >
              <Trash2 class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>

      <!-- Contenu Onglet 2 : Reçus & Frais réels -->
      <div v-if="activeTab === 'receipts'" class="space-y-3">
        <div v-if="report.receipts.length === 0" class="text-center py-8 bg-white border border-slate-200 rounded-2xl p-6">
          <Receipt class="w-8 h-8 text-slate-300 mx-auto mb-2" />
          <p class="text-xs text-slate-500 mb-3">Aucun reçu ou justificatif joint.</p>
          <router-link
            to="/add-receipt"
            class="inline-flex items-center gap-1 text-xs font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 px-3 py-1.5 rounded-xl transition-colors"
          >
            <Plus class="w-3.5 h-3.5" />
            Ajouter un reçu
          </router-link>
        </div>

        <div
          v-for="receipt in report.receipts"
          :key="receipt.id"
          class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs flex items-center justify-between gap-3"
        >
          <div class="flex items-center gap-3">
            <div
              v-if="receipt.receiptImagePath"
              @click="selectedPreviewImage = receipt.receiptImagePath"
              class="w-12 h-12 rounded-xl bg-slate-100 border border-slate-200 overflow-hidden cursor-pointer shrink-0 relative group"
            >
              <img :src="receipt.receiptImagePath" alt="Reçu" class="w-full h-full object-cover" />
              <div class="absolute inset-0 bg-black/30 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity text-white">
                <Eye class="w-4 h-4" />
              </div>
            </div>
            <div v-else class="w-12 h-12 rounded-xl bg-slate-100 flex items-center justify-center text-slate-400 shrink-0">
              <Receipt class="w-5 h-5" />
            </div>

            <div>
              <span class="text-[10px] font-bold text-blue-600 uppercase tracking-wider">
                {{ getCategoryLabel(receipt.category) }}
              </span>
              <h3 class="text-sm font-bold text-slate-900">{{ receipt.merchant }}</h3>
              <span class="text-xs text-slate-400">
                {{ new Date(receipt.date).toLocaleDateString('fr-FR') }}
              </span>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <div class="text-right">
              <span class="text-base font-black text-slate-900">{{ receipt.amountTtc.toFixed(2) }} {{ currencySymbol }}</span>
              <div v-if="receipt.vatAmount" class="text-[10px] text-slate-400">
                TVA {{ receipt.vatAmount.toFixed(2) }} {{ currencySymbol }}
              </div>
            </div>
            <button
              @click="deleteReceipt(receipt.id)"
              class="text-slate-400 hover:text-rose-600 p-1 transition-colors"
            >
              <Trash2 class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal d'aperçu d'image de reçu -->
    <div
      v-if="selectedPreviewImage"
      class="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4 backdrop-blur-xs"
      @click="selectedPreviewImage = null"
    >
      <div class="relative max-w-md w-full bg-white rounded-2xl overflow-hidden shadow-2xl p-2">
        <img :src="selectedPreviewImage" alt="Aperçu reçu" class="w-full max-h-[80vh] object-contain rounded-xl" />
      </div>
    </div>
  </div>
</template>
