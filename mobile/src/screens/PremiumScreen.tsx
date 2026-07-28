import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, Title } from 'react-native-paper';
import { usePurchases, PremiumProduct } from '../hooks/usePurchases';
import { colors } from '../theme/colors';

/** Satın alma / Premium ekranı. Planlar + durum + aktivasyon. */
export function PremiumScreen() {
  const { products, buy, restore, status, isPremium, loading } = usePurchases();
  const expiry = status?.premiumExpiryDate;

  return (
    <View style={styles.root}>
      <Title style={styles.title}>Premium'a Geç</Title>

      <Card style={[styles.statusCard, { backgroundColor: isPremium ? colors.success : colors.primary }]}>
        <Text style={styles.statusTitle}>{isPremium ? '✓ Premium Aktif' : 'Free Plan aktif'}</Text>
        <Text style={styles.statusSub}>
          {isPremium && expiry
            ? `Bitiş: ${new Date(expiry).toLocaleDateString('tr-TR')}`
            : 'Premium ile tüm özellikleri açın, reklamları kaldırın.'}
        </Text>
      </Card>

      <Card style={styles.featCard}>
        <Text style={styles.featTitle}>Premium ile kazançlar:</Text>
        <Text style={styles.feature}>✓ Sınırsız site & daire</Text>
        <Text style={styles.feature}>✓ Ek aidat yönetimi</Text>
        <Text style={styles.feature}>✓ Reklamsız deneyim</Text>
        <Text style={styles.feature}>✓ WhatsApp / SMS borç hatırlatma</Text>
        <Text style={styles.feature}>✓ KMK ihtarname PDF üretimi</Text>
        <Text style={styles.feature}>✓ Detaylı KMK raporları</Text>
      </Card>

      {products.map((p: PremiumProduct) => (
        <Card key={p.productId} style={[styles.plan, p.highlight ? { borderColor: colors.primary, borderWidth: 1.5 } : null]}>
          <View style={styles.row}>
            <View style={{ flex: 1 }}>
              <Text style={styles.planTitle}>{p.title}{p.highlight ? '  ⭐' : ''}</Text>
              <Text style={styles.price}>{p.price} <Text style={styles.period}>{p.period}</Text></Text>
            </View>
            <Button mode="contained" onPress={() => buy(p.productId)} disabled={loading} style={styles.btn}>
              {isPremium ? 'Yenile' : 'Satın Al'}
            </Button>
          </View>
        </Card>
      ))}

      <Button mode="text" onPress={restore} disabled={loading} style={{ marginTop: 4 }}>
        Satın Almayı Geri Yükle
      </Button>

      <Text style={styles.note}>
        Aboneliğin App Store hesabınla işlenir. Cihaz değiştirirsen “Geri Yükle” ile premium’unu kurtarırsın.
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.bg, padding: 16 },
  title: { color: colors.primary, marginBottom: 12, textAlign: 'center' },
  statusCard: { borderRadius: 14, padding: 16, marginBottom: 12 },
  statusTitle: { color: '#fff', fontSize: 18, fontWeight: '800' },
  statusSub: { color: '#fff', fontSize: 12, marginTop: 4, opacity: 0.9 },
  featCard: { borderRadius: 14, padding: 16, marginBottom: 14, backgroundColor: '#fff' },
  featTitle: { fontSize: 14, fontWeight: '700', color: colors.text, marginBottom: 8 },
  feature: { color: colors.text, fontSize: 14, marginVertical: 4 },
  plan: { borderRadius: 14, padding: 16, marginBottom: 10, backgroundColor: '#fff' },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  planTitle: { fontSize: 16, fontWeight: '700', color: colors.text },
  price: { fontSize: 18, color: colors.primary, fontWeight: '800', marginTop: 2 },
  period: { fontSize: 12, color: colors.textMuted, fontWeight: '500' },
  btn: { backgroundColor: colors.primary },
  note: { color: colors.textMuted, fontSize: 11, textAlign: 'center', marginTop: 12, lineHeight: 16 },
});
