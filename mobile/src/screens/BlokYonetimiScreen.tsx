import React, { useState } from 'react';
import { Alert, FlatList, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Card, TextInput, Title, ActivityIndicator } from 'react-native-paper';
import { MaterialCommunityIcons } from '@expo/vector-icons';
import { apartmentsApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { useRequireAuth } from '../hooks/useRequireAuth';
import { BannerAdSlot } from '../components/BannerAdSlot';
import { describeApiError } from '../utils/apiError';

/** 2.png — Blok/Bina Yönetimi: ekleme formu + blok listesi (silme dahil). */
export function BlokYonetimiScreen() {
  const qc = useQueryClient();
  const role = useAuthStore((s) => s.user?.role);
  // Backend rol enum'u sayı (0=SuperAdmin,1=SiteManager) olarak da gelebilir.
  const canManage = role === 'SiteManager' || role === 'SuperAdmin' || role === 1 || role === 0;
  const requireAuth = useRequireAuth();
  const [name, setName] = useState('');
  const [order, setOrder] = useState('1');
  const blocks = useQuery({ queryKey: ['blocks'], queryFn: apartmentsApi.blocks });

  const add = useMutation({
    mutationFn: () => apartmentsApi.createBlock(name.trim(), Number(order) || 1),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['blocks'] }); setName(''); },
    onError: (e) => Alert.alert('Blok eklenemedi', describeApiError(e)),
  });

  const del = useMutation({
    mutationFn: (id: string) => apartmentsApi.removeBlock(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['blocks'] }),
    onError: (e) => Alert.alert('Blok silinemedi', describeApiError(e)),
  });

  // Silme: boş blok silinir; içinde daire varsa backend 409 döner ve uyarı mesajı gösterilir.
  const onDelete = (item: any) =>
    Alert.alert('Blok Sil', `"${item.name}" silinsin mi?`, [
      { text: 'İptal', style: 'cancel' },
      { text: 'Sil', style: 'destructive', onPress: () => del.mutate(item.id) },
    ]);

  return (
    <View style={styles.root}>
      <Title style={styles.title}>Blok / Bina Yönetimi</Title>
      <BannerAdSlot placement="blok" />

      <Card style={styles.card}>
        <Text style={styles.sectionTitle}>Blok Bilgileri</Text>
        <TextInput label="Blok Adı *" value={name} onChangeText={setName} mode="outlined" placeholder="Örn: A Blok" style={styles.input} />
        <TextInput label="Sıra" value={order} onChangeText={setOrder} mode="outlined" keyboardType="numeric" style={styles.input} />
        <Button mode="contained" disabled={!name.trim() || add.isPending} onPress={() => { if (!requireAuth()) return; add.mutate(); }} style={styles.btn}>
          {add.isPending ? <ActivityIndicator color="#fff" /> : 'Kaydet'}
        </Button>
      </Card>

      <Text style={styles.sectionTitle}>Blok Listesi</Text>
      <FlatList
        data={blocks.data ?? []}
        refreshing={blocks.isFetching}
        onRefresh={blocks.refetch}
        keyExtractor={(i: any) => i.id}
        ListEmptyComponent={<Text style={styles.empty}>Henüz blok yok.</Text>}
        renderItem={({ item }: any) => (
          <Card style={styles.row}>
            <View style={styles.rowInner}>
              <View style={{ flex: 1 }}>
                <Text style={styles.bname}>{item.name}</Text>
                <Text style={styles.muted}>Sıra: {item.displayOrder}</Text>
              </View>
              {canManage && (
                <TouchableOpacity
                  onPress={() => onDelete(item)}
                  hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
                  style={styles.delBtn}
                  accessibilityLabel="Blok sil"
                >
                  <MaterialCommunityIcons name="trash-can-outline" size={20} color={colors.danger} />
                </TouchableOpacity>
              )}
            </View>
          </Card>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { color: colors.primary, marginBottom: 4 },
  card: { borderRadius: 14, padding: 14, marginBottom: 16 },
  sectionTitle: { fontSize: 14, fontWeight: '700', color: colors.text, marginVertical: 8 },
  input: { marginBottom: 10 },
  btn: { backgroundColor: colors.primary, marginTop: 4 },
  row: { borderRadius: 12, padding: 14, marginBottom: 8 },
  rowInner: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  bname: { fontSize: 16, fontWeight: '600', color: colors.text },
  delBtn: { paddingLeft: 8, paddingVertical: 2, justifyContent: 'center' },
  muted: { fontSize: 12, color: colors.textMuted },
  empty: { textAlign: 'center', color: colors.textMuted, marginTop: 20 },
});
