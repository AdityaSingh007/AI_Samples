import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AttachmentsConfig, CopilotChat, registerHumanInTheLoop } from '@copilotkit/angular';
import { Memorylist } from './memory-list/memorylist';
import { z } from 'zod';
import { Confirm } from './human-confirmation/confirm';
import { UserNameInput } from './frontend-tools/user-name-input';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CopilotChat, Memorylist, UserNameInput, Confirm],
  templateUrl: './app.html',
  styleUrls: ['./app.scss'],
})
export class App {
  protected readonly title = signal('my-copilot-app');
  protected readonly attachments: AttachmentsConfig = {
    enabled: true,
    accept: '.cae',
    maxSize: 10 * 1024 * 1024, // 10 MB limit
    onUpload: async (file: File) => {
      const base64Data = await this.fileToBase64(file);

      return {
        type: 'url',
        // Format as a raw base64 data URI string
        value: `data:application/octet-stream;base64,${base64Data}`,
        mimeType: 'application/octet-stream',
        metadata: {
          originalName: file.name,
          mediaType: 'application/octet-stream',
        },
      };
    },
  };

  constructor() {
    registerHumanInTheLoop({
      name: 'UserNameInput',
      description:
        'Ask the user to provide a name when the user asks to generate a random username.Invoke this tool when the user asks to generate a random username and you need to ask the user for a name.',
      parameters: z.object({
        name: z.string().describe('The name provided by the user to show the user'),
        isConfirmed: z.boolean().describe('Whether the user has confirmed the action'),
      }),
      component: UserNameInput,
    });
  }

  private async fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        const result = reader.result as string;
        const base64Data = result.split(',')[1];
        resolve(base64Data);
      };
      reader.onerror = (error) => reject(error);
    });
  }
}
