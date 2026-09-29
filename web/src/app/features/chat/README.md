# Chat feature

The floating panel shares the portal shell and keeps text history for the current login only.
It displays service-derived action cards; only a Yes click calls the existing Booking API.

`chat-widget.ts` owns conversation, request cancellation, confirmation and error state.
The template renders escaped text, accessible controls and price details; SCSS keeps it responsive.
`AgentApiService` sends full history with no chat retry. Session changes clear all state; close preserves it.
See `docs/services/agent/API.md` and `docs/services/common/ANGULAR.md` for flow and trade-offs.
