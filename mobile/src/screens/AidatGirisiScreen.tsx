import React, { useState } from 'react';
import { Alert, FlatList, StyleSheet, Text, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Card, Chip, IconButton, Modal, TextInput, Title } from 'react-native-paper';
import { duesApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { useRequireAuth } from '../hooks/useRequireAuth';
import { BannerAdSlot } from '../components/BannerAdSlot';

const MONTHS = ['Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu','Eyl','Eki','Kas','Ara'];

/** 4.png — Aidat Girişi: ay navigasyonu + aidat listesi + tahsilat/üretim. */
export function AidatGirisiScreen() {
  const qc = useQueryClient();
  const requireAuth = useRequireAuth();
  const [offset, setOffset] = useState(0);
  const [editing, setEditing] = useState<{ id: string; label: string } | null>(null);
  const [editAmount, setEditAmount] = useState('');
  const d = new Date(); d.setMonth(d.getMonth() + offset);
  const year = d.getFullYear(); const month = d.getMonth() + 1;

  const dues = useQuery({ queryKey: ['dues', year, month], queryFn: () => duesApi.list({ year, month }) });
  const gen = useMutation({ mutationFn: () => duesApi.generate(year, month), onSuccess: () => qc.invalidateQueries({ queryKey: ['dues'] }) });
  const collect = useMutation({
    mutationFn: (x: { id: string; amount: number }) => duesApi.collect(x.id, x.amount, false),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dues'] }),
  });
  const update = useMutation({
    mutationFn: (x: { id: string; amount: number }) => duesApi.update(x.id, x.amount),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['dues'] }); setEditing(null); },
  });

  const fmt = (v: number) => `₺ ${(v ?? 0).toLocaleString('tr-TR')}`;

  const onCollect = (item: any) => {
    if (!requireAuth()) return;
    Alert.alert('Tahsilat', `${item.apartmentLabel ?? 'Daire'} — Kalan: ${fmt(item.remaining)}\nTamamını tahsil et?`, [
      { text: 'İptal' },
      { text: 'Tahsil Et', onPress: () => collect.mutate({ id: item.id, amount: item.remaining }), style: 'default' },
    ]);
  };

  const startEdit = (item: any) => {
    setEditing({ id: item.id, label: item.apartmentLabel || 'Daire' });
    setEditAmount(String(item.amount ?? 0));
  };

  const saveEdit = () => {
    if (!requireAuth()) return;
    const amount = Number((editAmount || '').replace(',', '.'));
    if (!editing || isNaN(amount) || amount < 0) return;
    update.mutate({ id: editing.id, amount });
  };

  const statusColor = (s: string) => s === 'Paid' ? colors.success : s === 'PartiallyPaid' ? colors.warning : colors.danger;
  const statusText = (s: string) => s === 'Paid' ? 'Ödendi' : s === 'PartiallyPaid' ? 'Kısmi' : 'Bekliyor';

  return (
    <View style={styles.root}>
      <View style={styles.monthBar}>
        <IconButton icon="chevron-left" onPress={() => setOffset((o) => o - 1)} />
        <Text style={styles.monthText}>{MONTHS[d.getMonth()]} {year}</Text>
        <IconButton icon="chevron-right" onPress={() => setOffset((o) => o + 1)} disabled={offset >= 0} />
      </View>

      <BannerAdSlot placement="aidat" />

      <Button mode="contained" icon="cached" loading={gen.isPending} disabled={gen.isPending}
        onPress={() => { if (!requireAuth()) return; gen.mutate(); }} style={styles.genBtn}>Bu Ayın Aidatını Üret</Button>

      <View style={styles.headerRow}>
        <Text style={[styles.hCol, styles.hApartment]}>Daire</Text>
        <Text style={[styles.hCol, styles.hStatus]}>Durum</Text>
        <Text style={[styles.hCol, styles.hAmount]}>Tutar</Text>
        <Text style={[styles.hCol, styles.hAction]}>İşlem</Text>
      </View>

      <FlatList
        data={dues.data?.items ?? []}
        refreshing={dues.isFetching}
        onRefresh={dues.refetch}
        keyExtractor={(i: any) => i.id}
        ListEmptyComponent={<Text style={styles.empty}>Bu ay için aidat yok. "Üret" ile oluşturun.</Text>}
        renderItem={({ item }: any) => (
          <View style={styles.row}>
            <Text style={[styles.hCol, styles.hApartment, styles.cell]} numberOfLines={1}>{item.apartmentLabel || `${item.year}-${item.month}`}</Text>
            <View style={[styles.hCol, styles.hStatus]}><Chip textStyle={{ color: statusColor(item.status), fontSize: 10 }} style={styles.chip}>{statusText(item.status)}</Chip></View>
            <Text style={[styles.hCol, styles.hAmount, styles.cell]}>{fmt(item.amount)}</Text>
            <View style={[styles.hCol, styles.hAction, styles.actionCell]}>
              {item.remaining > 0 ? (
                <Button compact mode="text" labelStyle={{ color: colors.success, fontSize: 11 }} onPress={() => onCollect(item)}>Tahsil</Button>
              ) : <Text style={styles.ok}>✓</Text>}
              <IconButton icon="pencil" size={18} iconColor={colors.primary} onPress={() => startEdit(item)} />
            </View>
          </View>
        )}
      />

      <Modal visible={editing !== null} onDismiss={() => setEditing(null)} contentContainerStyle={styles.modal}>
        <Card>
          <Card.Title title="Aidat Tutarı Düzenle" subtitle={`${editing?.label ?? ''} · ${MONTHS[d.getMonth()]} ${year}`} titleStyle={{ color: colors.primary }} />
          <Card.Content>
            <TextInput
              label="Tutar (₺)"
              value={editAmount}
              onChangeText={setEditAmount}
              mode="outlined"
              keyboardType="numeric"
              autoFocus
            />
            <Text style={styles.modalHint}>Yalnızca bu aya uygulanır. Sonraki aylar dairenin aylık aidatını kullanır.</Text>
          </Card.Content>
          <Card.Actions>
            <Button onPress={() => setEditing(null)}>İptal</Button>
            <Button mode="contained" onPress={saveEdit} loading={update.isPending} disabled={update.isPending}>Kaydet</Button>
          </Card.Actions>
        </Card>
      </Modal>
    </View>
  );
}

const col = { color: colors.text };
const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 12 },
  monthBar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center' },
  monthText: { fontSize: 16, fontWeight: '700', color: colors.primary, minWidth: 130, textAlign: 'center' },
  genBtn: { backgroundColor: colors.primary, marginVertical: 8 },
  headerRow: { flexDirection: 'row', backgroundColor: colors.primary, borderRadius: 8, padding: 8, marginTop: 6 },
  row: { flexDirection: 'row', alignItems: 'center', padding: 8, borderBottomWidth: 1, borderBottomColor: colors.border, backgroundColor: '#fff' },
  hCol: { ...col, fontSize: 12 },
  hApartment: { flex: 3 },
  hStatus: { flex: 2 },
  hAmount: { flex: 2 },
  hAction: { flex: 3 },
  actionCell: { flexDirection: 'row', alignItems: 'center', justifyContent: 'flex-end' },
  cell: { color: colors.text },
  chip: { backgroundColor: 'transparent', height: 24 },
  ok: { color: colors.success, fontWeight: '700' },
  empty: { textAlign: 'center', color: colors.textMuted, marginTop: 20 },
  modal: { padding: 16 },
  modalHint: { fontSize: 11, color: colors.textMuted, marginTop: 8 },
});
