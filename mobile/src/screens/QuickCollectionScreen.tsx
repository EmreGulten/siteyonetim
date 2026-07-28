import React, { useState } from 'react';
import { Alert, StyleSheet, Text, View } from 'react-native';
import { Button, Card, TextInput, Title, ActivityIndicator, Snackbar } from 'react-native-paper';
import { useMutation } from '@tanstack/react-query';
import { duesApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { downloadAndShare } from '../utils/download';
import { useRequireAuth } from '../hooks/useRequireAuth';
import { useInterstitialAd } from '../hooks/useAds'; // FAZ 5: öncesi reklam

/**
 * Hızlı Tahsilat: daireyi bul (manuel kapı no), tutarı gir, "Ödendi" yap,
 * ardından PDF makbuzu indir. Makbuz öncesi interstitial reklam (FAZ 5).
 */
export function QuickCollectionScreen() {
  const requireAuth = useRequireAuth();
  const [duesId, setDuesId] = useState('');
  const [amount, setAmount] = useState('');
  const [snack, setSnack] = useState<string | null>(null);
  const interstitial = useInterstitialAd('collect');

  const collect = useMutation({
    mutationFn: () => duesApi.collect(duesId, Number(amount), true),
    onSuccess: async (data: any) => {
      setSnack('Tahsilat başarılı ✅');
      // Makbuz PDF'ini indir
      if (data?.receiptPdf) {
        await downloadAndShare(
          base64ToArrayBuffer(data.receiptPdf),
          `makbuz-${data.dues?.year}${String(data.dues?.month).padStart(2, '0')}.pdf`,
          'application/pdf'
        );
      }
    },
    onError: () => setSnack('Tahsilat başarısız.'),
  });

  const onSubmit = () => {
    if (!requireAuth()) return;
    if (!duesId || !amount) return Alert.alert('Eksik', 'Aidat ID ve tutar girin.');
    // FAZ 5: işlem öncesi interstitial reklam göster, kapandığında devam et.
    interstitial.showThen(() => collect.mutate());
  };

  return (
    <View style={styles.root}>
      <Title style={styles.title}>Hızlı Tahsilat</Title>
      <Card style={styles.card}>
        <Text style={styles.label}>Aidat / Daire bul</Text>
        <TextInput label="Aidat ID" value={duesId} onChangeText={setDuesId} mode="outlined" style={styles.input} />
        <TextInput label="Tutar (₺)" value={amount} onChangeText={setAmount}
          mode="outlined" keyboardType="numeric" style={styles.input} />
        <Button mode="contained" onPress={onSubmit} disabled={collect.isPending}
          style={styles.button} contentStyle={{ paddingVertical: 6 }}>
          {collect.isPending ? <ActivityIndicator color="#fff" /> : 'Tahsil Et + Makbuz'}
        </Button>
      </Card>
      <Snackbar visible={!!snack} onDismiss={() => setSnack(null)} duration={2500}>{snack}</Snackbar>
    </View>
  );
}

// Backend PDF'i base64 string de dönebilir (alternatif yol).
function base64ToArrayBuffer(b64: string): ArrayBuffer {
  const binary = globalThis.atob ? globalThis.atob(b64) : b64;
  const len = binary.length;
  const bytes = new Uint8Array(len);
  for (let i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
  return bytes.buffer;
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { color: colors.primary, marginBottom: 12 },
  card: { borderRadius: 14, padding: 16 },
  label: { color: colors.textMuted, marginBottom: 8, fontSize: 13 },
  input: { marginBottom: 12 },
  button: { backgroundColor: colors.primary },
});
