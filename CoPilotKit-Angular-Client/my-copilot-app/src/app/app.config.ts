import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideCopilotKit } from '@copilotkit/angular';
import { HttpAgent } from '@ag-ui/client';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideCopilotKit({
      selfManagedAgents: {
        default: new HttpAgent({ url: 'https://localhost:7275/' }),
      },
    }),
  ],
};
