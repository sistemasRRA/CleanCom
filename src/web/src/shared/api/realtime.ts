import { useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { API_URL, getSession, type Schemas } from './client';

export type LiveLocation = Schemas['LiveLocationDto'];

/**
 * ParadaChangedDto of the API. Hub messages are not in the OpenAPI document (no endpoint returns this one), so it is
 * the only type written here; keep it in line with CleanCom.Application/Common/Contracts.cs.
 */
export type ParadaChanged = { rutaDiaId: number; date: string; paradaId: number; state: Schemas['ParadaState'] };

export type LiveStatus = 'connecting' | 'live' | 'reconnecting' | 'offline';

/**
 * Whether a position is no longer current: it comes from the saved history, or no newer one arrived before its
 * signalLostAt (the API's Gps.SinSenalMinutos). `now` comes from useNow so the map greys it without a new event.
 */
export function isOld(l: LiveLocation, now: number) {
  return !l.live || (l.signalLostAt != null && now >= Date.parse(l.signalLostAt));
}

/** The current time, refreshed every `ms`. */
export function useNow(ms = 30_000) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), ms);
    return () => clearInterval(timer);
  }, [ms]);
  return now;
}

/**
 * The hub "operacion" (docs/pantallas/oficina/panel-del-dia.md): choferes' positions and paradas that change state,
 * pushed by the API. Reconnects by itself; the screens also reload on their own, so a missed event is not lost.
 */
export function useOperacion(handlers: { onLocation?: (l: LiveLocation) => void; onParada?: (p: ParadaChanged) => void }) {
  const [status, setStatus] = useState<LiveStatus>('connecting');
  // The latest handlers, so the connection is not rebuilt on every render.
  const current = useRef(handlers);
  useEffect(() => {
    current.current = handlers;
  });

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/operacion`, { accessTokenFactory: () => getSession()?.accessToken ?? '' })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.None)
      .build();
    // Handlers return nothing: a returned value would be taken as an answer to the server.
    connection.on('ubicacion', (l: LiveLocation) => {
      current.current.onLocation?.(l);
    });
    connection.on('parada', (p: ParadaChanged) => {
      current.current.onParada?.(p);
    });
    connection.onreconnecting(() => setStatus('reconnecting'));
    connection.onreconnected(() => setStatus('live'));
    connection.onclose(() => setStatus('offline'));

    let stopped = false;
    const start = () =>
      connection
        .start()
        .then(() => !stopped && setStatus('live'))
        .catch(() => {
          if (stopped) return;
          setStatus('offline');
          // Without the automatic reconnect (it only covers a connection that was up), try again in a while.
          setTimeout(() => !stopped && connection.state === HubConnectionState.Disconnected && void start(), 15_000);
        });
    void start();
    return () => {
      stopped = true;
      void connection.stop();
    };
  }, []);

  return status;
}
