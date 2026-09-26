# Step-3 — Identity verification

This record captures live commands and gateway responses from 2026-09-26.
It accompanies `src/Services/Identity/Identity.Api/identity.http` so the completion evidence is reviewable and repeatable.

## Build

Command: `dotnet build EventHub.sln --no-restore -m:1 /nodeReuse:false`

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The initial sandbox builds stalled before compiler output and were cancelled. The approved build completed;
NuGet/tool downloads needed approved network access. These were local execution restrictions, not code failures.

## Live gateway requests

The backend was started with `dotnet run --project src/Aspire/EventHub.AppHost --no-build`.
Requests were sent to `http://localhost:5100/identity`; bearer tokens were kept in memory and are omitted here.
The matching requests are saved in `identity.http`. No test project or test suite was created.

| Request | Actual status / response |
|---|---|
| Login admin, organizer, organizer2, attendee, attendee2 with demo password | Each returned 200 with its correct user and role |
| Login admin with wrong password | 401; detail `Invalid email or password.`; code `User.InvalidCredentials` |
| Login unknown email | 401; identical detail and code (request trace ids differ) |
| GET /auth/me without bearer token | 401 |
| GET /auth/me with attendee token | 200; id `00000000-0000-0000-0000-000000000004`, email `attendee@demo.com`, fullName `Demo Attendee`, role `Attendee` |
| GET /users with attendee token | 403 |
| GET /users with admin token | 200; all five seeded users; no password or hash fields |
| Register case-normalized duplicate admin email | 409; `This email is already registered.`; code `User.DuplicateEmail` |
| Register invalid email, empty name, short password | 400 with Email, FullName, and Password field errors |
| Change role to Guest | 400 with Role field error |
| Change nonexistent user's role | 404; `User not found.` |
| Change role with attendee token | 403 |
| Register new user while supplying extra role: Admin | 201; returned user role was Attendee |
| GET /auth/me with newly registered token | 200; correct registered profile |
| GET /auth/me with tampered token | 401 |

Decoded demo token example (admin):

```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "email": "admin@demo.com",
  "name": "Demo Admin",
  "role": "Admin",
  "nbf": 1790436822,
  "exp": 1790444022,
  "iss": "eventhub-identity",
  "aud": "eventhub"
}
```

All five tokens had `exp - nbf = 7200` seconds. Signing and validation explicitly use HMAC-SHA256.

## Role-change experiment

The admin promoted attendee2 to Organizer: PUT returned 200 with Organizer.
GET /auth/me with attendee2's existing token still returned Attendee.
After another login, GET /auth/me with the fresh token returned Organizer.
The admin then restored attendee2 to Attendee (200), preserving the demo role.

This demonstrates that a JWT is a signed snapshot: editing a database role does not rewrite issued tokens.

## SQL and restart

Queried only Identity's `identitydb` through the SQL container's `sqlcmd`, using its existing environment
credential without printing it. No password hashes or secrets were captured.

```text
MigrationId
20260926153158_InitialIdentity

UserCount   PlaintextPasswordCount
6           0

Email                                 Role         HashLength
admin@demo.com                        Admin        84
attendee@demo.com                     Attendee     84
attendee2@demo.com                    Attendee     84
organizer@demo.com                    Organizer    84
organizer2@demo.com                   Organizer    84
step3.1790436823397@demo.com           Attendee     84
```

After stopping and restarting Aspire:

```text
admin@demo.com login:                    200, Admin
step3.1790436823397@demo.com login:       200, Attendee
Identity health:                        200 Healthy
UserCount:                              6
MigrationCount:                         1
```

The verification attendee remains in local development data. Restart preserved its id and did not duplicate seeds.
The migration files are generator-owned; every new human-authored file was reviewed for its opening purpose explanation.
The tracked configuration contains no signing key, SQL password, or internal service credential.
