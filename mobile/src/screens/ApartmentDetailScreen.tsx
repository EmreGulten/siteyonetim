import React from 'react';
import { Dimensions, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Card, Title } from 'react-native-paper';
import { useRoute } from '@react-navigation/native';
import { useQuery } from '@tanstack/react-query';
import { LineChart } from 'react-native-chart-kit';
import { apartmentsApi } from '../api/endpoints';
import { colors } from '../theme/colors';

const screenW = Dimensions.get('window').width - 32;

/** Daire detayı: o dairenin aylık aidat grafiği. */
export function ApartmentDetailScreen() {
  const route = useRoute<any>();
  const apartmentId: string = route.params?.apartmentId;
  const label: string = route.params?.label ?? 'Daire';

  const { data, isLoading } = useQuery({
    queryKey: ['apartment-chart', apartmentId],
    queryFn: () => apartmentsApi.chart(apartmentId),
    enabled: !!apartmentId,
  });

  const labels = (data ?? []).map((d: any) => `${d.month}`);
  const amounts = (data ?? []).map((d: any) => Number(d.amount));
  const paid = (data ?? []).map((d: any) => Number(d.paid));

  return (
    <ScrollView style={styles.root}>
      <Title style={styles.title}>{label}</Title>
      <Card style={styles.card}>
        <Text style={styles.section}>Aylık Aidat (son 12 ay)</Text>
        {isLoading || !data?.length ? (
          <Text style={styles.empty}>Veri yok.</Text>
        ) : (
          <LineChart
            data={{
              labels,
              datasets: [
                { data: amounts, color: () => colors.primary, strokeWidth: 2 },
                { data: paid, color: () => colors.success, strokeWidth: 2 },
              ],
              legend: ['Aidat', 'Ödenen'],
            }}
            width={screenW}
            height={220}
            yAxisSuffix=" ₺"
            chartConfig={{
              backgroundGradientFrom: '#fff', backgroundGradientTo: '#fff',
              color: () => colors.primary, labelColor: () => colors.textMuted,
              propsForBackgroundLines: { stroke: colors.border },
            }}
            bezier
            style={{ borderRadius: 12, marginTop: 8 }}
          />
        )}
      </Card>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { color: colors.primary, marginBottom: 8 },
  card: { borderRadius: 14, padding: 12 },
  section: { fontSize: 15, color: colors.text, fontWeight: '600' },
  empty: { textAlign: 'center', color: colors.textMuted, padding: 24 },
});
