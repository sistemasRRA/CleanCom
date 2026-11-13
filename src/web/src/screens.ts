/**
 * Office web screens. Keys are the ones in `Screens` (CleanCom.Domain); the menu only shows
 * the screens the user can view. Names and paths are what the user sees, so they are in Spanish.
 * `week` is when the screen gets built according to docs/05-plan-de-trabajo.md; no week = available.
 */
export type MenuScreen = {
  key: string;
  name: string;
  path: string;
  group: string;
  week?: number;
};

export const SCREENS: MenuScreen[] = [
  { key: 'panel-del-dia', name: 'Panel del día', path: '/panel-del-dia', group: 'Operación diaria', week: 5 },
  { key: 'agendar', name: 'Agendar', path: '/agendar', group: 'Operación diaria' },
  { key: 'rutas', name: 'Rutas', path: '/rutas', group: 'Operación diaria', week: 3 },
  { key: 'paradas-fijas', name: 'Paradas fijas', path: '/paradas-fijas', group: 'Operación diaria', week: 3 },
  { key: 'pedidos', name: 'Pedidos', path: '/pedidos', group: 'Operación diaria', week: 4 },
  { key: 'tickets-en-planta', name: 'Tickets en planta', path: '/tickets-en-planta', group: 'Operación diaria', week: 4 },
  { key: 'vista-del-chofer', name: 'Vista del chofer', path: '/vista-del-chofer', group: 'Operación diaria', week: 5 },
  { key: 'clientes-crm', name: 'Clientes (CRM)', path: '/clientes', group: 'Clientes' },
  { key: 'promociones', name: 'Promociones', path: '/promociones', group: 'Clientes', week: 6 },
  { key: 'whatsapp-y-metas', name: 'WhatsApp y metas', path: '/whatsapp-y-metas', group: 'Clientes', week: 6 },
  { key: 'clientes-y-fuentes', name: 'Clientes y fuentes', path: '/clientes-y-fuentes', group: 'Gerencia', week: 7 },
  { key: 'kpis', name: 'KPIs', path: '/kpis', group: 'Gerencia', week: 7 },
  { key: 'evidencias-y-caja', name: 'Evidencias y caja', path: '/evidencias-y-caja', group: 'Gerencia', week: 7 },
  { key: 'unidades', name: 'Unidades', path: '/unidades', group: 'Administración', week: 5 },
  { key: 'operacion', name: 'Operación', path: '/operacion', group: 'Administración' },
  { key: 'usuarios', name: 'Usuarios', path: '/usuarios', group: 'Administración' },
  { key: 'roles-y-permisos', name: 'Roles y permisos', path: '/roles-y-permisos', group: 'Administración' },
];
