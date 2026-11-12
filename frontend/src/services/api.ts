import { Capacitor } from '@capacitor/core';

export enum VehicleType {
  Car = 0,
  Motorcycle = 1,
  Scooter = 2
}

export enum ReportStatus {
  Draft = 0,
  Submitted = 1,
  Approved = 2,
  Rejected = 3
}

export enum RateType {
  FiscalScale = 0,
  CustomFixedRate = 1
}

export enum ReceiptCategory {
  Toll = 0,
  Parking = 1,
  Meal = 2,
  Fuel = 3,
  Hotel = 4,
  Other = 5
}

export interface MileagePreset {
  code: string;
  name: string;
  country: string;
  currency: string;
  currencySymbol: string;
  defaultRatePerKm: number;
  supportsFiscalHorsepower: boolean;
  supportsElectricBonus: boolean;
  description: string;
}

export const DEFAULT_MILEAGE_PRESETS: MileagePreset[] = [
  {
    code: 'FR_URSSAF',
    name: 'France — Barème Fiscal Urssaf / DGFIP',
    country: 'FR',
    currency: 'EUR',
    currencySymbol: '€',
    defaultRatePerKm: 0.636,
    supportsFiscalHorsepower: true,
    supportsElectricBonus: true,
    description: 'Barème progressif par tranches (5 000 / 20 000 km) selon CV fiscaux et majoration +20% électrique'
  },
  {
    code: 'BE_FPS',
    name: 'Belgique — SPF Finances',
    country: 'BE',
    currency: 'EUR',
    currencySymbol: '€',
    defaultRatePerKm: 0.428,
    supportsFiscalHorsepower: false,
    supportsElectricBonus: false,
    description: 'Indemnité kilométrique forfaitaire officielle belge (0,428 €/km)'
  },
  {
    code: 'CH_TCS',
    name: 'Suisse — Barème TCS / Fédéral',
    country: 'CH',
    currency: 'CHF',
    currencySymbol: 'CHF',
    defaultRatePerKm: 0.7,
    supportsFiscalHorsepower: false,
    supportsElectricBonus: false,
    description: 'Indemnité kilométrique standard suisse (0,70 CHF/km)'
  },
  {
    code: 'CA_CRA',
    name: 'Canada — ARC / CRA Automobile Allowance',
    country: 'CA',
    currency: 'CAD',
    currencySymbol: 'CA$',
    defaultRatePerKm: 0.7,
    supportsFiscalHorsepower: false,
    supportsElectricBonus: false,
    description: 'Barème ARC : 0,70 CA$/km sur les 5 000 premiers km puis 0,64 CA$/km au-delà'
  },
  {
    code: 'UK_HMRC',
    name: 'Royaume-Uni — HMRC AMAP',
    country: 'GB',
    currency: 'GBP',
    currencySymbol: '£',
    defaultRatePerKm: 0.45,
    supportsFiscalHorsepower: false,
    supportsElectricBonus: false,
    description: "Barème HMRC : 0,45 £/km jusqu'à 16 000 km annuels puis 0,25 £/km au-delà"
  },
  {
    code: 'US_IRS',
    name: 'USA — IRS Standard Mileage',
    country: 'US',
    currency: 'USD',
    currencySymbol: '$',
    defaultRatePerKm: 0.67,
    supportsFiscalHorsepower: false,
    supportsElectricBonus: false,
    description: 'Barème fédéral IRS Standard Mileage (0,67 $/km)'
  },
  {
    code: 'CUSTOM',
    name: 'International — Forfait Entreprise Personnalisé',
    country: 'ALL',
    currency: 'EUR',
    currencySymbol: '€',
    defaultRatePerKm: 0.5,
    supportsFiscalHorsepower: false,
    supportsElectricBonus: false,
    description: "Taux kilométrique fixe librement défini par la politique voyage de l'organisation"
  }
];

export function formatCurrencySymbol(currency?: string | null): string {
  switch (currency?.trim().toUpperCase()) {
    case 'USD':
      return '$';
    case 'GBP':
      return '£';
    case 'CHF':
      return 'CHF';
    case 'CAD':
      return 'CA$';
    default:
      return '€';
  }
}

