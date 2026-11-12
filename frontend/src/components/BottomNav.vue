<script setup lang="ts">
import { useRoute } from 'vue-router'
import { FileText, PlusCircle, Receipt, Car, Calculator } from 'lucide-vue-next'

const route = useRoute()

const navItems = [
  { name: 'Notes', path: '/', icon: FileText },
  { name: 'Nouveau Trajet', path: '/add-trip', icon: PlusCircle, isPrimary: true },
  { name: 'Reçu', path: '/add-receipt', icon: Receipt },
  { name: 'Véhicules', path: '/vehicles', icon: Car },
  { name: 'Simulateur', path: '/simulator', icon: Calculator },
]

function isActive(path: string) {
  if (path === '/') return route.path === '/'
  return route.path.startsWith(path)
}
</script>

<template>
  <nav class="fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-md border-t border-slate-200 safe-bottom">
    <div class="max-w-md mx-auto px-4 h-16 flex items-center justify-around">
      <router-link
        v-for="item in navItems"
        :key="item.path"
        :to="item.path"
        class="flex flex-col items-center justify-center py-1 px-2 rounded-xl transition-all relative"
        :class="[
          isActive(item.path) ? 'text-blue-600 font-semibold' : 'text-slate-500 hover:text-slate-800',
          item.isPrimary ? 'scale-105' : ''
        ]"
      >
        <div
          v-if="item.isPrimary"
          class="w-10 h-10 -mt-5 rounded-full bg-blue-600 text-white flex items-center justify-center shadow-lg shadow-blue-500/30 ring-4 ring-white"
        >
          <component :is="item.icon" class="w-5 h-5" />
        </div>
        <template v-else>
          <component :is="item.icon" class="w-5 h-5 mb-1" />
          <span class="text-[11px] leading-none">{{ item.name }}</span>
        </template>
      </router-link>
    </div>
  </nav>
</template>
