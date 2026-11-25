import type { Schemas } from '../../../shared/api/client';

export type Aviso = Schemas['AvisoDto'];

export type Plantilla = Schemas['PlantillaDto'];

export const AVISO_STATE: Record<Schemas['AvisoState'], { text: string; tone: string }> = {
  Pendiente: { text: 'Por enviar', tone: '' },
  Enviado: { text: 'Enviado', tone: 'success' },
  Simulado: { text: 'Simulado (prueba)', tone: '' },
  ParaMandarAMano: { text: 'Por mandar a mano', tone: 'warning' },
  MandadoAMano: { text: 'Mandado a mano', tone: 'success' },
  Fallido: { text: 'Falló', tone: 'danger' },
  Omitido: { text: 'No se avisó', tone: '' },
};

export const CANAL: Record<NonNullable<Schemas['AvisoCanal']>, string> = { Push: 'Push', WhatsApp: 'WhatsApp' };
