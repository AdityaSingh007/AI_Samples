import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CopilotChat } from '@copilotkit/angular';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CopilotChat],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly title = signal('my-copilot-app');
}
