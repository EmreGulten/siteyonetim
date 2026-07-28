import React, { useEffect, useState } from 'react';
import { Alert, StyleSheet, View, ScrollView } from 'react-native';
import { Portal, Modal, Button, TextInput, Text, Chip, ActivityIndicator, Title, IconButton } from 'react-native-paper';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apartmentsApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { describeApiError } from '../utils/apiError';

interface BlockLite { id: string; name: string }

export function AddApartmentModal({
  visible, blocks, onClose, onSaved,
}: {
  visible: boolean;
  blocks: BlockLite[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const qc = useQueryClient();
  const [blockId, setBlockId] = useState<string>();
  const [dues, setDues] = useState('');
  const [door, setDoor] = useState('');
  const [floor, setFloor] = useState('1');

  // Tek blok varsa onu otomatik seç (Kaydet'in aktifleşmesi için blok zorunlu).
  useEffect(() => {
    if (!blockId && blocks.length === 1) setBlockId(blocks[0].id);
  }, [blocks, blockId]);
  const [owner, setOwner] = useState('');
  const [phone, setPhone] = useState('');

  // Yeni blok oluşturma (inline)
  const [newBlock, setNewBlock] = useState('');
  const createBlock = useMutation({
    mutationFn: () => apartmentsApi.createBlock(newBlock.trim()),
    onSuccess: (b: any) => { qc.invalidateQueries({ queryKey: ['blocks'] }); setBlockId(b.id); setNewBlock(''); },
    onError: (e) => Alert.alert('Blok oluşturulamadı', describeApiError(e)),
  });

  const create = useMutation({
    mutationFn: () => apartmentsApi.create({
      blockId: blockId!,
      monthlyDues: Number(dues) || 0,
      doorNumber: door.trim(),
      floor: Number(floor) || 0,
      ownerFullName: owner.trim() || undefined,
      ownerPhone: phone.trim() || undefined,
    }),
    onSuccess: () => {
      onSaved();
      setBlockId(undefined); setDues(''); setDoor(''); setOwner(''); setPhone('');
    },
    onError: (e) => Alert.alert('Daire eklenemedi', describeApiError(e)),
  });

  const canSave = blockId && door.trim().length > 0;

  return (
    <Portal>
      <Modal visible={visible} onDismiss={onClose} contentContainerStyle={styles.modal}>
        <Title style={styles.title}>Yeni Daire Ekle</Title>
        <ScrollView>
          <Text style={styles.label}>Blok seç *</Text>
          <View style={styles.chipRow}>
            {blocks.map((b) => (
              <Chip key={b.id} selected={blockId === b.id} onPress={() => setBlockId(b.id)}
                style={[styles.chip, blockId === b.id && { backgroundColor: colors.primary }]}
                textStyle={blockId === b.id ? { color: '#fff' } : undefined}>{b.name}</Chip>
            ))}
          </View>
          <View style={styles.inline}>
            <TextInput label="Yeni blok adı" value={newBlock} onChangeText={setNewBlock} mode="outlined" dense style={styles.flex} />
            <IconButton icon="plus-circle" iconColor={colors.primary} disabled={!newBlock.trim() || createBlock.isPending} onPress={() => createBlock.mutate()} />
          </View>
          {!blockId && (
            <Text style={styles.hint}>Kaydet için bir blok seçin veya yukarıdan yeni blok oluşturun.</Text>
          )}

          <Text style={styles.label}>Daire bilgileri</Text>
          <TextInput label="Kapı No *" value={door} onChangeText={setDoor} mode="outlined" style={styles.input} placeholder="Örn: 1, 2, 3A" />
          <TextInput label="Kat" value={floor} onChangeText={setFloor} mode="outlined" keyboardType="numeric" style={styles.input} />
          <TextInput label="Aylık Aidat (₺)" value={dues} onChangeText={setDues} mode="outlined" keyboardType="numeric" style={styles.input} placeholder="Örn: 500" />
          <TextInput label="Malik / Sakin adı" value={owner} onChangeText={setOwner} mode="outlined" style={styles.input} />
          <TextInput label="Telefon" value={phone} onChangeText={setPhone} mode="outlined" keyboardType="phone-pad" style={styles.input} />
        </ScrollView>
        <View style={styles.actions}>
          <Button onPress={onClose} mode="text">İptal</Button>
          <Button mode="contained" disabled={!canSave || create.isPending} onPress={() => create.mutate()} style={styles.save}>
            {create.isPending ? <ActivityIndicator color="#fff" /> : 'Kaydet'}
          </Button>
        </View>
      </Modal>
    </Portal>
  );
}

const styles = StyleSheet.create({
  modal: { backgroundColor: '#fff', margin: 20, borderRadius: 16, padding: 18, maxHeight: '88%' },
  title: { color: colors.primary, marginBottom: 8 },
  label: { color: colors.textMuted, fontSize: 13, marginTop: 10, marginBottom: 4 },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap' },
  chip: { marginRight: 8, marginBottom: 8 },
  inline: { flexDirection: 'row', alignItems: 'center' },
  flex: { flex: 1 },
  input: { marginBottom: 8 },
  hint: { color: colors.warning, fontSize: 12, marginTop: 2, marginBottom: 4 },
  actions: { flexDirection: 'row', justifyContent: 'flex-end', marginTop: 10, gap: 4 },
  save: { backgroundColor: colors.primary },
});
