import { Alert, Body, Button, Card, space, Title, useLayout, useTheme } from '@cleancom/mobile-shared';
import { useState } from 'react';
import { Alert as RNAlert, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { km, METHODS, money } from '../../../common/pagos';
import { type Ruta, time } from '../types/turno';
import { CerrarTurno } from './CerrarTurno';
import { IniciarTurno } from './IniciarTurno';
import { ReportarFalla } from './ReportarFalla';

type Open = 'iniciar' | 'cerrar' | 'falla' | null;

/**
 * Inicio and fin de turno of today's ruta, and "Reportar falla" while there is a unidad. The answers of the API reach
 * the day's list through the offline queue, so this card only opens the forms and says how it went.
 */
export function ShiftCard({ ruta }: { ruta: Ruta | null }) {
  const { colors } = useTheme();
  const { isTablet } = useLayout();
  const [open, setOpen] = useState<Open>(null);
  const shift = ruta?.shift;
  if (!ruta?.isToday || !ruta.ruta || !shift) return null;

  const done = (title: string) => (next: Ruta | null, queued: boolean) => {
    setOpen(null);
    RNAlert.alert(title, queued ? 'Sin señal: quedó guardado en el teléfono y se envía solo al volver la señal.' : (next?.message ?? title));
  };

  const unidad = shift.unidad;
  const falla = unidad && !shift.endedAt && (
    <Button
      title="Reportar falla"
      variant="secondary"
      icon={<Ionicons name="construct" size={18} color={colors.brand} />}
      onPress={() => setOpen('falla')}
    />
  );

  let title: string;
  let detail: string;
  let action: React.ReactNode = null;
  if (!shift.startedAt) {
    title = 'Inicio de turno';
    detail = unidad
      ? `Unidad ${unidad.plate} · ${unidad.name}. Inicia tu turno con el kilometraje del tablero antes de salir a tus paradas.`
      : 'Oficina no te ha asignado unidad hoy. Pide a Logística que te la asigne en Rutas para iniciar tu turno.';
    action = (
      <Button
        title="Iniciar turno"
        disabled={!unidad}
        icon={<Ionicons name="play" size={18} color={colors.onBrand} />}
        onPress={() => setOpen('iniciar')}
      />
    );
  } else if (!shift.endedAt) {
    title = 'Turno en curso';
    detail = `Desde las ${time(shift.startedAt)}${unidad ? ` · ${unidad.plate}` : ''} · ${km(shift.startKm ?? 0)} al salir.`;
    action = (
      <Button title="Cerrar turno" variant="danger" icon={<Ionicons name="stop-circle" size={18} color="#fff" />} onPress={() => setOpen('cerrar')} />
    );
  } else {
    const declared = shift.declared.filter((d) => d.amount > 0);
    title = 'Turno cerrado';
    detail =
      `De ${time(shift.startedAt)} a ${time(shift.endedAt)} · ${km((shift.endKm ?? 0) - (shift.startKm ?? 0))} recorridos. ` +
      (declared.length > 0
        ? `Declaraste ${declared.map((d) => `${money(d.amount)} en ${METHODS.find((m) => m.key === d.method)?.label.toLowerCase()}`).join(', ')}.`
        : 'Declaraste $0 en cobros.');
  }

  return (
    <Card>
      <View style={{ flexDirection: isTablet ? 'row' : 'column', alignItems: isTablet ? 'center' : 'stretch', gap: space.md }}>
        <View style={{ flex: isTablet ? 1 : undefined, gap: space.xs }}>
          <Title size="lg">{title}</Title>
          <Body muted>{detail}</Body>
        </View>
        <View style={{ flexDirection: isTablet ? 'row' : 'column', gap: space.sm }}>
          {falla}
          {action}
        </View>
      </View>
      {unidad && unidad.issues.length > 0 && !shift.endedAt && <Alert tone="warning">{unidad.issues.join(' ')}</Alert>}

      {open === 'iniciar' && unidad && <IniciarTurno unidad={unidad} onClose={() => setOpen(null)} onDone={done('Turno iniciado')} />}
      {open === 'cerrar' && <CerrarTurno shift={shift} onClose={() => setOpen(null)} onDone={done('Turno cerrado')} />}
      {open === 'falla' && unidad && <ReportarFalla unidad={unidad} onClose={() => setOpen(null)} onDone={done('Falla reportada')} />}
    </Card>
  );
}
