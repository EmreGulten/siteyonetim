import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Card, Title } from 'react-native-paper';
import { colors } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { BannerAdSlot } from '../components/BannerAdSlot';

/** 5.png — Ek Aidatlar: Premium özellik (backend CRUD + satın alma sonrası aktif). */
export function EkAidatScreen() {
  const premium = useAuthStore((s) => s.user?.isPremium);
  return (
    <View style={styles.root}>
      <Title style={styles.title}>Ek Aidat Yönetimi</Title>
      <BannerAdSlot placement="ekaidat" />
      <Card style={[styles.card, { backgroundColor: colors.pastel.pink }]}>
        <Text style={styles.big}>➕ Ek Aidatlar</Text>
        <Text style={styles.desc}>
          {premium
            ? 'Premium aktif. Ek aidat (asansör yenileme, bahçe vb.) kampanyaları buradan yönetilir.'
            : 'Ek Aidat yönetimi bir Premium özelliktir. (Asansör/bakım gibi ek tahsilatlar)'}
        </Text>
        <Text style={styles.note}>
          {premium ? 'Backend ek aidat CRUD uçları yakında eklenecek.' : 'Premium abonelik ile kullanılabilir.'}
        </Text>
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { color: colors.primary, marginBottom: 4 },
  card: { borderRadius: 14, padding: 18 },
  big: { fontSize: 20, fontWeight: '800', color: colors.text, marginBottom: 8 },
  desc: { fontSize: 14, color: colors.text, lineHeight: 20 },
  note: { fontSize: 12, color: colors.textMuted, marginTop: 10, fontStyle: 'italic' },
});
