import { apiClient } from './client';

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post('/api/auth/login', { email, password }).then((r) => r.data),
  register: (payload: unknown) =>
    apiClient.post('/api/auth/register', payload).then((r) => r.data),
};

export const financeApi = {
  summary: (year: number, month: number) =>
    apiClient.get(`/api/finance/summary/${year}/${month}`).then((r) => r.data),
  transactions: (params: { type?: string; page?: number }) =>
    apiClient.get('/api/finance/transactions', { params }).then((r) => r.data),
  addTransaction: (formData: FormData) =>
    apiClient.post('/api/finance/transactions', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((r) => r.data),
  // Dosyasız gelir/gider ekleme (FromForm uyumlu FormData)
  add: (data: { type: 'Income' | 'Expense'; category: string; description?: string; amount: number; date: string }) => {
    const fd = new FormData();
    fd.append('Type', data.type);
    fd.append('Category', data.category);
    if (data.description) fd.append('Description', data.description);
    fd.append('Amount', String(data.amount));
    fd.append('Date', data.date);
    return apiClient.post('/api/finance/transactions', fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((r) => r.data);
  },
};

export const duesApi = {
  list: (params: { year?: number; month?: number; page?: number }) =>
    apiClient.get('/api/dues', { params }).then((r) => r.data),
  collect: (duesId: string, amount: number, generateReceipt = true) =>
    apiClient.post('/api/dues/collect', { duesId, amount, generateReceipt }).then((r) => r.data),
  generate: (year: number, month: number) =>
    apiClient.post('/api/dues/generate', { year, month }).then((r) => r.data),
  update: (duesId: string, amount: number) =>
    apiClient.put(`/api/dues/${duesId}`, { amount }).then((r) => r.data),
};

export const apartmentsApi = {
  list: (params: { page?: number; blockId?: string }) =>
    apiClient.get('/api/apartments', { params }).then((r) => r.data),
  blocks: () => apiClient.get('/api/apartments/blocks').then((r) => r.data),
  types: () => apiClient.get('/api/apartments/types').then((r) => r.data),
  chart: (apartmentId: string) =>
    apiClient.get(`/api/apartments/${apartmentId}/chart`).then((r) => r.data),
  create: (payload: {
    blockId: string; monthlyDues: number; doorNumber: string;
    floor: number; ownerFullName?: string; ownerPhone?: string; ownerTc?: string;
  }) => apiClient.post('/api/apartments', payload).then((r) => r.data),
  createBlock: (name: string, displayOrder = 0) =>
    apiClient.post('/api/apartments/blocks', { name, displayOrder }).then((r) => r.data),
  removeBlock: (id: string) =>
    apiClient.delete(`/api/apartments/blocks/${id}`).then((r) => r.data),
  createType: (name: string, baseDues: number, arsaPayi = 0) =>
    apiClient.post('/api/apartments/types', { name, baseDues, arsaPayi }).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/apartments/${id}`).then((r) => r.data),
};

export const reportsApi = {
  // Borçlu daireler
  debtors: (year: number, month?: number) =>
    apiClient.get('/api/reports/debtors', { params: { year, month } }).then((r) => r.data),
  debtorsCsv: (year: number, month?: number) =>
    csv('/api/reports/debtors/export', { year, month }),
  // KMK — hazır olanlar listesi (Premium)
  kmk: (year: number) => apiClient.get('/api/reports/kmk', { params: { year } }).then((r) => r.data),
  kmkCsv: (year: number) => csv('/api/reports/kmk/export', { year }),
  // KMK ihtarname PDF (Premium) — arraybuffer
  ihtarname: (apartmentId: string, year: number) =>
    apiClient.get(`/api/reports/kmk/${apartmentId}/ihtarname`, { params: { year }, responseType: 'arraybuffer' })
      .then((r) => r.data as ArrayBuffer),
  // Aidat raporu
  dues: (year: number, month?: number) =>
    apiClient.get('/api/reports/dues', { params: { year, month } }).then((r) => r.data),
  duesCsv: (year: number, month?: number) => csv('/api/reports/dues/export', { year, month }),
  // Ek aidat raporu (Premium)
  extraDues: () => apiClient.get('/api/reports/extra-dues').then((r) => r.data),
  extraDuesCsv: () => csv('/api/reports/extra-dues/export', {}),
  // Daire raporu
  apartments: () => apiClient.get('/api/reports/apartments').then((r) => r.data),
  apartmentsCsv: () => csv('/api/reports/apartments/export', {}),
  // Gelir / Gider / Detaylı işlem
  income: (year: number) => apiClient.get('/api/reports/income', { params: { year } }).then((r) => r.data),
  incomeCsv: (year: number) => csv('/api/reports/income/export', { year }),
  expenses: (year: number) => apiClient.get('/api/reports/expenses', { params: { year } }).then((r) => r.data),
  expensesCsv: (year: number) => csv('/api/reports/expenses/export', { year }),
  transactions: (year: number) => apiClient.get('/api/reports/transactions', { params: { year } }).then((r) => r.data),
  transactionsCsv: (year: number) => csv('/api/reports/transactions/export', { year }),
  // Premium: yıllık bilanço PDF + veri yedeği ZIP
  balancePdf: (year: number) =>
    apiClient.get(`/api/reports/balance/${year}/pdf`, { responseType: 'arraybuffer' }).then((r) => r.data as ArrayBuffer),
  backup: () =>
    apiClient.get('/api/reports/backup', { responseType: 'arraybuffer' }).then((r) => r.data as ArrayBuffer),
};

/** CSV/Excel indir (arraybuffer) — yardımcı. */
function csv(url: string, params: Record<string, unknown>) {
  return apiClient
    .get(url, { params, responseType: 'arraybuffer' })
    .then((r) => r.data as ArrayBuffer);
}

export const subscriptionApi = {
  status: () => apiClient.get('/api/subscription/status').then((r) => r.data),
  verify: (store: string, payload: string, productId: string) =>
    apiClient.post('/api/subscription/verify', { store, receiptPayload: payload, productId }).then((r) => r.data),
  // Manuel premium aktifleştirme (yalnızca SuperAdmin — test/geliştirme).
  grant: (productId: string) =>
    apiClient.post('/api/subscription/grant', { productId }).then((r) => r.data),
};
