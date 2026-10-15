import { createServer } from 'node:http';
import { CopilotKitIntelligence, CopilotRuntime } from '@copilotkit/runtime/v2';
import { createCopilotNodeListener } from '@copilotkit/runtime/v2/node';
import { HttpAgent } from '@ag-ui/client';

const runtime = new CopilotRuntime({
  agents: {
    default: new HttpAgent({ url: 'https://localhost:7160/api/agents/compareModels' }),
  },
  /* intelligence: new CopilotKitIntelligence({
    apiKey: process.env['COPILOTKIT_API_KEY'] ?? '',
  }),
  identifyUser: async (request) => ({
    id: request.headers.get('x-user-id') ?? 'user-123',
    name: request.headers.get('x-user-name') ?? 'Local Development User',
  }), */
  /* memory: {
    access: async () => ({
      user: 'read-write',
      project: 'none',
    }),
  }, */
  //exposeMemoryRoutes: true,
});

const port = Number(process.env['PORT'] ?? 8200);
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
createServer(
  createCopilotNodeListener({
    runtime,
    basePath: '/api/copilotkit',
    cors: true,
  }),
).listen(port, () => {
  console.log(`Copilot Runtime listening at http://localhost:${port}/api/copilotkit`);
});
