export const colors = {
  // Referans uygulama paleti (1.png)
  primary: '#3498db',      // ana mavi
  primaryDark: '#2980b9',
  accent: '#9b59b6',       // mor vurgu

  // Durum renkleri
  success: '#2ecc71',      // tahsil/ödendi (yeşil)
  warning: '#f39c12',      // uyarı/gider (turuncu)
  danger: '#e74c3c',       // borç (kırmızı)
  info: '#3498db',

  // Yüzey
  bg: '#f8f9fa',           // açık gri bölüm arka planı
  card: '#ffffff',
  text: '#2c3e50',
  textMuted: '#7f8c8d',
  border: '#ecf0f1',

  // Pastel kart arka planları (referans 1.png)
  pastel: {
    purple: '#e8daef',     // Toplam Daire / Ödeme Oranı
    green: '#d5f5e3',      // Gelir / Kira&Diğer
    blue: '#d6eaf8',       // Net Bakiye
    pink: '#fadbd8',       // Ek Aidatlar
    orange: '#fdebd0',     // Giderler
  },
};

export const API_BASE_URL =
  (process.env.EXPO_PUBLIC_API_BASE_URL as string) || 'https://api.dairom.site';
