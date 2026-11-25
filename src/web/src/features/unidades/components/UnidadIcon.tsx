import type { ReactNode } from 'react';
import type { Schemas } from '../../../shared/api/client';

type UnidadType = Schemas['UnidadType'];

/**
 * Side view of a vehicle by its type, in the current text color, for a unidad without a photo.
 * Line drawings on a 64 × 40 grid, so they read at small sizes and in both themes.
 */
export function UnidadIcon({ type, size = 56 }: { type: UnidadType; size?: number }) {
  return (
    <svg
      viewBox="0 0 64 40"
      width={size}
      height={(size * 40) / 64}
      fill="none"
      stroke="currentColor"
      strokeWidth={2.4}
      strokeLinecap="round"
      strokeLinejoin="round"
      role="img"
      aria-label={LABEL[type]}
    >
      {SHAPES[type]}
    </svg>
  );
}

export const LABEL: Record<UnidadType, string> = {
  Furgoneta: 'Furgoneta',
  Pickup: 'Pickup',
  Trimoto: 'Trimoto',
  Otro: 'Otro',
};

const wheel = (cx: number) => (
  <g key={cx}>
    <circle cx={cx} cy={31} r={5} />
    <circle cx={cx} cy={31} r={1.6} fill="currentColor" stroke="none" />
  </g>
);

// The bottom line stops at each wheel (wheels at x 16 and 48 span ±5).
const SHAPES: Record<UnidadType, ReactNode> = {
  // Van: one long body with a sloped front.
  Furgoneta: (
    <>
      <path d="M5 30V14a4 4 0 0 1 4-4h31l11 9h4a4 4 0 0 1 4 4v7" />
      <path d="M5 30h6M21 30h22M53 30h6" />
      <path d="M40 10v9h11" />
      <path d="M27 10v20" />
      {wheel(16)}
      {wheel(48)}
    </>
  ),
  // Pickup: cab in front, open bed behind.
  Pickup: (
    <>
      <path d="M5 30V19h23V11h12l8 8h7a4 4 0 0 1 4 4v7" />
      <path d="M5 30h6M21 30h22M53 30h6" />
      <path d="M31 19h14l-6-6h-8z" />
      {wheel(16)}
      {wheel(48)}
    </>
  ),
  // Cargo tricycle: closed box at the back, motorcycle front.
  Trimoto: (
    <>
      <rect x={4} y={8} width={28} height={17} rx={2} />
      <path d="M32 21h8l7-11h6" />
      <path d="M50 10l2 16" />
      <path d="M36 21v-5h5" />
      {wheel(11)}
      {wheel(25)}
      {wheel(53)}
    </>
  ),
  // Box truck.
  Otro: (
    <>
      <rect x={4} y={6} width={36} height={19} rx={2} />
      <path d="M40 12h10l8 8v10h-5M43 30h-3V25" />
      <path d="M44 12v8h14" />
      <path d="M4 25v5h7M21 30h19" />
      {wheel(16)}
      {wheel(48)}
    </>
  ),
};
