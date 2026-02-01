import { Component } from '@angular/core';
import { DocumentUploadComponent } from './components/document-upload/document-upload.component';
import { ChatInterfaceComponent } from './components/chat-interface/chat-interface.component';
import { ConversationListComponent } from './components/conversation-list/conversation-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [DocumentUploadComponent, ChatInterfaceComponent, ConversationListComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  currentConversationId: string | null = null;

  onConversationSelected(id: string | null): void {
    this.currentConversationId = id;
  }
}
