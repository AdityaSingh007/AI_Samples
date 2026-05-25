import { Component, signal, viewChild, ElementRef, effect, inject, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AgentService } from '../../services/agent.service';
import { FileAttachment } from '../../models/chat.models';
import { marked } from 'marked';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatToolbarModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressBarModule,
    MatChipsModule,
    MatTooltipModule,
    MatSnackBarModule,
  ],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
})
export class ChatComponent {
  private readonly agentService = inject(AgentService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly sanitizer = inject(DomSanitizer);

  readonly messages = this.agentService.messages;
  readonly isLoading = this.agentService.isLoading;
  readonly error = this.agentService.error;
  readonly hasMessages = this.agentService.hasMessages;

  readonly userInput = signal('');
  readonly pendingFiles = signal<FileAttachment[]>([]);

  private readonly messagesContainer = viewChild<ElementRef>('messagesContainer');
  private readonly fileInput = viewChild<ElementRef>('fileInput');

  constructor() {
    effect(() => {
      this.messages();
      this.scrollToBottom();
    });

    effect(() => {
      const err = this.error();
      if (err) {
        this.snackBar.open('Error occurred', 'Dismiss', {
          duration: 5000,
          panelClass: 'error-snackbar',
        });
      }
    });
  }

  async onSend(): Promise<void> {
    const text = this.userInput().trim();
    if (!text && this.pendingFiles().length === 0) return;

    const files = [...this.pendingFiles()];
    this.userInput.set('');
    this.pendingFiles.set([]);

    await this.agentService.sendMessage(text, files);
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSend();
    }
  }

  onCancel(): void {
    this.agentService.cancelRun();
  }

  onClear(): void {
    this.agentService.clearMessages();
  }

  triggerFileUpload(): void {
    this.fileInput()?.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;

    const files = Array.from(input.files);
    for (const file of files) {
      if (file.size > 10 * 1024 * 1024) {
        this.snackBar.open(`File "${file.name}" exceeds 10MB limit`, 'OK', { duration: 3000 });
        continue;
      }

      const reader = new FileReader();
      reader.onload = () => {
        const attachment: FileAttachment = {
          name: file.name,
          type: file.type,
          size: file.size,
          dataUrl: reader.result as string,
        };
        this.pendingFiles.update((f) => [...f, attachment]);
      };
      reader.readAsDataURL(file);
    }

    input.value = '';
  }

  removeFile(index: number): void {
    this.pendingFiles.update((files) => files.filter((_, i) => i !== index));
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  renderMarkdown(content: string): SafeHtml {
    if (!content) return '';
    const html = marked.parse(content, { async: false }) as string;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.messagesContainer()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 50);
  }
}
