# 🚀 Plan de Desarrollo Modular: Vaya Preguntita

Este documento detalla el plan de desarrollo para llevar el proyecto "Vaya Preguntita" desde su estado inicial hasta su puesta en producción. Incluye buenas prácticas, SEO, testing y el flujo completo de autenticación.

---

## 📋 Registro de Progreso

### Última Actualización: 12 de mayo de 2026, 19:05 UTC

#### FASE 1: Estado Actual - 60% Completada ✅

**Cambios Implementados:**

- ✅ **Refactorización del Modelo de Datos**: Creada entidad `QuestionMetadata` con soporte para todos los 5 tipos de preguntas (The Superlative, The Deathmatch, The Scale, The Secret Pairing). Implementada como tipo poseído (Owned Type) mapeado a columna JSONB en PostgreSQL.
- ✅ **Configuración EF Core**: Actualizado `AppDbContext.OnModelCreating()` para mapear `Metadata` a tipo `jsonb` de Supabase con soporte para colecciones anidadas (`Teams`).
- ✅ **Migraciones**: Generada y aplicada migración `20260511152838_UpdateQuestionMetadata` a base de datos Supabase. Consolidadas columnas dispersas en una única columna JSON flexible.
- ✅ **Dependencias**: Instalados paquetes `FluentValidation.AspNetCore` (v11.3.1) y `FluentValidation.DependencyInjectionExtensions` (v11.11.0).
- ✅ **Configuración de Conexión**: Actualizado `Program.cs` para soportar tanto `ConnectionStrings:Supabase` como fallback a `DefaultConnection`. Connection string actualizado a pooler endpoint con IPv4 compatible.
- ✅ **Control de Versiones**: Rama `refactor/question-metadata` creada y subida a GitHub con commit semántico. Nueva rama `feat/question-validators` lista para siguiente paso.
- ✅ **Validaciones de Dominio (FluentValidation)**: Validadores para `CreateQuestionDto`, `CreateVoteDto` y `CreateOptionDto` con reglas específicas por tipo de pregunta y consistencia de respuestas.
- ✅ **Alineación con Specs**: DTOs, validadores y controladores actualizados para los tipos de pregunta definidos en el spec (Superlative, Deathmatch, Scale, Secret Pairing, Custom Poll).

**Pendiente en FASE 1:**

- ⏳ **Setup Testing Backend**: Crear proyecto `VayaPreguntita.API.Tests` con xUnit, Moq y FluentAssertions. Configurar tests unitarios e integración.

---

## FASE 1: Alineación de Arquitectura y Base de Datos (Backend Core)

_Objetivo: Adaptar el esquema actual para soportar los 5 tipos de preguntas especificados en el documento._

1. **Refactorización del Modelo de Datos para Tipos de Preguntas:**
   - Implementar herencia (TPT o TPH en Entity Framework) o utilizar columnas JSONB en PostgreSQL (Supabase) para el campo `Metadata` de las preguntas, soportando los requisitos dinámicos:
     - `The Superlative`: Soporte para `AllowNobody` y `BlacklistedUserIds`.
     - `The Deathmatch`: Arreglos anidados para `Teams`.
     - `The Scale`: Valores `RangeMin / RangeMax` (y `TargetUserId`).
     - `The Secret Pairing`: Restricciones de `MinSelections / MaxSelections`.
2. **Validaciones de Dominio (FluentValidation):**
   - Integrar `FluentValidation` en .NET para validar los DTOs. Por ejemplo, evitar que en un "Deathmatch" haya usuarios repetidos en equipos contrarios, o asegurar que las selecciones de "Secret Pairing" sean exactamente dos.
3. **Setup Testing Backend (Unit & Integration):**
   - Configurar `xUnit`, `Moq` (o NSubstitute) y `FluentAssertions`.
   - Crear una base de datos en memoria o usar _Testcontainers_ (PostgreSQL) para pruebas de integración del `AppDbContext` y persistencia.

## FASE 2: Identidad, Autenticación y Seguridad (Gestión de Usuarios)

_Objetivo: Implementar un sistema de registro y login robusto, soportando el método convencional y mediante Google._

1. **Gestión de Identidad (Base de Datos):**
   - Ampliar o configurar el modelo `User` para incluir el manejo de contraseñas de forma segura (Hash/Salt) y una columna `AuthProvider` u `OAuthId` para enlazar cuentas sociales de manera segura.
