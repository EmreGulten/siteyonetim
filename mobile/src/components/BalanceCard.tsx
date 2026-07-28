import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors } from '../theme/colors';

/** Dashboard bakiye kartı (Gelir/Gider/Borç). */
export function BalanceCard({ title, amount, tone = 'primary', subtitle }: {
  title: string;
  amount: string;
  tone?: 'primary' | 'success' | 'danger' | 'warning';
  subtitle?: string;
}) {
  const bg = colors[tone];
  return (
    <View style={[styles.card, { backgroundColor: bg }]}>
      <Text style={styles.title}>{title}</Text>
      <Text style={styles.amount}>{amount}</Text>
      {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: { flex: 1, borderRadius: 14, padding: 16, marginRight: 10, minHeight: 96 },
  title: { color: 'rgba(255,255,255,0.85)', fontSize: 12 },
  amount: { color: '#fff', fontSize: 20, fontWeight: '700', marginTop: 6 },
  subtitle: { color: 'rgba(255,255,255,0.8)', fontSize: 11, marginTop: 4 },
});
