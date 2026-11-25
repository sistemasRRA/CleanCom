import type { ReactNode } from 'react';

export function Field({ label, hint, children, wide }: { label: string; hint?: ReactNode; children: ReactNode; wide?: boolean }) {
  return (
    <label className={`field${wide ? ' wide' : ''}`}>
      <span>{label}</span>
      {children}
      {hint && <small>{hint}</small>}
    </label>
  );
}

export function Checkbox({
  checked,
  onChange,
  children,
  wide,
}: {
  checked: boolean;
  onChange: (v: boolean) => void;
  children: ReactNode;
  wide?: boolean;
}) {
  return (
    <label className={`checkbox${wide ? ' wide' : ''}`}>
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      {children}
    </label>
  );
}
