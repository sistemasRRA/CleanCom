import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { type Schemas } from '../api/client';
import type { Point } from './LocationMap';
import { Field } from './Form';
import { Loader } from './Loader';
import { direccionesApi } from '../api/direccionesApi';

// Leaflet (~150 kB) loads only when an address form opens.
const LocationMap = lazy(() => import('./LocationMap').then((m) => ({ default: m.LocationMap })));

export type AddressQuery = { street?: string; exteriorNumber?: string; neighborhood?: string; borough?: string; postalCode?: string };

const PRECISION: Record<NonNullable<Schemas['GeocodePrecision']>, string> = {
  Direccion: 'Ubicada por la dirección. Revisa que el pin esté en el lugar.',
  Calle: 'Ubicada por la calle; el número no está en el mapa. Mueve el pin al lugar.',
  Colonia: 'Solo se encontró la colonia. Mueve el pin al lugar.',
  CodigoPostal: 'Solo se encontró el código postal. Mueve el pin al lugar.',
};

/**
 * "19.3608, -99.1968", or a Google Maps link with the point in it ("@19.36,-99.19", "!3d19.36!4d-99.19",
 * "q=19.36,-99.19"), to a point for the map. Null when there is none; the API still reads the text when saving.
 */
export function parsePoint(text: string): Point | null {
  const t = text.trim();
  const patterns = [
    /^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/,
    /!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)/,
    /@(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/,
    /[?&](?:q|query|ll)=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/,
  ];
  for (const p of patterns) {
    const m = t.match(p);
    if (m) {
      const lat = Number(m[1]);
      const lng = Number(m[2]);
      if (Math.abs(lat) <= 90 && Math.abs(lng) <= 180) return { lat, lng };
    }
  }
  return null;
}

const clean = (s?: string) => (s && s.trim() !== '' ? s.trim() : undefined);

/**
 * Coordinates field with a small map. With enough address typed (<paramref name="canLocate"/>), the API places the
 * pin (GET /direcciones/ubicar) and the user drags it or clicks the map. A point the user placed, or one the address
 * already had, is not overwritten by the automatic search, only by "Ubicar con la dirección".
 */
export function AddressLocation({
  query,
  canLocate,
  value,
  onChange,
  label,
  placeholder,
  hint,
}: {
  query: AddressQuery;
  canLocate: boolean;
  value: string;
  onChange: (coordinates: string) => void;
  label: string;
  placeholder?: string;
  hint: string;
}) {
  const latest = useRef(query);
  latest.current = query;
  const [manual, setManual] = useState(value.trim() !== '');
  const [locating, setLocating] = useState(false);
  const [note, setNote] = useState<string | null>(null);

  async function locate() {
    const q = latest.current;
    setLocating(true);
    try {
      const r = await direccionesApi.locate({
        street: clean(q.street),
        exteriorNumber: clean(q.exteriorNumber),
        neighborhood: clean(q.neighborhood),
        borough: clean(q.borough),
        postalCode: clean(q.postalCode),
      });
      if (r.found && r.lat != null && r.lng != null) {
        onChange(`${r.lat}, ${r.lng}`);
        setManual(false);
        setNote(r.precision ? PRECISION[r.precision] : null);
      } else {
        setNote('No se encontró en el mapa. Haz clic en el mapa donde está el lugar.');
      }
    } catch (e) {
      setNote((e as Error).message);
    } finally {
      setLocating(false);
    }
  }

  const key = [query.street, query.exteriorNumber, query.neighborhood, query.borough, query.postalCode].join('|');
  useEffect(() => {
    if (manual || !canLocate) return;
    const t = setTimeout(() => void locate(), 1000);
    return () => clearTimeout(t);
    // Again when the typed address changes, after a pause.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [manual, canLocate, key]);

  return (
    <div className="wide location-field">
      <Field label={label} hint={locating ? 'Buscando la dirección en el mapa…' : (note ?? hint)}>
        <input
          value={value}
          onChange={(e) => {
            setManual(e.target.value.trim() !== '');
            setNote(null);
            onChange(e.target.value);
          }}
          placeholder={placeholder}
        />
      </Field>
      <button
        type="button"
        className="small"
        disabled={!canLocate || locating}
        onClick={() => void locate()}
        title={canLocate ? undefined : 'Captura la dirección primero'}
      >
        Ubicar con la dirección
      </button>
      <Suspense
        fallback={
          <div className="location-map">
            <Loader label="Cargando mapa…" />
          </div>
        }
      >
        <LocationMap
          point={parsePoint(value)}
          onChange={(p) => {
            setManual(true);
            setNote('Pin colocado a mano.');
            onChange(`${p.lat}, ${p.lng}`);
          }}
        />
      </Suspense>
    </div>
  );
}
