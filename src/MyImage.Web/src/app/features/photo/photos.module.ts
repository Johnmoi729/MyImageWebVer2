// src/MyImage.Web/src/app/features/photo/photos.module.ts
import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// FIXED: Complete Angular Material imports for form fields
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input'; // FIXED: Missing input module
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip'; // FIXED: Missing tooltip module

import { PhotoGalleryComponent } from './photo-gallery/photo-gallery.component';
import { PhotoPreviewComponent } from './photo-preview/photo-preview.component';
import { PhotoUploadComponent } from './photo-upload/photo-upload.component';
import { PrintSelectorComponent } from './print-selector/print-selector.component';

const routes: Routes = [
  { path: '', component: PhotoGalleryComponent },
  { path: 'upload', component: PhotoUploadComponent }
];

@NgModule({
  declarations: [
    PhotoGalleryComponent,
    PhotoUploadComponent,
    PhotoPreviewComponent,
    PrintSelectorComponent
  ],
  imports: [
    CommonModule,
    FormsModule, // FIXED: Added FormsModule for template-driven forms
    ReactiveFormsModule,
    RouterModule.forChild(routes),

    // Angular Material Modules - FIXED: Complete list
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatGridListModule,
    MatDialogModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatPaginatorModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule, // REQUIRED for matInput directive
    MatSelectModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatTooltipModule // For matTooltip directive
  ]
})
export class PhotosModule { }
