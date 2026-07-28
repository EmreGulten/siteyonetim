import React, { useEffect, useState } from 'react';
import { StyleSheet, View, KeyboardAvoidingView, Platform, ScrollView, Modal, TouchableOpacity } from 'react-native';
import { Button, Text, TextInput, Title, ActivityIndicator } from 'react-native-paper';
import { MaterialCommunityIcons } from '@expo/vector-icons';
import { useAuthStore } from '../store/authStore';
import { authApi } from '../api/endpoints';
import { colors } from '../theme/colors';

type Mode = 'login' | 'register';

/**
 * Misafir (giriş yapmamış) kullanıcı bir write aksiyonuna kalkıştığında açılan
 * auth modalı. Tek formda giriş (login) ve kayıt (register) sekmeleri sunar.
 * openAuthModal('register') ile açılır; başarılı giriş/kayıt setAuth üzerinden
 * modalı kapatır ve kullanıcı app içine devam eder.
 */
export function AuthModal() {
  const visible = useAuthStore((s) => s.authModalVisible);
  const storeMode = useAuthStore((s) => s.authModalMode);
  const close = useAuthStore((s) => s.closeAuthModal);
  const setAuth = useAuthStore((s) => s.setAuth);

  const [mode, setMode] = useState<Mode>('login');
  const [email, setEmail] = useState(__DEV__ ? 'yonetici@test.com' : '');
  const [password, setPassword] = useState(__DEV__ ? 'Deneme.12345' : '');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [siteName, setSiteName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Modal her açılışında store'daki mode ile senkronla.
  useEffect(() => {
    if (visible) {
      setMode(storeMode);
      setError(null);
    }
  }, [visible, storeMode]);

  const switchMode = (m: Mode) => {
    setMode(m);
    setError(null);
  };

  const handleSubmit = async () => {
    setLoading(true);
    setError(null);
    try {
      const data =
        mode === 'login'
          ? await authApi.login(email.trim(), password)
          : await authApi.register({
              email: email.trim(),
              password,
              fullName: fullName.trim(),
              phone: phone.trim() || undefined,
              siteName: siteName.trim() || undefined,
            });
      // setAuth modalı da kapatır (authModalVisible: false).
      await setAuth(data.token.accessToken, data, data.token.refreshToken);
    } catch (e: any) {
      setError(e?.response?.data?.detail ?? (mode === 'login'
        ? 'Giriş başarısız. Bilgileri kontrol edin.'
        : 'Kayıt başarısız. Bilgileri kontrol edin.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={close} transparent={false}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={[styles.container, { backgroundColor: colors.bg }]}>
          <View style={styles.header}>
            <Title style={styles.title}>Site & Apartman</Title>
            <Text style={styles.subtitle}>Yönetim Sistemi</Text>
          </View>

          {/* Sekme geçişi — segmented control */}
          <View style={styles.tabs}>
            <TabButton active={mode === 'login'} label="Giriş Yap" icon="login" onPress={() => switchMode('login')} />
            <TabButton active={mode === 'register'} label="Kayıt Ol" icon="account-plus" onPress={() => switchMode('register')} />
          </View>

          <View style={styles.form}>
            {mode === 'register' && (
              <TextInput label="Ad Soyad" value={fullName} onChangeText={setFullName}
                mode="outlined" style={styles.input} />
            )}
            <TextInput label="E-posta" value={email} onChangeText={setEmail}
              mode="outlined" keyboardType="email-address" autoCapitalize="none" style={styles.input} />
            <TextInput label="Parola" value={password} onChangeText={setPassword}
              mode="outlined" secureTextEntry style={styles.input} />
            {mode === 'register' && (
              <>
                <TextInput label="Telefon (opsiyonel)" value={phone} onChangeText={setPhone}
                  mode="outlined" keyboardType="phone-pad" style={styles.input} />
                <TextInput label="Site / Apartman Adı (opsiyonel)" value={siteName} onChangeText={setSiteName}
                  mode="outlined" style={styles.input} />
                <Text style={styles.hint}>
                  Kayıt olduğunuzda kendi siteniz oluşturulur ve eklediğiniz verileri görürsünüz.
                </Text>
              </>
            )}
            {error && <Text style={styles.error}>{error}</Text>}
            <Button mode="contained" onPress={handleSubmit} disabled={loading} style={styles.button}>
              {loading ? <ActivityIndicator color="#fff" /> : mode === 'login' ? 'Giriş Yap' : 'Kayıt Ol'}
            </Button>

            <TouchableOpacity onPress={close} style={styles.closeLink}>
              <Text style={styles.closeText}>Devam et (gezin)</Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </Modal>
  );
}

function TabButton({ active, label, icon, onPress }: { active: boolean; label: string; icon: string; onPress: () => void }) {
  return (
    <TouchableOpacity onPress={onPress} style={[styles.tab, active && styles.tabActive]}>
      {active ? (
        <View style={styles.tabInner}>
          <MaterialCommunityIcons name={icon as any} size={16} color="#fff" />
          <Text style={[styles.tabText, styles.tabTextActive]}>{label}</Text>
        </View>
      ) : (
        <Text style={styles.tabText}>{label}</Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, justifyContent: 'center', padding: 24 },
  header: { alignItems: 'center', marginBottom: 24 },
  title: { fontSize: 26, color: colors.primary },
  subtitle: { color: colors.textMuted, marginTop: 4 },
  tabs: { flexDirection: 'row', backgroundColor: colors.border, borderRadius: 12, padding: 4, marginBottom: 18 },
  tab: { flex: 1, paddingVertical: 10, alignItems: 'center', borderRadius: 9 },
  tabActive: { backgroundColor: colors.primary, elevation: 2 },
  tabInner: { flexDirection: 'row', alignItems: 'center' },
  tabText: { color: colors.textMuted, fontSize: 14, fontWeight: '600' },
  tabTextActive: { color: '#fff', fontWeight: '700', marginLeft: 6 },
  form: {},
  input: { marginBottom: 12 },
  hint: { color: colors.textMuted, fontSize: 12, marginBottom: 12, marginTop: -4 },
  button: { marginTop: 8, paddingVertical: 6, backgroundColor: colors.primary },
  error: { color: colors.danger, marginBottom: 8, textAlign: 'center' },
  closeLink: { alignItems: 'center', marginTop: 20 },
  closeText: { color: colors.textMuted },
});
