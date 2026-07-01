import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CopilotChat, registerHumanInTheLoop } from '@copilotkit/angular';
import { z } from 'zod';
import { Confirm } from './human-in-the-loop/confirm';

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
      name: 'confirmAction',
      description: 'Ask the user to confirm before performing a sensitive action',
      parameters: z.object({
        message: z.string().describe('The confirmation question to show the user'),
        tripHighlights: z
          .array(z.string())
          .optional()
          .describe(
            'Optional bullet points summarizing the trip details for review before confirmation',
          ),
      }),
      component: Confirm,
    });
  }
}
