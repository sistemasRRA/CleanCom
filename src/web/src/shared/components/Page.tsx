import type { ReactNode } from 'react';
import { Loader, useScreenLoading } from './Loader';

export function PageHeader({ title, description, children }: { title: string; description?: ReactNode; children?: ReactNode }) {
  return (
    <header className="page-header">
      <div>
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </div>
      {children && <div className="actions">{children}</div>}
    </header>
  );
}

/**
 * Query state: loading, error or the content. While loading, a screen or dialog shows the full-screen CleanCom
 * loader; <paramref name="inline"/> keeps a small one in place instead, for searches as you type and panels inside
 * a form, where covering the screen would interrupt the user.
 */
export function QueryState({ loading, error, children, inline }: { loading: boolean; error: Error | null; children: ReactNode; inline?: boolean }) {
  useScreenLoading(loading && !error && !inline);
  if (error) return <div className="alert error">{error.message}</div>;
  if (loading) return inline ? <Loader size="small" /> : <div className="loading-placeholder" />;
  return <>{children}</>;
}

export function ActiveBadge({ active }: { active: boolean }) {
  return active ? <span className="badge success">Activo</span> : <span className="badge">Inactivo</span>;
}
