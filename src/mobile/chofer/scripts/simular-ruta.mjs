// Development tool: plays a chofer driving today's ruta, so the office can follow it on the live map without a phone.
// It signs in as a real chofer user and sends GPS readings to POST /chofer/ubicacion, the same endpoint the app uses
// in the background. It goes in a straight line from parada to parada (no street routing).
//
//   npm run simular -- --usuario luis.chofer [--visitas] [--reiniciar]
//                      [--api http://localhost:5111] [--kmh 30] [--rapido 10] [--cada 3] [--pausa 15]
//
// --visitas   Also marks En camino when leaving for each parada and Llegué when reaching it, through the same endpoints
//             as the app, so the ruta goes "en ruta" in the office. It never closes visits.
// --reiniciar Before starting, puts back as Pendiente the paradas of today's ruta that are En camino or En sitio (not the
//             closed ones), and an entrega's pedido back to the etapa it had before En camino, with an event in its
//             history. Straight to the development database in `dotnet user-secrets` of the API, since the app has
//             no way back; only with a local API. Alone (without --visitas) it only resets and ends.
//
// The password comes from CHOFER_PASSWORD, or is asked for. Readings and actions are saved like real ones.
import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createInterface } from 'node:readline/promises';

process.on('uncaughtException', (e) => {
  console.error(e.message);
  process.exit(1);
});

/** --name value, or --name alone for a switch. */
const args = {};
const argv = process.argv.slice(2);
for (let i = 0; i < argv.length; i++) {
  if (!argv[i].startsWith('--')) continue;
  const value = argv[i + 1];
  args[argv[i].slice(2)] = value === undefined || value.startsWith('--') ? true : (i++, value);
}
const api = (args.api ?? process.env.API_URL ?? 'http://localhost:5111').replace(/\/$/, '');
const userName = args.usuario;
const visits = args.visitas === true;
const reset = args.reiniciar === true;
const kmh = Number(args.kmh ?? 30); // Simulated speed.
const fast = Number(args.rapido ?? 10); // Time multiplier: 10 = ten times faster than real.
const every = Number(args.cada ?? 3); // Real seconds between readings.
const pause = Number(args.pausa ?? 15); // Real seconds stopped at each parada.

