import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Linking, Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Card, Title, api, radius, space, touch, unwrap, useTheme, type Schemas } from '@cleancom/mobile-shared';

type Ruta = Schemas['ChoferRutaDto'];
type Parada = Schemas['ChoferParadaDto'];

const KIND: Record<Schemas['ParadaKind'], string> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Recolección y entrega',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
  Salida: 'Salida',
  Regreso: 'Regreso',
  Comida: 'Comida',
};
const TURNO: Record<Schemas['Turno'], string> = {
  Manana: 'Mañana',
  Tarde: 'Tarde',
  CualquierHora: 'Cualquier hora',
  HoraEspecifica: 'Hora específica',
};

/** yyyy-MM-dd plus n days, without time zone shifts. */
function addDays(iso: string, n: number) {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d + n)).toISOString().slice(0, 10);
}

/**
 * The chofer's paradas on a day (GET /chofer/ruta). The first load asks for today: the API decides what today is
 * in Mexico City, and the arrows move from there.
 */
export function RutaDelDia() {
  const { colors } = useTheme();
  const [date, setDate] = useState<string | null>(null);
  const [ruta, setRuta] = useState<Ruta | null>(null);
  const [today, setToday] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (fecha: string | null) => {
    setLoading(true);
    setError(null);
    try {
      const data = await unwrap(api.GET('/chofer/ruta', { params: { query: fecha ? { fecha } : {} } }));
      setRuta(data);
      if (!fecha) setToday(data.date);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load(date);
  }, [date, load]);

  const current = ruta?.date ?? date;
  const pending = ruta?.paradas.filter((p) => p.state !== 'Realizada' && p.state !== 'NoRealizada').length ?? 0;

  return (
    <Card>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm, flexWrap: 'wrap' }}>
        <Ionicons name="list" size={20} color={colors.brand} />
        <View style={{ flex: 1, minWidth: 160 }}>
          <Title size="lg">{ruta?.ruta ? `Ruta ${ruta.ruta.code}` : 'Paradas del día'}</Title>
          {ruta && (
            <Text style={{ color: colors.textMuted, fontSize: 13, textTransform: 'capitalize' }}>
              {ruta.label}
              {ruta.paradas.length > 0 && ` · ${ruta.paradas.length} paradas, ${pending} pendientes`}
            </Text>
          )}
        </View>
        <DayButton icon="chevron-back" label="Día anterior" disabled={!current || loading} onPress={() => current && setDate(addDays(current, -1))} />
        <DayButton icon="today-outline" label="Hoy" disabled={loading || current === today} onPress={() => setDate(today)} />
        <DayButton icon="chevron-forward" label="Día siguiente" disabled={!current || loading} onPress={() => current && setDate(addDays(current, 1))} />
        <DayButton icon="refresh" label="Actualizar" disabled={loading} onPress={() => void load(date)} />
      </View>

      {loading && !ruta && <ActivityIndicator color={colors.brand} style={{ padding: space.lg }} />}
      {error && <Alert>{error}</Alert>}
      {ruta?.notice && <Alert tone="info">{ruta.notice}</Alert>}
      {ruta?.ruta && ruta.paradas.length > 0 && !ruta.assignedByOffice && (
        <Body muted>Es tu ruta habitual. El orden es provisional por turno hasta que oficina optimice la ruta.</Body>
      )}

      <View style={{ gap: space.sm, opacity: loading ? 0.5 : 1 }}>
        {ruta?.paradas.map((p) => (
          <ParadaCard key={p.id} parada={p} color={ruta.ruta?.color} />
        ))}
      </View>
    </Card>
  );
}

function ParadaCard({ parada: p, color }: { parada: Parada; color?: string }) {
  const { colors } = useTheme();
  const when = p.turno === 'HoraEspecifica' && p.specificTime ? p.specificTime.slice(0, 5) : TURNO[p.turno];
  const done = p.state === 'Realizada' || p.state === 'NoRealizada';
  const destination = p.lat != null && p.lng != null ? `${p.lat},${p.lng}` : p.address;

  return (
    <View
      style={{
        gap: space.sm,
        padding: space.md,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        borderLeftWidth: 5,
        borderLeftColor: color ?? colors.brand,
        backgroundColor: colors.surface,
        opacity: done ? 0.55 : 1,
      }}
    >
      <View style={{ flexDirection: 'row', gap: space.md, alignItems: 'flex-start' }}>
        <View style={{ width: 36, height: 36, borderRadius: 18, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}>
          <Text style={{ color: colors.brand, fontWeight: '700' }}>{p.order}</Text>
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Text style={{ color: colors.text, fontWeight: '700', fontSize: 16 }}>{p.invitadoName ?? KIND[p.kind]}</Text>
          <Text style={{ color: colors.textMuted, fontSize: 13 }}>
            {KIND[p.kind]} · {when}
            {p.zonaCode ? ` · ${p.zonaCode}` : ''}
            {p.priority ? ' · PRIORIDAD' : ''}
          </Text>
          {p.address && <Text style={{ color: colors.text, fontSize: 14 }}>{p.address}</Text>}
          {p.references && <Text style={{ color: colors.textMuted, fontSize: 13 }}>Ref.: {p.references}</Text>}
          {(p.lineasServicio.length > 0 || p.garmentsApprox != null) && (
            <Text style={{ color: colors.textMuted, fontSize: 13 }}>
              {p.lineasServicio.join(', ')}
              {p.garmentsApprox != null && `${p.lineasServicio.length ? ' · ' : ''}${p.garmentsApprox} prendas aprox.`}
            </Text>
          )}
          {p.notes && <Text style={{ color: colors.warning, fontSize: 13 }}>Nota: {p.notes}</Text>}
        </View>
      </View>
      <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
        {p.phone && <Action icon="call" label="Llamar" onPress={() => void Linking.openURL(`tel:${p.phone}`)} />}
        {p.phone && <Action icon="logo-whatsapp" label="WhatsApp" onPress={() => void Linking.openURL(`https://wa.me/52${p.phone}`)} />}
        {destination && (
          <Action
            icon="navigate"
            label="Cómo llegar"
            onPress={() => void Linking.openURL(`https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}`)}
          />
        )}
      </View>
    </View>
  );
}

function Action({ icon, label, onPress }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={label}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
        minHeight: touch - 4,
        paddingHorizontal: space.md,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: pressed ? colors.surface2 : colors.surface,
      })}
    >
      <Ionicons name={icon} size={18} color={colors.brand} />
      <Text style={{ color: colors.text, fontWeight: '600' }}>{label}</Text>
    </Pressable>
  );
}

function DayButton({ icon, label, onPress, disabled }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void; disabled?: boolean }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityLabel={label}
      hitSlop={4}
      style={({ pressed }) => ({
        width: touch - 4,
        height: touch - 4,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: pressed ? colors.surface2 : colors.surface,
        opacity: disabled ? 0.4 : 1,
      })}
    >
      <Ionicons name={icon} size={20} color={colors.text} />
    </Pressable>
  );
}
