import { Component, computed, DestroyRef, inject, input } from '@angular/core';
import { HumanInTheLoopToolCall, HumanInTheLoopToolRenderer } from '@copilotkit/angular';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

type ConfirmArgs = { message: string; tripHighlights?: string[] };

@Component({
  selector: 'app-confirm',
  imports: [],
  templateUrl: './confirm.html',
  styleUrls: ['./confirm.scss'],
})
export class Confirm implements HumanInTheLoopToolRenderer<ConfirmArgs> {
  private readonly http = inject(HttpClient);
  private destroyRef = inject(DestroyRef);

  readonly toolCall = input.required<HumanInTheLoopToolCall<ConfirmArgs>>();
  readonly message = computed(() => this.toolCall().args.message ?? 'Are you sure?');
  readonly tripHighlights = computed(() =>
    (this.toolCall().args.tripHighlights ?? []).filter((highlight) => highlight.trim().length > 0),
  );

  approve(): void {
    this.executeDecisionCallback('approved');
    this.toolCall().respond('approved');
  }

  reject(): void {
    this.executeDecisionCallback('rejected');
    this.toolCall().respond('rejected');
  }

  private executeDecisionCallback(decision: 'approved' | 'rejected'): void {
    this.http
      .post('https://localhost:7275/api/trips/approval-callback', {
        decision,
        message: this.message(),
        highlights: this.tripHighlights(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          console.log('Callback response:', response);
        },
        error: (error: HttpErrorResponse) => {
          console.error('Error sending callback:', error);
        },
      });
  }
}
