# EventHub browser application

This standalone Angular application provides login, event browsing, ticket booking,
organizer management, sales dashboards, and admin screens. It connects to the local gateway
through the development proxy; the chat widget will be added in Step-11.

## Run

Run `npm install` here once after cloning. From the repository root, run
`dotnet run --project src/Aspire/EventHub.AppHost` to start the backend and Angular together.
Aspire manages the `web` resource and its console logs; its endpoint opens this application.
For frontend-only work, stop the Aspire `web` resource before running `npm start` here to avoid a port conflict.
Open `http://localhost:4200`. `npm run build` creates the production bundle; `npm run lint` checks source and templates.
No test projects, scripts, or test files are included for v1.0 (CP-08).

See [Angular shell and authentication](../docs/services/common/ANGULAR.md) for behavior, folder responsibilities,
token/cookie trade-offs, production proxy needs, and the owner acceptance checklist.

## Configuration files

- `package.json` defines scripts and runtime/build dependencies; `package-lock.json` is npm's generated reproducible dependency lock.
- `angular.json` configures build budgets, routing assets, lint, and the development server on port 4200.
- `proxy.conf.json` matches `/api/**`, forwards to port 5100, and removes `/api` with `pathRewrite`.
- `tsconfig.json` enables strict TypeScript and Angular template checks; `tsconfig.app.json` selects browser application sources.
- `.prettierrc` and `.editorconfig` provide formatting preferences; `eslint.config.js` configures Angular/TypeScript lint and template accessibility.
- `.gitignore` excludes dependencies, build output, and local caches. CLI-generated editor preferences remain local.

These strict JSON files cannot contain purpose comments, so their purpose is documented here.
