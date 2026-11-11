import { createContext, use, useState, type ReactNode } from 'react';

/**
 * Development only: lets the screens be reviewed before the phone + code sign-in exists
 * (POST /auth/codigo and /auth/verificar arrive in week 5). Never available in a release build.
 */
type PreviewContext = { preview: boolean; setPreview: (on: boolean) => void };
const Context = createContext<PreviewContext>({ preview: false, setPreview: () => undefined });

export function PreviewProvider({ children }: { children: ReactNode }) {
  const [preview, setPreview] = useState(false);
  return <Context value={{ preview: __DEV__ && preview, setPreview }}>{children}</Context>;
}

export const usePreview = () => use(Context);
