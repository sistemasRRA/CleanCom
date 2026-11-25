import { useTheme } from '../theme/Theme';

/** CleanCom logo taken from cleancom.mx. Files live in public/brand. */

type LogoProps = {
  /** "full" shows mark and name; "mark" shows only the symbol. */
  variant?: 'full' | 'mark';
  /** "light" for white backgrounds, "dark" for dark ones (white text), "auto" follows the app theme. */
  tone?: 'auto' | 'light' | 'dark';
  className?: string;
};

export function Logo({ variant = 'full', tone = 'auto', className }: LogoProps) {
  const { theme } = useTheme();
  const onDark = tone === 'dark' || (tone === 'auto' && theme === 'dark');
  const base = variant === 'full' ? '/brand/cleancom-logo' : '/brand/cleancom-mark';
  return <img src={onDark ? `${base}-light.png` : `${base}.png`} alt="CleanCom" className={className} />;
}
