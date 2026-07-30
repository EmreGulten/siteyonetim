import React, { useState } from 'react';
import { Alert, FlatList, StyleSheet, Text, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Card, Chip, TextInput, Title, ActivityIndicator } from 'react-native-paper';
import { financeApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { useRequireAuth } from '../hooks/useRequireAuth';
import { useAuthStore } from '../store/authStore';
import { BannerAdSlot } from './BannerAdSlot';

const CATS = {
  Income: ['Kira', 'Aidat', 'Diğer'],
  Expense: ['Elektrik', 'Su', 'Doğalgaz', 'Personel', 'Bakım', 'Onarım', 'Diğer'],
};

/**
 * expo-image-picker native module'ü lazy yükler. Expo Go'da module yoksa (SDK
 * uyumsuzluğu) null döner → fatura özelliği "geliştirme derlemesi gerekir" uyarısı
 * verir, ekran crash olmaz. Gerçek build'te (expo run:ios / EAS) module mevcut.
 */
function getImagePicker(): any {
  try { return require('expo-image-picker'); } catch { return null; }
}

/** 6.png / 7.png — Gelir/Gider yönetimi: ekleme formu + liste. */
export function TransactionsView({ type, title }: { type: 'Income' | 'Expense'; title: string }) {
  const qc = useQueryClient();
  const requireAuth = useRequireAuth();
  const accent = type === 'Income' ? colors.success : colors.warning;
  const [amount, setAmount] = useState('');
  const [category, setCategory] = useState('');
  const [desc, setDesc] = useState('');
  const [docImage, setDocImage] = useState<string | null>(null);

  const pickImage = async () => {
    const ImagePicker = getImagePicker();
    if (!ImagePicker) { Alert.alert('Fatura ekleme', 'Bu özellik geliştirme derlemesinde çalışır (Expo Go değil).'); return; }
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') { Alert.alert('İzin gerekli', 'Galeriye erişim izni gerekli.'); return; }
    const res = await ImagePicker.launchImageLibraryAsync({ mediaTypes: ImagePicker.MediaTypeOptions.Images, quality: 0.7 });
    if (!res.canceled) setDocImage(res.assets[0].uri);
  };

  const premium = useAuthStore((s) => s.user?.isPremium);

  // Kamera ile fatura/fiş çekme — Premium'a özel.
  const pickFromCamera = async () => {
    if (!premium) { Alert.alert('Premium gerekli', 'Kamera ile fatura eklemek bir Premium özelliktir.'); return; }
    const ImagePicker = getImagePicker();
    if (!ImagePicker) { Alert.alert('Fatura ekleme', 'Bu özellik geliştirme derlemesinde çalışır (Expo Go değil).'); return; }
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') { Alert.alert('İzin gerekli', 'Kamera erişim izni gerekli.'); return; }
    const res = await ImagePicker.launchCameraAsync({ mediaTypes: ImagePicker.MediaTypeOptions.Images, quality: 0.7 });
    if (!res.canceled) setDocImage(res.assets[0].uri);
  };

  const showFaturaPicker = () => {
    Alert.alert('Fatura / Fiş Ekle', undefined, [
      { text: '🖼️ Galeriden Seç', onPress: pickImage },
      { text: premium ? '📷 Kamera ile Çek' : '📷 Kamera (Premium)', onPress: pickFromCamera },
      { text: 'İptal', style: 'cancel' },
    ]);
  };

  const list = useQuery({
    queryKey: ['transactions', type],
    queryFn: () => financeApi.transactions({ type, page: 1 }),
  });
  const add = useMutation({
    mutationFn: async () => {
      if (docImage) {
        const fd = new FormData();
        fd.append('Type', type);
        fd.append('Category', category || 'Diğer');
        if (desc) fd.append('Description', desc);
        fd.append('Amount', String(Number(amount)));
        fd.append('Date', new Date().toISOString());
        fd.append('document', { uri: docImage, name: 'fatura.jpg', type: 'image/jpeg' } as any);
        return financeApi.addTransaction(fd);
      }
      return financeApi.add({ type, category: category || 'Diğer', description: desc || undefined, amount: Number(amount), date: new Date().toISOString() });
    },
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['transactions'] }); setAmount(''); setDesc(''); setCategory(''); setDocImage(null); },
    onError: (e: any) => Alert.alert('Eklenemedi', e?.response?.data?.detail || e?.response?.data?.error || 'Tekrar deneyin.'),
  });

  const fmt = (v: number) => `₺ ${(v ?? 0).toLocaleString('tr-TR')}`;
  const total = (list.data?.items ?? []).reduce((s: number, t: any) => s + (t.amount || 0), 0);

  return (
    <View style={styles.root}>
      <Title style={styles.title}>{title}</Title>
      <BannerAdSlot placement={type === 'Income' ? 'gelirler' : 'giderler'} />

      <Card style={[styles.card, { borderLeftColor: accent, borderLeftWidth: 4 }]}>
        <Text style={styles.sectionTitle}>Yeni {type === 'Income' ? 'Gelir' : 'Gider'} Ekle</Text>
        <TextInput label="Tutar (₺)" value={amount} onChangeText={setAmount} mode="outlined" keyboardType="numeric" style={styles.input} />
        <Text style={styles.label}>Kategori</Text>
        <View style={styles.chips}>
          {CATS[type].map((c) => (
            <Chip key={c} selected={category === c} onPress={() => setCategory(c)}
              style={[styles.chipBase, category === c && { backgroundColor: colors.primary }]}
              textStyle={category === c ? { color: '#fff' } : undefined}>{c}</Chip>
          ))}
        </View>
        <TextInput label="Açıklama" value={desc} onChangeText={setDesc} mode="outlined" style={styles.input} />
        <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 10 }}>
          <Button
            mode="outlined"
            icon={premium ? 'camera' : 'crown-outline'}
            onPress={() => premium ? showFaturaPicker() : Alert.alert('Premium gerekli', 'Fatura/fiş ekleme bir Premium özelliktir.')}
            textColor={colors.primary}
          >
            {docImage ? 'Fatura Seçildi ✓' : premium ? 'Fatura Ekle (opsiyonel)' : 'Fatura (Premium)'}
          </Button>
          {docImage && <Button mode="text" onPress={() => setDocImage(null)} textColor={colors.danger} labelStyle={{ fontSize: 12 }}>Kaldır</Button>}
        </View>
        <Button mode="contained" disabled={!amount || add.isPending} onPress={() => { if (!requireAuth()) return; add.mutate(); }} style={[styles.btn, { backgroundColor: accent }]}>
          {add.isPending ? <ActivityIndicator color="#fff" /> : 'Kaydet'}
        </Button>
      </Card>

      <View style={styles.sumBar}>
        <Text style={styles.sumLabel}>{type === 'Income' ? 'Toplam Gelir' : 'Toplam Gider'}</Text>
        <Text style={[styles.sumVal, { color: accent }]}>{fmt(total)}</Text>
      </View>

      <FlatList
        data={list.data?.items ?? []}
        refreshing={list.isFetching}
        onRefresh={list.refetch}
        keyExtractor={(i: any) => i.id}
        ListEmptyComponent={<Text style={styles.empty}>Kayıt yok.</Text>}
        renderItem={({ item }: any) => (
          <Card style={styles.row}>
            <View style={styles.rowInner}>
              <View>
                <Text style={styles.cat}>{item.category}</Text>
                <Text style={styles.muted}>{item.description} · {new Date(item.date).toLocaleDateString('tr-TR')}</Text>
              </View>
              <Text style={[styles.amt, { color: accent }]}>{type === 'Income' ? '+' : '−'}{fmt(item.amount)}</Text>
            </View>
          </Card>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 14 },
  title: { color: colors.primary, marginBottom: 8 },
  card: { borderRadius: 14, padding: 14, marginBottom: 12 },
  sectionTitle: { fontSize: 14, fontWeight: '700', color: colors.text, marginBottom: 8 },
  label: { fontSize: 12, color: colors.textMuted, marginBottom: 4 },
  input: { marginBottom: 10 },
  chips: { flexDirection: 'row', flexWrap: 'wrap', marginBottom: 8 },
  chipBase: { marginRight: 6, marginBottom: 6, backgroundColor: '#eef2f5' },
  btn: { marginTop: 2 },
  sumBar: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', backgroundColor: '#fff', borderRadius: 12, padding: 12, marginBottom: 8 },
  sumLabel: { fontSize: 13, color: colors.textMuted, fontWeight: '600' },
  sumVal: { fontSize: 18, fontWeight: '800' },
  row: { borderRadius: 12, padding: 12, marginBottom: 8 },
  rowInner: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  cat: { fontSize: 15, fontWeight: '600', color: colors.text },
  muted: { fontSize: 11, color: colors.textMuted },
  amt: { fontSize: 15, fontWeight: '700' },
  empty: { textAlign: 'center', color: colors.textMuted, marginTop: 20 },
});
