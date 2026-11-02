# Control Vehicular E2E

This NUnit project runs browser tests against the real Blazor portal. It does not start a mock application or replace the Java API.

## Configuration

- `E2E_BASE_URL`: portal URL; defaults to `http://127.0.0.1:5114`.
- `E2E_TIMEOUT_MS`: Playwright action and navigation timeout; defaults to `15000`.
- `E2E_ARTIFACTS_DIR`: screenshot and trace output directory; defaults to `tests/ControlVehicularPortalWeb.E2E/artifacts` from the repository root.

## Run in Codespaces

From the repository root, start the actual portal in one terminal:

```bash
dotnet run --project ControlVehicularPortalWeb/ControlVehicularPortalWeb.csproj --launch-profile https
```

In another terminal, build the test project and install its matching Chromium browser and Linux dependencies:

```bash
dotnet build tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj
npx --yes playwright@1.56.0 install --with-deps chromium
```

Run the CV-69 test from the repository root:

```bash
E2E_BASE_URL=https://localhost:7293 dotnet test tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj --logger "trx;LogFileName=cv-69.trx"
```

The test creates a fresh browser context with no cookies. Screenshots and Playwright traces are saved under `artifacts/` and attached to Allure results. Generate an HTML report after running tests with:

```bash
allure generate tests/ControlVehicularPortalWeb.E2E/allure-results --clean -o tests/ControlVehicularPortalWeb.E2E/allure-report
```

CV-69 only visits the unauthenticated portal route. It does not log in or submit vehicle operations to the Java API.

## CV-60 remote-write safety

CV-60 is skipped unless `E2E_ALLOW_REMOTE_WRITES=true`. It also requires a dedicated test user's credentials and an existing owner ID:

```bash
export E2E_BASE_URL=https://localhost:7293
export E2E_EMAIL='dedicated-test-user@example.invalid'
export E2E_PASSWORD='set-in-your-shell'
export E2E_OWNER_ID='123'
export E2E_ALLOW_REMOTE_WRITES=true
```

Use only a dedicated test account and a valid owner ID accepted by the Java API. The test creates a unique brand, model, and plate, then updates and logically archives only that record. Archiving does not imply physical deletion. If the flow fails after registration, the run marker is written to NUnit output and attached to Allure so the record can be located; the test does not automatically retry or modify other records.

Run only CV-60 when explicitly authorizing remote writes:

```bash
dotnet test tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj --filter FullyQualifiedName~CV60FlujoGestionVehiculosTests
```

The test captures actual browser screenshots at login, module entry, registration, consultation, edit, persisted update, and archive verification stages. With the write flag disabled or required variables missing, it is reported as skipped and no screenshots or traces are saved for that skipped CV-60 run.

## Fase 7 consolidated run

The official Jira/Zephyr cases are categorized as `CV-60`, `CV-61`, `CV-69`, and `CV-70`. The login/owner lookup helper is categorized `Preflight` and excluded from the consolidated six-case run. CV-69 JWT invalid is an API-only Playwright test. It currently expects HTTP 401/403; an observed HTTP 500 is reported as a real failure, not accepted as rejection.

Keep `E2E_ALLOW_REMOTE_WRITES=false` for read-only validation. For an explicitly authorized six-case run, use a dedicated QA user and the verified QA owner ID, then set the credentials through the hidden terminal prompt. Render may take over a minute to wake, so use a 120-second Playwright timeout. The consolidated filter excludes only the helper preflight:

```bash
read -r -p 'Correo QA: ' E2E_EMAIL
read -r -s -p 'Contraseña QA: ' E2E_PASSWORD; printf '\n'
export E2E_EMAIL E2E_PASSWORD E2E_OWNER_ID=2 E2E_ALLOW_REMOTE_WRITES=true
export E2E_TIMEOUT_MS=120000
export ALLURE_CONFIG="$PWD/tests/ControlVehicularPortalWeb.E2E/allureConfig.fase7.json"
dotnet test tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj \
	--filter 'TestCategory=CV-60|TestCategory=CV-61|TestCategory=CV-69|TestCategory=CV-70' \
	--logger "trx;LogFileName=fase7-consolidado.trx"
```

Set `ALLURE_CONFIG` to the absolute path of `allureConfig.fase7.json` for that run. It writes results to `allure-results-fase7-consolidado` without clearing historical `allure-results`; generate the phase report separately:

```bash
allure generate tests/ControlVehicularPortalWeb.E2E/allure-results-fase7-consolidado \
	--clean -o tests/ControlVehicularPortalWeb.E2E/allure-report-fase7-consolidado
```