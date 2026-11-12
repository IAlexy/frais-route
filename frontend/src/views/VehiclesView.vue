<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api, type Vehicle, VehicleType } from '@/services/api'
import { Car, Zap, Plus, Trash2, CheckCircle2, ShieldCheck } from 'lucide-vue-next'

const vehicles = ref<Vehicle[]>([])
const isLoading = ref(true)
const showAddModal = ref(false)

const newName = ref('')
const newPlate = ref('')
const newType = ref<VehicleType>(VehicleType.Car)
const newCv = ref<number>(5)
const newIsElectric = ref(false)
const newIsDefault = ref(false)
const newCustomRate = ref<number | null>(null)

async function loadVehicles() {
  isLoading.value = true
  try {
    vehicles.value = await api.getVehicles()
  } finally {
    isLoading.value = false
  }
}

onMounted(loadVehicles)

async function createVehicle() {
  try {
    await api.createVehicle({
      name: newName.value,
      licensePlate: newPlate.value.toUpperCase(),
      type: newType.value,
      fiscalHorsepower: newCv.value,
      isElectric: newIsElectric.value,
      isDefault: newIsDefault.value,
      defaultCustomRatePerKm: newCustomRate.value
    })
    showAddModal.value = false
    // Reset form
    newName.value = ''
    newPlate.value = ''
    newCv.value = 5
    newIsElectric.value = false
    await loadVehicles()
  } catch (err) {
    alert('Erreur lors de l’ajout du véhicule')
  }
}

async function deleteVehicle(id: number) {
  if (!confirm('Supprimer ce véhicule ?')) return
  await api.deleteVehicle(id)
  await loadVehicles()
}
</script>

