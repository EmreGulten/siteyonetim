import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createDrawerNavigator, DrawerContentScrollView, DrawerItemList } from '@react-navigation/drawer';
import { MaterialCommunityIcons } from '@expo/vector-icons';
import { Button } from 'react-native-paper';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { colors } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { AuthModal } from '../screens/AuthModal';
import { DashboardScreen } from '../screens/DashboardScreen';
import { BlokYonetimiScreen } from '../screens/BlokYonetimiScreen';
import { ApartmentsScreen } from '../screens/ApartmentsScreen';
import { ApartmentDetailScreen } from '../screens/ApartmentDetailScreen';
import { AidatGirisiScreen } from '../screens/AidatGirisiScreen';
import { EkAidatScreen } from '../screens/EkAidatScreen';
import { GelirlerScreen } from '../screens/GelirlerScreen';
import { GiderlerScreen } from '../screens/GiderlerScreen';
import { ReportsScreen } from '../screens/ReportsScreen';
import { PremiumScreen } from '../screens/PremiumScreen';

const Drawer = createDrawerNavigator();

const icon = (name: string) => ({ color, size }: { color: string; size: number }) =>
  <MaterialCommunityIcons name={name} color={color} size={size} />;

const headerOpts = {
  headerStyle: { backgroundColor: colors.primary },
  headerTintColor: '#fff',
};

/** Drawer içeriği: kullanıcı başlığı + giriş/çıkış + standart menü. */
function CustomDrawerContent(props: any) {
  const token = useAuthStore((s) => s.token);
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const openAuthModal = useAuthStore((s) => s.openAuthModal);

  return (
    <DrawerContentScrollView {...props} contentContainerStyle={{ paddingTop: 0 }}>
      {/* Kullanıcı başlığı */}
      <View style={drawerStyles.header}>
        <View style={drawerStyles.avatarWrap}>
          <MaterialCommunityIcons
            name={token ? 'account-circle' : 'account-question-outline'}
            size={46}
            color="#fff"
          />
        </View>
        <View style={drawerStyles.headerText}>
          <Text style={drawerStyles.headerName} numberOfLines={1}>{user ? user.fullName : 'Misafir modu'}</Text>
          <Text style={drawerStyles.headerSub} numberOfLines={1}>
            {token ? (user?.email ?? 'Giriş yapıldı') : 'Devam etmek için giriş yapın'}
          </Text>
        </View>
      </View>

      {/* Giriş / Çıkış */}
      <View style={drawerStyles.authRow}>
        {token ? (
          <Button
            mode="contained"
            icon="logout"
            onPress={logout}
            buttonColor={colors.danger}
            style={drawerStyles.logoutBtn}
          >
            Çıkış Yap
          </Button>
        ) : (
          <View style={drawerStyles.authBtns}>
            <Button
              mode="outlined"
              icon="login"
              onPress={() => openAuthModal('login')}
              textColor={colors.primary}
              style={drawerStyles.authBtn}
            >
              Giriş
            </Button>
            <Button
              mode="contained"
              icon="account-plus"
              onPress={() => openAuthModal('register')}
              buttonColor={colors.primary}
              style={drawerStyles.authBtn}
            >
              Kayıt Ol
            </Button>
          </View>
        )}
      </View>

      {/* Standart menü items */}
      <DrawerItemList {...props} />
    </DrawerContentScrollView>
  );
}

const drawerStyles = StyleSheet.create({
  header: { backgroundColor: colors.primary, padding: 18, paddingTop: 38, flexDirection: 'row', alignItems: 'center' },
  avatarWrap: { marginRight: 14 },
  headerText: { flex: 1 },
  headerName: { color: '#fff', fontSize: 17, fontWeight: '700' },
  headerSub: { color: 'rgba(255,255,255,0.85)', fontSize: 12, marginTop: 2 },
  authRow: { paddingHorizontal: 14, paddingVertical: 14, borderBottomWidth: 1, borderBottomColor: colors.border, marginBottom: 6 },
  authBtns: { flexDirection: 'row' },
  authBtn: { flex: 1, marginHorizontal: 3, borderRadius: 10 },
  logoutBtn: { borderRadius: 10 },
});

const SECTIONS = [
  { name: 'Dashboard', title: 'Dashboard', iconName: 'view-dashboard-outline', comp: DashboardScreen },
  { name: 'Blok', title: 'Blok / Bina Yönetimi', iconName: 'office-building', comp: BlokYonetimiScreen },
  { name: 'Daireler', title: 'Daire Yönetimi', iconName: 'home-city-outline', comp: ApartmentsScreen },
  { name: 'Aidat', title: 'Aidat Girişi', iconName: 'currency-try', comp: AidatGirisiScreen },
  { name: 'EkAidat', title: 'Ek Aidatlar', iconName: 'plus-circle-outline', comp: EkAidatScreen },
  { name: 'Gelirler', title: 'Gelirler', iconName: 'trending-up', comp: GelirlerScreen },
  { name: 'Giderler', title: 'Giderler', iconName: 'trending-down', comp: GiderlerScreen },
  { name: 'Raporlar', title: 'Raporlar', iconName: 'file-chart-outline', comp: ReportsScreen },
  { name: 'Premium', title: 'Premium', iconName: 'crown-outline', comp: PremiumScreen },
] as const;

export function AppNavigator() {
  // Misafir (token yok) artık login'e düşmez; Drawer'ı gezer. Write aksiyonunda
  // AuthModal (useRequireAuth üzerinden) açılır. Bkz. hooks/useRequireAuth.ts.
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <NavigationContainer>
        <Drawer.Navigator
          initialRouteName="Dashboard"
          drawerContent={(props) => <CustomDrawerContent {...props} />}
          screenOptions={{
            ...headerOpts,
            drawerActiveTintColor: colors.primary,
            drawerInactiveTintColor: colors.textMuted,
            drawerLabelStyle: { fontSize: 14 },
          }}
        >
          {SECTIONS.map((s) => (
            <Drawer.Screen
              key={s.name}
              name={s.name}
              component={s.comp}
              options={{ title: s.title, drawerIcon: icon(s.iconName) }}
            />
          ))}
          {/* Detay ekranı — menüde gizli */}
          <Drawer.Screen
            name="ApartmentDetail"
            component={ApartmentDetailScreen}
            options={{ title: 'Daire Detayı', drawerItemStyle: { height: 0, overflow: 'hidden' } as any }}
          />
        </Drawer.Navigator>
      </NavigationContainer>
      <AuthModal />
    </GestureHandlerRootView>
  );
}
