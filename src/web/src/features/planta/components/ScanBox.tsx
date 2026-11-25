import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useRef, useState } from 'react';
import '../../../shared/api/client';
import { refetch } from '../services/queryKeys';
import { plantaApi } from '../services/plantaApi';

/** Continuous capture: a barcode scanner types the folio and presses Enter; an error never blocks the next one. */
export function ScanBox() {
  const queryClient = useQueryClient();
  const input = useRef<HTMLInputElement>(null);
  const [folio, setFolio] = useState('');
  const [result, setResult] = useState<{ ok: boolean; text: string } | null>(null);
  const ready = useMutation({
    mutationFn: (f: string) => plantaApi.markReady(f),
    onSuccess: async (r) => {
      setResult({ ok: true, text: r.message });
      await Promise.all(refetch.map((k) => queryClient.invalidateQueries({ queryKey: k })));
    },
    onError: (e) => setResult({ ok: false, text: e.message }),
    onSettled: () => input.current?.focus(),
  });

  return (
    <form
      className="planta-scan"
      onSubmit={(e) => {
        e.preventDefault();
        const f = folio.trim();
        if (!f) return;
        setFolio('');
        ready.mutate(f);
      }}
    >
      <input
        ref={input}
        autoFocus
        value={folio}
        onChange={(e) => setFolio(e.target.value)}
        placeholder="Escanea o escribe el folio y presiona Enter"
        aria-label="Folio del ticket"
        autoComplete="off"
      />
      <button type="submit" className="primary" disabled={ready.isPending || !folio.trim()}>
        Marcar listo
      </button>
      {result && <div className={`alert ${result.ok ? 'success' : 'error'}`}>{result.text}</div>}
    </form>
  );
}
