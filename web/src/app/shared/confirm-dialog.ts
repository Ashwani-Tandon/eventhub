// This dialog makes cancellation and deletion an explicit user choice.
// The caller performs the operation only after receiving a true result.
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
export interface Confirmation {
  title: string;
  message: string;
  action: string;
}
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, MatButtonModule],
  template: `<h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content
      ><p>{{ data.message }}</p></mat-dialog-content
    ><mat-dialog-actions align="end"
      ><button mat-button [mat-dialog-close]="false">Keep it</button
      ><button mat-flat-button [mat-dialog-close]="true">
        {{ data.action }}
      </button></mat-dialog-actions
    >`,
})
export class ConfirmDialog {
  readonly data = inject<Confirmation>(MAT_DIALOG_DATA);
}
