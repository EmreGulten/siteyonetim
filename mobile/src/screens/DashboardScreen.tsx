import React, { useState } from 'react';
import { StyleSheet, Text, View, ScrollView, RefreshControl } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Card, IconButton, Title } from 'react-native-paper';
import { financeApi, apartmentsApi } from '../api/endpoints';
import { DashboardCard } from '../components/DashboardCard';
import { colors } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { BannerAdSlot } from '../components/BannerAdSlot';

const MONTHS = ['Ocak','Şubat','Mart','Nisan','Mayıs','Haziran','Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'];

export function DashboardScreen() {
  const [offset, setOffset] = useState(0);
  const d = new Date(); d.setMonth(d.getMonth() + offset);
  const year = d.getFullYear();
  const month = d.getMonth() + 1;

  const summary = useQuery({
    queryKey: ['finance-summary', year, month],
    queryFn: () => financeApi.summary(year, month),
  });
  const apts = useQuery({ queryKey: ['apartments-count'], queryFn: () => apartmentsApi.list({ page: 1, pageSize: 1 }) });
  const txQuery = useQuery({ queryKey: ['recent-transactions'], queryFn: () => financeApi.transactions({ page: 1 }) });

  const premium = useAuthStore((s) => s.user?.isPremium);
  const s = summary.data;
  const fmt = (v: number) => `₺ ${(v ?? 0).toLocaleString('tr-TR')}`;

  const collected = s?.collected ?? 0;
  const expected = s?.expectedIncome ?? 0;
  const outstanding = s?.outstanding ?? 0;
  const rate = s?.collectionRate ?? 0;
  const net = s?.netBalance ?? 0;
  const expenses = s?.expenses ?? 0;
  const otherIncome = s?.otherIncome ?? 0;
  const totalApts = apts.data?.total ?? 0;

  // Tahsilat bar'ı: collected / expected (0..1). Sıfır bölme korunmuş.
  const ratio = expected > 0 ? Math.min(collected / expected, 1) : collected > 0 ? 1 : 0;

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      {!premium && <BannerAdSlot placement="dashboard" />}
      <ScrollView
        style={styles.root}
        refreshControl={<RefreshControl refreshing={summary.isFetching} onRefresh={summary.refetch} colors={[colors.primary]} />}
      >
        {/* Ay navigasyonu */}
        <View style={styles.monthBar}>
          <IconButton icon="chevron-left" onPress={() => setOffset((o) => o - 1)} />
          <Text style={styles.monthText}>{MONTHS[d.getMonth()]} {year}</Text>
          <IconButton icon="chevron-right" onPress={() => setOffset((o) => o + 1)} disabled={offset >= 0} />
        </View>

        {/* HERO — Net Bakiye + tahsilat bar'ı */}
        <View style={[styles.hero, { backgroundColor: colors.pastel.blue }]}>
          <View style={styles.heroTop}>
            <View style={styles.flex1}>
              <Text style={styles.heroLabel}>NET BAKİYE</Text>
              <Text style={[styles.heroValue, { color: net >= 0 ? colors.success : colors.danger }]} numberOfLines={1}>
                {fmt(net)}
              </Text>
            </View>
            <View style={styles.heroRate}>
              <Text style={styles.heroRateLabel}>Tahsilat</Text>
              <Text style={[styles.heroRateVal, { color: colors.primary }]}>%{Math.round(rate)}</Text>
            </View>
          </View>
          <View style={styles.barTrack}>
            <View style={[styles.barFill, { width: `${Math.round(ratio * 100)}%`, backgroundColor: colors.success }]} />
          </View>
          <Text style={styles.barCaption}>{fmt(collected)} / {fmt(expected)} tahsil edildi</Text>
        </View>

        {/* 3'lü stat şerit — her değer tek yerde */}
        <View style={styles.strip}>
          <View style={styles.stripItem}>
            <Text style={[styles.stripLabel, { color: colors.success }]}>Tahsil</Text>
            <Text style={styles.stripVal} numberOfLines={1}>{fmt(collected)}</Text>
          </View>
          <View style={styles.stripDivider} />
          <View style={styles.stripItem}>
            <Text style={styles.stripLabel}>Beklenen</Text>
            <Text style={styles.stripVal} numberOfLines={1}>{fmt(expected)}</Text>
          </View>
          <View style={styles.stripDivider} />
          <View style={styles.stripItem}>
            <Text style={[styles.stripLabel, { color: colors.danger }]}>Kalan Borç</Text>
            <Text style={styles.stripVal} numberOfLines={1}>{fmt(outstanding)}</Text>
          </View>
        </View>

        {/* Kompakt ızgara — geriye kalan tekil metrikler */}
        <View style={styles.miniGrid}>
          <DashboardCard title="Toplam Daire" value={String(totalApts)} icon="🏠" bg={colors.pastel.purple} />
          <DashboardCard title="Kira & Diğer" value={fmt(otherIncome)} icon="💰" bg={colors.pastel.green} valueColor={colors.success} />
          <DashboardCard title="Giderler" value={fmt(expenses)} icon="➖" bg={colors.pastel.orange} valueColor={colors.danger} />
        </View>

        {/* Son işlemler */}
        <Card style={styles.chartCard}>
          <Title style={styles.sectionTitle}>Son İşlemler</Title>
          {(txQuery.data?.items ?? []).slice(0, 5).map((t: any) => (
            <View key={t.id} style={styles.txRow}>
              <View style={styles.flex1}>
                <Text style={styles.txCategory}>{t.category}</Text>
                <Text style={styles.txDate}>{t.type === 'Income' ? 'Gelir' : 'Gider'} · {new Date(t.date).toLocaleDateString('tr-TR')}</Text>
              </View>
              <Text style={[styles.txAmount, { color: t.type === 'Income' ? colors.success : colors.danger }]}>
                {t.type === 'Income' ? '+' : '−'}{fmt(t.amount)}
              </Text>
            </View>
          ))}
          {txQuery.data?.items?.length === 0 && <Text style={styles.centerText}>Henüz işlem yok.</Text>}
        </Card>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, paddingHorizontal: 12 },
  flex1: { flex: 1 },
  monthBar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', paddingVertical: 4 },
  monthText: { fontSize: 16, fontWeight: '700', color: colors.primary, minWidth: 140, textAlign: 'center' },
  sectionTitle: { fontSize: 15, color: colors.text, marginBottom: 8 },
  chartCard: { marginTop: 14, borderRadius: 14, padding: 12 },

  // HERO
  hero: { borderRadius: 16, padding: 16, marginTop: 8 },
  heroTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  heroLabel: { fontSize: 12, fontWeight: '700', color: colors.textMuted, letterSpacing: 0.5 },
  heroValue: { fontSize: 26, fontWeight: '800', color: colors.text, marginTop: 2 },
  heroRate: { alignItems: 'flex-end' },
  heroRateLabel: { fontSize: 11, color: colors.textMuted, fontWeight: '600' },
  heroRateVal: { fontSize: 22, fontWeight: '800', marginTop: 2 },
  barTrack: { height: 8, borderRadius: 4, backgroundColor: 'rgba(255,255,255,0.7)', marginTop: 14, overflow: 'hidden' },
  barFill: { height: 8, borderRadius: 4 },
  barCaption: { fontSize: 11, color: colors.textMuted, marginTop: 6 },

  // STAT ŞERİT
  strip: { flexDirection: 'row', alignItems: 'center', backgroundColor: colors.card, borderRadius: 14, marginTop: 12, paddingVertical: 12, elevation: 1 },
  stripItem: { flex: 1, alignItems: 'center', paddingHorizontal: 4 },
  stripDivider: { width: 1, height: 28, backgroundColor: colors.border },
  stripLabel: { fontSize: 11, fontWeight: '700', color: colors.textMuted },
  stripVal: { fontSize: 15, fontWeight: '800', color: colors.text, marginTop: 3 },

  // KOMPAKT IZGARA (3 kart tek sıra)
  miniGrid: { flexDirection: 'row', marginTop: 8 },

  // SON İŞLEMLER
  txRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: colors.border },
  txCategory: { fontSize: 14, color: colors.text, fontWeight: '500' },
  txDate: { fontSize: 11, color: colors.textMuted, marginTop: 1 },
  txAmount: { fontSize: 14, fontWeight: '700' },
  centerText: { textAlign: 'center', color: colors.textMuted, marginBottom: 8 },
});
