<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api, type ExpenseReportSummary, type Vehicle, RateType, type AddressResult } from '@/services/api'
import AddressAutocomplete from '@/components/AddressAutocomplete.vue'
import { Sparkles, Check, Zap, ArrowLeftRight } from 'lucide-vue-next'

const router = useRouter()

const reports = ref<ExpenseReportSummary[]>([])
const vehicles = ref<Vehicle[]>([])
const isLoading = ref(true)
const isSubmitting = ref(false)

const selectedReportId = ref<number | null>(null)
const tripDate = ref(new Date().toISOString().slice(0, 10))
const purpose = ref('Rendez-vous client')
const clientOrLocation = ref('')
const departureAddress = ref('')
const arrivalAddress = ref('')
const distanceKm = ref<number>(35)
const isRoundTrip = ref(true)
const selectedVehicleId = ref<number | null>(null)
const notes = ref('')

const fromCoords = ref<{ lon: number; lat: number } | null>(null)
const toCoords = ref<{ lon: number; lat: number } | null>(null)
const isCalculatingDistance = ref(false)

const commonPurposes = [
  'Rendez-vous client',
  'Visite commerciale',
  'Intervention technique',
  'Formation',
  'Déplacement siège'
]

const selectedVehicle = computed(() => {
  return vehicles.value.find((v: Vehicle) => v.id === selectedVehicleId.value) || vehicles.value[0] || null
})

const totalEffectiveKm = computed(() => {
  return (distanceKm.value || 0) * (isRoundTrip.value ? 2 : 1)
})

// Estimation en temps réel
const estimatedAmount = ref(0)
const estimatedRate = ref(0)

async function updateEstimate() {
  if (!totalEffectiveKm.value || !selectedVehicle.value) {
    estimatedAmount.value = 0
    return
  }

  try {
    const res = await api.simulate({
      distanceKm: distanceKm.value,
      isRoundTrip: isRoundTrip.value,
      vehicleType: selectedVehicle.value.type,
      fiscalHorsepower: selectedVehicle.value.fiscalHorsepower,
      isElectric: selectedVehicle.value.isElectric,
      customRatePerKm: selectedVehicle.value.defaultCustomRatePerKm
    })
    estimatedAmount.value = res.calculatedAmount
    estimatedRate.value = res.ratePerKm
  } catch {
    estimatedAmount.value = 0
  }
}

watch([distanceKm, isRoundTrip, selectedVehicleId], updateEstimate)

onMounted(async () => {
  try {
    const [reportsData, vehiclesData] = await Promise.all([
      api.getReports(),
      api.getVehicles()
    ])
    reports.value = reportsData
    vehicles.value = vehiclesData

    if (reportsData.length > 0) {
      selectedReportId.value = reportsData[0].id
    }
    const defaultV = vehiclesData.find((v: Vehicle) => v.isDefault) || vehiclesData[0]
    if (defaultV) {
      selectedVehicleId.value = defaultV.id
    }

    await updateEstimate()
  } finally {
    isLoading.value = false
  }
})

function onSelectDeparture(item: AddressResult) {
  fromCoords.value = { lon: item.longitude, lat: item.latitude }
  tryAutoCalculateRoute()
}

function onSelectArrival(item: AddressResult) {
  toCoords.value = { lon: item.longitude, lat: item.latitude }
  tryAutoCalculateRoute()
}

async function tryAutoCalculateRoute() {
  if (fromCoords.value && toCoords.value) {
    isCalculatingDistance.value = true
    try {
      const route = await api.calculateRoute(
        fromCoords.value.lon,
        fromCoords.value.lat,
        toCoords.value.lon,
        toCoords.value.lat
      )
      if (route.distanceKm > 0) {
        distanceKm.value = route.distanceKm
      }
    } catch (err) {
      console.warn('Impossible de calculer l’itinéraire automatique', err)
    } finally {
      isCalculatingDistance.value = false
    }
  }
}

