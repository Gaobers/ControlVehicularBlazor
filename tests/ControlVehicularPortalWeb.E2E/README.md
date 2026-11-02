# Control Vehicular E2E

Pruebas NUnit contra el portal Blazor real y la API Java; no arrancan el portal ni sustituyen la API.

## Configuración

- `E2E_BASE_URL`: `https://localhost:7293` por defecto.
- `E2E_API_BASE_URL`: `https://controlvehicular-6s7g.onrender.com/` por defecto.
- `E2E_TIMEOUT_MS`: 15000 ms por defecto para acciones y navegación. Para una ejecución remota autorizada se recomienda configurar 120000 ms por el arranque de Render. Las nuevas aserciones con reintentos usan también este valor.
- `E2E_ARTIFACTS_DIR`: directorio de capturas y trazas; por defecto `tests/ControlVehicularPortalWeb.E2E/artifacts`.
- `E2E_ALLOW_REMOTE_WRITES`: desactivado por defecto. Las cuatro pruebas con escrituras requieren `true`, `E2E_EMAIL`, `E2E_PASSWORD` y un `E2E_OWNER_ID` positivo válido.
- `ALLURE_CONFIG`: ruta absoluta al JSON de configuración de la ejecución.

No guardar credenciales en archivos versionados. Sin autorización o configuración QA las pruebas con escrituras se omiten; una omisión no demuestra que el escenario haya aprobado. El SetUp crea Chromium incluso para pruebas omitidas y para JWT.

## Preparación del entorno

Desde la raíz del repositorio, iniciar el portal en una terminal:

```bash
dotnet run --project ControlVehicularPortalWeb/ControlVehicularPortalWeb.csproj --launch-profile https
```

Compilar sin ejecutar pruebas e instalar Chromium si hace falta:

```bash
dotnet build ControlVehicularPortalWeb.slnx -m:1
npx --yes playwright@1.56.0 install --with-deps chromium
```

## Escenarios oficiales

| Jira | Clase | Alcance |
|---|---|---|
| CV-60 | CV60FlujoGestionVehiculosTests | Registro, consulta, actualización y archivado |
| CV-61 | CV61ConsistenciaConsultasTests | Comparación del detalle Blazor con GET individual de API |
| CV-69 | CV69AccesoSinAutenticacionTests | Protección del portal sin sesión |
| CV-69 | CV69JwtInvalidoApiTests | GET protegido con firma JWT inválida |
| CV-70 | CV70RegistroInvalidoTests | Año inválido, error visible y corrección dentro del mismo formulario |
| CV-70 | CV70ActualizacionInvalidaTests | Actualización inválida y conservación de datos |

Son seis escenarios. `Preflight_LoginYModuloVehiculos_SoloLectura`, categoría `Preflight`, es auxiliar y no debe contarse como séptimo escenario oficial. Seleccionar la clase CV-60 completa incluye ese auxiliar; utilizar el nombre del método para ejecutar solo el caso oficial.

## Preservación y separación de evidencias

Los TRX, HTML, capturas, trazas y resultados Allure históricos están excluidos de Git. Respaldarlos fuera del Codespace antes de eliminar o recrear el entorno. No borrar ni regenerar los directorios históricos de fase 7 ni sobrescribir `fase7-consolidado-final.trx`.

Para cada nueva ejecución autorizada, crear un directorio exclusivo. Ejecutar este bloque desde la raíz **antes de cada comando dotnet test**, incluso al repetir un caso. No reutilizar el directorio de una ejecución anterior:

```bash
export E2E_BASE_URL=https://localhost:7293
export E2E_TIMEOUT_MS=120000
E2E_RUN_DIR=$(mktemp -d "$PWD/tests/ControlVehicularPortalWeb.E2E/artifacts/fase8-XXXXXXXX")
export E2E_ARTIFACTS_DIR="$E2E_RUN_DIR/browser"
export ALLURE_CONFIG="$E2E_RUN_DIR/allureConfig.json"
printf '{"allure":{"directory":"%s"}}\n' "$E2E_RUN_DIR/allure-results" > "$ALLURE_CONFIG"
```

