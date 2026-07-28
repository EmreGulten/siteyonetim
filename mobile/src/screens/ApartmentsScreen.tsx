import React, { useState } from 'react';
import { Alert, FlatList, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Card, Chip, FAB, Title } from 'react-native-paper';
import { MaterialCommunityIcons } from '@expo/vector-icons';
import { apartmentsApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { useRequireAuth } from '../hooks/useRequireAuth';
import { BannerAdSlot } from '../components/BannerAdSlot';
import { AddApartmentModal } from '../components/AddApartmentModal';

export function ApartmentsScreen() {
  const navigation = useNavigation();
  const qc = useQueryClient();
  const role = useAuthStore((s) => s.user?.role);
  const isGuest = !useAuthStore((s) => s.token);
  // Backend rol enum'u sayı (0=SuperAdmin,1=SiteManager) olarak gelir — ikisini de kabul et
  const canManage = role === 'SiteManager' || role === 'SuperAdmin' || role === 1 || role === 0;
  const requireAuth = useRequireAuth();
  // Ekleme butonu: misafir de görsün (tıklayınca auth modalı açılır), Resident gizli kalsın.
  const canAdd = isGuest || canManage;
  const [blockId, setBlockId] = useState<string | undefined>();
  const [showAdd, setShowAdd] = useState(false);

  const blocks = useQuery({ queryKey: ['blocks'], queryFn: apartmentsApi.blocks });
  const list = useQuery({
    queryKey: ['apartments', blockId],
    queryFn: () => apartmentsApi.list({ page: 1, blockId }),
  });

  const del = useMutation({
    mutationFn: (id: string) => apartmentsApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['apartments'] }),
  });
  const onDelete = (item: any) =>
    Alert.alert('Dairei sil', `${item.blockName} / Daire ${item.doorNumber} silinsin mi?`, [
      { text: 'İptal', style: 'cancel' },
      { text: 'Sil', style: 'destructive', onPress: () => del.mutate(item.id) },
    ]);

  return (
    <View style={styles.root}>
      <Title style={styles.title}>Daireler</Title>
      <BannerAdSlot placement="daireler" />

      {/* Blok bazlı filtreleme */}
      <FlatList
        horizontal
        data={[{ id: undefined, name: 'Tümü' }, ...(blocks.data ?? [])]}
        keyExtractor={(item: any) => item.id ?? 'all'}
        renderItem={({ item }: any) => (
          <Chip
            selected={blockId === item.id}
            onPress={() => setBlockId(item.id)}
            style={[styles.chip, blockId === item.id && { backgroundColor: colors.primary }]}
            textStyle={blockId === item.id ? { color: '#fff' } : undefined}
          >
            {item.name}
          </Chip>
        )}
        style={[styles.chipRow, { height: 40, flexGrow: 0 }]}
        contentContainerStyle={{ flexGrow: 0 }}
      />

      <FlatList
        data={list.data?.items ?? []}
        refreshing={list.isFetching}
        onRefresh={list.refetch}
        keyExtractor={(i: any) => i.id}
        ListEmptyComponent={<Text style={styles.empty}>Daire bulunamadı.</Text>}
        contentContainerStyle={{ paddingBottom: 80, flexGrow: 0 }}
        style={{ flex: 1 }}
        renderItem={({ item }: any) => (
          <TouchableOpacity onPress={() => navigation.navigate('ApartmentDetail', { apartmentId: item.id, label: `${item.blockName} / ${item.doorNumber}` })}>
            <Card style={styles.card}>
              <View style={styles.row}>
                <View style={styles.flex}>
                  <Text style={styles.door}>{item.blockName} / Daire {item.doorNumber}</Text>
                  <Text style={styles.meta}>{item.monthlyDues ? `Aidat ₺${item.monthlyDues} · ` : ''}Kat {item.floor}</Text>
                  {item.ownerName ? <Text style={styles.owner}>👤 {item.ownerName}</Text> : null}
                </View>
                {canManage && (
                  <TouchableOpacity
                    onPress={() => onDelete(item)}
                    hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
                    style={styles.delBtn}
                    accessibilityLabel="Daire sil"
                  >
                    <MaterialCommunityIcons name="trash-can-outline" size={20} color={colors.danger} />
                  </TouchableOpacity>
                )}
              </View>
            </Card>
          </TouchableOpacity>
        )}
      />

      {canAdd && (
        <FAB style={styles.fab} icon="plus" color="#fff" onPress={() => { if (!requireAuth()) return; setShowAdd(true); }} />
      )}

      <AddApartmentModal
        visible={showAdd}
        blocks={blocks.data ?? []}
        onClose={() => setShowAdd(false)}
        onSaved={() => {
          setShowAdd(false);
          qc.invalidateQueries({ queryKey: ['apartments'] });
        }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { color: colors.primary, marginBottom: 8 },
  chipRow: { marginBottom: 8 },
  chip: { marginRight: 8 },
  card: { borderRadius: 12, padding: 14, marginBottom: 10 },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  flex: { flex: 1 },
  delBtn: { paddingLeft: 8, paddingVertical: 2, justifyContent: 'center' },
  door: { fontSize: 16, fontWeight: '600', color: colors.text },
  meta: { fontSize: 12, color: colors.textMuted, marginTop: 2 },
  owner: { fontSize: 12, color: colors.text, marginTop: 4 },
  status: { fontSize: 12, fontWeight: '600' },
  empty: { textAlign: 'center', color: colors.textMuted, marginTop: 30 },
  fab: { position: 'absolute', margin: 16, right: 0, bottom: 0, backgroundColor: colors.primary },
});
