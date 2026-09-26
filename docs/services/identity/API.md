# Identity service — purpose and API reference

Identity registers users, checks credentials, issues access tokens, and lets administrators manage roles.
This reference describes its five business APIs, request/response fields, permissions, and validation/error messages.

## Responsibilities

Identity owns `identitydb`, including normalized email addresses, names, salted password hashes, and roles.
Registration always creates an Attendee; only an Admin can change a user's role. Passwords and hashes are never returned.
Login and registration issue a signed, two-hour JWT containing user ID (`sub`), email, name, and role.
Every service validates that token independently. Tokens are signed, not encrypted; their claims are readable.

## Endpoint overview

Base URL: `http://localhost:5100/identity`. Protected calls require `Authorization: Bearer <accessToken>`.

| Method | Path               | Purpose                                 | Access         | Success                  |
| ------ | ------------------ | --------------------------------------- | -------------- | ------------------------ |
| POST   | `/auth/register`   | Create an Attendee and sign them in     | Anonymous      | 201, login response      |
| POST   | `/auth/login`      | Verify credentials and issue a token    | Anonymous      | 200, login response      |
| GET    | `/auth/me`         | Read the profile from the current token | Logged-in user | 200, user object         |
| GET    | `/users`           | List user profiles                      | Admin          | 200, user array          |
| PUT    | `/users/{id}/role` | Change a user's stored role             | Admin          | 200, updated user object |

Missing, expired, or invalid tokens on protected routes yield 401; insufficient role yields 403. These authentication middleware responses do not guarantee the business error messages below.

## 1. Registration — POST `/auth/register`

```json
{
    "email": "reader@example.com",
    "fullName": "Example Reader",
    "password": "ExamplePassword123"
}
```

| Body field | Rule                                          | Validation message                                                                                                                                               |
| ---------- | --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `email`    | Required, valid email, at most 256 characters | `'Email' must not be empty.`; `'Email' is not a valid email address.`; `The length of 'Email' must be 256 characters or fewer. You entered {length} characters.` |
| `fullName` | Required, at most 100 characters              | `'Full Name' must not be empty.`; `The length of 'Full Name' must be 100 characters or fewer. You entered {length} characters.`                                  |
| `password` | Required, at least 8 characters               | `'Password' must not be empty.`; `The length of 'Password' must be at least 8 characters. You entered {length} characters.`                                      |

The current validator does not impose uppercase, number, or special-character rules.
The server trims and lowercases email and trims the name before storing them. The body cannot choose an Admin/Organizer role.
Duplicate email returns 409, code `User.DuplicateEmail`, detail `This email is already registered.`; a unique SQL index also protects simultaneous registrations.
Success returns a token and profile, with Location `/identity/auth/me`.

## 2. Login — POST `/auth/login`

```json
{
    "email": "attendee@demo.com",
    "password": "Demo@123"
}
```

Email uses the same validation rules/messages as registration. Password must be nonempty (`'Password' must not be empty.`); login does not apply registration's minimum-length rule.
The email is trimmed/lowercased before lookup. A missing user or incorrect password returns the same 401 error: `User.InvalidCredentials`, `Invalid email or password.` This avoids revealing which emails exist through login errors.
Success returns a fresh token containing the user's current stored role.

## 3. Current profile — GET `/auth/me`

No body or parameters. Returns `id`, `email`, `fullName`, and `role` from the signed token, not a fresh database lookup.
If a role changed after this token was issued, this endpoint still shows the old role. A handler without usable identity returns 401, `User.Unauthenticated`, `Authentication is required.`

## 4. User listing — GET `/users`

Admin only; no body, filters, or pagination. Returns safe user profiles from Identity's database, with no password hashes.
An empty result is `[]`. Non-Admin callers receive 403.

## 5. Change role — PUT `/users/{id}/role`

Supply the user's GUID in the route and this body:

```json
{
    "role": "Organizer"
}
```

| Failure                           | HTTP | Code                | Message                                                              |
| --------------------------------- | ---- | ------------------- | -------------------------------------------------------------------- |
| Empty GUID                        | 400  | `Validation.Failed` | `errors.Id`: `'Id' must not be empty.`                               |
| Unknown or wrong-case role        | 400  | `Validation.Failed` | `errors.Role`: `Choose Attendee, Organizer, or Admin.`               |
| User does not exist               | 404  | `User.NotFound`     | `User not found.`                                                    |
| Domain rejects role independently | 400  | `User.InvalidRole`  | `Choose a valid role.` (normally caught by request validation first) |

Allowed values are exactly `Attendee`, `Organizer`, and `Admin`. Success saves the role and returns the updated profile.
Existing tokens are not revoked or rewritten: the affected user must log in again to receive the new permissions. There is no server-side logout/revocation or refresh-token API currently.

## Response and error formats

| Object             | Fields                                                                |
| ------------------ | --------------------------------------------------------------------- |
| User               | `id` (GUID), `email`, `fullName`, `role`                              |
| Login/registration | `accessToken`, `expiresAt` (offset timestamp), `user` (profile above) |

Field-validation failures return 400 ProblemDetails with title `Validation failed`, code `Validation.Failed`, detail `One or more validation errors occurred.`, and an `errors` dictionary keyed by C# property name (`FullName`, `Email`, etc.). Multiple messages may appear together.
`{length}` represents the submitted string length. Expected business errors use the status/code/detail above, with a trace ID. Malformed JSON or route/type binding failures are framework errors and may have different messages.

## Development-only diagnostic and operational endpoints

These are not additional Identity business APIs. In Development, `POST /debug/echo` (gateway `/identity/debug/echo`) accepts `{ "message": "hello" }` to exercise the shared mediator.
The message is required and limited to 20 characters. Query `fail` can request `Validation`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`, or `Unprocessable`; `throw=true` exercises safe unexpected-exception handling.
An unrecognized `fail` yields 400 with `errors.fail`: `'{value}' is not a recognized error type.` This endpoint has no authorization requirement and is not mapped outside Development.
Development `/health` and `/alive` report service health/liveness.

Example business requests are in [identity.http](../../../src/Services/Identity/Identity.Api/identity.http). See [shared infrastructure](../common/ASPIRE.md) for token validation and configuration.
