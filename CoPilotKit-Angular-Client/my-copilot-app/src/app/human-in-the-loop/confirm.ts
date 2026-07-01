import { Component, computed, input } from '@angular/core';
import { HumanInTheLoopToolCall, HumanInTheLoopToolRenderer } from '@copilotkit/angular';
type ConfirmArgs = { message: string; tripHighlights?: string[] };

@Component({
  selector: 'app-confirm',
  imports: [],
  templateUrl: './confirm.html',
  styleUrls: ['./confirm.scss'],
})
export class Confirm implements HumanInTheLoopToolRenderer<ConfirmArgs> {
  readonly toolCall = input.required<HumanInTheLoopToolCall<ConfirmArgs>>();
  readonly message = computed(() => this.toolCall().args.message ?? 'Are you sure?');
  readonly tripHighlights = computed(() =>
    (this.toolCall().args.tripHighlights ?? []).filter((highlight) => highlight.trim().length > 0),
  );
}
