import { Button, radius, space, touch, useTheme } from '@cleancom/mobile-shared';
import { Linking, Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { at, CATEGORY, KIND, type Parada, pointOf, TURNO } from '../types/ruta';
import { openNavigation } from '../../../common/ubicacion';

export function ParadaCard({
  parada: p,
  color,
  isNext,
  canStart,
  busy,
  onEnCamino,
  onLlegue,
  canClose,
  onClose,
}: {
  parada: Parada;
  color?: string;
  isNext: boolean;
  canStart: boolean;
  busy: boolean;
  onEnCamino: () => void;
  onLlegue: () => void;
  canClose: boolean;
  onClose: () => void;
}) {
  const { colors } = useTheme();
  const when = p.turno === 'HoraEspecifica' && p.specificTime ? p.specificTime.slice(0, 5) : TURNO[p.turno];
  const done = p.state === 'Realizada' || p.state === 'NoRealizada';

  return (
    <View
      style={{
        gap: space.sm,
        padding: space.md,
        borderRadius: radius.md,
        borderWidth: isNext ? 2 : 1,
        borderColor: isNext ? colors.brand : colors.border,
        borderLeftWidth: 5,
        borderLeftColor: color ?? colors.brand,
        backgroundColor: colors.surface,
        opacity: done ? 0.55 : 1,
      }}
    >
      <View style={{ flexDirection: 'row', gap: space.md, alignItems: 'flex-start' }}>
        <View style={{ alignItems: 'center', gap: 2 }}>
          <View
            style={{ width: 36, height: 36, borderRadius: 18, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}
          >
            <Text style={{ color: colors.brand, fontWeight: '700' }}>{p.order}</Text>
          </View>
          {p.estimatedArrival && <Text style={{ color: colors.textMuted, fontSize: 12, fontWeight: '600' }}>{p.estimatedArrival.slice(0, 5)}</Text>}
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Text style={{ color: colors.text, fontWeight: '700', fontSize: 16 }}>{p.invitadoName ?? KIND[p.kind]}</Text>
          <Text style={{ color: colors.textMuted, fontSize: 13 }}>
            {p.category && p.category !== 'ClienteFinal' ? `${CATEGORY[p.category]} · ` : ''}
            {KIND[p.kind]} · {when}
            {p.zonaCode ? ` · ${p.zonaCode}` : ''}
            {p.priority ? ' · PRIORIDAD' : ''}
          </Text>
          <StateLine parada={p} />
          {p.viaje && <Text style={{ color: colors.brand, fontSize: 13, fontWeight: '600' }}>{p.viaje}</Text>}
          {p.activity && <Text style={{ color: colors.text, fontSize: 14, fontWeight: '600' }}>{p.activity}</Text>}
          {p.before && <Text style={{ color: colors.warning, fontSize: 13, fontWeight: '600' }}>Antes de las {p.before.slice(0, 5)}</Text>}
          {p.address && <Text style={{ color: colors.text, fontSize: 14 }}>{p.address}</Text>}
          {p.references && <Text style={{ color: colors.textMuted, fontSize: 13 }}>Ref.: {p.references}</Text>}
          {(p.lineasServicio.length > 0 || p.garmentsApprox != null) && (
            <Text style={{ color: colors.textMuted, fontSize: 13 }}>
              {p.lineasServicio.join(', ')}
              {p.garmentsApprox != null && `${p.lineasServicio.length ? ' · ' : ''}${p.garmentsApprox} prendas aprox.`}
            </Text>
          )}
          {p.notes && <Text style={{ color: colors.warning, fontSize: 13 }}>Nota: {p.notes}</Text>}
          {(p.beneficios ?? []).map((b) => (
            // A sello or cupón the invitado applied: tell planta; the POS applies the discount.
            <View
              key={b.code}
              style={{
                flexDirection: 'row',
                gap: space.xs,
                alignItems: 'center',
                backgroundColor: colors.successSoft,
                borderRadius: radius.sm,
                paddingHorizontal: space.sm,
                paddingVertical: 4,
                marginTop: 2,
              }}
            >
              <Ionicons name="gift" size={15} color={colors.success} />
              <Text style={{ color: colors.text, fontSize: 13, flex: 1 }}>
                <Text style={{ fontWeight: '700' }}>{b.name}</Text> · {b.mechanic}
                {b.condition ? ` · ${b.condition}` : ''} · {b.code}
                {b.used ? ' · usado' : ''}
              </Text>
            </View>
          ))}
        </View>
      </View>
      {canStart && (
        <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
          <Button title="En camino" icon={<Ionicons name="car" size={18} color={colors.onBrand} />} busy={busy} onPress={onEnCamino} />
          <Button title="Llegué" variant="secondary" disabled={busy} onPress={onLlegue} />
          <Button title="Cerrar visita" variant="ghost" disabled={busy} onPress={onClose} />
        </View>
      )}
      {canClose && <Button title="Cerrar visita" icon={<Ionicons name="checkmark-done" size={18} color={colors.onBrand} />} onPress={onClose} />}
      <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
        {p.phone && <ActionButton icon="call" label="Llamar" onPress={() => void Linking.openURL(`tel:${p.phone}`)} />}
        {p.phone && <ActionButton icon="logo-whatsapp" label="WhatsApp" onPress={() => void Linking.openURL(`https://wa.me/52${p.phone}`)} />}
        {(pointOf(p) || p.address) && (
          <ActionButton
            icon="navigate"
            label="Cómo llegar"
            onPress={() => openNavigation(p.invitadoName ?? KIND[p.kind], pointOf(p), p.address ?? null)}
          />
        )}
      </View>
    </View>
  );
}

function StateLine({ parada: p }: { parada: Parada }) {
  const { colors } = useTheme();
  if (p.state === 'EnCamino') {
    return <Text style={{ color: colors.brand, fontSize: 13, fontWeight: '700' }}>En camino desde las {at(p.enCaminoAt)}</Text>;
  }
  if (p.state === 'EnSitio') {
    return (
      <Text style={{ color: p.arrivedOutsideRadius ? colors.warning : colors.success, fontSize: 13, fontWeight: '700' }}>
        Llegaste a las {at(p.arrivedAt)}
        {p.arrivedOutsideRadius && p.arrivedDistanceMeters != null && ` · a ${p.arrivedDistanceMeters} m (fuera de radio)`}
        {' · falta cerrar la visita'}
      </Text>
    );
  }
  if (p.state === 'Realizada' || p.state === 'NoRealizada') {
    const done = p.state === 'Realizada';
    return (
      <Text style={{ color: done ? colors.success : colors.danger, fontSize: 13, fontWeight: '700' }}>
        {done ? 'Realizada' : 'No realizada'}
        {p.closedAt && ` a las ${at(p.closedAt)}`}
        {p.photos > 0 && ` · ${p.photos} foto${p.photos === 1 ? '' : 's'}`}
        {p.signed && ' · firmada'}
      </Text>
    );
  }
  return null;
}

function ActionButton({ icon, label, onPress }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void }) {
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
