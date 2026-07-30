import React, { useState } from 'react';
import { Alert, FlatList, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Button, IconButton, Title } from 'react-native-paper';
import { MaterialCommunityIcons } from '@expo/vector-icons';
type IconName = React.ComponentProps<typeof MaterialCommunityIcons>['name'];
import { reportsApi } from '../api/endpoints';
import { colors } from '../theme/colors';
import { ProgressBar } from '../components/ProgressBar';
import { BannerAdSlot } from '../components/BannerAdSlot';
import { downloadAndShare } from '../utils/download';
import { remindDebt } from '../utils/messaging';
import { useInterstitialAd } from '../hooks/useAds';
import { useAuthStore } from '../store/authStore';

const fmt = (v: number) => `₺ ${(v ?? 0).toLocaleString('tr-TR')}`;
const fmtDate = (d: string) => (d ? new Date(d).toLocaleDateString('tr-TR') : '-');
const TR_MONTHS = ['', 'Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];

/** "A Blok / Daire 12" → { block: "A Blok", door: "12" } (apartman etiketini iki sütuna ayırmak için). */
function parseApt(label?: string): { block: string; door: string } {
  const m = (label || '').match(/^(.*?)\s*\/\s*Daire\s+(.+)$/);
  return m ? { block: m[1] || '-', door: m[2] || '-' } : { block: label || '-', door: '-' };
}

/** Borçlu daire için WhatsApp/SMS hatırlatma metni. */
function debtReminderMsg(row: any): string {
  const who = row.residentName || 'Sakin';
  const apt = row.apartmentLabel || 'bağımsız bölüm';
  const debt = (row.totalDebt ?? 0).toLocaleString('tr-TR');
  return `Sayın ${who}, ${apt} bağımsız bölümüne ait ${debt} ₺ aidat borcunuz bulunmaktadır. Ödemenizi rica ederiz. Saygılarımla, Site Yönetimi.`;
}

/** Borçlu satırı için premium'a özel hatırlatma butonu (free'de kilit). */
function DebtorActions({ row, ctx }: { row: any; ctx: RowActionCtx }) {
  if (!ctx.premium) return <MaterialCommunityIcons name="crown-outline" size={16} color={colors.textMuted} />;
  if (!row.phone) return <Text style={{ color: colors.textMuted, fontSize: 11, textAlign: 'center' }}>-</Text>;
  return (
    <IconButton
      icon="bell-outline"
      size={18}
      iconColor={colors.primary}
      onPress={() => remindDebt(row.phone, row.apartmentLabel, debtReminderMsg(row))}
    />
  );
}

/** Hazır olanlar satırı için premium'a özel KMK ihtarname PDF butonu. */
function KmkActions({ row, ctx }: { row: any; ctx: RowActionCtx }) {
  const [busy, setBusy] = useState(false);
  if (!ctx.premium) return <MaterialCommunityIcons name="crown-outline" size={16} color={colors.textMuted} />;
  const download = async () => {
    setBusy(true);
    try {
      const bytes = await reportsApi.ihtarname(row.apartmentId, ctx.year);
      await downloadAndShare(bytes, `ihtarname-${row.apartmentId}.pdf`, 'application/pdf');
    } catch (e: any) {
      const code = e?.response?.status;
      Alert.alert('İhtarname alınamadı', code === 403 ? 'Bu bir Premium özelliktir.' : 'Sunucu hatası, tekrar deneyin.');
    } finally {
      setBusy(false);
    }
  };
  return (
    <IconButton
      icon="file-pdf-box"
      size={18}
      iconColor={colors.danger}
      disabled={busy}
      onPress={download}
    />
  );
}

interface Column {
  label: string;
  flex: number;
  render: (row: any) => string;
  color?: (row: any) => string | undefined;
  weight?: '700';
  align?: 'right';
}

interface TotalItem {
  label: string;
  value: string;
  color?: string;
}

interface RowActionCtx {
  premium: boolean;
  year: number;
}

interface ReportConfig {
  key: string;
  title: string;
  subtitle: string;
  icon: IconName; // MaterialCommunityIcons name
  accent: string;
  yearScoped: boolean;
  premium?: boolean;
  queryKey: (year: number) => any[];
  queryFn: (year: number) => Promise<any[]>;
  exportCsv: (year: number) => Promise<ArrayBuffer>;
  fileName: (year: number) => string;
  columns: Column[];
  totals: (rows: any[]) => TotalItem[];
  /** Opsiyonel satır eylemleri (örn. hatırlatma, ihtarname). */
  actionsHeader?: string;
  rowActions?: (row: any, ctx: RowActionCtx) => React.ReactNode;
}

/** 8 raporun tanımı. Her biri menüde bir kart, seçilince detay görünümü render edilir. */
const REPORTS: ReportConfig[] = [
  {
    key: 'borclular',
    title: 'Borçlu Daireler',
    subtitle: 'Kalan borcu olan daireler',
    icon: 'currency-try',
    accent: colors.danger,
    yearScoped: true,
    queryKey: (y) => ['report-borclular', y],
    queryFn: (y) => reportsApi.debtors(y),
    exportCsv: (y) => reportsApi.debtorsCsv(y),
    fileName: (y) => `borclular-${y}.csv`,
    columns: [
      { label: 'Blok', flex: 2, render: (r) => parseApt(r.apartmentLabel).block },
      { label: 'Daire', flex: 1.5, render: (r) => parseApt(r.apartmentLabel).door },
      { label: 'Sakin', flex: 3, render: (r) => r.residentName || '-' },
      { label: 'Borç', flex: 2, render: (r) => fmt(r.totalDebt), align: 'right', color: () => colors.danger, weight: '700' },
    ],
    totals: (rows) => [
      { label: 'Borçlu Daire', value: String(rows.length) },
      { label: 'Toplam Borç', value: fmt(rows.reduce((s, r) => s + (r.totalDebt || 0), 0)), color: colors.danger },
    ],
    actionsHeader: 'Hatırlat',
    rowActions: (row, ctx) => <DebtorActions row={row} ctx={ctx} />,
  },
  {
    key: 'aidat',
    title: 'Aidat Raporu',
    subtitle: 'Dönemsel aidat ve tahsilat',
    icon: 'calendar-cursor',
    accent: colors.primary,
    yearScoped: true,
    queryKey: (y) => ['report-aidat', y],
    queryFn: (y) => reportsApi.dues(y),
    exportCsv: (y) => reportsApi.duesCsv(y),
    fileName: (y) => `aidat-${y}.csv`,
    columns: [
      { label: 'Blok', flex: 2, render: (r) => parseApt(r.apartmentLabel).block },
      { label: 'Daire', flex: 1.5, render: (r) => parseApt(r.apartmentLabel).door },
      { label: 'Dönem', flex: 2, render: (r) => `${TR_MONTHS[r.month] || r.month} ${r.year}` },
      { label: 'Aidat', flex: 2, render: (r) => fmt(r.amount), align: 'right' },
      { label: 'Tahsil', flex: 2, render: (r) => fmt(r.paidAmount), align: 'right', color: () => colors.success },
      { label: 'Borç', flex: 2, render: (r) => fmt(r.remaining), align: 'right', color: (r) => (r.remaining > 0 ? colors.danger : colors.textMuted) },
    ],
    totals: (rows) => [
      { label: 'Toplam Aidat', value: fmt(rows.reduce((s, r) => s + (r.amount || 0), 0)) },
      { label: 'Tahsil Edilen', value: fmt(rows.reduce((s, r) => s + (r.paidAmount || 0), 0)), color: colors.success },
      { label: 'Kalan Borç', value: fmt(rows.reduce((s, r) => s + (r.remaining || 0), 0)), color: colors.danger },
    ],
  },
  {
    key: 'ekaidat',
    title: 'Ek Aidat Raporu',
    subtitle: 'Kampanyalar ve daire tipi farkları',
    icon: 'plus-circle-outline',
    accent: colors.accent,
    yearScoped: false,
    premium: true,
    queryKey: () => ['report-ekaidat'],
    queryFn: () => reportsApi.extraDues(),
    exportCsv: () => reportsApi.extraDuesCsv(),
    fileName: () => 'ek-aidat.csv',
    columns: [
      { label: 'Kampanya', flex: 3, render: (r) => r.title },
      { label: 'Daire Tipi', flex: 2, render: (r) => r.apartmentTypeName || '-' },
      { label: 'Taksit', flex: 1, render: (r) => `${r.installmentCount}x` },
      { label: 'Tutar', flex: 2, render: (r) => fmt(r.amount), align: 'right' },
    ],
    totals: (rows) => [
      { label: 'Kampanya', value: String(rows.length) },
      { label: 'Aktif', value: String(rows.filter((r) => r.isActive).length), color: colors.success },
    ],
  },
  {
    key: 'daire',
    title: 'Daire Raporu',
    subtitle: 'Bağımsız bölümler listesi',
    icon: 'home-city-outline',
    accent: colors.primaryDark,
    yearScoped: false,
    queryKey: () => ['report-daire'],
    queryFn: () => reportsApi.apartments(),
    exportCsv: () => reportsApi.apartmentsCsv(),
    fileName: () => 'daireler.csv',
    columns: [
      { label: 'Blok', flex: 2, render: (r) => r.blockName || '-' },
      { label: 'Daire', flex: 1.5, render: (r) => r.doorNumber || '-' },
      { label: 'Malik', flex: 3, render: (r) => r.ownerName || '-' },
      { label: 'Aidat', flex: 2, render: (r) => fmt(r.monthlyDues), align: 'right' },
    ],
    totals: (rows) => [
      { label: 'Toplam Daire', value: String(rows.length) },
      { label: 'Dolu', value: String(rows.filter((r) => r.isOccupied).length) },
      { label: 'Aylık Aidat', value: fmt(rows.reduce((s, r) => s + (r.monthlyDues || 0), 0)) },
    ],
  },
  {
    key: 'hazirlar',
    title: 'Hazır Olanlar Listesi',
    subtitle: 'Bildirim için hazır daireler (KMK)',
    icon: 'bell-check-outline',
    accent: colors.success,
    yearScoped: true,
    queryKey: (y) => ['report-hazirlar', y],
    queryFn: (y) => reportsApi.kmk(y),
    exportCsv: (y) => reportsApi.kmkCsv(y),
    fileName: (y) => `hazir-olanlar-${y}.csv`,
    columns: [
      { label: 'Blok', flex: 2, render: (r) => parseApt(r.apartmentLabel).block },
      { label: 'Daire', flex: 1.5, render: (r) => parseApt(r.apartmentLabel).door },
      { label: 'Malik', flex: 2, render: (r) => r.ownerName || '-' },
      { label: 'Yıllık', flex: 1.5, render: (r) => fmt(r.annualDues), align: 'right' },
      { label: 'Tahsil', flex: 1.5, render: (r) => fmt(r.collectedThisYear), align: 'right', color: () => colors.success },
      { label: 'Durum', flex: 2, render: (r) => r.note || '-', color: (r) => (r.isKmkReady ? colors.success : colors.textMuted) },
    ],
    totals: (rows) => [
      { label: 'Hazır Olan', value: String(rows.filter((r) => r.isKmkReady).length), color: colors.success },
      { label: 'Toplam Daire', value: String(rows.length) },
    ],
    actionsHeader: 'İhtarname',
    rowActions: (row, ctx) => <KmkActions row={row} ctx={ctx} />,
  },
  {
    key: 'gelir',
    title: 'Gelir Raporu',
    subtitle: 'Tüm gelir kayıtları',
    icon: 'trending-up',
    accent: colors.success,
    yearScoped: true,
    queryKey: (y) => ['report-gelir', y],
    queryFn: (y) => reportsApi.income(y),
    exportCsv: (y) => reportsApi.incomeCsv(y),
    fileName: (y) => `gelir-${y}.csv`,
    columns: [
      { label: 'Kategori', flex: 3, render: (r) => r.category },
      { label: 'Açıklama', flex: 4, render: (r) => r.description || '-' },
      { label: 'Tarih', flex: 3, render: (r) => fmtDate(r.date) },
      { label: 'Tutar', flex: 3, render: (r) => fmt(r.amount), align: 'right', color: () => colors.success, weight: '700' },
    ],
    totals: (rows) => [
      { label: 'Kayıt', value: String(rows.length) },
      { label: 'Toplam Gelir', value: fmt(rows.reduce((s, r) => s + (r.amount || 0), 0)), color: colors.success },
    ],
  },
  {
    key: 'gider',
    title: 'Gider Raporu',
    subtitle: 'Tüm gider kayıtları',
    icon: 'trending-down',
    accent: colors.warning,
    yearScoped: true,
    queryKey: (y) => ['report-gider', y],
    queryFn: (y) => reportsApi.expenses(y),
    exportCsv: (y) => reportsApi.expensesCsv(y),
    fileName: (y) => `gider-${y}.csv`,
    columns: [
      { label: 'Kategori', flex: 3, render: (r) => r.category },
      { label: 'Açıklama', flex: 4, render: (r) => r.description || '-' },
      { label: 'Tarih', flex: 3, render: (r) => fmtDate(r.date) },
      { label: 'Tutar', flex: 3, render: (r) => fmt(r.amount), align: 'right', color: () => colors.warning, weight: '700' },
    ],
    totals: (rows) => [
      { label: 'Kayıt', value: String(rows.length) },
      { label: 'Toplam Gider', value: fmt(rows.reduce((s, r) => s + (r.amount || 0), 0)), color: colors.warning },
    ],
  },
  {
    key: 'detayli',
    title: 'Detaylı İşlem Raporu',
    subtitle: 'Gelir + gider tüm hareketler',
    icon: 'file-document-multiple-outline',
    accent: colors.primary,
    yearScoped: true,
    queryKey: (y) => ['report-detayli', y],
    queryFn: (y) => reportsApi.transactions(y),
    exportCsv: (y) => reportsApi.transactionsCsv(y),
    fileName: (y) => `islem-${y}.csv`,
    columns: [
      { label: 'Tür', flex: 1, render: (r) => (r.type === 'Income' ? 'Gelir' : 'Gider'), color: (r) => (r.type === 'Income' ? colors.success : colors.warning), weight: '700' },
      { label: 'Kategori', flex: 3, render: (r) => r.category },
      { label: 'Açıklama', flex: 3, render: (r) => r.description || '-' },
      { label: 'Tarih', flex: 2, render: (r) => fmtDate(r.date) },
      { label: 'Tutar', flex: 2, render: (r) => fmt(r.amount), align: 'right', color: (r) => (r.type === 'Income' ? colors.success : colors.warning) },
    ],
    totals: (rows) => {
      const inc = rows.filter((r) => r.type === 'Income').reduce((s, r) => s + (r.amount || 0), 0);
      const exp = rows.filter((r) => r.type === 'Expense').reduce((s, r) => s + (r.amount || 0), 0);
      return [
        { label: 'Gelir', value: fmt(inc), color: colors.success },
        { label: 'Gider', value: fmt(exp), color: colors.warning },
        { label: 'Net', value: fmt(inc - exp), color: inc - exp >= 0 ? colors.success : colors.danger },
      ];
    },
  },
];

/** 8.png — Raporlar: 8 rapor tipinden seçim → detay tablosu + Excel/Yazdır. */
export function ReportsScreen() {
  const [selected, setSelected] = useState<ReportConfig | null>(null);
  const [busy, setBusy] = useState(false);
  const premium = useAuthStore((s) => s.user?.isPremium);
  const year = new Date().getFullYear();

  const premiumDownload = async (kind: 'balance' | 'backup') => {
    if (!premium) return Alert.alert('Premium gerekli', 'Bu özellik Premium abonelik gerektirir.');
    setBusy(true);
    try {
      if (kind === 'balance') {
        const bytes = await reportsApi.balancePdf(year);
        await downloadAndShare(bytes, `bilanco-${year}.pdf`, 'application/pdf');
      } else {
        const bytes = await reportsApi.backup();
        await downloadAndShare(bytes, `site-yedek-${year}.zip`, 'application/zip');
      }
    } catch (e: any) {
      Alert.alert('İndirilemedi', e?.response?.status === 403 ? 'Premium özellik.' : 'Tekrar deneyin.');
    } finally { setBusy(false); }
  };

  if (selected) {
    return <ReportDetail config={selected} onBack={() => setSelected(null)} />;
  }
  return (
    <View style={styles.root}>
      <Title style={styles.title}>Raporlar</Title>
      <Text style={styles.hint}>İndirmek istediğiniz raporu seçin</Text>
      {!premium && <BannerAdSlot placement="reports" />}

      {/* Premium araçlar: yıllık bilanço PDF + veri yedeği */}
      <View style={styles.toolsRow}>
        <TouchableOpacity
          style={[styles.toolCard, { borderColor: colors.accent }]}
          disabled={busy}
          onPress={() => premiumDownload('balance')}
        >
          <MaterialCommunityIcons name={premium ? 'file-chart-outline' : 'crown-outline'} size={22} color={colors.accent} />
          <Text style={styles.toolTitle}>Yıllık Bilanço</Text>
          <Text style={styles.toolSub}>PDF · {year}</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.toolCard, { borderColor: colors.primary }]}
          disabled={busy}
          onPress={() => premiumDownload('backup')}
        >
          <MaterialCommunityIcons name={premium ? 'database-export-outline' : 'crown-outline'} size={22} color={colors.primary} />
          <Text style={styles.toolTitle}>Veri Yedeği</Text>
          <Text style={styles.toolSub}>ZIP</Text>
        </TouchableOpacity>
      </View>

      <FlatList
        data={REPORTS}
        keyExtractor={(r) => r.key}
        numColumns={2}
        columnWrapperStyle={styles.gridRow}
        contentContainerStyle={{ paddingBottom: 12 }}
        renderItem={({ item }) => (
          <ReportCard
            config={item}
            locked={!!item.premium && !premium}
            onPress={() => setSelected(item)}
          />
        )}
      />
    </View>
  );
}

