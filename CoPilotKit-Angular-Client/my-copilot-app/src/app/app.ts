import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CopilotChat, registerHumanInTheLoop } from '@copilotkit/angular';
import { z } from 'zod';
import { Confirm } from './human-confirmation/confirm';
import { UserNameInput } from './frontend-tools/user-name-input';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CopilotChat],
  templateUrl: './app.html',
  styleUrls: ['./app.scss'],
})
export class App {
  protected readonly title = signal('my-copilot-app');
  constructor() {
    registerHumanInTheLoop({
      name: 'UserNameInput',
      description:
        'Ask the user to provide a name when the user asks to generate a random username.',
      parameters: z.object({
        name: z.string().describe('The name provided by the user to show the user'),
        isConfirmed: z.boolean().describe('Whether the user has confirmed the action'),
      }),
      component: UserNameInput,
    });
  }
}
