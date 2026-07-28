import type { AxiosError } from 'axios';

/**
 * Bir API/mutasyon hatasını kullanıcıya gösterilecek Türkçe mesaja çevirir.
 * Amaç: "kayıt yapılamıyor" gibi belirsiz durumlar yerine gerçek sebebi göstermek
 * (örn. bağlantı yok, yetkisiz, geçersiz veri).
 */
export function describeApiError(e: unknown): string {
  const ax = e as AxiosError<any> | undefined;
  const status = ax?.response?.status;

  if (status === undefined || status === 0) {
    return 'Sunucuya ulaşılamadı. İnternet bağlantısını veya backend tünelini kontrol edin.';
  }

  const detail: string | undefined =
    ax?.response?.data?.title || ax?.response?.data?.message || ax?.response?.data?.error;

  // 4xx: backend'in kendi Türkçe açıklaması varsa onu göster (409 uyarıları dahil).
  if (status >= 400 && status < 500) {
    if (detail) return detail;
    if (status === 401) return 'Oturum süresi dolmuş. Çıkış yapıp tekrar giriş yapın.';
    if (status === 403) return 'Bu işlem için yetkiniz yok.';
    if (status === 404) return 'Kayıt bulunamadı.';
    return 'İstek reddedildi (' + status + ').';
  }
  return 'Sunucu hatası (' + status + (detail ? ' — ' + detail : '') + ').';
}
