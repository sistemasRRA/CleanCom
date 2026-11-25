import type { Schemas } from '../../../shared/api/client';

// ---------------- Tiendas y bodegas ----------------

export type Lugar = Schemas['LugarDto'];

export const LUGAR_TYPES: Record<Lugar['type'], string> = { Base: 'Base', Bodega: 'Bodega / insumos', Tienda: 'Tienda 7clean' };

// ---------------- Cobertura ----------------

export type Coverage = Schemas['CoverageDto'];

// ---------------- Otros catálogos ----------------

export type Reason = Schemas['ReasonDto'];

export const REASON_TYPES: Record<Reason['type'], string> = {
  FailedEntrega: 'No entrega',
  PlantaDelay: 'Atraso en planta',
  ContactResult: 'Resultado de contacto',
  HowFoundUs: 'Cómo nos conoció',
};

export type ParadaType = Schemas['ParadaTypeDto'];

export type LineaServicio = Schemas['LineaServicioDto'];
