// This floating panel keeps a signed-in user's conversation and displays confirmation cards.
// Only a Yes click can submit a Booking write; closing preserves history, while logout cancels work and clears it.
import {
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { finalize, of, Subscription, switchMap, TimeoutError } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { AgentApiService } from '../../core/api/agent-api.service';
import { BookingApiService } from '../../core/api/booking-api.service';
import { CatalogApiService } from '../../core/api/catalog-api.service';
import { ActionProposal, ChatTurn } from '../../core/models/chat.models';
import { Booking } from '../../core/models/booking.models';
import { apiError } from '../../shared/api-error';
interface PendingCard {
  proposal: ActionProposal;
  key: string;
  submitted: boolean;
}
@Component({
  selector: 'app-chat-widget',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, MatButtonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chat-widget.html',
  styleUrl: './chat-widget.scss',
})
export class ChatWidget {
  readonly auth = inject(AuthService);
  private readonly agent = inject(AgentApiService);
  private readonly booking = inject(BookingApiService);
  private readonly catalog = inject(CatalogApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly transcript = viewChild<ElementRef<HTMLElement>>('transcript');
  private readonly input = viewChild<ElementRef<HTMLTextAreaElement>>('input');
  private readonly launcher = viewChild<
    ElementRef<HTMLButtonElement>,
    ElementRef<HTMLButtonElement>
  >('launcher', { read: ElementRef });
  private request?: Subscription;
  readonly opened = signal(false);
  readonly messages = signal<ChatTurn[]>([]);
  readonly busy = signal<'chat' | 'action' | null>(null);
  readonly error = signal('');
  readonly card = signal<PendingCard | null>(null);
  readonly draft = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(4000)],
  });

  // Session changes clear even a same-user re-login; profile refreshes do not erase a conversation.
  constructor() {
    effect(() => {
      this.auth.sessionVersion();
      untracked(() => this.reset());
    });
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
    // Scroll after Angular paints new messages/cards, including replies received while the panel was closed.
    afterRenderEffect(() => {
      this.messages();
      this.card();
      this.busy();
      this.error();
      if (this.opened()) {
        const element = this.transcript()?.nativeElement;
        if (element) element.scrollTop = element.scrollHeight;
      }
    });
  }

  // Opening the panel preserves its session history; focus follows the control the user selected.
  toggle() {
    this.opened.update((value) => !value);
    if (this.opened()) setTimeout(() => this.input()?.nativeElement.focus());
    else this.launcher()?.nativeElement.focus();
  }

  // Enter sends, Shift+Enter inserts a newline, and composing text with an input method is left alone.
  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      this.send();
    }
  }

  // Only text goes to the model. A new question discards the old unsubmitted proposal, never executing it.
  send() {
    const content = this.draft.value.trim();
    if (!content || this.draft.invalid || this.busy() || !this.auth.currentUser()) return;
    // Reserve space for the next answer and the user's next turn within the server's 30-message limit.
    if (this.messages().length >= 28) {
      this.error.set('This conversation is full. Clear chat to start a new one.');
      return;
    }
    this.card.set(null);
    this.error.set('');
    this.append('user', content);
    this.draft.reset();
    this.busy.set('chat');
    this.request = this.agent
      .send(this.messages())
      .pipe(finalize(() => this.busy.set(null)))
      .subscribe({
        next: (response) => {
          this.append('assistant', response.reply);
          if (response.action)
            this.card.set({
              proposal: response.action,
              key: crypto.randomUUID(),
              submitted: false,
            });
        },
        error: (error: unknown) => this.error.set(this.chatError(error)),
      });
  }

  // Dismissing a card performs no HTTP call. After an uncertain write it cannot undo a possible saved purchase.
  dismiss() {
    if (this.busy()) return;
    const submitted = this.card()?.submitted;
    this.card.set(null);
    this.error.set('');
    this.append(
      'assistant',
      submitted
        ? 'Card dismissed. Check My Bookings for the result of the earlier request.'
        : 'Proposal dismissed. No action was submitted.',
    );
  }

  // This is the only chat entry point for writes. Recheck a new proposal; explicit recovery reuses the original purchase key.
  confirm() {
    const pending = this.card();
    if (!pending || this.busy() || !this.auth.currentUser()) return;
    this.busy.set('action');
    this.error.set('');
    const proposal = pending.proposal;
    const operation =
      proposal.kind === 'book'
        ? pending.submitted
          ? this.submitBooking(pending)
          : this.catalog.get(proposal.eventId).pipe(
              switchMap((event) => {
                // If the preview changed, show the new facts and wait for another Yes rather than charging a different amount.
                if (
                  event.price !== proposal.unitPrice ||
                  event.title !== proposal.eventTitle ||
                  event.startsAt !== proposal.eventStartsAt
                ) {
                  this.card.set({
                    proposal: {
                      ...proposal,
                      eventTitle: event.title,
                      eventStartsAt: event.startsAt,
                      unitPrice: event.price,
                      total: event.price * proposal.quantity,
                    },
                    key: crypto.randomUUID(),
                    submitted: false,
                  });
                  this.error.set(
                    'Event details changed. Review the updated card and click Yes again.',
                  );
                  return of(null);
                }
                if (
                  event.seatsLeft < proposal.quantity ||
                  new Date(event.startsAt).getTime() <= Date.now()
                ) {
                  this.error.set(
                    'This event no longer has the requested seats available or has already started.',
                  );
                  return of(null);
                }
                return this.submitBooking(pending);
              }),
            )
        : this.submitCancellation(pending);
    this.request = operation.pipe(finalize(() => this.busy.set(null))).subscribe({
      next: (result: Booking | null) => {
        if (!result) return;
        this.booking.refreshHistory();
        this.card.set(null);
        this.append(
          'assistant',
          result.status === 'Confirmed'
            ? `Booked ${result.quantity} ticket(s) for ${result.eventTitle}. Booking #${result.id}; total INR ${result.total.toFixed(2)}.`
            : result.seatReleasePending
              ? `Booking #${result.id} is cancelled; seat return is still pending. Use My Bookings to finish cancellation.`
              : `Booking #${result.id} for ${result.eventTitle} is cancelled. Seats returned.`,
        );
      },
      error: (error: unknown) => {
        // Refresh durable state even after a lost response: cancellation may already have been saved.
        this.booking.refreshHistory();
        if (
          proposal.kind === 'book' &&
          error instanceof HttpErrorResponse &&
          [400, 404, 422].includes(error.status)
        ) {
          this.card.set({ ...pending, key: crypto.randomUUID(), submitted: false });
        }
        this.error.set(
          `${apiError(error)} Check My Bookings before retrying. A booking retry uses the same purchase reference unless the API definitively rejected it.`,
        );
      },
    });
  }

  // Mark cancellation as submitted too, so an uncertain result is not described as an untouched proposal.
  private submitCancellation(pending: PendingCard) {
    this.card.set({ ...pending, submitted: true });
    return this.booking.cancel(pending.proposal.bookingId!);
  }

  // Capture the submitted state before subscribing so later retries cannot create a fresh purchase reference.
  private submitBooking(pending: PendingCard) {
    this.card.set({ ...pending, submitted: true });
    return this.booking.book(
      pending.proposal.eventId,
      pending.proposal.quantity,
      false,
      pending.key,
    );
  }

  // Clear is disabled during work so it cannot hide the outcome of a submitted action.
  clear() {
    if (this.busy()) return;
    this.messages.set([]);
    this.card.set(null);
    this.error.set('');
    this.draft.reset();
  }

  // Clearing session data cancels browser work; a submitted server action can still finish and appears in My Bookings.
  private reset() {
    this.request?.unsubscribe();
    this.request = undefined;
    this.busy.set(null);
    this.opened.set(false);
    this.clear();
  }

  // Store plain text only; Angular interpolates it safely without executing model HTML.
  private append(role: ChatTurn['role'], content: string) {
    this.messages.update((messages) => [...messages, { role, content }]);
  }

  // Distinguish a slow/offline model from a normal validation or permission rejection.
  private chatError(error: unknown) {
    if (
      error instanceof TimeoutError ||
      (error instanceof HttpErrorResponse && error.status === 504)
    )
      return 'The assistant took too long. Please send your question again when ready.';
    if (error instanceof HttpErrorResponse && error.status === 429)
      return 'The assistant is busy. Please try again shortly.';
    if (error instanceof HttpErrorResponse && [0, 502, 503].includes(error.status))
      return 'The assistant is offline or temporarily unavailable. Please try again shortly.';
    return apiError(error);
  }
}
