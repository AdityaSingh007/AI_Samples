import { Component, OnInit } from '@angular/core';
import { injectMemories } from '@copilotkit/angular';

@Component({
  selector: 'app-memorylist',
  imports: [],
  templateUrl: './memorylist.html',
  styleUrl: './memorylist.scss',
})
export class Memorylist implements OnInit {
  ngOnInit(): void {
    //this.addPreference();
  }
  readonly memory = injectMemories();
  protected remove(id: string): void {
    this.memory.removeMemory(id).catch(() => undefined);
  }
  protected addPreference(): void {
    this.memory
      .addMemory({
        kind: 'operational',
        content: 'Prefer concise status updates.',
      })
      .catch(() => undefined);
  }
}
