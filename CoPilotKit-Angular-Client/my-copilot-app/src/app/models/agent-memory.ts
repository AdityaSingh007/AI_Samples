export interface AgentMemory {
  id: string;
  kind: string;
  scope: string;
  content: string;
  sourceThreadId: string[];
  InvalidatedAt: string;
}
