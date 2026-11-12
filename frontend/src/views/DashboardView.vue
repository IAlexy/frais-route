<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { api, type ExpenseReportSummary, ReportStatus } from '@/services/api'
import { Plus, Download, FileText, Calendar, ChevronRight, CheckCircle2, Clock, AlertCircle } from 'lucide-vue-next'
import { Share } from '@capacitor/share'
import { Capacitor } from '@capacitor/core'

const reports = ref<ExpenseReportSummary[]>([])
const isLoading = ref(true)
const showCreateModal = ref(false)

// Formulaire nouvelle note
const newTitle = ref(`Frais de route - ${new Date().toLocaleString('fr-FR', { month: 'long', year: 'numeric' })}`)
const newEmployeeName = ref('Alexy Martin')
const newCompanyName = ref('Mon Entreprise')
const newPeriodStart = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10))
const newPeriodEnd = ref(new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0).toISOString().slice(0, 10))
const newReimbursementRate = ref(100)

async function loadReports() {
  isLoading.value = true
  try {
    reports.value = await api.getReports()
  } catch (err) {
    console.error('Erreur chargement notes de frais', err)
  } finally {
    isLoading.value = false
  }
}

onMounted(loadReports)

const totalReimbursableAll = computed(() => {
  return reports.value.reduce((acc: number, r: ExpenseReportSummary) => acc + r.totalReimbursableAmount, 0)
})

const totalKmAll = computed(() => {
  return reports.value.reduce((acc: number, r: ExpenseReportSummary) => acc + r.totalDistanceKm, 0)
})

async function createReport() {
  try {
    await api.createReport({
      title: newTitle.value,
      employeeName: newEmployeeName.value,
      companyName: newCompanyName.value,
      periodStart: new Date(newPeriodStart.value).toISOString(),
      periodEnd: new Date(newPeriodEnd.value).toISOString(),
      reimbursementRatePercentage: newReimbursementRate.value
    })
    showCreateModal.value = false
    await loadReports()
  } catch (err) {
    alert('Erreur lors de la création de la note de frais')
  }
}

async function shareOrDownloadPdf(reportId: number, title: string, event: Event) {
  event.stopPropagation()
  const pdfUrl = api.getPdfDownloadUrl(reportId)

  if (Capacitor.isNativePlatform()) {
    try {
      await Share.share({
        title: `Note de frais - ${title}`,
        text: `Voici ma note de frais pour validation.`,
        url: window.location.origin + pdfUrl,
        dialogTitle: 'Partager ma note de frais'
      })
      return
    } catch {
      // Fallback vers ouverture directe si annulé
    }
  }

  // Web download
  window.open(pdfUrl, '_blank')
}

function getStatusBadge(status: ReportStatus) {
  switch (status) {
    case ReportStatus.Approved:
      return { text: 'Validée', class: 'bg-emerald-50 text-emerald-700 border-emerald-200', icon: CheckCircle2 }
    case ReportStatus.Submitted:
      return { text: 'Soumise', class: 'bg-amber-50 text-amber-700 border-amber-200', icon: Clock }
    case ReportStatus.Rejected:
      return { text: 'Refusée', class: 'bg-rose-50 text-rose-700 border-rose-200', icon: AlertCircle }
    default:
      return { text: 'Brouillon', class: 'bg-slate-100 text-slate-700 border-slate-200', icon: FileText }
  }
}
</script>

