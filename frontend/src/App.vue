<script setup lang="ts">
import BottomNav from '@/components/BottomNav.vue'
import { useI18n } from '@/i18n'
import { Languages } from 'lucide-vue-next'

const { locale, availableLocales, setLocale, t } = useI18n()
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-900 safe-top flex flex-col font-sans">
    <!-- Barre supérieure globale avec sélecteur de langue i18n -->
    <div class="bg-white/90 backdrop-blur-md border-b border-slate-200 sticky top-0 z-30">
      <div class="max-w-xl mx-auto px-4 h-11 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="w-2 h-2 rounded-full bg-blue-600"></span>
          <span class="text-xs font-extrabold tracking-tight text-slate-900">{{ t('app.name') }}</span>
          <span class="text-[11px] text-slate-400 hidden sm:inline">· {{ t('app.tagline') }}</span>
        </div>

        <div class="flex items-center gap-1" role="group" :aria-label="t('app.language')">
          <Languages class="w-3.5 h-3.5 text-slate-400 mr-1" />
          <button
            v-for="opt in availableLocales"
            :key="opt.code"
            type="button"
            :data-testid="`lang-${opt.code}`"
            @click="setLocale(opt.code)"
            class="px-2 py-0.5 text-[11px] font-bold rounded-md transition-all"
            :class="
              locale === opt.code
                ? 'bg-blue-600 text-white shadow-xs'
                : 'text-slate-600 hover:bg-slate-100'
            "
          >
            {{ opt.label }}
          </button>
        </div>
      </div>
    </div>

    <main class="flex-1 w-full pb-6">
      <router-view />
    </main>

    <!-- Navigation mobile & desktop unifiée -->
    <BottomNav />
  </div>
</template>