2. **Autenticación Tradicional (Email y Contraseña):**
   - Crear endpoints de `POST /api/auth/register` (Sign Up) y `POST /api/auth/login`.
   - Generación, firma y validación de JSON Web Tokens (JWT) en .NET.
   - Manejo de Refresh Tokens para mantener la sesión del usuario viva de forma segura sin pedir credenciales continuamente.
3. **Autenticación con Google (OAuth 2.0):**
   - Configurar la API en Google Cloud Console para obtener `Client ID` y `Client Secret`.
   - Implementar un endpoint `POST /api/auth/google` que reciba el token de identidad de Google desde el frontend, valide su autenticidad con las librerías oficiales de Google y emita el JWT interno de "Vaya Preguntita" para ese usuario (creándolo si no existe en BD).
4. **Protección de Endpoints:**
   - Implementar el decorador `[Authorize]` y configurar la validación del JWT en el `Program.cs` para proteger todas las rutas privadas del juego.

## FASE 3: Lógica de Negocio y Gamificación (Backend Features)

_Objetivo: Construir los motores de selección diaria, votaciones y puntuaciones._

1. **Gestión de Sesiones/Juegos:**
   - **Sistema de Selección Diaria**: Crear un servicio en segundo plano (`IHostedService` o `BackgroundService`) o un endpoint trigger para seleccionar (o rotar al usuario que selecciona) la pregunta del día usando un pool de preguntas no contestadas.
   - **Sistema de Prioridad:** Algoritmo en el backend que asigne peso/prioridad a preguntas apoyadas por tokens o configuraciones del grupo.
2. **Sistema de Votación y Respuestas Flexibles:**
   - Refactorizar la entidad `Vote/Response` para aceptar tanto IDs como valores escalares (para el tipo "The Scale").
3. **Módulo de Gamificación:**
   - Endpoint/Lógica para gestionar el "Guess the Author" (adivinar quién hizo la pregunta) y un sistema de asignación de puntos.

## FASE 4: Arquitectura Frontend y State Management (Angular)

_Objetivo: Sentar unas bases de arquitectura limpia e integrar el flujo de Login en el cliente._

1. **Pantallas y Flujo de Login (Autenticación en UI):**
   - Formularios reactivos de _Sign In_ y _Sign Up_ con validadores personalizados.
   - Integración del botón oficial / SDK de "Sign in with Google" (@abacritt/angularx-social-login o SDK puro).
2. **State Management (Señales / NgRx):**
   - Usar _Signals_ (Angular v19+) para mantener el estado global de la sesión (ej. `AuthService.currentUser()`), Grupo activo, Pregunta del Día.
3. **Core Interceptors y Seguridad (Guards):**
   - `AuthInterceptor`: Para adjuntar automáticamente el JWT en cada petición HTTP al backend.
   - `AuthGuard` / `NoAuthGuard`: Proteger rutas privadas (dashboard, votaciones) redirigiendo al login si no hay token válido. Redirigir al dashboard si alguien ya logueado intenta entrar al login.
   - Manejo global de errores (interceptor 401 Unauthorized para cerrar sesión).

## FASE 5: Desarrollo de UI / UX y Formularios Dinámicos

_Objetivo: Implementar las pantallas principales y la resolución de preguntas._

1. **Fábrica de Componentes de Preguntas (Polimorfismo en UI):**
   - Crear un componente base y componentes hijos para cada tipo de pregunta.
   - Usar control flow (`@switch`) en Angular para renderizar el componente correspondiente.
2. **Flujo de Creación de Preguntas (Reactive Forms):**
   - Formularios reactivos dinámicos: ej. si seleccionan "The Superlative", mostrar controles para añadir usuarios a la _Blacklist_.
3. **Dashboard / Resultados Diarios:**
   - Interfaz con gráficos para mostrar el resultado de las votaciones.
   - Animación para la revelación del "Autor de la pregunta".

## FASE 6: Optimizaciones Específicas: SEO, Tiempo Real y Rendimiento

_Objetivo: Adiciones de valor técnico y producto._

1. **Tiempo Real (SignalR):**
   - Integrar SignalR en .NET y el cliente en Angular. Esto permitirá que cuando los usuarios voten, los resultados o el cierre de votaciones se reflejen en la pantalla en tiempo real sin recargar.
