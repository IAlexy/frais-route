<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue'
import {
  api,
  VehicleType,
  DEFAULT_MILEAGE_PRESETS,
  formatCurrencySymbol,
  type MileagePreset
} from '@/services/api'
import { Sparkles, ArrowLeftRight, Zap, Info, Globe } from 'lucide-vue-next'

const presets = ref<MileagePreset[]>(DEFAULT_MILEAGE_PRESETS)
const countryScale = ref<string>('FR_URSSAF')
const distanceKm = ref<number>(85)
const isRoundTrip = ref(true)
const vehicleType = ref<VehicleType>(VehicleType.Car)
const fiscalCv = ref<number>(5)
const isElectric = ref(false)
const customRate = ref<number | null>(null)

const activePreset = computed(() => {
  return presets.value.find((p) => p.code === countryScale.value) || presets.value[0]
})

const currencySymbol = computed(() => {
  return formatCurrencySymbol(result.value?.currency || activePreset.value?.currency)
})

const result = ref<{
  totalDistanceKm: number
  ratePerKm: number
  calculatedAmount: number
  calculationMethodDescription: string
  countryScale?: string
  currency?: string
} | null>(null)

async function calculate() {
  if (!distanceKm.value || distanceKm.value <= 0) return
  try {
    result.value = await api.simulate({
      distanceKm: distanceKm.value,
      isRoundTrip: isRoundTrip.value,
      vehicleType: vehicleType.value,
      fiscalHorsepower: fiscalCv.value,
      isElectric: isElectric.value,
      customRatePerKm: countryScale.value === 'CUSTOM' ? (customRate.value || 0.5) : customRate.value,
      countryScale: countryScale.value,
      currency: activePreset.value?.currency
    })
  } catch (err) {
    console.error(err)
  }
}

watch([countryScale, distanceKm, isRoundTrip, vehicleType, fiscalCv, isElectric, customRate], calculate)
onMounted(calculate)
</script>

