import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of operacion: the screens never call `api` directly. */
export const operacionApi = {
  /** GET /cobertura */
  listCobertura: (query: Query<'/cobertura', 'get'>) => unwrap(api.GET('/cobertura', { params: { query } })),
  /** POST /cobertura */
  createCobertura: (body: Body<'/cobertura', 'post'>) => unwrap(api.POST('/cobertura', { body })),
  /** DELETE /cobertura/{id} */
  removeCobertura: (id: number) => unwrap(api.DELETE('/cobertura/{id}', { params: { path: { id } } })),
  /** PUT /cobertura/{id} */
  updateCobertura: (id: number, body: Body<'/cobertura/{id}', 'put'>) => unwrap(api.PUT('/cobertura/{id}', { params: { path: { id } }, body })),
  /** GET /codigos-postales */
  searchPostalCodes: (query: Query<'/codigos-postales', 'get'>) => unwrap(api.GET('/codigos-postales', { params: { query } })),
  /** GET /codigos-postales/{code} */
  postalCode: (code: string) => unwrap(api.GET('/codigos-postales/{code}', { params: { path: { code } } })),
  /** POST /codigos-postales/importar */
  importPostalCodes: (body: Body<'/codigos-postales/importar', 'post'>) =>
    unwrap(
      api.POST('/codigos-postales/importar', {
        body,
        bodySerializer: (body) => {
          const data = new FormData();
          data.append('file', body.file as unknown as Blob);
          return data;
        },
      }),
    ),
  /** GET /codigos-postales/resumen */
  postalCodesSummary: () => unwrap(api.GET('/codigos-postales/resumen')),
  /** GET /configuracion */
  listSettings: () => unwrap(api.GET('/configuracion')),
  /** PUT /configuracion */
  updateSetting: (body: Body<'/configuracion', 'put'>) => unwrap(api.PUT('/configuracion', { body })),
  /** GET /configuracion/historial */
  settingHistory: (query: Query<'/configuracion/historial', 'get'>) => unwrap(api.GET('/configuracion/historial', { params: { query } })),
  /** GET /dias-inhabiles */
  listNonWorkingDays: () => unwrap(api.GET('/dias-inhabiles')),
  /** POST /dias-inhabiles */
  createNonWorkingDay: (body: Body<'/dias-inhabiles', 'post'>) => unwrap(api.POST('/dias-inhabiles', { body })),
  /** DELETE /dias-inhabiles/{id} */
  removeNonWorkingDay: (id: number) => unwrap(api.DELETE('/dias-inhabiles/{id}', { params: { path: { id } } })),
  /** GET /lineas-servicio */
  listLineasServicio: () => unwrap(api.GET('/lineas-servicio')),
  /** POST /lineas-servicio */
  createLineaServicio: (body: Body<'/lineas-servicio', 'post'>) => unwrap(api.POST('/lineas-servicio', { body })),
  /** PUT /lineas-servicio/{id} */
  updateLineaServicio: (id: number, body: Body<'/lineas-servicio/{id}', 'put'>) =>
    unwrap(api.PUT('/lineas-servicio/{id}', { params: { path: { id } }, body })),
  /** GET /lugares */
  listLugares: (query?: Query<'/lugares', 'get'>) => unwrap(api.GET('/lugares', { params: { query } })),
  /** POST /lugares */
  createLugar: (body: Body<'/lugares', 'post'>) => unwrap(api.POST('/lugares', { body })),
  /** DELETE /lugares/{id} */
  removeLugar: (id: number) => unwrap(api.DELETE('/lugares/{id}', { params: { path: { id } } })),
  /** PUT /lugares/{id} */
  updateLugar: (id: number, body: Body<'/lugares/{id}', 'put'>) => unwrap(api.PUT('/lugares/{id}', { params: { path: { id } }, body })),
  /** GET /motivos */
  listMotivos: (query?: Query<'/motivos', 'get'>) => unwrap(api.GET('/motivos', { params: { query } })),
  /** POST /motivos */
  createMotivo: (body: Body<'/motivos', 'post'>) => unwrap(api.POST('/motivos', { body })),
  /** PUT /motivos/{id} */
  updateMotivo: (id: number, body: Body<'/motivos/{id}', 'put'>) => unwrap(api.PUT('/motivos/{id}', { params: { path: { id } }, body })),
  /** GET /rutas */
  listRutas: () => unwrap(api.GET('/rutas')),
  /** POST /rutas */
  createRuta: (body: Body<'/rutas', 'post'>) => unwrap(api.POST('/rutas', { body })),
  /** PUT /rutas/{id} */
  updateRuta: (id: number, body: Body<'/rutas/{id}', 'put'>) => unwrap(api.PUT('/rutas/{id}', { params: { path: { id } }, body })),
  /** GET /tipos-parada */
  listParadaTypes: () => unwrap(api.GET('/tipos-parada')),
  /** POST /tipos-parada */
  createParadaType: (body: Body<'/tipos-parada', 'post'>) => unwrap(api.POST('/tipos-parada', { body })),
  /** PUT /tipos-parada/{id} */
  updateParadaType: (id: number, body: Body<'/tipos-parada/{id}', 'put'>) =>
    unwrap(api.PUT('/tipos-parada/{id}', { params: { path: { id } }, body })),
  /** GET /zonas */
  listZonas: (query?: Query<'/zonas', 'get'>) => unwrap(api.GET('/zonas', { params: { query } })),
  /** POST /zonas */
  createZona: (body: Body<'/zonas', 'post'>) => unwrap(api.POST('/zonas', { body })),
  /** DELETE /zonas/{id} */
  removeZona: (id: number) => unwrap(api.DELETE('/zonas/{id}', { params: { path: { id } } })),
  /** PUT /zonas/{id} */
  updateZona: (id: number, body: Body<'/zonas/{id}', 'put'>) => unwrap(api.PUT('/zonas/{id}', { params: { path: { id } }, body })),
  /** PUT /zonas/{id}/dias */
  updateZonaDays: (id: number, body: Body<'/zonas/{id}/dias', 'put'>) => unwrap(api.PUT('/zonas/{id}/dias', { params: { path: { id } }, body })),
};
