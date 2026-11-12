import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import SimulatorView from '@/views/SimulatorView.vue'
import VehiclesView from '@/views/VehiclesView.vue'
import DashboardView from '@/views/DashboardView.vue'
import ReportDetailView from '@/views/ReportDetailView.vue'
import AddTripView from '@/views/AddTripView.vue'
import AddReceiptView from '@/views/AddReceiptView.vue'
import { api, VehicleType, ReportStatus, RateType, ReceiptCategory } from '@/services/api'

vi.mock('@/services/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/services/api')>()
  return {
    ...actual,
    api: {
      ...actual.api,
      simulate: vi.fn(),
      getVehicles: vi.fn(),
      createVehicle: vi.fn(),
      deleteVehicle: vi.fn(),
      getReports: vi.fn(),
      getReport: vi.fn(),
      createReport: vi.fn(),
      addTrip: vi.fn(),
      deleteTrip: vi.fn(),
      addReceipt: vi.fn(),
      uploadReceiptImage: vi.fn(),
      deleteReceipt: vi.fn(),
      autocompleteAddress: vi.fn(),
      calculateRoute: vi.fn(),
      getPdfDownloadUrl: vi.fn((id: number) => `/api/reports/${id}/pdf`)
    }
  }
})

describe('Main Views UI Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('SimulatorView calculates mileage allowance on mount and updates on parameter change', async () => {
    vi.mocked(api.simulate).mockResolvedValue({
      unitDistanceKm: 85,
      isRoundTrip: true,
      totalDistanceKm: 170,
      ratePerKm: 0.636,
      calculatedAmount: 108.12,
      calculationMethodDescription: 'Barème fiscal officiel Urssaf (5 CV, thermique/hybride)'
    })

    const wrapper = mount(SimulatorView)
    await flushPromises()

    expect(api.simulate).toHaveBeenCalledTimes(1)
    expect(wrapper.text()).toContain('108.12 €')
    expect(wrapper.text()).toContain('170 km parcourus')

    // Toggle 6 CV button
    vi.mocked(api.simulate).mockResolvedValueOnce({
      unitDistanceKm: 85,
      isRoundTrip: true,
      totalDistanceKm: 170,
      ratePerKm: 0.665,
      calculatedAmount: 113.05,
      calculationMethodDescription: 'Barème fiscal officiel Urssaf (6 CV, thermique/hybride)'
    })

    const cv6Button = wrapper.findAll('button').find((b) => b.text().startsWith('6CV'))
    await cv6Button?.trigger('click')
    await flushPromises()

    expect(api.simulate).toHaveBeenCalledWith(
      expect.objectContaining({ fiscalHorsepower: 6 })
    )
    expect(wrapper.text()).toContain('113.05 €')
  })

  it('VehiclesView lists vehicles and opens modal to create a new vehicle', async () => {
    vi.mocked(api.getVehicles).mockResolvedValue([
      {
        id: 1,
        name: 'Peugeot 208',
        licensePlate: 'GH-456-JK',
        type: VehicleType.Car,
        fiscalHorsepower: 5,
        isElectric: false,
        isDefault: true,
        createdAt: '2026-01-01T00:00:00Z'
      },
      {
        id: 2,
        name: 'Tesla Model 3',
        licensePlate: 'EV-999-FR',
        type: VehicleType.Car,
        fiscalHorsepower: 6,
        isElectric: true,
        isDefault: false,
        createdAt: '2026-01-01T00:00:00Z'
      }
    ])

    vi.mocked(api.createVehicle).mockResolvedValue({
      id: 3,
      name: 'Renault Clio',
      licensePlate: 'AA-123-BB',
      type: VehicleType.Car,
      fiscalHorsepower: 4,
      isElectric: false,
      isDefault: false,
      createdAt: '2026-02-01T00:00:00Z'
    })

    const wrapper = mount(VehiclesView)
    await flushPromises()

    expect(wrapper.text()).toContain('Peugeot 208')
    expect(wrapper.text()).toContain('GH-456-JK')
    expect(wrapper.text()).toContain('Tesla Model 3')
    expect(wrapper.text()).toContain('+20% élec')

    // Click "Ajouter" button to open modal
    const addButton = wrapper.findAll('button').find((b) => b.text().includes('Ajouter'))
    await addButton?.trigger('click')
    expect(wrapper.text()).toContain('Ajouter un véhicule')

    const inputs = wrapper.findAll('form input[type="text"]')
    await inputs[0].setValue('Renault Clio')
    await inputs[1].setValue('aa-123-bb')

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(api.createVehicle).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Renault Clio',
        licensePlate: 'AA-123-BB'
      })
    )
  })

  it('DashboardView displays global KPIs, report cards, and creates a new report', async () => {
    vi.mocked(api.getReports).mockResolvedValue([
      {
        id: 1,
        title: 'Frais de route - Mars 2026',
        employeeName: 'Alexy Martin',
        companyName: 'TechCorp',
        periodStart: '2026-03-01T00:00:00Z',
        periodEnd: '2026-03-31T00:00:00Z',
        status: ReportStatus.Approved,
        reimbursementRatePercentage: 100,
        tripsCount: 2,
        totalDistanceKm: 240,
        totalMileageAmount: 152.64,
        receiptsCount: 1,
        totalReceiptsAmount: 25.0,
        totalGrossAmount: 177.64,
        totalReimbursableAmount: 177.64,
        createdAt: '2026-03-01T00:00:00Z'
      }
    ])

    vi.mocked(api.createReport).mockResolvedValue({
      id: 2,
      title: 'Frais Avril 2026',
      employeeName: 'Alexy Martin',
      companyName: 'TechCorp',
      periodStart: '2026-04-01T00:00:00Z',
      periodEnd: '2026-04-30T00:00:00Z',
      status: ReportStatus.Draft,
      reimbursementRatePercentage: 100,
      tripsCount: 0,
      totalDistanceKm: 0,
      totalMileageAmount: 0,
      receiptsCount: 0,
      totalReceiptsAmount: 0,
      totalGrossAmount: 0,
      totalReimbursableAmount: 0,
      trips: [],
      receipts: [],
      createdAt: '2026-04-01T00:00:00Z'
    })

    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: DashboardView },
        { path: '/reports/:id', component: { template: '<div>Detail</div>' } }
      ]
    })

    const wrapper = mount(DashboardView, {
      global: {
        plugins: [router]
      }
    })
    await flushPromises()

    expect(wrapper.text()).toContain('177.64 €')
    expect(wrapper.text()).toContain('240')
    expect(wrapper.text()).toContain('Frais de route - Mars 2026')
    expect(wrapper.text()).toContain('Validée')

    // Open create report modal
    const newReportBtn = wrapper.findAll('button').find((b) => b.text().includes('Nouvelle note'))
    await newReportBtn?.trigger('click')
    expect(wrapper.text()).toContain('Nouvelle note de frais')

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(api.createReport).toHaveBeenCalledTimes(1)
  })

  it('ReportDetailView renders report summary, switches between trips and receipts tabs', async () => {
    vi.mocked(api.getReport).mockResolvedValue({
      id: 10,
      title: 'Frais Mars 2026',
      employeeName: 'Alexy Martin',
      companyName: 'TechCorp',
      periodStart: '2026-03-01T00:00:00Z',
      periodEnd: '2026-03-31T00:00:00Z',
      status: ReportStatus.Draft,
      reimbursementRatePercentage: 80,
      tripsCount: 1,
      totalDistanceKm: 100,
      totalMileageAmount: 63.6,
      receiptsCount: 1,
      totalReceiptsAmount: 36.4,
      totalGrossAmount: 100,
      totalReimbursableAmount: 80,
      createdAt: '2026-03-01T00:00:00Z',
      trips: [
        {
          id: 1,
          expenseReportId: 10,
          date: '2026-03-05T00:00:00Z',
          purpose: 'Rendez-vous Lyon',
          clientOrLocation: 'Client A',
          departureAddress: 'Paris',
          arrivalAddress: 'Lyon',
          distanceKm: 50,
          isRoundTrip: true,
          totalDistanceKm: 100,
          calculationMethod: RateType.FiscalScale,
          calculatedAmount: 63.6,
          createdAt: '2026-03-05T00:00:00Z'
        }
      ],
      receipts: [
        {
          id: 1,
          expenseReportId: 10,
          date: '2026-03-05T00:00:00Z',
          category: ReceiptCategory.Toll,
          merchant: 'APRR A6',
          amountTtc: 36.4,
          vatRate: 20,
          vatAmount: 6.07,
          createdAt: '2026-03-05T00:00:00Z'
        }
      ]
    })

    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Dashboard</div>' } },
        { path: '/reports/:id', component: ReportDetailView },
        { path: '/add-trip', component: { template: '<div>AddTrip</div>' } },
        { path: '/add-receipt', component: { template: '<div>AddReceipt</div>' } }
      ]
    })
    router.push('/reports/10')
    await router.isReady()

    const wrapper = mount(ReportDetailView, {
      global: { plugins: [router] }
    })
    await flushPromises()

    expect(wrapper.text()).toContain('80.00 €')
    expect(wrapper.text()).toContain('Prise en charge 80%')
    expect(wrapper.text()).toContain('Rendez-vous Lyon')

    // Switch to Receipts tab
    const receiptsTab = wrapper.findAll('button').find((b) => b.text().includes('Justificatifs & Reçus'))
    await receiptsTab?.trigger('click')
    expect(wrapper.text()).toContain('APRR A6')
    expect(wrapper.text()).toContain('Péage')
  })

  it('AddTripView and AddReceiptView load initial data and submit forms', async () => {
    vi.mocked(api.getReports).mockResolvedValue([
      {
        id: 10,
        title: 'Frais Mars 2026',
        employeeName: 'Alexy',
        companyName: 'TechCorp',
        periodStart: '2026-03-01T00:00:00Z',
        periodEnd: '2026-03-31T00:00:00Z',
        status: ReportStatus.Draft,
        reimbursementRatePercentage: 100,
        tripsCount: 0,
        totalDistanceKm: 0,
        totalMileageAmount: 0,
        receiptsCount: 0,
        totalReceiptsAmount: 0,
        totalGrossAmount: 0,
        totalReimbursableAmount: 0,
        createdAt: '2026-03-01T00:00:00Z'
      }
    ])

    vi.mocked(api.getVehicles).mockResolvedValue([
      {
        id: 1,
        name: 'Peugeot 208',
        licensePlate: 'GH-456-JK',
        type: VehicleType.Car,
        fiscalHorsepower: 5,
        isElectric: false,
        isDefault: true,
        createdAt: '2026-01-01T00:00:00Z'
      }
    ])

    vi.mocked(api.simulate).mockResolvedValue({
      unitDistanceKm: 35,
      isRoundTrip: true,
      totalDistanceKm: 70,
      ratePerKm: 0.636,
      calculatedAmount: 44.52,
      calculationMethodDescription: 'Barème Urssaf'
    })

    vi.mocked(api.addTrip).mockResolvedValue({
      id: 99,
      expenseReportId: 10,
      date: '2026-03-10T00:00:00Z',
      purpose: 'Rendez-vous client',
      clientOrLocation: '',
      departureAddress: 'Paris',
      arrivalAddress: 'Versailles',
      distanceKm: 35,
      isRoundTrip: true,
      totalDistanceKm: 70,
      calculationMethod: RateType.FiscalScale,
      calculatedAmount: 44.52,
      createdAt: '2026-03-10T00:00:00Z'
    })

    vi.mocked(api.addReceipt).mockResolvedValue({
      id: 88,
      expenseReportId: 10,
      date: '2026-03-10T00:00:00Z',
      category: ReceiptCategory.Toll,
      merchant: 'APRR',
      amountTtc: 24.5,
      vatRate: 20,
      vatAmount: 4.08,
      createdAt: '2026-03-10T00:00:00Z'
    })

    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: DashboardView },
        { path: '/add-trip', component: AddTripView },
        { path: '/add-receipt', component: AddReceiptView },
        { path: '/reports/:id', component: { template: '<div>Detail</div>' } }
      ]
    })
    router.push('/add-trip')
    await router.isReady()

    const tripWrapper = mount(AddTripView, { global: { plugins: [router] } })
    await flushPromises()
    expect(tripWrapper.text()).toContain('44.52 €')
    await tripWrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    expect(api.addTrip).toHaveBeenCalledTimes(1)

    const receiptWrapper = mount(AddReceiptView, { global: { plugins: [router] } })
    await flushPromises()
    await receiptWrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    expect(api.addReceipt).toHaveBeenCalledTimes(1)
  })
})
