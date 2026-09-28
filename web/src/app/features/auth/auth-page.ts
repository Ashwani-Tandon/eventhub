// This page provides typed login and registration forms against Identity.
// Shared presentation keeps validation, pending state, and server errors consistent.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
@Component({
  selector: 'app-auth-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './auth-page.html',
  styleUrl: './auth-page.scss',
})
export class AuthPage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
  readonly registering = this.route.snapshot.data['register'] === true;
  readonly pending = signal(false);
  readonly error = signal('');
  readonly form = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: this.registering
        ? [Validators.required, Validators.maxLength(100), Validators.pattern(/\S/)]
        : [],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(256)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: this.registering
        ? [Validators.required, Validators.minLength(8)]
        : [Validators.required],
    }),
  });
  // Mark invalid fields visibly; a pending request cannot be submitted twice.
  submit() {
    if (this.pending()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.pending.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    const request = this.registering
      ? this.auth.register(value)
      : this.auth.login({ email: value.email, password: value.password });
    request.pipe(finalize(() => this.pending.set(false))).subscribe({
      next: () => {
        void this.router.navigateByUrl(
          this.auth.safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')),
        );
      },
      error: (error: unknown) => this.error.set(this.errorMessage(error)),
    });
  }
  // Show safe business details and validation messages without printing request data.
  private errorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) return 'Something went wrong. Please try again.';
    if (error.status === 401) return 'Invalid email or password.';
    if (error.status === 0 || error.status >= 500)
      return 'Service temporarily unavailable. Please try again.';
    const body: unknown = error.error;
    if (body && typeof body === 'object') {
      if ('errors' in body && body.errors && typeof body.errors === 'object') {
        const messages = Object.values(body.errors)
          .flat()
          .filter((value): value is string => typeof value === 'string');
        if (messages.length) return messages.join(' ');
      }
      if ('detail' in body && typeof body.detail === 'string') return body.detail;
    }
    return 'Please check your details and try again.';
  }
}