async function submitTrip() {
  if (!selectedReportId.value) {
    alert('Veuillez sélectionner ou créer une note de frais d’abord.')
    return
  }

  isSubmitting.value = true
  try {
    await api.addTrip(selectedReportId.value, {
      date: new Date(tripDate.value).toISOString(),
      purpose: purpose.value,
      clientOrLocation: clientOrLocation.value,
      departureAddress: departureAddress.value,
      arrivalAddress: arrivalAddress.value,
      distanceKm: distanceKm.value,
      isRoundTrip: isRoundTrip.value,
      vehicleId: selectedVehicleId.value,
      calculationMethod: RateType.FiscalScale,
      notes: notes.value
    })

    router.push(`/reports/${selectedReportId.value}`)
  } catch (err) {
    alert('Erreur lors de l’enregistrement du trajet')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="max-w-xl mx-auto px-4 pt-6 pb-28">
    <div class="mb-5">
      <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Nouveau trajet</h1>
      <p class="text-xs text-slate-500 mt-0.5">Calcul automatique des indemnités kilométriques</p>
    </div>

    <form @submit.prevent="submitTrip" class="space-y-4">
      <!-- Sélection de la Note de Frais -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">
          Rattacher à la Note de frais
        </label>
        <select
          v-model="selectedReportId"
          required
          class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2.5 text-sm text-slate-900 font-medium focus:bg-white outline-none"
        >
          <option v-for="r in reports" :key="r.id" :value="r.id">
            {{ r.title }} ({{ r.totalReimbursableAmount.toFixed(2) }} €)
          </option>
        </select>
      </div>

      <!-- Date & Motif -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-3">
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">Date</label>
            <input
              v-model="tripDate"
              type="date"
              required
              class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
            />
          </div>
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">Client / Lieu</label>
            <input
              v-model="clientOrLocation"
              type="text"
              placeholder="Ex: Client ABC, Chantier 4"
              class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
            />
          </div>
        </div>

        <div>
          <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">Motif professionnel</label>
          <input
            v-model="purpose"
            type="text"
            required
            placeholder="Ex: Rendez-vous client, Formation..."
            class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none mb-2"
          />
          <div class="flex flex-wrap gap-1.5">
            <button
              v-for="p in commonPurposes"
              :key="p"
              type="button"
              @click="purpose = p"
              class="text-[11px] px-2.5 py-1 rounded-lg border transition-all"
              :class="purpose === p ? 'bg-blue-50 text-blue-600 border-blue-200 font-semibold' : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'"
            >
              {{ p }}
            </button>
          </div>
        </div>
      </div>

      <!-- Itinéraire (Adresses avec API data.gouv.fr) -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-3">
        <AddressAutocomplete
          v-model="departureAddress"
          label="Adresse de départ"
          placeholder="Ex: Lyon, 12 rue de la Paix Paris..."
          @select="onSelectDeparture"
        />

        <AddressAutocomplete
          v-model="arrivalAddress"
          label="Adresse d'arrivée"
          placeholder="Ex: Marseille, Centre d'affaires Nantes..."
          @select="onSelectArrival"
        />

        <!-- Distance & Aller-Retour -->
        <div class="pt-2 grid grid-cols-2 gap-3 items-center">
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">
              Distance simple (km)
            </label>
            <div class="relative">
              <input
                v-model.number="distanceKm"
                type="number"
                step="0.1"
                min="0.5"
                required
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2.5 text-base font-bold text-slate-900 outline-none"
              />
              <span class="absolute right-3 top-2.5 text-xs text-slate-400 font-semibold">km</span>
            </div>
          </div>

          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">
              Type de trajet
            </label>
            <button
              type="button"
              @click="isRoundTrip = !isRoundTrip"
              class="w-full flex items-center justify-between px-3 py-2.5 rounded-xl border text-xs font-semibold transition-all"
              :class="isRoundTrip ? 'bg-blue-50 text-blue-700 border-blue-200' : 'bg-slate-50 text-slate-600 border-slate-200'"
            >
              <span class="flex items-center gap-1.5">
                <ArrowLeftRight class="w-3.5 h-3.5" />
                {{ isRoundTrip ? 'Aller-Retour (x2)' : 'Aller simple' }}
              </span>
              <span class="font-bold text-xs">{{ totalEffectiveKm }} km</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Véhicule & Estimation en direct -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-3">
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1">
          Véhicule utilisé
        </label>
        <div class="grid grid-cols-2 gap-2">
          <button
            v-for="v in vehicles"
            :key="v.id"
            type="button"
            @click="selectedVehicleId = v.id"
            class="text-left p-3 rounded-xl border transition-all"
            :class="selectedVehicleId === v.id ? 'border-blue-600 bg-blue-50/50 shadow-xs ring-1 ring-blue-600' : 'border-slate-200 hover:bg-slate-50'"
          >
            <div class="flex items-center justify-between mb-1">
              <span class="text-xs font-bold text-slate-900">{{ v.name }}</span>
              <span v-if="v.isElectric" class="inline-flex items-center text-[10px] text-emerald-600 font-bold">
                <Zap class="w-3 h-3 mr-0.5 fill-emerald-500" /> Élec
              </span>
            </div>
            <div class="text-[11px] text-slate-500">{{ v.fiscalHorsepower }} CV · {{ v.licensePlate }}</div>
          </button>
        </div>

        <!-- Encadré d'estimation Urssaf -->
        <div class="mt-3 bg-gradient-to-br from-blue-600 to-indigo-700 text-white rounded-xl p-3.5 shadow-sm">
          <div class="flex items-center justify-between text-xs text-blue-100 mb-1">
            <span class="flex items-center gap-1">
              <Sparkles class="w-3.5 h-3.5 text-amber-300" />
              Barème Urssaf ({{ selectedVehicle?.fiscalHorsepower || 5 }} CV{{ selectedVehicle?.isElectric ? ' +20% élec' : '' }})
            </span>
            <span>{{ estimatedRate.toFixed(3) }} €/km</span>
          </div>

          <div class="flex items-baseline justify-between mt-1">
            <span class="text-xs font-medium text-blue-100">{{ totalEffectiveKm }} km au total</span>
            <span class="text-2xl font-black">{{ estimatedAmount.toFixed(2) }} €</span>
          </div>
        </div>
      </div>

      <!-- Bouton d'enregistrement -->
      <button
        type="submit"
        :disabled="isSubmitting"
        class="w-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 text-white font-bold py-3 px-4 rounded-xl shadow-md shadow-blue-500/20 transition-all flex items-center justify-center gap-2"
      >
        <Check class="w-5 h-5" />
        {{ isSubmitting ? 'Enregistrement...' : 'Enregistrer le trajet' }}
      </button>
    </form>
  </div>
</template>
