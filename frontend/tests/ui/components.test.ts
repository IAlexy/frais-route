import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import BottomNav from '@/components/BottomNav.vue'
import AddressAutocomplete from '@/components/AddressAutocomplete.vue'
import App from '@/App.vue'
import { router as appRouter } from '@/router'
import { api } from '@/services/api'
import { setLocale } from '@/i18n'

vi.mock('@/services/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/services/api')>()
  return {
    ...actual,
    api: {
      ...actual.api,
      getReports: vi.fn().mockResolvedValue([]),
      autocompleteAddress: vi.fn()
    }
  }
})

describe('UI Components Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setLocale('fr')
  })

  it('renders BottomNav with 5 navigation links and highlights active route', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Dashboard</div>' } },
        { path: '/add-trip', component: { template: '<div>AddTrip</div>' } },
        { path: '/add-receipt', component: { template: '<div>AddReceipt</div>' } },
        { path: '/vehicles', component: { template: '<div>Vehicles</div>' } },
        { path: '/simulator', component: { template: '<div>Simulator</div>' } }
      ]
    })

    router.push('/vehicles')
    await router.isReady()

    const wrapper = mount(BottomNav, {
      global: {
        plugins: [router]
      }
    })

    const links = wrapper.findAll('a')
    expect(links).toHaveLength(5)
    expect(wrapper.text()).toContain('Notes')
    expect(wrapper.text()).toContain('Reçu')
    expect(wrapper.text()).toContain('Véhicules')
    expect(wrapper.text()).toContain('Simulateur')

    const vehiclesLink = links.find((l) => l.text().includes('Véhicules'))
    expect(vehiclesLink?.classes()).toContain('text-blue-600')
  })

  it('AddressAutocomplete searches addresses with debounce, selects suggestion, and clears input', async () => {
    vi.useFakeTimers()

    const mockSuggestions = [
      {
        label: '12 Rue de la Paix 75002 Paris',
        city: 'Paris',
        postcode: '75002',
        longitude: 2.331,
        latitude: 48.869
      }
    ]

    vi.mocked(api.autocompleteAddress).mockResolvedValue(mockSuggestions)

    const wrapper = mount(AddressAutocomplete, {
      props: {
        modelValue: '',
        label: 'Adresse de départ',
        placeholder: 'Saisir une adresse'
      }
    })

    expect(wrapper.text()).toContain('Adresse de départ')

    const input = wrapper.find('input')
    await input.setValue('12 Rue de la Paix')

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['12 Rue de la Paix'])

    vi.advanceTimersByTime(350)
    await flushPromises()

    expect(api.autocompleteAddress).toHaveBeenCalledWith('12 Rue de la Paix', undefined)
    expect(wrapper.text()).toContain('12 Rue de la Paix 75002 Paris')
    expect(wrapper.text()).toContain('75002 Paris')

    // Click suggestion
    const suggestionButton = wrapper.findAll('button').find((b) => b.text().includes('75002 Paris'))
    await suggestionButton?.trigger('click')

    expect(wrapper.emitted('select')?.[0]).toEqual([mockSuggestions[0]])

    // Click clear button
    const clearButton = wrapper.find('button')
    await clearButton.trigger('click')
    expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual([''])

    vi.useRealTimers()
  })

  it('mounts root App component with application router cleanly', async () => {
    appRouter.push('/')
    await appRouter.isReady()

    const wrapper = mount(App, {
      global: {
        plugins: [appRouter]
      }
    })
    await flushPromises()

    expect(wrapper.find('main').exists()).toBe(true)
    expect(wrapper.find('nav').exists()).toBe(true)
    expect(wrapper.text()).toContain('Frais de route')
    expect(wrapper.text()).toContain('Simulateur')

    // Click EN button in header to switch UI language to English in real time
    const enButton = wrapper.find('[data-testid="lang-en"]')
    expect(enButton.exists()).toBe(true)
    await enButton.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Travel Expenses')
    expect(wrapper.text()).toContain('Simulator')
    expect(wrapper.text()).toContain('Vehicles')

    // Switch back to FR
    const frButton = wrapper.find('[data-testid="lang-fr"]')
    await frButton.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Frais de route')
  })
})
