import { useState } from 'react';
import { mapZonas, toPoints, type Zona } from '../types/rutasZonas';
import { useRutas } from '../services/queries';
import { useConfirm } from '../../../shared/components/Toasts';
import type { Point } from '../../../shared/components/LocationMap';
import { useSave } from '../../../shared/hooks/useSave';
import { type Schemas } from '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { PolygonMap } from './PolygonMap';
import { operacionApi } from '../services/operacionApi';

export function ZonaForm({ zona, zonas, onClose }: { zona: Zona | null; zonas: Zona[]; onClose: () => void }) {
  const rutas = useRutas();
  const [code, setCode] = useState(zona?.code ?? '');
  const [rutaId, setRutaId] = useState(zona?.rutaId.toString() ?? '');
  const [nucleoId, setNucleoId] = useState(zona?.zonaNucleoId?.toString() ?? '');
  const [polygon, setPolygon] = useState(zona?.polygon ? polygonText(toPoints(zona.polygon)) : '');
  const [active, setActive] = useState(zona?.active ?? true);
  const ask = useConfirm();
  const nucleos = zonas.filter((z) => !z.zonaNucleoId && z.id !== zona?.id && z.active);
  // The map edits the same text: what is typed or pasted shows on the map, and what is drawn rewrites the text.
  const vertices = toPoints(parsePolygon(polygon) ?? []);
  const setVertices = (list: Point[]) => setPolygon(polygonText(list));
  const color = rutas.data?.find((r) => r.id === Number(rutaId))?.color;
  const others = mapZonas(
    zonas.filter((z) => z.id !== zona?.id),
    rutas.data ?? [],
  );

  const save = useSave(
    () => {
      const body = {
        code: code.trim(),
        rutaId: Number(rutaId),
        zonaNucleoId: nucleoId ? Number(nucleoId) : null,
        polygon: parsePolygon(polygon),
        active,
      };
      return zona ? operacionApi.updateZona(zona.id, body) : operacionApi.createZona(body);
    },
    { refetch: [keys.zonas], message: zona ? 'Zona actualizada.' : 'Zona creada.', onDone: onClose },
  );

  return (
    <Dialog
      title={zona ? `Zona ${zona.code}` : 'Nueva zona'}
      className="map-dialog"
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Código" hint="Por ejemplo F1 o F1EXT.">
          <input autoFocus value={code} onChange={(e) => setCode(e.target.value)} required />
        </Field>
        <Field label="Ruta">
          <select value={rutaId} onChange={(e) => setRutaId(e.target.value)} required>
            <option value="">Elige…</option>
            {rutas.data?.map((r) => (
              <option key={r.id} value={r.id}>
                {r.code} · {r.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Núcleo" hint="Solo para zonas EXT.">
          <select value={nucleoId} onChange={(e) => setNucleoId(e.target.value)}>
            <option value="">Es núcleo</option>
            {nucleos.map((z) => (
              <option key={z.id} value={z.id}>
                {z.code}
              </option>
            ))}
          </select>
        </Field>
        <div className="field wide">
          <div className="polygon-toolbar">
            <span>Polígono</span>
            <span className="muted">{vertices.length === 0 ? 'sin dibujar' : `${vertices.length} vértices`}</span>
            <button type="button" className="small" disabled={vertices.length === 0} onClick={() => setVertices(vertices.slice(0, -1))}>
              Deshacer último
            </button>
            <button
              type="button"
              className="small danger"
              disabled={vertices.length === 0}
              onClick={async () =>
                (await ask('¿Borrar todo el polígono?', {
                  detail: 'Se quita del mapa; se guarda hasta que presiones Guardar.',
                  confirmText: 'Borrar',
                  danger: true,
                })) && setPolygon('')
              }
            >
              Borrar polígono
            </button>
          </div>
          <PolygonMap value={vertices} color={color} others={others} onChange={setVertices} />
          <small>
            Haz clic en el mapa para agregar vértices. Arrastra un vértice para moverlo o haz clic en él para quitarlo; arrastra el punto pequeño
            entre dos vértices para agregar uno en medio. Las demás zonas se ven de fondo. Sin polígono, la zona se busca por código postal.
          </small>
        </div>
        <Field label="Coordenadas" hint="Un vértice por renglón: latitud, longitud. Puedes pegarlas aquí y se dibujan en el mapa." wide>
          <textarea
            rows={4}
            value={polygon}
            onChange={(e) => setPolygon(e.target.value)}
            placeholder={'19.3550, -99.1900\n19.3550, -99.1800\n19.3450, -99.1800'}
          />
        </Field>
        <Checkbox wide checked={active} onChange={setActive}>
          Activa
        </Checkbox>
      </div>
    </Dialog>
  );
}

const polygonText = (list: Point[]) => list.map((v) => `${v.lat}, ${v.lng}`).join('\n');

/** Only splits the text into numbers; the API validates and closes the polygon. */
function parsePolygon(text: string): Schemas['Vertex'][] | null {
  const lines = text
    .split('\n')
    .map((l) => l.trim())
    .filter(Boolean);
  if (lines.length === 0) return null;
  return lines.map((l) => {
    const [lat, lng] = l.split(',').map((n) => Number(n.trim()));
    return { lat, lng };
  });
}
