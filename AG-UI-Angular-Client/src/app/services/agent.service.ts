import { Injectable, signal, computed } from '@angular/core';
import { HttpAgent } from '@ag-ui/client';
import type { Message, InputContent } from '@ag-ui/client';
import { v4 as uuidv4 } from 'uuid';
import { ChatMessage, FileAttachment } from '../models/chat.models';
import { AgentSubscriber } from '@ag-ui/client';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly AGENT_URL = 'https://localhost:7094';

  private readonly _messages = signal<ChatMessage[]>([]);
  private readonly _isLoading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly messages = this._messages.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly hasMessages = computed(() => this._messages().length > 0);

  private agent: HttpAgent;
  private abortController: AbortController | null = null;
  private currentAssistantMessageId: string | null = null;

  private readonly subscriber: AgentSubscriber = {
    onTextMessageContentEvent: ({ event }) => {
      if (!this.currentAssistantMessageId) {
        return;
      }

      this._messages.update((msgs) =>
        msgs.map((message) =>
          message.id === this.currentAssistantMessageId
            ? { ...message, content: message.content + event.delta }
            : message,
        ),
      );
    },
    onRunFinishedEvent: () => {
      this.currentAssistantMessageId = null;
      this._isLoading.set(false);
    },
    onRunErrorEvent: ({ event }) => {
      console.error('Agent run error:', event);
      this.currentAssistantMessageId = null;
      this._error.set(event.message || 'An error occurred');
      this._isLoading.set(false);
    },
    onRunFailed: ({ error }) => {
      this.currentAssistantMessageId = null;
      this._error.set(error.message || 'Connection error');
      this._isLoading.set(false);
    },
  };

  constructor() {
    this.agent = new HttpAgent({
      url: this.AGENT_URL,
    });
  }

  async sendMessage(text: string, attachments: FileAttachment[] = []): Promise<void> {
    this._error.set(null);

    const userMessageId = uuidv4();
    const userContent = this.buildUserContent(text, attachments);

    // Add user message to the agent's internal message list
    this.agent.messages.push({
      id: userMessageId,
      role: 'user',
      content: userContent,
    } as Message);

    const userMessage: ChatMessage = {
      id: userMessageId,
      role: 'user',
      content: text,
      attachments: attachments.length > 0 ? attachments : undefined,
      timestamp: new Date(),
    };

    this._messages.update((msgs) => [...msgs, userMessage]);
    this._isLoading.set(true);

    const assistantMessageId = uuidv4();
    this._messages.update((msgs) => [
      ...msgs,
      { id: assistantMessageId, role: 'assistant', content: '', timestamp: new Date() },
    ]);

    try {
      this.abortController = new AbortController();
      this.currentAssistantMessageId = assistantMessageId;

      await this.agent.runAgent(
        {
          runId: uuidv4(),
          tools: [],
          context: [],
          forwardedProps: {},
          abortController: this.abortController,
        },
        this.subscriber,
      );
    } catch (err: any) {
      if (err?.name === 'AbortError') return;
      this.currentAssistantMessageId = null;
      this._error.set(err?.message || 'Failed to communicate with agent');
      this._isLoading.set(false);
    }
  }

  cancelRun(): void {
    this.abortController?.abort();
    this.currentAssistantMessageId = null;
    this._isLoading.set(false);
  }

  clearMessages(): void {
    this._messages.set([]);
    this._error.set(null);
    this.currentAssistantMessageId = null;
    // Reset agent with fresh instance
    this.agent = new HttpAgent({
      url: this.AGENT_URL,
    });
  }

  private buildUserContent(text: string, attachments: FileAttachment[]): string | InputContent[] {
    if (attachments.length === 0) return text;

    const parts: InputContent[] = [{ type: 'text' as const, text }];

    for (const file of attachments) {
      const base64Data = file.dataUrl.split(',')[1] || file.dataUrl;

      if (file.type.startsWith('image/')) {
        parts.push({
          type: 'image' as const,
          source: { type: 'data' as const, value: base64Data, mimeType: file.type },
        });
      } else {
        parts.push({
          type: 'document' as const,
          source: { type: 'data' as const, value: base64Data, mimeType: file.type },
          metadata: { filename: file.name },
        });
      }
    }

    return parts;
  }
}
