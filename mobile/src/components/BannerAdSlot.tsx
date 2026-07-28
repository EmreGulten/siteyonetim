import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { IS_DEV, adsNativeAvailable, bannerUnitId } from '../config/ads';

/**
 * Banner reklam. Premium kullanıcılar reklam görmez.
 * Native reklam modülü yoksa (Expo Go) render edilmez → crash olmaz.
 * Ekranın ÜSTÜNE sabitlenir (başlığın altında, kaydırılan listenin dışında — her zaman görünür).
 */
export function BannerAdSlot({ placement = 'default' }: { placement?: string }) {
  const premium = useAuthStore((s) => s.user?.isPremium);
  if (premium || !adsNativeAvailable) return null; // Premium = reklamsız; modül yoksa atla

  // Lazy require — yalnızca native modül varken buraya ulaşılır.
  const { BannerAd, BannerAdSize } = require('react-native-google-mobile-ads');

  return (
    <View style={styles.wrap}>
      {IS_DEV && <Text style={styles.dev}>[AdMob test banner · {placement}]</Text>}
      {/* @ts-ignore — require ile gelen tip any */}
      <BannerAd unitId={bannerUnitId()} size={BannerAdSize.ANCHORED_ADAPTIVE_BANNER} />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { alignItems: 'center', marginTop: 2, marginBottom: 8, paddingBottom: 6, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: colors.border },
  dev: { fontSize: 10, color: colors.textMuted, marginBottom: 2 },
});
