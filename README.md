🚀 Developer Onboarding: VayaPreguntita API
Este documento contiene las instrucciones para configurar el entorno de desarrollo local de la API desde cero en cualquier ordenador.

🛠️ 1. Requisitos Previos (Instalación en la máquina)
Antes de clonar el repositorio, el nuevo ordenador debe tener instalado:

.NET 9 SDK: El motor principal.

VS Code + Extensión C# Dev Kit.

Entity Framework Core Tools (Herramienta global). Se instala ejecutando una sola vez en la terminal:

Bash
dotnet tool install --global dotnet-ef
📦 2. Stack Tecnológico y Paquetes (Ya configurados)
El proyecto .csproj ya incluye las siguientes dependencias clave:

Npgsql.EntityFrameworkCore.PostgreSQL: El "traductor" que permite a EF Core hablar con PostgreSQL.

Microsoft.EntityFrameworkCore.Design: Las herramientas necesarias para generar las migraciones desde el código.

⚙️ 3. Pasos para arrancar el proyecto en un PC nuevo
Paso 1: Clonar e ir al directorio
Descarga el código de GitHub y navega hasta la carpeta de la API:

Bash
git clone <tu-url-del-repo>
cd VayaPreguntita/server/VayaPreguntita.API
Paso 2: Restaurar paquetes
Descarga todas las dependencias listadas en el proyecto:

Bash
dotnet restore
Paso 3: Configurar los Secretos Locales (¡Crucial!)
Por seguridad, la contraseña de la base de datos de Supabase no está en el código fuente. El nuevo ordenador necesita vincular la cadena de conexión localmente usando la ID que ya está en el archivo .csproj:

Bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "TU_CADENA_URI_DE_SUPABASE_AQUI"
Paso 4: Sincronizar la Base de Datos
Si es una base de datos nueva o ha habido cambios en los modelos, aplica las migraciones pendientes:

Bash
dotnet ef database update
Paso 5: Arrancar el servidor
Levanta la API en modo desarrollo:

Bash
dotnet run
