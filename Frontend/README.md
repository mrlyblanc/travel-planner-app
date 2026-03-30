# Travel Planner Frontend

This frontend is ready to deploy to Azure Static Web Apps.

## What is already configured

- React Router deep links and security headers are generated into `dist/staticwebapp.config.json` during the Vite build, so routes like `/login`, `/register`, and `/itineraries/:id` refresh correctly in Azure.
- Node is pinned in [`package.json`](./package.json) so Azure uses a Vite-compatible runtime during CI builds.
- The API client in [`src/lib/api.ts`](./src/lib/api.ts) only falls back to localhost when the app is running on localhost. Production builds now require a real `VITE_API_BASE_URL`.
- GitHub Actions deployment is defined in [`../.github/workflows/travel-planner-frontend-swa.yml`](../.github/workflows/travel-planner-frontend-swa.yml).
- A centralized Content Security Policy is generated from [`config/csp.ts`](./config/csp.ts), and defaults to `Content-Security-Policy-Report-Only` until you switch it to enforced mode.

## Recommended Azure target

Use **Azure Static Web Apps** for the frontend.

Why this fits the app:

- Vite builds to static assets in `dist`
- React Router needs SPA fallback support
- the site is frontend-only
- GitHub Actions deployment is built in

## Prerequisites

- An Azure subscription
- A GitHub repository containing this project
- A deployed backend URL, for example `https://your-api.azurewebsites.net`
- Optional: a Geoapify API key for live place search

## Step-by-step deployment

### 1. Push the repo to GitHub

Make sure the repo is on GitHub and your default deployment branch is `main`.

### 2. Create the Azure Static Web App

This repo already contains a workflow, so use the **Other** source option and let GitHub Actions deploy with a deployment token.

1. Open the [Azure portal](https://portal.azure.com).
2. Create a new **Static Web App** resource.
3. In **Basics**:
   - Choose your subscription
   - Pick or create a resource group
   - Set a name, for example `trip-board-frontend`
   - Choose **Free** or **Standard**
4. In **Deployment details**, set **Source** to **Other**.
5. Review and create the resource.

### 3. Copy the deployment token

1. Open the new Static Web App resource in Azure.
2. Go to **Overview**.
3. Select **Manage deployment token**.
4. Copy the token.

### 4. Add GitHub repository secrets and variables

Open your GitHub repository and go to **Settings** -> **Secrets and variables** -> **Actions**.

Add these **repository secrets**:

- `AZURE_STATIC_WEB_APPS_API_TOKEN_TRIP_BOARD`: the deployment token from Azure
- `VITE_GEOAPIFY_API_KEY`: optional, only if you want live Geoapify location search

Add these **repository variables**:

- `VITE_API_BASE_URL`: your deployed backend base URL, for example `https://your-api.azurewebsites.net`
- `VITE_API_VERSION`: `1.0`
- `VITE_CSP_MODE`: `report-only` to start safely, then `enforce` once you are comfortable with the reports
- `VITE_CSP_REPORT_URI`: optional override for CSP reporting. If omitted, the build uses `<VITE_API_BASE_URL>/security/csp-reports` when `VITE_API_BASE_URL` is set.

Important:

- `VITE_*` values are injected at build time by Vite, so changing them later in Azure does not rewrite the already-built frontend.
- Because `VITE_*` values are bundled into browser code, do not treat them as server-only secrets. If you use Geoapify, use a browser-safe key and provider-side restrictions when available.

### 5. Verify backend access from the frontend

Before deploying the frontend, make sure the backend is ready for the new frontend origin.

- Allow CORS from your Static Web App domain
- Keep the backend on HTTPS
- Make sure the backend base URL you use in `VITE_API_BASE_URL` is the public URL, not `localhost`

If your backend is hosted on Azure App Service, update CORS there before the first production login test.

### 6. Trigger deployment

Push to `main`, or run the workflow manually from GitHub Actions.

The workflow:

- builds the Vite app from `Frontend/`
- injects the `VITE_*` build variables
- publishes `Frontend/dist` to Azure Static Web Apps

### 7. Validate the deployed site

After the GitHub Actions run finishes:

1. Open the Azure Static Web App URL
2. Test `/login`
3. Refresh on `/register`
4. Open an itinerary detail route directly
5. Log in and confirm data loads from the backend
6. Create or edit an event and confirm SignalR updates still flow

## GitHub Actions workflow details

The workflow assumes this monorepo layout:

- app location: `Frontend`
- output location: `dist`
- API location: empty

That is why Azure portal should use **Source = Other** for this setup. If Azure creates its own workflow on top of this one, you can end up with duplicate deployments.

## Troubleshooting

### The deployed site says "Failed to fetch"

Usually means one of these:

- `VITE_API_BASE_URL` points to the wrong backend URL
- the backend blocks the Static Web App origin with CORS
- the backend is offline

Because Vite replaces `VITE_*` values at build time, fix the GitHub variable and redeploy.

### Refreshing a deep link gives a 404

Confirm `dist/staticwebapp.config.json` was generated during the build.

### You are ready to enforce CSP after running report-only

Set `VITE_CSP_MODE=enforce` in your deployment environment and redeploy. The same centralized policy will then ship as `Content-Security-Policy` instead of `Content-Security-Policy-Report-Only`.

### Geoapify search does not work in production

Check that:

- `VITE_GEOAPIFY_API_KEY` is set in GitHub Actions secrets
- the provider key is allowed for your production domain
- the frontend was redeployed after the key was added
