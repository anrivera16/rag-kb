import { Component, EventEmitter, Output, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatService, ConversationDto } from '../../services/chat.service';

@Component({
  selector: 'app-conversation-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.scss'
})
export class ConversationListComponent implements OnInit {
  @Output() conversationSelected = new EventEmitter<string | null>();

  conversations: ConversationDto[] = [];
  selectedId: string | null = null;
  error: string | null = null;

  constructor(
    private chatService: ChatService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadConversations();
  }

  loadConversations(): void {
    this.chatService.getConversations().subscribe({
      next: (convos) => {
        this.conversations = convos;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load conversations';
        this.cdr.detectChanges();
      }
    });
  }

  selectConversation(id: string | null): void {
    this.selectedId = id;
    this.conversationSelected.emit(id);
  }

  newConversation(): void {
    this.selectConversation(null);
  }

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation();
    this.chatService.deleteConversation(id).subscribe({
      next: () => {
        this.conversations = this.conversations.filter(c => c.id !== id);
        if (this.selectedId === id) {
          this.selectConversation(null);
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to delete conversation';
        this.cdr.detectChanges();
      }
    });
  }
}
