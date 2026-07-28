import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors } from '../theme/colors';

/**
 * Referans 1.png'deki dashboard kartı: pastel arka plan, başlık (sol) + ikon (sağ),
 * büyük değer, alt metin satırları.
 */
export function DashboardCard({
  title, value, lines = [], bg, icon, valueColor, minHeight,
}: {
  title: string;
  value: string;
  lines?: { label?: string; value?: string; color?: string }[];
  bg: string;
  icon?: string;
  valueColor?: string;
  minHeight?: number;
}) {
  return (
    <View style={[styles.card, { backgroundColor: bg, minHeight: minHeight ?? 110 }]}>
      <View style={styles.header}>
        <Text style={styles.title} numberOfLines={2}>{title}</Text>
        {icon ? <Text style={styles.icon}>{icon}</Text> : null}
      </View>
      <Text style={[styles.value, valueColor ? { color: valueColor } : null]} numberOfLines={1}>{value}</Text>
      {lines.filter((l) => l.label || l.value).map((l, i) => (
        <Text key={i} style={[styles.sub, l.color ? { color: l.color } : null]}>
          {l.label ? `${l.label}: ` : ''}{l.value}
        </Text>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  card: { flex: 1, borderRadius: 14, padding: 12, margin: 4 },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' },
  title: { fontSize: 12, fontWeight: '600', color: colors.text, flex: 1, flexWrap: 'wrap' },
  icon: { fontSize: 18, marginLeft: 6 },
  value: { fontSize: 22, fontWeight: '800', color: colors.text, marginTop: 6, marginBottom: 2 },
  sub: { fontSize: 11, color: colors.textMuted, marginTop: 1 },
});
