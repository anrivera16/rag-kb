import { Component, Input, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, SourceReference } from '../../services/chat.service';

interface DisplayMessage {
  role: string;
  content: string;
  sources?: SourceReference[];
}

@Component({
  selector: 'app-chat-interface',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-interface.component.html',
  styleUrl: './chat-interface.component.scss'
})
export class ChatInterfaceComponent implements OnChanges {
  @Input() conversationId: string | null = null;

  messages: DisplayMessage[] = [];
  question = '';
  isLoading = false;
  error: string | null = null;

  constructor(
    private chatService: ChatService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['conversationId'] && this.conversationId) {
      this.loadConversation();
    } else if (!this.conversationId) {
      this.messages = [];
    }
  }

  loadConversation(): void {
    if (!this.conversationId) return;

    this.chatService.getMessages(this.conversationId).subscribe({
      next: (messages) => {
        this.messages = messages.map(m => ({
          role: m.role,
          content: m.content,
          sources: m.sources ? JSON.parse(m.sources) : undefined
        }));
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load conversation';
        this.cdr.detectChanges();
      }
    });
  }

  sendMessage(): void {
    if (!this.question.trim() || this.isLoading) return;

    const userQuestion = this.question;
    this.question = '';
    this.isLoading = true;
    this.error = null;

    this.messages.push({ role: 'user', content: userQuestion });
    this.cdr.detectChanges();

    this.chatService.ask({
      question: userQuestion,
      conversationId: this.conversationId ?? undefined
    }).subscribe({
      next: (response) => {
        if (!this.conversationId) {
          this.conversationId = response.conversationId;
        }
        this.messages.push({
          role: 'assistant',
          content: response.answer,
          sources: response.sources
        });
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to get response. Please try again.';
        this.messages.pop();
        this.question = userQuestion;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