export interface Vehicle {
  id: number;
  name: string;
  licensePlate: string;
  type: VehicleType;
  fiscalHorsepower: number;
  isElectric: boolean;
  defaultCustomRatePerKm?: number | null;
  isDefault: boolean;
  createdAt: string;
}

export interface Trip {
  id: number;
  expenseReportId: number;
  date: string;
  purpose: string;
  clientOrLocation: string;
  departureAddress: string;
  arrivalAddress: string;
  distanceKm: number;
  isRoundTrip: boolean;
  totalDistanceKm: number;
  vehicleId?: number | null;
  vehicle?: Vehicle | null;
  calculationMethod: RateType;
  customRatePerKm?: number | null;
  calculatedAmount: number;
  notes?: string | null;
  createdAt: string;
}

export interface ReceiptExpense {
  id: number;
  expenseReportId: number;
  date: string;
  category: ReceiptCategory;
  merchant: string;
  amountTtc: number;
  vatRate?: number | null;
  vatAmount?: number | null;
  receiptImagePath?: string | null;
  notes?: string | null;
  createdAt: string;
}

export interface ExpenseReportSummary {
  id: number;
  title: string;
  employeeName: string;
  companyName: string;
  periodStart: string;
  periodEnd: string;
  status: ReportStatus;
  reimbursementRatePercentage: number;
  tripsCount: number;
  totalDistanceKm: number;
  totalMileageAmount: number;
  receiptsCount: number;
  totalReceiptsAmount: number;
  totalGrossAmount: number;
  totalReimbursableAmount: number;
  createdAt: string;
  countryScale?: string;
  currency?: string;
}

export interface ExpenseReportDetail extends ExpenseReportSummary {
  employeeEmail?: string | null;
  trips: Trip[];
  receipts: ReceiptExpense[];
  updatedAt?: string | null;
}

export interface AddressResult {
  label: string;
  city: string;
  postcode: string;
  country?: string;
  longitude: number;
  latitude: number;
}

export interface RouteResult {
  distanceKm: number;
  durationMinutes: number;
  source: string;
}

export const getApiBase = (): string => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL;
  if (Capacitor.isNativePlatform()) {
    return Capacitor.getPlatform() === 'android' ? 'http://10.0.2.2:5000/api' : 'http://localhost:5000/api';
  }
  return '/api';
};

const API_BASE = getApiBase();

async function handleJsonResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    throw new Error(`HTTP Error ${res.status}: ${res.statusText || 'Request failed'}`);
  }
  return res.json() as Promise<T>;
}

async function handleVoidResponse(res: Response): Promise<void> {
  if (!res.ok) {
    throw new Error(`HTTP Error ${res.status}: ${res.statusText || 'Request failed'}`);
  }
}

