import { createServer } from 'node:http';
import { CopilotRuntime } from '@copilotkit/runtime/v2';
import { createCopilotNodeListener } from '@copilotkit/runtime/v2/node';
import { HttpAgent } from '@ag-ui/client';

const runtime = new CopilotRuntime({
  agents: {
    default: new HttpAgent({ url: 'https://localhost:7275/human_in_the_loop' }),
    'agent-with-memory': new HttpAgent({ url: 'https://localhost:7275/human_in_the_loop' }),
  },
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