Este bloque solo prepara rutas; no autoriza escrituras. Antes de los casos de vehículos, configurar las variables QA en la sesión y habilitar `E2E_ALLOW_REMOTE_WRITES=true` únicamente con autorización. Cada prueba genera datos únicos y archiva lógicamente solo su registro. No reutilizar ni modificar los vehículos históricos #13 y #14. Si falla tras registrar, conservar el marcador para localizar el registro propio; no hay limpieza automática que oculte el fallo.

### Ejecución futura de los escenarios afectados

Estos comandos son instrucciones para una fase posterior; la fase 8 de estabilización no los ejecuta. Preparar un directorio nuevo con el bloque anterior para cada comando:

```bash
# Los tres escenarios de vehículos afectados; requiere autorización de escrituras.
dotnet test tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj --no-build \
  --filter 'FullyQualifiedName~FlujoCompleto_DejaElVehiculoArchivado|FullyQualifiedName~CV70RegistroInvalidoTests|FullyQualifiedName~CV70ActualizacionInvalidaTests' \
  --results-directory "$E2E_RUN_DIR/trx" --logger 'trx;LogFileName=fase8-afectados.trx'
```

```bash
# JWT: GET sin escrituras de vehículos; conserva el esperado 401/403.
dotnet test tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj --no-build \
  --filter 'FullyQualifiedName~CV69JwtInvalidoApiTests' \
  --results-directory "$E2E_RUN_DIR/trx" --logger 'trx;LogFileName=fase8-jwt.trx'
```

```bash
# Protección sin sesión: selección explícita, sin ejecutar los otros casos.
dotnet test tests/ControlVehicularPortalWeb.E2E/ControlVehicularPortalWeb.E2E.csproj --no-build \
  --filter 'FullyQualifiedName~CV69AccesoSinAutenticacionTests' \
  --results-directory "$E2E_RUN_DIR/trx" --logger 'trx;LogFileName=fase8-sin-sesion.trx'
```

CV-61 comparte el selector y la verificación de archivado: incluirlo en la regresión posterior autorizada. Para un futuro consolidado de seis escenarios, el filtro oficial es `TestCategory=CV-60|TestCategory=CV-61|TestCategory=CV-69|TestCategory=CV-70`; no ejecutarlo durante la estabilización.

Generar HTML únicamente desde los resultados de la nueva ejecución y hacia una ruta nueva, sin `--clean` sobre reportes históricos:

```bash
allure generate "$E2E_RUN_DIR/allure-results" -o "$E2E_RUN_DIR/allure-report"
```

## Interpretación de resultados y evidencias

- El consolidado histórico final tiene 2 aprobadas y 4 fallidas en NUnit; Allure las clasifica como 2 passed, 3 failed y 1 broken. Los JSON de fase 7 incluyen más de una ejecución: no sumarlos como escenarios distintos.
- El helper de campos ya tenía esperas de visibilidad antes de fase 8. Se mantiene esa sincronización y se precisa la etiqueta completa dentro de `.field-group`.
- `InputNumber` usa `@bind-Value` y el evento `onchange`. CV-70 registro corrige el año, pulsa Tab para confirmar el cambio y espera la clase `valid` de Blazor y la desaparición del mensaje antes del envío. No cierra ni recrea el formulario. Si persiste el mensaje tras confirmar el cambio, conservar evidencias como posible defecto funcional.
- El archivado es lógico. CV-60 exige estado `Archivado` en la lista recargada y `ARCHIVADO` en el detalle; la ausencia en activos no basta. El helper compartido también espera explícitamente `Archivado`.
- Las pruebas de navegador adjuntan capturas por etapas y una captura final junto con la traza redactada. El login queda fuera de la traza. Los mensajes de etapas identifican el caso correspondiente.
- JWT registra y adjunta método, ruta, estado observado y esperado antes de la aserción. No adjunta cuerpos ni cabeceras potencialmente sensibles. El HTTP 500 histórico permanece como posible defecto de autenticación de la API Java; nunca se acepta como aprobado. No genera capturas de una página vacía.
- Compilar solo verifica el código. Hasta una nueva ejecución autorizada, los ajustes de fase 8 no constituyen resultados E2E aprobados.
