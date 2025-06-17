import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';

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
    ReactiveFormsModule,
    RouterModule.forChild(routes),
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
    MatSelectModule
  ]
})
export class PhotosModule { }