function ReportCard({ config, locked, onPress }: { config: ReportConfig; locked: boolean; onPress: () => void }) {
  return (
    <TouchableOpacity
      activeOpacity={0.7}
      onPress={onPress}
      style={[styles.card, { borderColor: config.accent }]}
    >
      <View style={[styles.iconWrap, { backgroundColor: config.accent }]}>
        <MaterialCommunityIcons name={locked ? 'crown-outline' : config.icon} color="#fff" size={22} />
      </View>
      <Text style={styles.cardTitle} numberOfLines={1}>{config.title}</Text>
      <Text style={styles.cardSub} numberOfLines={2}>{config.subtitle}</Text>
    </TouchableOpacity>
  );
}

function ReportDetail({ config, onBack }: { config: ReportConfig; onBack: () => void }) {
  const [year, setYear] = useState(new Date().getFullYear());
  const [progress, setProgress] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const interstitial = useInterstitialAd('export');
  const premium = useAuthStore((s) => s.user?.isPremium);

  const rows = useQuery({
    queryKey: config.queryKey(year),
    queryFn: () => config.queryFn(year),
  });

  const data = rows.data ?? [];
  const totals = config.totals(data);

  const exportCsv = () => {
    setBusy(true); setProgress(10);
    interstitial.showThen(async () => {
      try {
        const bytes = await config.exportCsv(year);
        setProgress(60);
        await downloadAndShare(bytes, config.fileName(year), 'text/csv', setProgress);
      } finally { setBusy(false); setProgress(null); }
    });
  };

  return (
    <View style={styles.root}>
      <View style={styles.detailHead}>
        <IconButton icon="arrow-left" size={24} iconColor={colors.primary} onPress={onBack} />
        <View style={{ flex: 1 }}>
          <Text style={styles.detailTitle} numberOfLines={1}>{config.title}</Text>
          <Text style={styles.detailSub} numberOfLines={1}>{config.subtitle}</Text>
        </View>
        {config.yearScoped && (
          <View style={styles.yearRow}>
            <Button compact onPress={() => setYear((y) => y - 1)} icon="chevron-left" labelStyle={{ color: colors.primary }}> </Button>
            <Text style={styles.year}>{year}</Text>
            <Button compact onPress={() => setYear((y) => y + 1)} icon="chevron-right" labelStyle={{ color: colors.primary }}> </Button>
          </View>
        )}
      </View>

      <View style={styles.actionRow}>
        <Button mode="contained" icon="file-excel" onPress={exportCsv} disabled={busy} loading={busy} style={[styles.actBtn, { backgroundColor: colors.success }]}>Excel</Button>
        <Button mode="contained" icon="printer" onPress={exportCsv} style={[styles.actBtn, { backgroundColor: colors.primary }]}>Yazdır</Button>
      </View>
      {progress !== null && (
        <View style={{ marginVertical: 8 }}>
          <ProgressBar value={progress} />
          <Text style={styles.progTxt}>İndiriliyor… %{progress}</Text>
        </View>
      )}

      {totals.length > 0 && (
        <View style={styles.sumBar}>
          {totals.map((t, i) => (
            <View key={i} style={{ flex: 1 }}>
              <Text style={styles.sumLabel}>{t.label}</Text>
              <Text style={[styles.sumVal, t.color ? { color: t.color } : null]}>{t.value}</Text>
            </View>
          ))}
        </View>
      )}

      <View style={[styles.headerRow, { backgroundColor: config.accent }]}>
        {config.columns.map((c) => (
          <Text key={c.label} style={[styles.h, { flex: c.flex, textAlign: c.align === 'right' ? 'right' : 'left' }]}>{c.label}</Text>
        ))}
        {config.rowActions && (
          <Text style={[styles.h, { flex: 2, textAlign: 'center' }]}>{config.actionsHeader ?? ''}</Text>
        )}
      </View>

      {rows.isLoading ? (
        <Text style={styles.empty}>Yükleniyor…</Text>
      ) : rows.isError ? (
        <Text style={styles.empty}>Rapor yüklenemedi.</Text>
      ) : (
        <FlatList
          data={data}
          refreshing={rows.isFetching}
          onRefresh={rows.refetch}
          keyExtractor={(_: any, idx: number) => String(idx)}
          ListEmptyComponent={<Text style={styles.empty}>Kayıt yok.</Text>}
          renderItem={({ item }: any) => (
            <View style={styles.row}>
              {config.columns.map((c, idx) => (
                <Text
                  key={idx}
                  numberOfLines={1}
                  style={[
                    styles.c,
                    { flex: c.flex, textAlign: c.align === 'right' ? 'right' : 'left' },
                    c.color ? { color: c.color(item) } : null,
                    c.weight ? { fontWeight: c.weight } : null,
                  ]}
                >
                  {c.render(item)}
                </Text>
              ))}
              {config.rowActions && (
                <View style={{ flex: 2, alignItems: 'center', justifyContent: 'center' }}>
                  {config.rowActions(item, { premium: !!premium, year })}
                </View>
              )}
            </View>
          )}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 12 },
  title: { color: colors.primary, marginBottom: 2 },
  hint: { color: colors.textMuted, fontSize: 13, marginBottom: 10 },
  gridRow: { gap: 10, marginBottom: 10 },
  toolsRow: { flexDirection: 'row', gap: 10, marginBottom: 14 },
  toolCard: { flex: 1, borderRadius: 14, borderWidth: 1.5, backgroundColor: colors.card, padding: 14, alignItems: 'center' },
  toolTitle: { fontSize: 13, fontWeight: '800', color: colors.text, marginTop: 6, textAlign: 'center' },
  toolSub: { fontSize: 10, color: colors.textMuted, marginTop: 2 },
  card: { flex: 1, borderRadius: 14, borderWidth: 1.5, backgroundColor: colors.card, padding: 12, alignItems: 'center' },
  iconWrap: { width: 44, height: 44, borderRadius: 14, alignItems: 'center', justifyContent: 'center', marginBottom: 8 },
  cardTitle: { fontSize: 13, fontWeight: '800', color: colors.text, textAlign: 'center' },
  cardSub: { fontSize: 10, color: colors.textMuted, textAlign: 'center', marginTop: 2 },

  detailHead: { flexDirection: 'row', alignItems: 'center', marginBottom: 4 },
  detailTitle: { fontSize: 18, fontWeight: '800', color: colors.primary },
  detailSub: { fontSize: 12, color: colors.textMuted },
  yearRow: { flexDirection: 'row', alignItems: 'center' },
  year: { fontSize: 15, fontWeight: '700', color: colors.primary, marginHorizontal: 4 },
  actionRow: { flexDirection: 'row', gap: 8, marginVertical: 8 },
  actBtn: { flex: 1 },
  progTxt: { color: colors.textMuted, fontSize: 12, textAlign: 'center', marginTop: 4 },
  sumBar: { flexDirection: 'row', backgroundColor: '#fff', borderRadius: 12, padding: 12, marginVertical: 8, gap: 8 },
  sumLabel: { fontSize: 11, color: colors.textMuted },
  sumVal: { fontSize: 14, fontWeight: '800', color: colors.text, marginTop: 2 },
  headerRow: { flexDirection: 'row', borderRadius: 8, padding: 8 },
  h: { color: '#fff', fontSize: 11, fontWeight: '700' },
  row: { flexDirection: 'row', alignItems: 'center', padding: 8, borderBottomWidth: 1, borderBottomColor: colors.border, backgroundColor: '#fff' },
  c: { fontSize: 12, color: colors.text },
  empty: { textAlign: 'center', color: colors.textMuted, marginTop: 20 },
});
