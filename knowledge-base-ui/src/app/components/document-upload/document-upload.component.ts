import { Component, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentService, DocumentDto } from '../../services/document.service';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-upload.component.html',
  styleUrl: './document-upload.component.scss'
})
export class DocumentUploadComponent {
  @Output() documentUploaded = new EventEmitter<DocumentDto>();

  documents: DocumentDto[] = [];
  isUploading = false;
  isDragging = false;
  error: string | null = null;

  constructor(
    private documentService: DocumentService,
    private cdr: ChangeDetectorRef
  ) {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.documentService.getAll().subscribe({
      next: (docs) => {
        this.documents = docs;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load documents';
        this.cdr.detectChanges();
      }
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.uploadFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile(input.files[0]);
    }
  }

  uploadFile(file: File): void {
    const allowedTypes = [
      'application/pdf',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'text/plain'
    ];

    if (!allowedTypes.includes(file.type)) {
      this.error = 'Unsupported file type. Please upload PDF, DOCX, or TXT files.';
      return;
    }

    this.isUploading = true;
    this.error = null;

    this.documentService.upload(file).subscribe({
      next: (doc) => {
        this.documents.unshift(doc);
        this.documentUploaded.emit(doc);
        this.isUploading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to upload document. Please try again.';
        this.isUploading = false;
        this.cdr.detectChanges();
      }
    });
  }

  deleteDocument(id: string): void {
    this.documentService.delete(id).subscribe({
      next: () => {
        this.documents = this.documents.filter(d => d.id !== id);
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to delete document';
        this.cdr.detectChanges();
      }
    });
  }
}
