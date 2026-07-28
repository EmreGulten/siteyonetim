import { Alert, Linking, Platform } from 'react-native';

/**
 * WhatsApp / SMS derin bağlantı yardımcıları.
 * Premium özellik: borçlu dairelere hatırlatma göndermek için kullanılır.
 * Backend mesajlaşma entegrasyonu gerektirmez — cihazdaki uygulamayı açar.
 */

/** Türk telefonunu wa.me için uluslararası formata çevirir (0555... -> 90555...). */
export function normalizePhone(raw?: string | null): string | null {
  if (!raw) return null;
  const d = raw.replace(/\D/g, '');
  if (!d) return null;
  if (d.startsWith('90')) return d;
  if (d.startsWith('0')) return '9' + d;
  if (d.length === 10) return '90' + d; // başında 0 yok (555...)
  return d;
}

/** WhatsApp'ı açar; numara yoksa paylaşım seçici (telefon seç) açar. */
export async function openWhatsApp(phone: string | null | undefined, message: string) {
  const n = normalizePhone(phone);
  const url = n
    ? `https://wa.me/${n}?text=${encodeURIComponent(message)}`
    : `https://wa.me/?text=${encodeURIComponent(message)}`;
  await open(url, 'WhatsApp');
}

/** Varsayılan SMS uygulamasını açar (numara + gövde önceden dolu). */
export async function openSms(phone: string, message: string) {
  // iOS: `&body=`, Android: `?body=` ayracı kullanır.
  const sep = Platform.OS === 'ios' ? '&' : '?';
  const url = `sms:${phone}${sep}body=${encodeURIComponent(message)}`;
  await open(url, 'SMS');
}

/** WhatsApp / SMS seçenekli hatırlatma diyaloğu gösterir. */
export function remindDebt(phone: string | null | undefined, label: string, message: string) {
  Alert.alert(
    'Borç Hatırlatması',
    `${label}${phone ? `\n${phone}` : ''}`,
    [
      { text: 'İptal', style: 'cancel' },
      { text: 'WhatsApp', onPress: () => openWhatsApp(phone, message) },
      { text: 'SMS', onPress: () => phone ? openSms(phone, message) : Alert.alert('Telefon yok', 'Bu daire için telefon kaydı bulunamadı.') },
    ],
  );
}

async function open(url: string, label: string) {
  try {
    await Linking.openURL(url);
  } catch {
    Alert.alert(`${label} açılamadı`, 'İlgili uygulama cihazda yüklü olmayabilir.');
  }
}