export const api = {
  // Véhicules
  async getVehicles(): Promise<Vehicle[]> {
    const res = await fetch(`${API_BASE}/vehicles`);
    return handleJsonResponse<Vehicle[]>(res);
  },

  async createVehicle(data: Partial<Vehicle>): Promise<Vehicle> {
    const res = await fetch(`${API_BASE}/vehicles`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return handleJsonResponse<Vehicle>(res);
  },

  async updateVehicle(id: number, data: Partial<Vehicle>): Promise<Vehicle> {
    const res = await fetch(`${API_BASE}/vehicles/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return handleJsonResponse<Vehicle>(res);
  },

  async deleteVehicle(id: number): Promise<void> {
    const res = await fetch(`${API_BASE}/vehicles/${id}`, { method: 'DELETE' });
    return handleVoidResponse(res);
  },

  // Notes de frais
  async getReports(): Promise<ExpenseReportSummary[]> {
    const res = await fetch(`${API_BASE}/reports`);
    return handleJsonResponse<ExpenseReportSummary[]>(res);
  },

  async getReport(id: number): Promise<ExpenseReportDetail> {
    const res = await fetch(`${API_BASE}/reports/${id}`);
    return handleJsonResponse<ExpenseReportDetail>(res);
  },

  async createReport(data: {
    title: string;
    employeeName: string;
    employeeEmail?: string;
    companyName: string;
    periodStart: string;
    periodEnd: string;
    reimbursementRatePercentage?: number;
    countryScale?: string;
    currency?: string;
  }): Promise<ExpenseReportDetail> {
    const res = await fetch(`${API_BASE}/reports`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return handleJsonResponse<ExpenseReportDetail>(res);
  },

  async updateReportStatus(id: number, status: ReportStatus): Promise<ExpenseReportDetail> {
    const res = await fetch(`${API_BASE}/reports/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status })
    });
    return handleJsonResponse<ExpenseReportDetail>(res);
  },

  async deleteReport(id: number): Promise<void> {
    const res = await fetch(`${API_BASE}/reports/${id}`, { method: 'DELETE' });
    return handleVoidResponse(res);
  },

  // Trajets
  async addTrip(reportId: number, data: {
    date: string;
    purpose: string;
    clientOrLocation: string;
    departureAddress: string;
    arrivalAddress: string;
    distanceKm: number;
    isRoundTrip: boolean;
    vehicleId?: number | null;
    calculationMethod: RateType;
    customRatePerKm?: number | null;
    notes?: string;
  }): Promise<Trip> {
    const res = await fetch(`${API_BASE}/reports/${reportId}/trips`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return handleJsonResponse<Trip>(res);
  },

  async deleteTrip(reportId: number, tripId: number): Promise<void> {
    const res = await fetch(`${API_BASE}/reports/${reportId}/trips/${tripId}`, { method: 'DELETE' });
    return handleVoidResponse(res);
  },

  // Justificatifs & Reçus
  async addReceipt(reportId: number, data: {
    date: string;
    category: ReceiptCategory;
    merchant: string;
    amountTtc: number;
    vatRate?: number | null;
    vatAmount?: number | null;
    notes?: string;
  }): Promise<ReceiptExpense> {
    const res = await fetch(`${API_BASE}/reports/${reportId}/receipts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return handleJsonResponse<ReceiptExpense>(res);
  },

  async uploadReceiptImage(reportId: number, receiptId: number, file: File): Promise<{ receiptImagePath: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_BASE}/reports/${reportId}/receipts/${receiptId}/upload`, {
      method: 'POST',
      body: formData
    });
    return handleJsonResponse<{ receiptImagePath: string }>(res);
  },

  async deleteReceipt(reportId: number, receiptId: number): Promise<void> {
    const res = await fetch(`${API_BASE}/reports/${reportId}/receipts/${receiptId}`, { method: 'DELETE' });
    return handleVoidResponse(res);
  },

  // Simulateur & Barèmes Internationaux
  async getPresets(): Promise<MileagePreset[]> {
    const res = await fetch(`${API_BASE}/calculator/presets`);
    return handleJsonResponse<MileagePreset[]>(res);
  },

  async simulate(data: {
    distanceKm: number;
    isRoundTrip: boolean;
    vehicleType: VehicleType;
    fiscalHorsepower: number;
    isElectric: boolean;
    customRatePerKm?: number | null;
    countryScale?: string;
    currency?: string;
  }): Promise<{
    unitDistanceKm: number;
    isRoundTrip: boolean;
    totalDistanceKm: number;
    ratePerKm: number;
    calculatedAmount: number;
    calculationMethodDescription: string;
    countryScale?: string;
    currency?: string;
  }> {
    const res = await fetch(`${API_BASE}/calculator/simulate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return handleJsonResponse(res);
  },

  // Adresses & Itinéraire (France + International)
  async autocompleteAddress(query: string, country?: string): Promise<AddressResult[]> {
    if (!query || query.trim().length < 3) return [];
    const countryParam = country ? `&country=${encodeURIComponent(country)}` : '';
    const res = await fetch(`${API_BASE}/addresses/autocomplete?q=${encodeURIComponent(query.trim())}${countryParam}`);
    return handleJsonResponse<AddressResult[]>(res);
  },

  async calculateRoute(fromLon: number, fromLat: number, toLon: number, toLat: number): Promise<RouteResult> {
    const res = await fetch(`${API_BASE}/addresses/route?fromLon=${fromLon}&fromLat=${fromLat}&toLon=${toLon}&toLat=${toLat}`);
    return handleJsonResponse<RouteResult>(res);
  },

  // URL de téléchargement PDF
  getPdfDownloadUrl(reportId: number): string {
    return `${API_BASE}/reports/${reportId}/pdf`;
  }
};
