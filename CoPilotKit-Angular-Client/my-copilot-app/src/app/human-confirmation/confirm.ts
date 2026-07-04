import { Component, computed, input } from '@angular/core';
import { HumanInTheLoopToolCall, HumanInTheLoopToolRenderer } from '@copilotkit/angular';

type ConfirmArgs = { message: string; isConfirmed: boolean };

@Component({
  selector: 'app-confirm',
  imports: [],
  templateUrl: './confirm.html',
  styleUrls: ['./confirm.scss'],
})
export class Confirm implements HumanInTheLoopToolRenderer<ConfirmArgs> {
  readonly toolCall = input.required<HumanInTheLoopToolCall<ConfirmArgs>>();
  readonly message = computed(() => this.toolCall().args.message ?? '');
  readonly isConfirmed = computed(() => this.toolCall().args.isConfirmed ?? false);

  approve(): void {
    this.respondWithConfirmation(true);
  }

  reject(): void {
    this.respondWithConfirmation(false);
  }

  private respondWithConfirmation(isConfirmed: boolean): void {
    this.toolCall().respond({
      ...this.toolCall().args,
      isConfirmed,
    });
  }
}