<template>
  <div class="max-w-xl mx-auto px-4 pt-6 pb-28">
    <div class="flex items-center justify-between mb-5">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 tracking-tight">Mes Véhicules</h1>
        <p class="text-xs text-slate-500 mt-0.5">Configuration des barèmes fiscaux (CV)</p>
      </div>
      <button
        @click="showAddModal = true"
        class="inline-flex items-center gap-1 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 px-3 py-2 rounded-xl shadow-xs transition-all"
      >
        <Plus class="w-4 h-4" />
        Ajouter
      </button>
    </div>

    <!-- Bannière explicative Urssaf -->
    <div class="bg-blue-50 border border-blue-200 rounded-2xl p-4 mb-5 flex items-start gap-3">
      <ShieldCheck class="w-5 h-5 text-blue-600 shrink-0 mt-0.5" />
      <div class="text-xs text-blue-950">
        <span class="font-bold">Barème officiel français :</span>
        Le remboursement dépend directement de la puissance fiscale (sur la carte grise, case P.6).
        Les véhicules 100% électriques bénéficient automatiquement d'une <span class="font-bold">majoration de +20%</span>.
      </div>
    </div>

    <!-- Liste des Véhicules -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 2" :key="i" class="h-24 bg-slate-200/60 rounded-2xl animate-pulse"></div>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="v in vehicles"
        :key="v.id"
        class="bg-white border border-slate-200 rounded-2xl p-4 shadow-xs flex items-center justify-between"
      >
        <div class="flex items-center gap-3.5">
          <div
            class="w-12 h-12 rounded-xl flex items-center justify-center shrink-0"
            :class="v.isElectric ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-700'"
          >
            <Zap v-if="v.isElectric" class="w-6 h-6 fill-emerald-500" />
            <Car v-else class="w-6 h-6" />
          </div>

          <div>
            <div class="flex items-center gap-1.5 mb-0.5">
              <h3 class="text-sm font-bold text-slate-900">{{ v.name }}</h3>
              <span
                v-if="v.isDefault"
                class="text-[10px] font-bold bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full"
              >
                Par défaut
              </span>
            </div>
            <div class="text-xs text-slate-500 flex items-center gap-2">
              <span class="font-mono bg-slate-100 px-1.5 py-0.5 rounded text-[11px] font-semibold text-slate-700">
                {{ v.licensePlate }}
              </span>
              <span>· {{ v.fiscalHorsepower }} CV</span>
              <span v-if="v.isElectric" class="text-emerald-600 font-semibold">+20% élec</span>
            </div>
          </div>
        </div>

        <button
          @click="deleteVehicle(v.id)"
          class="text-slate-400 hover:text-rose-600 p-2 transition-colors"
        >
          <Trash2 class="w-4 h-4" />
        </button>
      </div>
    </div>

    <!-- Modal d'ajout de véhicule -->
    <div
      v-if="showAddModal"
      class="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-xs flex items-center justify-center p-4"
    >
      <div class="bg-white rounded-2xl max-w-md w-full p-5 shadow-2xl border border-slate-200">
        <h3 class="text-lg font-bold text-slate-900 mb-1">Ajouter un véhicule</h3>
        <p class="text-xs text-slate-500 mb-4">Renseignez les détails pour le calcul kilométrique.</p>

        <form @submit.prevent="createVehicle" class="space-y-3.5">
          <div>
            <label class="block text-xs font-semibold text-slate-600 mb-1">Marque & Modèle</label>
            <input
              v-model="newName"
              type="text"
              required
              placeholder="Ex: Peugeot 208, Renault Megane..."
              class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none"
            />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Immatriculation</label>
              <input
                v-model="newPlate"
                type="text"
                required
                placeholder="Ex: AA-123-BB"
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 uppercase outline-none"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">
                Puissance fiscale : {{ newCv }} CV
              </label>
              <select
                v-model.number="newCv"
                class="w-full bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-900 outline-none font-bold"
              >
                <option :value="3">3 CV et moins</option>
                <option :value="4">4 CV</option>
                <option :value="5">5 CV (Le plus courant)</option>
                <option :value="6">6 CV</option>
                <option :value="7">7 CV et plus</option>
              </select>
            </div>
          </div>

          <!-- Toggle Électrique -->
          <div
            @click="newIsElectric = !newIsElectric"
            class="p-3 rounded-xl border flex items-center justify-between cursor-pointer transition-all"
            :class="newIsElectric ? 'bg-emerald-50 border-emerald-300' : 'bg-slate-50 border-slate-200'"
          >
            <div class="flex items-center gap-2">
              <Zap class="w-4 h-4" :class="newIsElectric ? 'text-emerald-600 fill-emerald-500' : 'text-slate-400'" />
              <div>
                <div class="text-xs font-bold" :class="newIsElectric ? 'text-emerald-900' : 'text-slate-700'">
                  Véhicule 100% Électrique
                </div>
                <div class="text-[10px] text-slate-500">Majoration légale de +20% sur les indemnités</div>
              </div>
            </div>
            <div
              class="w-5 h-5 rounded-full border flex items-center justify-center transition-all"
              :class="newIsElectric ? 'bg-emerald-600 border-emerald-600 text-white' : 'border-slate-300 bg-white'"
            >
              <CheckCircle2 v-if="newIsElectric" class="w-4 h-4" />
            </div>
          </div>

          <!-- Toggle Défaut -->
          <label class="flex items-center gap-2 cursor-pointer pt-1">
            <input v-model="newIsDefault" type="checkbox" class="w-4 h-4 rounded text-blue-600 accent-blue-600" />
            <span class="text-xs text-slate-700 font-medium">Définir comme véhicule principal par défaut</span>
          </label>

          <div class="pt-2 flex justify-end gap-2">
            <button
              type="button"
              @click="showAddModal = false"
              class="px-4 py-2 text-xs font-medium text-slate-600 hover:bg-slate-100 rounded-xl"
            >
              Annuler
            </button>
            <button
              type="submit"
              class="px-4 py-2 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-xs"
            >
              Enregistrer
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
