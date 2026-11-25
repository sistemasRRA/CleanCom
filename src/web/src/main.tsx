// Leaflet first, so the app styles (global and of each feature) win over it, as before the split.
import 'leaflet/dist/leaflet.css';
import './shared/styles/global.css';
import { createRoot } from 'react-dom/client';
import { StrictMode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router';
import { ThemeProvider } from './shared/theme/Theme';
import { App, queryClient } from './app/App';
import { ConfirmProvider, ToastProvider } from './shared/components/Toasts';
import { SessionProvider } from './shared/session/Session';
import { LoadingOverlay } from './shared/components/Loader';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <ConfirmProvider>
            <SessionProvider>
              <BrowserRouter>
                <App />
                <LoadingOverlay />
              </BrowserRouter>
            </SessionProvider>
          </ConfirmProvider>
        </ToastProvider>
      </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>,
);
