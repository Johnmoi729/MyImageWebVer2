import { AppModule } from './app/app.module';

// For standalone bootstrap (if using standalone components)
// bootstrapApplication(AppComponent, {
//   providers: [
//     importProvidersFrom(
//       BrowserAnimationsModule,
//       HttpClientModule,
//       RouterModule.forRoot([])
//     )
//   ]
// });

// Using traditional module bootstrap
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

platformBrowserDynamic()
  .bootstrapModule(AppModule)
  .catch(err => console.error(err));
