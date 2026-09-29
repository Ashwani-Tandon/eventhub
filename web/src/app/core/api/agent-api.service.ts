// This adapter sends complete conversation history to the authenticated Agent endpoint.
// Chat is a POST without retries; the Agent can prepare cards but cannot execute purchases.
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { timeout } from 'rxjs';
import { ChatReply, ChatTurn } from '../models/chat.models';
@Injectable({ providedIn: 'root' })
export class AgentApiService {
  private readonly http = inject(HttpClient);
  // Allow the server's 120-second budget plus transport time, then free the browser's waiting state.
  send(messages: readonly ChatTurn[]) {
    return this.http.post<ChatReply>('/api/agent/chat', { messages }).pipe(timeout(125000));
  }
}
