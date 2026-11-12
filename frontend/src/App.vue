<script setup lang="ts">
import BottomNav from '@/components/BottomNav.vue'
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-900 safe-top flex flex-col font-sans">
    <main class="flex-1 w-full pb-6">
      <router-view />
    </main>

    <!-- Navigation mobile & desktop unifiée -->
    <BottomNav />
  </div>
</template>
