import React, { useState } from 'react';
import { FlatList, StyleSheet, Text, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Card, Chip, TextInput, Title, ActivityIndicator } from 'react-native-paper';
import { financeApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { useRequireAuth } from '../hooks/useRequireAuth';
import { BannerAdSlot } from './BannerAdSlot';

const CATS = {
  Income: ['Kira', 'Aidat', 'Diğer'],
  Expense: ['Elektrik', 'Su', 'Doğalgaz', 'Personel', 'Bakım', 'Onarım', 'Diğer'],
};

/** 6.png / 7.png — Gelir/Gider yönetimi: ekleme formu + liste. */
export function TransactionsView({ type, title }: { type: 'Income' | 'Expense'; title: string }) {
  const qc = useQueryClient();
  const requireAuth = useRequireAuth();
  const accent = type === 'Income' ? colors.success : colors.warning;
  const [amount, setAmount] = useState('');
  const [category, setCategory] = useState('');
  const [desc, setDesc] = useState('');

  const list = useQuery({
    queryKey: ['transactions', type],
    queryFn: () => financeApi.transactions({ type, page: 1 }),
  });
  const add = useMutation({
    mutationFn: () => financeApi.add({ type, category: category || 'Diğer', description: desc || undefined, amount: Number(amount), date: new Date().toISOString() }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['transactions'] }); setAmount(''); setDesc(''); setCategory(''); },
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
