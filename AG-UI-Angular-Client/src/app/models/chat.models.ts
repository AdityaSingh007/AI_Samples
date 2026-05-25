import { InputContent } from '@ag-ui/client';

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  attachments?: FileAttachment[];
  timestamp: Date;
}

export interface FileAttachment {
  name: string;
  type: string;
  size: number;
  dataUrl: string;
}