<template>
  <div class="max-w-xl mx-auto px-4 pt-6 pb-24">
    <!-- En-tête -->
    <header class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Frais de route</h1>
        <p class="text-xs text-slate-500 mt-0.5">Indemnités kilométriques & Justificatifs</p>
      </div>
      <button
        @click="showCreateModal = true"
        class="inline-flex items-center gap-1.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-xs font-semibold px-3.5 py-2 rounded-xl shadow-sm transition-all"
      >
        <Plus class="w-4 h-4" />
        Nouvelle note
      </button>
    </header>

    <!-- Résumé KPI Global -->
    <div class="grid grid-cols-2 gap-3 mb-6">
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs">
        <span class="text-xs font-medium text-slate-500">Total à rembourser</span>
        <div class="text-2xl font-black text-blue-600 mt-1">
          {{ totalReimbursableAll.toFixed(2) }} €
        </div>
        <span class="text-[11px] text-slate-400">Toutes notes confondues</span>
      </div>

      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs">
        <span class="text-xs font-medium text-slate-500">Total distance</span>
        <div class="text-2xl font-black text-slate-800 mt-1">
          {{ totalKmAll.toFixed(0) }} <span class="text-sm font-semibold text-slate-500">km</span>
        </div>
        <span class="text-[11px] text-slate-400">Barème fiscal Urssaf</span>
      </div>
    </div>

    <!-- Liste des Notes de Frais -->
    <div class="mb-4 flex items-center justify-between">
      <h2 class="text-sm font-bold text-slate-800 uppercase tracking-wider">Mes Notes de frais</h2>
      <span class="text-xs text-slate-400">{{ reports.length }} dossier(s)</span>
    </div>

    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-24 bg-slate-200/60 rounded-2xl animate-pulse"></div>
    </div>

    <div v-else-if="reports.length === 0" class="text-center py-12 bg-white border border-dashed border-slate-300 rounded-2xl p-8">
      <FileText class="w-10 h-10 text-slate-300 mx-auto mb-3" />
      <h3 class="text-sm font-semibold text-slate-800">Aucune note de frais</h3>
      <p class="text-xs text-slate-500 mt-1 mb-4">Créez votre première note mensuelle pour y enregistrer vos trajets.</p>
      <button
        @click="showCreateModal = true"
        class="bg-blue-600 text-white text-xs font-semibold px-4 py-2 rounded-xl shadow-xs"
      >
        Créer une note
      </button>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="report in reports"
        :key="report.id"
        @click="$router.push(`/reports/${report.id}`)"
        class="bg-white border border-slate-200 hover:border-blue-300 rounded-2xl p-4 shadow-xs transition-all cursor-pointer group"
      >
        <div class="flex items-start justify-between gap-3 mb-2">
          <div>
            <div class="flex items-center gap-2 mb-1">
              <span
                class="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full border"
                :class="getStatusBadge(report.status).class"
              >
                <component :is="getStatusBadge(report.status).icon" class="w-3 h-3" />
                {{ getStatusBadge(report.status).text }}
              </span>
              <span class="text-[11px] text-slate-400 font-mono">NDF-{{ String(report.id).padStart(5, '0') }}</span>
            </div>
            <h3 class="text-base font-bold text-slate-900 group-hover:text-blue-600 transition-colors">
              {{ report.title }}
            </h3>
            <p class="text-xs text-slate-500 mt-0.5 flex items-center gap-1.5">
              <Calendar class="w-3.5 h-3.5 text-slate-400" />
              Du {{ new Date(report.periodStart).toLocaleDateString('fr-FR') }} au {{ new Date(report.periodEnd).toLocaleDateString('fr-FR') }}
            </p>
          </div>

          <div class="text-right shrink-0">
            <div class="text-lg font-black text-slate-900">
              {{ report.totalReimbursableAmount.toFixed(2) }} €
            </div>
            <div class="text-[11px] text-slate-400">
              {{ report.totalDistanceKm.toFixed(0) }} km · {{ report.tripsCount }} trajet(s)
            </div>
          </div>
        </div>

        <!-- Boutons d'action rapides -->
        <div class="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between">
          <div class="text-[11px] text-slate-500 font-medium">
            IK : <span class="font-semibold text-slate-800">{{ report.totalMileageAmount.toFixed(2) }} €</span>
            <span v-if="report.totalReceiptsAmount > 0"> · Reçus : <span class="font-semibold text-slate-800">{{ report.totalReceiptsAmount.toFixed(2) }} €</span></span>
          </div>

          <div class="flex items-center gap-2">
            <button
              @click="shareOrDownloadPdf(report.id, report.title, $event)"
              class="inline-flex items-center gap-1 text-xs font-semibold text-blue-600 bg-blue-50 hover:bg-blue-100 px-2.5 py-1.5 rounded-lg transition-colors"
              title="Exporter ou partager en PDF"
            >
              <Download class="w-3.5 h-3.5" />
              PDF
            </button>
            <ChevronRight class="w-4 h-4 text-slate-300 group-hover:text-blue-500 transition-colors" />
          </div>
        </div>
      </div>
    </div>

    <!-- Modal Création Note de Frais -->
    <div
      v-if="showCreateModal"
      class="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-xs flex items-center justify-center p-4"
    >
      <div class="bg-white rounded-2xl max-w-md w-full p-5 shadow-2xl border border-slate-200">
        <h3 class="text-lg font-bold text-slate-900 mb-1">Nouvelle note de frais</h3>
        <p class="text-xs text-slate-500 mb-4">Définissez la période et l'organisme bénéficiaire.</p>

        <form @submit.prevent="createReport" class="space-y-3.5">
          <div>
            <label class="block text-xs font-semibold text-slate-600 mb-1">Titre du dossier</label>
            <input
              v-model="newTitle"
              type="text"
              required
              class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 focus:bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none"
            />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Nom du salarié</label>
              <input
                v-model="newEmployeeName"
                type="text"
                required
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Entreprise / Client</label>
              <input
                v-model="newCompanyName"
                type="text"
                required
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Date début</label>
              <input
                v-model="newPeriodStart"
                type="date"
                required
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Date fin</label>
              <input
                v-model="newPeriodEnd"
                type="date"
                required
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
              />
            </div>
          </div>

          <div>
            <label class="block text-xs font-semibold text-slate-600 mb-1">
              Taux de remboursement employeur ({{ newReimbursementRate }}%)
            </label>
            <input
              v-model.number="newReimbursementRate"
              type="range"
              min="10"
              max="100"
              step="5"
              class="w-full accent-blue-600 cursor-pointer"
            />
            <div class="flex justify-between text-[10px] text-slate-400 mt-0.5">
              <span>Partiel (50%)</span>
              <span>Complet (100%)</span>
            </div>
          </div>

          <div class="pt-2 flex justify-end gap-2">
            <button
              type="button"
              @click="showCreateModal = false"
              class="px-4 py-2 text-xs font-medium text-slate-600 hover:bg-slate-100 rounded-xl transition-colors"
            >
              Annuler
            </button>
            <button
              type="submit"
              class="px-4 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-xs transition-colors"
            >
              Créer la note
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
