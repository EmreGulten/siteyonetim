import * as FileSystem from 'expo-file-system';
import * as Sharing from 'expo-sharing';

/**
 * PDF/CSV byte dizisini (arraybuffer) indirir, dosyaya yazar ve paylaşım ekranı açar.
 * İndirme sırasında progress callback ile ilerleme yüzdesi bildirilir (Raporlar progress bar'ı).
 */
export async function downloadAndShare(
  bytes: ArrayBuffer,
  filename: string,
  mimetype: string,
  onProgress?: (pct: number) => void
) {
  const fileUri = `${FileSystem.cacheDirectory}${filename}`;
  // arraybuffer → base64 (RN FileSystem string bekler)
  const base64 = arrayBufferToBase64(bytes);

  onProgress?.(20);
  await FileSystem.writeAsStringAsync(fileUri, base64, {
    encoding: FileSystem.EncodingType.Base64,
  });
  onProgress?.(80);

  if (await Sharing.isAvailableAsync()) {
    await Sharing.shareAsync(fileUri, { mimeType: mimetype, dialogTitle: filename });
  }
  onProgress?.(100);
}

function arrayBufferToBase64(buffer: ArrayBuffer): string {
  let binary = '';
  const bytes = new Uint8Array(buffer);
  const chunk = 0x8000;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunk));
  }
  // @ts-ignore — btoa RN'de global
  return globalThis.btoa ? globalThis.btoa(binary) : base64Polyfill(binary);
}

function base64Polyfill(binary: string): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
  let out = '';
  for (let i = 0; i < binary.length; i += 3) {
    const b1 = binary.charCodeAt(i) & 0xff;
    const b2 = binary.charCodeAt(i + 1) & 0xff;
    const b3 = binary.charCodeAt(i + 2) & 0xff;
    out += chars[b1 >> 2] + chars[((b1 & 3) << 4) | (b2 >> 4)]
         + (isNaN(b2) ? '=' : chars[((b2 & 15) << 2) | (b3 >> 6)])
         + (isNaN(b3) ? '=' : chars[b3 & 63]);
  }
  return out;
}
