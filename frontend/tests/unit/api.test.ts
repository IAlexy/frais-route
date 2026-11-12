import { describe, it, expect, vi, beforeEach } from 'vitest'
import {
  api,
  VehicleType,
  ReportStatus,
  RateType,
  ReceiptCategory
} from '@/services/api'

describe('api service unit tests', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })

  it('fetches, creates, updates, and deletes vehicles', async () => {
    const mockVehicles = [
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
    ]

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockVehicles
    })
    const vehicles = await api.getVehicles()
    expect(vehicles).toEqual(mockVehicles)
    expect(fetchMock).toHaveBeenCalledWith('/api/vehicles')

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockVehicles[0]
    })
    const created = await api.createVehicle({ name: 'Peugeot 208', fiscalHorsepower: 5 })
    expect(created.name).toBe('Peugeot 208')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/vehicles',
      expect.objectContaining({ method: 'POST' })
    )

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ ...mockVehicles[0], name: 'Peugeot 208 GT' })
    })
    const updated = await api.updateVehicle(1, { name: 'Peugeot 208 GT' })
    expect(updated.name).toBe('Peugeot 208 GT')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/vehicles/1',
      expect.objectContaining({ method: 'PUT' })
    )

    fetchMock.mockResolvedValueOnce({ ok: true })
    await api.deleteVehicle(1)
    expect(fetchMock).toHaveBeenCalledWith('/api/vehicles/1', { method: 'DELETE' })
  })

  it('manages expense reports (list, detail, create, status patch, delete, pdf url)', async () => {
    const mockSummary = {
      id: 10,
      title: 'Frais Mars 2026',
      employeeName: 'Alexy Martin',
      companyName: 'TechCorp',
      periodStart: '2026-03-01T00:00:00Z',
      periodEnd: '2026-03-31T00:00:00Z',
      status: ReportStatus.Draft,
      reimbursementRatePercentage: 100,
      tripsCount: 1,
      totalDistanceKm: 100,
      totalMileageAmount: 63.6,
      receiptsCount: 0,
      totalReceiptsAmount: 0,
      totalGrossAmount: 63.6,
      totalReimbursableAmount: 63.6,
      createdAt: '2026-03-01T00:00:00Z'
    }

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => [mockSummary]
    })
    const reports = await api.getReports()
    expect(reports).toHaveLength(1)
    expect(reports[0].title).toBe('Frais Mars 2026')

    const mockDetail = { ...mockSummary, trips: [], receipts: [] }
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockDetail
    })
    const detail = await api.getReport(10)
    expect(detail.id).toBe(10)

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockDetail
    })
    const created = await api.createReport({
      title: 'Frais Mars 2026',
      employeeName: 'Alexy Martin',
      companyName: 'TechCorp',
      periodStart: '2026-03-01T00:00:00Z',
      periodEnd: '2026-03-31T00:00:00Z',
      reimbursementRatePercentage: 100
    })
    expect(created.id).toBe(10)

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ ...mockDetail, status: ReportStatus.Approved })
    })
    const patched = await api.updateReportStatus(10, ReportStatus.Approved)
    expect(patched.status).toBe(ReportStatus.Approved)

    fetchMock.mockResolvedValueOnce({ ok: true })
    await api.deleteReport(10)
    expect(fetchMock).toHaveBeenCalledWith('/api/reports/10', { method: 'DELETE' })

    expect(api.getPdfDownloadUrl(10)).toBe('/api/reports/10/pdf')
  })

  it('adds and deletes trips on a report', async () => {
    const mockTrip = {
      id: 5,
      expenseReportId: 10,
      date: '2026-03-10T00:00:00Z',
      purpose: 'Client meeting',
      clientOrLocation: 'Lyon',
      departureAddress: 'Paris',
      arrivalAddress: 'Lyon',
      distanceKm: 50,
      isRoundTrip: true,
      totalDistanceKm: 100,
      calculationMethod: RateType.FiscalScale,
      calculatedAmount: 63.6,
      createdAt: '2026-03-10T00:00:00Z'
    }

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockTrip
    })

    const trip = await api.addTrip(10, {
      date: '2026-03-10T00:00:00Z',
      purpose: 'Client meeting',
      clientOrLocation: 'Lyon',
      departureAddress: 'Paris',
      arrivalAddress: 'Lyon',
      distanceKm: 50,
      isRoundTrip: true,
      vehicleId: 1,
      calculationMethod: RateType.FiscalScale
    })
    expect(trip.calculatedAmount).toBe(63.6)

    fetchMock.mockResolvedValueOnce({ ok: true })
    await api.deleteTrip(10, 5)
    expect(fetchMock).toHaveBeenCalledWith('/api/reports/10/trips/5', { method: 'DELETE' })
  })

  it('adds, uploads image, and deletes receipts on a report', async () => {
    const mockReceipt = {
      id: 7,
      expenseReportId: 10,
      date: '2026-03-12T00:00:00Z',
      category: ReceiptCategory.Toll,
      merchant: 'APRR',
      amountTtc: 24.0,
      vatRate: 20,
      vatAmount: 4.0,
      createdAt: '2026-03-12T00:00:00Z'
    }

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockReceipt
    })
    const added = await api.addReceipt(10, {
      date: '2026-03-12T00:00:00Z',
      category: ReceiptCategory.Toll,
      merchant: 'APRR',
      amountTtc: 24.0,
      vatRate: 20
    })
    expect(added.id).toBe(7)

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ receiptImagePath: '/uploads/receipts/test.jpg' })
    })
    const dummyFile = new File(['dummy'], 'test.jpg', { type: 'image/jpeg' })
    const uploaded = await api.uploadReceiptImage(10, 7, dummyFile)
    expect(uploaded.receiptImagePath).toBe('/uploads/receipts/test.jpg')

    fetchMock.mockResolvedValueOnce({ ok: true })
    await api.deleteReceipt(10, 7)
    expect(fetchMock).toHaveBeenCalledWith('/api/reports/10/receipts/7', { method: 'DELETE' })
  })

  it('simulates mileage allowance and queries addresses/routes', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        unitDistanceKm: 100,
        isRoundTrip: false,
        totalDistanceKm: 100,
        ratePerKm: 0.636,
        calculatedAmount: 63.6,
        calculationMethodDescription: 'Barème fiscal officiel Urssaf (5 CV, thermique/hybride)'
      })
    })

    const sim = await api.simulate({
      distanceKm: 100,
      isRoundTrip: false,
      vehicleType: VehicleType.Car,
      fiscalHorsepower: 5,
      isElectric: false
    })
    expect(sim.calculatedAmount).toBe(63.6)

    // Short query returns empty array without calling fetch
    const shortRes = await api.autocompleteAddress('ab')
    expect(shortRes).toEqual([])
    expect(fetchMock).toHaveBeenCalledTimes(1)

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => [
        { label: '10 Rue de Rivoli Paris', city: 'Paris', postcode: '75004', longitude: 2.35, latitude: 48.85 }
      ]
    })
    const addrRes = await api.autocompleteAddress('Paris')
    expect(addrRes).toHaveLength(1)
    expect(addrRes[0].city).toBe('Paris')

    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ distanceKm: 465.2, durationMinutes: 280, source: 'OSRM Driving Route' })
    })
    const route = await api.calculateRoute(2.35, 48.85, 4.83, 45.76)
    expect(route.distanceKm).toBe(465.2)
  })

  it('throws descriptive errors when HTTP status is not ok', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: false,
      status: 409,
      statusText: 'Conflict'
    })

    await expect(
      api.createReport({
        title: 'Duplicate',
        employeeName: 'Alexy',
        companyName: 'Acme',
        periodStart: '2026-03-01T00:00:00Z',
        periodEnd: '2026-03-31T00:00:00Z'
      })
    ).rejects.toThrow('HTTP Error 409: Conflict')

    fetchMock.mockResolvedValueOnce({
      ok: false,
      status: 404,
      statusText: 'Not Found'
    })

    await expect(api.deleteVehicle(999)).rejects.toThrow('HTTP Error 404: Not Found')
  })
})
