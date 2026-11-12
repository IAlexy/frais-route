import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'
import ReportDetailView from '@/views/ReportDetailView.vue'
import AddTripView from '@/views/AddTripView.vue'
import AddReceiptView from '@/views/AddReceiptView.vue'
import VehiclesView from '@/views/VehiclesView.vue'
import SimulatorView from '@/views/SimulatorView.vue'

const routes = [
  { path: '/', name: 'Dashboard', component: DashboardView },
  { path: '/reports/:id', name: 'ReportDetail', component: ReportDetailView },
  { path: '/add-trip', name: 'AddTrip', component: AddTripView },
  { path: '/add-receipt', name: 'AddReceipt', component: AddReceiptView },
  { path: '/vehicles', name: 'Vehicles', component: VehiclesView },
  { path: '/simulator', name: 'Simulator', component: SimulatorView },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})
