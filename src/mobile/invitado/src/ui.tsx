import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Body, radius, space, touch, useTheme, type Schemas } from '@cleancom/mobile-shared';

/** A choice among a few options (línea de servicio, turno, día). */
export function Chip({ label, selected, onPress, disabled, note }: { label: string; selected: boolean; onPress: () => void; disabled?: boolean; note?: string }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityState={{ selected, disabled }}
      style={({ pressed }) => ({
        minHeight: touch,
        paddingHorizontal: space.lg,
        paddingVertical: space.sm,
        borderRadius: radius.lg,
        borderWidth: 1.5,
        borderColor: selected ? colors.brand : colors.border,
        backgroundColor: selected ? colors.brandSoft : pressed ? colors.surface2 : colors.surface,
        justifyContent: 'center',
        opacity: disabled ? 0.45 : 1,
      })}
    >
      <Text style={{ color: selected ? colors.brand : colors.text, fontWeight: selected ? '700' : '500', fontSize: 15 }}>{label}</Text>
      {note && <Text style={{ color: colors.textMuted, fontSize: 12 }}>{note}</Text>}
    </Pressable>
  );
}

/** A tappable row with an icon, a title, a line under it and a chevron. */
export function Row({ icon, title, text, onPress, right }: { icon: keyof typeof Ionicons.glyphMap; title: string; text?: string; onPress?: () => void; right?: ReactNode }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={!onPress}
      accessibilityRole={onPress ? 'button' : undefined}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: space.md,
        padding: space.md,
        borderRadius: radius.md,
        backgroundColor: pressed ? colors.surface2 : colors.surface,
        borderWidth: 1,
        borderColor: colors.border,
      })}
    >
      <View style={{ width: 40, height: 40, borderRadius: 20, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}>
        <Ionicons name={icon} size={21} color={colors.brand} />
      </View>
      <View style={{ flex: 1, gap: 2 }}>
        <Body>{title}</Body>
        {text ? <Body muted>{text}</Body> : null}
      </View>
      {right ?? (onPress ? <Ionicons name="chevron-forward" size={20} color={colors.textMuted} /> : null)}
    </Pressable>
  );
}

/** Loads data from the API and keeps loading, error and a way to reload. */
export function useLoad<T>(load: () => Promise<T>, deps: unknown[]) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const latest = useRef(load);
  useEffect(() => {
    latest.current = load;
  });
  const reload = useCallback(async () => {
    setLoading(true);
    try {
      setData(await latest.current());
      setError(null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => void reload(), deps);
  return { data, error, loading, reload };
}

/** "2026-11-23" → "lun 23 nov". Business dates are already Mexico City days. */
export function day(iso: string | null | undefined, long = false) {
  if (!iso) return '';
  const d = new Date(`${iso.slice(0, 10)}T12:00:00Z`);
  return d.toLocaleDateString('es-MX', { weekday: long ? 'long' : 'short', day: 'numeric', month: long ? 'long' : 'short', timeZone: 'UTC' });
}

/** A time of the API (UTC) in Mexico City, "10:42". */
export function clock(iso: string | null | undefined) {
  return iso ? new Date(iso).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' }) : '';
}

export const TURNOS: { key: Schemas['Turno']; label: string; note: string }[] = [
  { key: 'CualquierHora', label: 'Cualquier hora', note: '9:00 a 18:00' },
  { key: 'Manana', label: 'Mañana', note: '9:00 a 13:00' },
  { key: 'Tarde', label: 'Tarde', note: '13:00 a 18:00' },
];

export const ETAPA_ICON: Record<Schemas['PedidoEtapa'], keyof typeof Ionicons.glyphMap> = {
  Agendado: 'calendar-outline',
  Recolectado: 'bag-handle-outline',
  EnPlanta: 'water-outline',
  Listo: 'checkmark-done-outline',
  EntregaConfirmada: 'calendar-number-outline',
  EnCamino: 'car-outline',
  Entregado: 'home-outline',
  Cancelado: 'close-circle-outline',
};

/** The address in one line. */
export function addressLine(a: Schemas['AddressDto']) {
  const street = [a.street, a.exteriorNumber, a.interiorNumber ? `int. ${a.interiorNumber}` : null].filter(Boolean).join(' ');
  return [street, a.neighborhood].filter(Boolean).join(', ');
}

/** A moment of the API in Mexico City, "lun 23 nov, 10:42". */
export function stamp(iso: string | null | undefined) {
  return iso
    ? new Date(iso).toLocaleString('es-MX', { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' })
    : '';
}
