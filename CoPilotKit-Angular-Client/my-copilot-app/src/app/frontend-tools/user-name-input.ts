import { Component, computed, input, signal } from '@angular/core';
import { HumanInTheLoopToolCall, HumanInTheLoopToolRenderer } from '@copilotkit/angular';

type UserNameArgs = { name: string | null; isConfirmed: boolean };

@Component({
  selector: 'app-user-name-input',
  imports: [],
  templateUrl: './user-name-input.html',
  styleUrl: './user-name-input.scss',
})
export class UserNameInput implements HumanInTheLoopToolRenderer<UserNameArgs> {
  readonly toolCall = input.required<HumanInTheLoopToolCall<UserNameArgs>>();
  readonly userName = signal('');
  readonly isSubmitted = signal(false);
  readonly hasValidationError = computed(() => this.isSubmitted() && !this.isUserNameValid());

  ngOnInit(): void {
    this.userName.set(this.toolCall().args.name ?? '');
  }

  onUserNameInput(event: Event): void {
    const value = (event.target as HTMLInputElement | null)?.value ?? '';
    this.userName.set(value);
  }

  confirm(): void {
    this.isSubmitted.set(true);
    if (!this.isUserNameValid()) {
      return;
    }

    this.respondWith(true);
  }

  cancel(): void {
    this.respondWith(false);
  }

  private isUserNameValid(): boolean {
    return this.userName().trim().length > 0;
  }

  private respondWith(isConfirmed: boolean): void {
    this.toolCall().respond({
      ...this.toolCall().args,
      name: this.userName().trim(),
      isConfirmed,
    });
  }
}