if (!userName || userName === true) {
  console.error('Falta --usuario (por ejemplo: npm run simular -- --usuario luis.chofer).');
  process.exit(1);
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let token = '';

async function signIn() {
  let password = process.env.CHOFER_PASSWORD;
  if (!password) {
    const rl = createInterface({ input: process.stdin, output: process.stdout });
    password = await rl.question(`Contraseña de ${userName}: `);
    rl.close();
  }
  const res = await fetch(`${api}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userName, password }),
  });
  if (!res.ok) throw new Error(`No se pudo iniciar sesión: ${await problem(res)}`);
  token = (await res.json()).accessToken;
  process.env.CHOFER_PASSWORD = password; // To sign in again if the token expires.
}

async function call(method, path, body) {
  for (let attempt = 0; ; attempt++) {
    const res = await fetch(`${api}${path}`, {
      method,
      headers: { Authorization: `Bearer ${token}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
    if (res.status === 401 && attempt === 0) {
      await signIn();
      continue;
    }
    if (!res.ok) throw new Error(`${method} ${path}: ${await problem(res)}`);
    return res.json();
  }
}

/** The Spanish message of an API error (ProblemDetails), or the status. */
async function problem(res) {
  const text = await res.text();
  try {
    const p = JSON.parse(text);
    return p.detail ?? p.title ?? `${res.status}`;
  } catch {
    return text || `${res.status}`;
  }
}

/** Meters between two points (haversine). */
function distance(a, b) {
  const rad = Math.PI / 180;
  const dLat = (b.lat - a.lat) * rad;
  const dLng = (b.lng - a.lng) * rad;
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(a.lat * rad) * Math.cos(b.lat * rad) * Math.sin(dLng / 2) ** 2;
  return 2 * 6_371_000 * Math.asin(Math.sqrt(h));
}

async function send(point) {
  // A little noise, like a real phone.
  const jitter = () => (Math.random() - 0.5) * 0.00008;
  const result = await call('POST', '/chofer/ubicacion', {
    readings: [{ lat: point.lat + jitter(), lng: point.lng + jitter(), accuracyMeters: 8 + Math.random() * 10 }],
  });
  if (!result.accepted) throw new Error(result.notice ?? 'El API no aceptó la ubicación.');
}

/**
 * Puts the given paradas back as Pendiente in the development database, with sqlcmd and the API's connection string
 * from `dotnet user-secrets`. The password goes to sqlcmd through SQLCMDPASSWORD, never on the command line.
 */
function resetParadas(ids) {
  if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(api)) {
    throw new Error('--reiniciar solo funciona con un API local: escribe directo en la base de desarrollo.');
  }
  const apiProject = resolve(dirname(fileURLToPath(import.meta.url)), '../../../api/CleanCom.Api');
  const secrets = execFileSync('dotnet', ['user-secrets', 'list', '--project', apiProject], { encoding: 'utf8' });
  const line = secrets.split(/\r?\n/).find((l) => l.startsWith('ConnectionStrings:DefaultConnection'));
  if (!line) throw new Error('No encontré ConnectionStrings:DefaultConnection en los user-secrets del API.');
  const parts = Object.fromEntries(
    line.slice(line.indexOf('=') + 1).split(';').filter((p) => p.includes('=')).map((p) => {
      const at = p.indexOf('=');
      return [p.slice(0, at).trim().toLowerCase().replace(/\s+/g, ''), p.slice(at + 1).trim()];
    }),
  );
  const server = parts.server ?? parts['datasource'];
  const database = parts.database ?? parts.initialcatalog;
  if (!server || !database) throw new Error('La cadena de conexión no trae servidor o base.');

  const list = ids.map((id) => Number(id)).filter(Number.isInteger).join(',');
  const sql = `
SET NOCOUNT ON; SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
-- An entrega's pedido goes back to the etapa it had before En camino; the history keeps both moves.
DECLARE @back TABLE (pedido_id int, etapa nvarchar(50));
INSERT @back
SELECT pe.id, ev.from_etapa
FROM paradas p
JOIN pedidos pe ON pe.id = p.pedido_id
CROSS APPLY (SELECT TOP 1 e.from_etapa FROM pedido_eventos e
             WHERE e.pedido_id = pe.id AND e.kind = 'Etapa' AND e.to_etapa = 'EnCamino' ORDER BY e.id DESC) ev
WHERE p.id IN (${list}) AND p.kind = 'Entrega' AND pe.etapa = 'EnCamino' AND ev.from_etapa IS NOT NULL;
INSERT pedido_eventos (pedido_id, kind, from_etapa, to_etapa, automatic, note, created_at)
SELECT pedido_id, 'Etapa', 'EnCamino', etapa, 0, N'Reinicio de la simulación del recorrido.', @now FROM @back;
UPDATE pe SET etapa = b.etapa, etapa_since = @now, modified_at = @now FROM pedidos pe JOIN @back b ON b.pedido_id = pe.id;
UPDATE paradas
SET state = 'Pendiente', en_camino_at = NULL, arrived_at = NULL, arrived_location = NULL,
    arrived_distance_meters = NULL, arrived_outside_radius = 0, modified_at = @now
WHERE id IN (${list}) AND state IN ('EnCamino', 'EnSitio') AND closed_at IS NULL;
SELECT CONCAT(@@ROWCOUNT, ' paradas y ', (SELECT COUNT(*) FROM @back), ' pedidos regresados.');
COMMIT;`;
  const auth = parts.userid ?? parts.uid ?? parts.user;
  const sqlArgs = ['-S', server, '-d', database, '-I', '-b', '-h', '-1', '-W', '-Q', sql];
  if (/^true$/i.test(parts.trustservercertificate ?? '')) sqlArgs.push('-C');
  const env = { ...process.env };
  if (auth) {
    sqlArgs.push('-U', auth);
    env.SQLCMDPASSWORD = parts.password ?? parts.pwd ?? '';
  } else {
    sqlArgs.push('-E');
  }
  return execFileSync('sqlcmd', sqlArgs, { encoding: 'utf8', env }).trim();
}

const pending = (p) => p.state === 'Pendiente' || p.state === 'EnCamino';
const label = (p) => `${p.order}. ${p.invitadoName ?? p.kind}${p.address ? ` — ${p.address}` : ''}`;

await signIn();
let ruta = await call('GET', '/chofer/ruta');
if (!ruta.ruta || !ruta.isToday) {
  console.error(ruta.notice ?? ruta.message ?? 'Este chofer no tiene ruta hoy.');
  process.exit(1);
}
console.log(`Ruta ${ruta.ruta.code} de ${ruta.operadorName ?? userName}, ${ruta.date}.`);

