import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of Evidencias y caja: the screens never call `api` directly. */
export const evidenciasApi = {
  /** GET /visitas */
  visitas: (query: Query<'/visitas', 'get'>) => unwrap(api.GET('/visitas', { params: { query } })),
  /** GET /visitas/exportar (Excel workbook) */
  exportar: (query: Query<'/visitas/exportar', 'get'>) => unwrap(api.GET('/visitas/exportar', { params: { query }, parseAs: 'blob' })),
  /** GET /pedidos/{id}/evidencias */
  galeria: (id: number) => unwrap(api.GET('/pedidos/{id}/evidencias', { params: { path: { id } } })),
  /** GET /cortes */
  cortes: (query: Query<'/cortes', 'get'>) => unwrap(api.GET('/cortes', { params: { query } })),
  /** POST /cortes/{id}/cerrar */
  cerrarCorte: (id: number, body: Body<'/cortes/{id}/cerrar', 'post'>) =>
    unwrap(api.POST('/cortes/{id}/cerrar', { params: { path: { id } }, body })),
  /** POST /cortes/{id}/reabrir */
  reabrirCorte: (id: number, body: Body<'/cortes/{id}/reabrir', 'post'>) =>
    unwrap(api.POST('/cortes/{id}/reabrir', { params: { path: { id } }, body })),
  /** POST /rutas/dia/{id}/turno/cerrar */
  cerrarTurno: (rutaDiaId: number, body: Body<'/rutas/dia/{id}/turno/cerrar', 'post'>) =>
    unwrap(api.POST('/rutas/dia/{id}/turno/cerrar', { params: { path: { id: rutaDiaId } }, body })),
};
