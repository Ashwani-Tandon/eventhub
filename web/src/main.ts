// Starts the standalone Angular application with its routes and HTTP providers.
// This browser entry point loads the root component that hosts the shared shell.
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