if (reset) {
  const started = ruta.paradas.filter((p) => p.state === 'EnCamino' || p.state === 'EnSitio').map((p) => p.id);
  console.log(started.length === 0 ? 'Reiniciar: no hay paradas en camino ni en sitio.' : `Reiniciar: ${resetParadas(started)}`);
  const closed = ruta.paradas.filter((p) => p.state === 'Realizada' || p.state === 'NoRealizada').length;
  if (closed) console.log(`  ${closed} parada(s) cerrada(s) se quedan como están.`);
  if (!visits) {
    console.log('Recarga la Vista del chofer o el Panel del día para ver el cambio.');
    process.exit(0);
  }
  ruta = await call('GET', '/chofer/ruta');
}

const ordered = [...ruta.paradas].sort((a, b) => a.order - b.order);
const located = (p) => p.lat != null && p.lng != null;
const withCoordinates = ordered.filter(located);
const missing = ordered.length - withCoordinates.length;
// With --visitas every parada is visited, because the API asks for the assigned order: one without coordinates (an
// address captured without a map) is marked where the car is. Without --visitas only the trip is drawn.
const stops = visits ? ordered : withCoordinates;
console.log(`${withCoordinates.length} paradas con coordenadas` +
  (missing ? ` (${missing} sin coordenadas: ${visits ? 'se visitan donde esté el carro' : 'se omiten'})` : '') + '.');
if (withCoordinates.length < 2) {
  console.error('Se necesitan al menos dos paradas con coordenadas para simular el recorrido.');
  process.exit(1);
}

// With --visitas the paradas already reached or closed are skipped: it starts from the last of them.
let start = 0;
if (visits) {
  const next = stops.findIndex(pending);
  if (next === -1) {
    console.log('Todas las paradas ya se visitaron. Usa --reiniciar para empezar de nuevo.');
    process.exit(0);
  }
  start = next;
  if (next > 0) console.log(`Sigue desde la parada ${stops[next].order}; las anteriores ya se visitaron.`);
}

const step = (kmh / 3.6) * every * fast; // Meters per reading.
console.log(`A ${kmh} km/h, ${fast}× más rápido: una lectura cada ${every} s avanza ${Math.round(step)} m.` +
  `${visits ? ' Marca En camino y Llegué.' : ''} Ctrl+C para detener.\n`);

// Start at the last place with coordinates before the first stop (or the first one of the ruta).
let here = stops.slice(0, Math.max(1, start)).filter(located).pop() ?? withCoordinates[0];
await send(here);
for (let i = start; i < stops.length; i++) {
  const stop = stops[i];
  // A parada without coordinates is reached where the car is.
  const to = located(stop) ? stop : { ...stop, lat: here.lat, lng: here.lng };
  if (!located(stop)) console.log(`  ${label(stop)} no tiene ubicación en el mapa: el carro se queda donde está.`);
  if (visits && to.state === 'Pendiente') {
    await call('POST', `/chofer/paradas/${to.id}/en-camino`, { lat: here.lat, lng: here.lng, localId: randomUUID() });
    console.log(`En camino a ${label(to)}`);
  } else {
    console.log(`→ ${label(to)}`);
  }
  const meters = distance(here, to);
  const ticks = Math.ceil(meters / step);
  if (ticks > 0) console.log(`  ${(meters / 1000).toFixed(1)} km, ~${Math.max(1, Math.round((ticks * every) / 60))} min reales`);
  for (let t = 1; t <= ticks; t++) {
    await sleep(every * 1000);
    const f = t / ticks;
    await send({ lat: here.lat + (to.lat - here.lat) * f, lng: here.lng + (to.lng - here.lng) * f });
  }
  if (visits && pending(to)) {
    await call('POST', `/chofer/paradas/${to.id}/llegue`, { lat: to.lat, lng: to.lng, confirmOutsideRadius: false, localId: randomUUID() });
    console.log(`  Llegué. Espera ${pause} s.`);
  } else {
    console.log(`  Llegó. Espera ${pause} s.`);
  }
  for (let waited = 0; waited < pause; waited += every) {
    await sleep(every * 1000);
    await send(to);
  }
  here = to;
}
console.log(`\nRecorrido terminado.${visits ? ' Las paradas quedan En sitio: el simulador no cierra visitas.' : ''}`);
