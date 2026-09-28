// Gives one page region its loading, failure, and empty presentation.
// Successful content is projected inside so a failed region cannot hide unrelated data.
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
@Component({
  selector: 'app-panel-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule],
  template: `
    @if (loading()) {
      <p class="notice" role="status">{{ loadingText() }}</p>
    } @else if (error()) {
      <div class="notice error" role="alert">
        <p>Temporarily unavailable</p>
        <p>{{ error() }}</p>
        <button mat-stroked-button type="button" (click)="retryRequested.emit()">Retry</button>
      </div>
    } @else if (empty()) {
      <p class="notice" role="status">{{ emptyText() }}</p>
      <ng-content select="[panel-empty]" />
    } @else {
      <ng-content />
    }
  `,
})
export class PanelState {
  readonly loading = input(false);
  readonly error = input('');
  readonly empty = input(false);
  readonly loadingText = input('Loading…');
  readonly emptyText = input('Nothing here yet.');
  readonly retryRequested = output<void>();
}
