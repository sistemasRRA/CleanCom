/**
 * "Cargando…" with the CleanCom mark drawn in SVG (from public/brand/cleancom-mark.png) so its parts can move:
 * the C turns, the dot pulses and the cube floats. With reduced motion it only fades in and out.
 * The C and the dot follow the text color (dark mode included); the cube keeps the brand blue.
 */
export function Loader({ label = 'Cargando…', size = 'normal' }: { label?: string; size?: 'normal' | 'small' }) {
  return (
    <div className={`loader loader--${size}`} role="status" aria-live="polite">
      <svg className="loader__mark" viewBox="0 0 132 112" aria-hidden="true">
        {/* The C: a thick ring open to the right, where the cube sits. */}
        <g className="loader__ring">
          <circle cx="52" cy="56" r="40" fill="none" stroke="currentColor" strokeWidth="21" strokeDasharray="200 51.3" transform="rotate(37 52 56)" />
        </g>
        <circle className="loader__dot" cx="52" cy="56" r="13" fill="currentColor" />
        <g className="loader__cube">
          <polygon points="102,44 122.8,56 102,68 81.2,56" fill="#8fb4ea" />
          <polygon points="81.2,56 102,68 102,92 81.2,80" fill="#6f9fe0" />
          <polygon points="102,68 122.8,56 122.8,80 102,92" fill="#5a8fd6" />
          <polygon points="102,44 122.8,56 122.8,80 102,92 81.2,80 81.2,56" fill="none" className="loader__cube-edge" strokeWidth="3.5" strokeLinejoin="round" />
        </g>
      </svg>
      {label && <span className="loader__label">{label}</span>}
    </div>
  );
}
