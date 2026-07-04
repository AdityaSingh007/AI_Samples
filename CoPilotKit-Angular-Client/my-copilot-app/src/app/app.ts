import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CopilotChat, registerHumanInTheLoop } from '@copilotkit/angular';
import { z } from 'zod';
import { Confirm } from './human-confirmation/confirm';

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
      name: 'ConfirmSensitiveAction',
      description: 'Ask the user to confirm before performing a action.',
      parameters: z.object({
        message: z.string().describe('The confirmation question to show the user'),
        isConfirmed: z.boolean().describe('Whether the user has confirmed the action'),
      }),
      component: Confirm,
    });
  }
}