<template>
  <div class="max-w-xl mx-auto px-4 pt-6 pb-28">
    <div class="mb-5">
      <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Simulateur IK</h1>
      <p class="text-xs text-slate-500 mt-0.5">Barèmes fiscaux officiels internationaux & Forfaits entreprise</p>
    </div>

    <div class="space-y-4">
      <!-- Sélection du Pays / Barème Fiscal -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-2.5">
        <label class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-600">
          <Globe class="w-3.5 h-3.5 text-blue-600" />
          Pays & Barème Fiscal Applicable
        </label>
        <select
          v-model="countryScale"
          class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2.5 text-sm font-bold text-slate-900 outline-none focus:border-blue-600"
        >
          <option v-for="p in presets" :key="p.code" :value="p.code">
            {{ p.name }} ({{ p.currencySymbol }})
          </option>
        </select>
        <p class="text-[11px] text-slate-500 leading-snug">
          {{ activePreset?.description }}
        </p>

        <div v-if="countryScale === 'CUSTOM'" class="pt-2">
          <label class="block text-xs font-semibold text-slate-600 mb-1">
            Taux forfaitaire personnalisé ({{ currencySymbol }}/km)
          </label>
          <input
            v-model.number="customRate"
            type="number"
            step="0.01"
            min="0.05"
            placeholder="0.50"
            class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold text-slate-900 outline-none"
          />
        </div>
      </div>

      <!-- Paramètres du déplacement -->
      <div class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-3.5">
        <div>
          <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 mb-1.5">
            Distance (km aller simple)
          </label>
          <div class="relative">
            <input
              v-model.number="distanceKm"
              type="number"
              step="1"
              min="1"
              class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2.5 text-base font-black text-slate-900 outline-none"
            />
            <span class="absolute right-3 top-2.5 text-xs text-slate-400 font-bold">km</span>
          </div>
        </div>

        <button
          type="button"
          @click="isRoundTrip = !isRoundTrip"
          class="w-full flex items-center justify-between px-3 py-2.5 rounded-xl border text-xs font-semibold transition-all"
          :class="isRoundTrip ? 'bg-blue-50 text-blue-700 border-blue-200' : 'bg-slate-50 text-slate-600 border-slate-200'"
        >
          <span class="flex items-center gap-1.5">
            <ArrowLeftRight class="w-3.5 h-3.5" />
            {{ isRoundTrip ? 'Aller-Retour (Distance doublée)' : 'Aller simple' }}
          </span>
          <span class="font-bold text-xs">{{ (distanceKm || 0) * (isRoundTrip ? 2 : 1) }} km</span>
        </button>
      </div>

      <!-- Paramètres du véhicule (Puissance fiscale CV & Bonus Électrique) -->
      <div
        v-if="activePreset?.supportsFiscalHorsepower"
        class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs space-y-3.5"
      >
        <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600">
          Puissance fiscale (CV)
        </label>
        <div class="grid grid-cols-5 gap-1.5">
          <button
            v-for="cv in [3, 4, 5, 6, 7]"
            :key="cv"
            type="button"
            @click="fiscalCv = cv"
            class="py-2.5 rounded-xl border text-center transition-all"
            :class="fiscalCv === cv ? 'bg-blue-600 text-white font-bold border-blue-600 shadow-xs' : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'"
          >
            <span class="text-sm block leading-none">{{ cv }}</span>
            <span class="text-[9px] opacity-80">{{ cv === 7 ? 'CV+' : 'CV' }}</span>
          </button>
        </div>

        <!-- Option Électrique -->
        <div
          @click="isElectric = !isElectric"
          class="p-3 rounded-xl border flex items-center justify-between cursor-pointer transition-all"
          :class="isElectric ? 'bg-emerald-50 border-emerald-300' : 'bg-slate-50 border-slate-200'"
        >
          <div class="flex items-center gap-2">
            <Zap class="w-4 h-4" :class="isElectric ? 'text-emerald-600 fill-emerald-500' : 'text-slate-400'" />
            <div>
              <div class="text-xs font-bold" :class="isElectric ? 'text-emerald-900' : 'text-slate-700'">
                Véhicule 100% Électrique
              </div>
              <div class="text-[10px] text-slate-500">+20% de majoration fiscale</div>
            </div>
          </div>
          <span class="text-xs font-bold" :class="isElectric ? 'text-emerald-700' : 'text-slate-400'">
            {{ isElectric ? 'OUI (+20%)' : 'NON' }}
          </span>
        </div>
      </div>

      <!-- Résultat Calculé -->
      <div v-if="result" class="bg-gradient-to-br from-blue-600 to-indigo-700 text-white rounded-2xl p-5 shadow-lg shadow-blue-500/20">
        <div class="flex items-center justify-between text-xs text-blue-100 mb-2">
          <span class="flex items-center gap-1.5 font-semibold">
            <Sparkles class="w-4 h-4 text-amber-300" />
            {{ result.calculationMethodDescription }}
          </span>
          <span class="font-mono bg-white/10 px-2 py-0.5 rounded-md text-[11px]">
            {{ result.ratePerKm.toFixed(3) }} {{ currencySymbol }}/km
          </span>
        </div>

        <div class="flex items-baseline justify-between mt-3 pt-3 border-t border-white/15">
          <div>
            <span class="text-xs text-blue-100 block">Indemnité kilométrique due</span>
            <span class="text-xs text-blue-200">{{ result.totalDistanceKm }} km parcourus</span>
          </div>
          <span class="text-3xl font-black tracking-tight">{{ result.calculatedAmount.toFixed(2) }} {{ currencySymbol }}</span>
        </div>
      </div>

      <!-- Aide barème -->
      <div class="text-[11px] text-slate-400 p-3 bg-slate-100/70 rounded-xl flex items-start gap-2">
        <Info class="w-4 h-4 text-slate-400 shrink-0 mt-0.5" />
        <div>
          Les montants calculés sont exonérés de cotisations sociales et d'impôts dans les limites du barème fiscal officiel sélectionné (Urssaf, SPF Finances, TCS, ARC/CRA, HMRC, IRS).
        </div>
      </div>
    </div>
  </div>
</template>