2. **SEO y Compartición Social (Angular SSR):**
   - Aplicar optimizaciones para el SEO / Compartición.
   - Angular Universal / SSR nativo: Para inyectar meta tags dinámicos (OpenGraph, Twitter Cards) para que al compartir un enlace al grupo o encuesta, tenga un formato atractivo en WhatsApp/Redes.
3. **Caché (Rendimiento):**
   - Implementar caché en memoria (`IMemoryCache` o Redis) en la .NET API para reducir consultas repetitivas (como el perfil de usuario activo o la pregunta del día).

## FASE 7: Quality Assurance (QA) en el Frontend

_Objetivo: Asegurar la estabilidad de las interfaces y flujos de usuario._

1. **Unit Testing Frontend:**
   - Utilizar Jasmine/Karma (o Jest) para probar tuberías, utilidades (ej. parsers del token JWT) y validadores.
2. **End-to-End (E2E) Testing:**
   - Integrar **Cypress** o **Playwright**.
   - Automatizar flujos críticos: Login -> Selección de grupo -> Votación de la pregunta del día -> Comprobación del voto emitido.

## FASE 8: CI / CD, Despliegue y Monitorización

_Objetivo: Automatizar las pruebas y despliegues._

1. **DevOps / GitHub Actions pipelines:**
   - _Backend_: CI de compilación -> Run xUnit Tests -> Deploy automático a **Render**.
   - _Frontend_: Build de producción (Angular AOT/SSR) -> Linting & Testing -> Deploy automático a **Vercel / Netlify**.
2. **Base de Datos (Supabase):**
   - Estrategia de migraciones segura durante despliegues para EF Core.
3. **Monitorización (Uptime y Telemetry):**
   - UptimeRobot para mantener el Backend de Render despierto.
   - Logging estructurado (Serilog) y Endpoints de `HealthCheck`.

---

## 🌟 ANEXO: Reglas de Oro (Development & Git Guidelines)

Para garantizar la mantenibilidad, escalabilidad y calidad del código, durante todo el ciclo de vida del proyecto se deben seguir estrictamente estas directrices:

1. **Git Workflow & Branching:**
   - Nadie empuja código directamente a `main` o `master`.
   - Crear siempre ramas con prefijos semánticos que indiquen la intención: `feat/` (nuevas características), `fix/` (soluciones de bugs), `refactor/` (mejoras de código sin alterar comportamiento), `docs/` (documentación). Ejemplo: `feat/auth-models`.
2. **Commits Atómicos y Semánticos:**
   - Realizar commits pequeños, frecuentes y que representen una única unidad lógica de cambio (evitar commits gigantes con cambios mezclados).
   - Usar la convención [Conventional Commits](https://www.conventionalcommits.org/): `feat: add user entity with password hash`, `fix: resolve JWT validation error`, etc.
3. **Test-Driven / Testing Continuo:**
   - Cada nueva pieza fundamental de lógica de negocio, servicio o endpoint debe ir acompañada de su correspondiente prueba unitaria y/o de integración (tanto en Backend como en Frontend).
4. **Idioma (English First):**
   - **TODOS** los nombres de variables, clases, métodos, DTOs y bases de datos deben estar en inglés.
   - Los comentarios de código, la documentación (XML Comments en C# y JSDoc en TypeScript) y los mensajes de commit también deben estar íntegramente en inglés.
5. **Clean Code & Arquitectura:**
   - Respetar los patrones de diseño definidos (Ej: uso estricto de DTOs para comunicación API, separación de responsabilidades, no acoplar lógica de base de datos en los controladores).
   - Mantener componentes de Angular pequeños y específicos, delegando lógica compleja a servicios.
6. **Base de Datos y Migraciones (EF Core + Supabase):**
   - La base de datos se gestiona íntegramente mediante migraciones de Entity Framework Core desde el proyecto .NET. Cualquier modificación en los modelos (Entities) requiere obligatoriamente generar una nueva migración y aplicarla a Supabase para actualizar el esquema de la base de datos y mantener todo sincronizado.
7. **Registro de Cambios en el Roadmap:**
   - Cada vez que se implemente un cambio (código, configuración, migración o documentación), debe registrarse en este roadmap para mantener el estado del proyecto siempre actualizado.
8. **Spec-First Rules:**
   - Always follow the spec rules located in spec folder unless it is not directly mentioned, in which case you will ask the user for the prefferences. When something that is relevant to the funtcionality of the final app, and it's confirmed by the user, it should also be updated in the corresponding section of specs folder so the same question will be never needed again.
   - Always create and push atomic commits whenever a task is approved.
