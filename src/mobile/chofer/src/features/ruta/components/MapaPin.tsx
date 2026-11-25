import { useTheme } from '@cleancom/mobile-shared';
import { Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { done, type Parada } from '../types/mapa';

/** Numbered circle: ruta color while pending, bigger for the next one, muted when closed. */
export function Pin({ parada: p, color, isNext }: { parada: Parada; color: string; isNext: boolean }) {
  const { colors } = useTheme();
  const size = isNext ? 38 : 30;
  const background = p.state === 'NoRealizada' ? colors.danger : done(p) ? colors.textMuted : p.state === 'Pendiente' ? color : colors.success;
  return (
    <View
      style={{
        width: size,
        height: size,
        borderRadius: size / 2,
        backgroundColor: background,
        borderWidth: isNext ? 3 : 2,
        borderColor: '#ffffff',
        alignItems: 'center',
        justifyContent: 'center',
        opacity: done(p) ? 0.75 : 1,
      }}
    >
      {done(p) ? (
        <Ionicons name={p.state === 'Realizada' ? 'checkmark' : 'close'} size={16} color="#ffffff" />
      ) : (
        <Text style={{ color: '#ffffff', fontWeight: '800', fontSize: isNext ? 16 : 14 }}>{p.order}</Text>
      )}
    </View>
  );
}
