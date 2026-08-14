import { Component, computed, effect, signal } from '@angular/core';
import { JsonPipe, CommonModule } from '@angular/common';
import { injectAgentStore } from '@copilotkit/angular';
import { AgentMemory } from '../models/agent-memory';

@Component({
  selector: 'app-memorylist',
  imports: [JsonPipe, CommonModule],
  templateUrl: './memorylist.html',
  styleUrls: ['./memorylist.scss'],
})
export class Memorylist {
  // Inject the store using your registered agent identifier
  readonly agentStore = injectAgentStore('default');

  // Safely extract the memory/state object into a computed signal
  readonly agentMemory = computed<AgentMemory>(() => this.agentStore().state() as AgentMemory);

  // Optional: track if the agent is actively processing data
  readonly isAgentWorking = computed(() => this.agentStore().isRunning());

  // Track added states
  readonly addedStates = signal<Array<AgentMemory>>([]);

  constructor() {}

  /**
   * Add a new agent state entry with current timestamp
   */
  addAgentState(): void {
    const newState: AgentMemory = {
      id: `state-${Date.now()}`,
      kind: 'user-memory',
      scope: 'user',
      content: `New state added at ${new Date().toLocaleString()}`,
      sourceThreadId: [],
      InvalidatedAt: '',
    };
    this.addedStates.update((states) => [...states, newState]);
    console.log('Agent state added:', newState);
  }
}
