// Player-facing release notes (rama 26, spec §14 "Releases"). Newest first: the first
// entry is the current app version. Adding a release = a new entry here + the same
// version in package.json + a `vX.Y.Z` tag on master once merged.

export interface ReleaseItem {
  emoji: string;
  title: string;
  text: string;
}

export interface Release {
  version: string;
  // ISO date (YYYY-MM-DD) the release reached production.
  date: string;
  title: string;
  items: ReleaseItem[];
}

export const RELEASES: readonly Release[] = [
  {
    version: '1.7.0',
    date: '2026-10-11',
    title: 'Pruébalo sin cuenta',
    items: [
      {
        emoji: '🎮',
        title: 'Un día de prueba',
        text: 'Ahora puedes jugar un día entero en un grupo de mentira sin registrarte: votas, ves quién ha votado a quién, eliges la pregunta de mañana y estrenas tu primer marco. Está en la portada, en «Cómo funciona» y, si aún no tienes grupo, en tu lista de grupos.',
      },
      {
        emoji: '📲',
        title: 'Para convencer a tus amigos',
        text: 'Si alguien no acaba de pillar de qué va, mándale la demo: en menos de un minuto lo entiende.',
      },
    ],
  },
  {
    version: '1.6.1',
    date: '2026-10-11',
    title: 'Actualizaciones al momento',
    items: [
      {
        emoji: '✨',
        title: 'Aviso de versión nueva',
        text: 'Cuando haya una versión nueva, la app te avisa y se actualiza con un toque. Ya no hace falta cerrarla del todo para ver las novedades.',
      },
    ],
  },
  {
    version: '1.6.0',
    date: '2026-10-11',
    title: 'Una portada para los nuevos',
    items: [
      {
        emoji: '🏠',
        title: 'Portada nueva',
        text: 'Quien entre en la web sin cuenta ve primero de qué va el juego, en tres pasos y con ejemplos, antes de registrarse. Ideal para mandársela a quien quieras invitar.',
      },
      {
        emoji: '👋',
        title: 'Más fácil empezar',
        text: 'Si aún no tienes grupo te explicamos cómo arrancar, y un grupo con una sola persona te deja compartir la invitación con un toque: el juego empieza en cuanto sois dos.',
      },
      {
        emoji: '🔗',
        title: 'Invitaciones en español',
        text: 'El mensaje al compartir la invitación de un grupo ya no sale en inglés.',
      },
    ],
  },
  {
    version: '1.5.0',
    date: '2026-10-11',
    title: 'Cómo funciona y novedades',
    items: [
      {
        emoji: '📖',
        title: 'Nueva sección «Cómo funciona»',
        text: 'El ciclo diario, los seis tipos de pregunta, los packs y las rachas, explicados con ejemplos. La tienes en el menú de tu avatar.',
      },
      {
        emoji: '📰',
        title: 'Esta página',
        text: 'Cada versión de la app con lo que ha cambiado. Cuando haya algo nuevo verás un punto en tu avatar.',
      },
    ],
  },
  {
    version: '1.4.0',
    date: '2026-10-08',
    title: 'Más preguntas, y mejores',
    items: [
      {
        emoji: '🆕',
        title: 'Tres packs nuevos',
        text: '«Qué prefieres: absurdos», «Talentos del grupo» y «Viajes y planes».',
      },
      {
        emoji: '➕',
        title: '74 preguntas nuevas',
        text: 'Repartidas entre los packs nuevos y Base, Comida, Confesiones, Deportes, Polémicas y Vida nocturna.',
      },
      {
        emoji: '🧹',
        title: 'Limpieza',
        text: 'Fuera 43 preguntas repetidas o flojas. Otras 9 se han reescrito para responderse en una frase o servir a más grupos.',
      },
      {
        emoji: '📏',
        title: 'Escalas sin víctima al azar',
        text: 'Las preguntas de escala de los packs ya no van «sobre» alguien elegido al azar: cada uno se puntúa a sí mismo o da su opinión.',
      },
      {
        emoji: '✍️',
        title: 'Vuelve el «Otro»',
        text: 'Las encuestas de los packs que lo tenían previsto ya dejan escribir tu propia respuesta.',
      },
    ],
  },
  {
    version: '1.3.1',
    date: '2026-10-07',
    title: 'Adiós a los cierres de sesión',
    items: [
      {
        emoji: '🔐',
        title: 'No más expulsiones',
        text: 'La app ya no te saca de tu cuenta al abrirla sin conexión o mientras el servidor se actualiza.',
      },
      {
        emoji: '📅',
        title: 'Sesiones de un año',
        text: 'Tu sesión dura un año desde la última vez que entras (antes eran 90 días).',
      },
    ],
  },
  {
    version: '1.3.0',
    date: '2026-10-06',
    title: 'Llegan las rachas',
    items: [
      {
        emoji: '🔥',
        title: 'Una racha en cada grupo',
        text: 'Cada día que votas la pregunta, tu racha sube uno. Si hay pregunta y no votas, vuelve a cero.',
      },
      {
        emoji: '🖼️',
        title: 'Siete marcos, siete títulos',
        text: 'Desde el tercer día tu avatar estrena marco: madera, piedra, bronce, plata, oro, zafiro y amatista. Cada uno trae un título que es tuyo para siempre.',
      },
      {
        emoji: '👑',
        title: 'La corona',
        text: 'Para quien lleve la racha más larga del grupo.',
      },
      {
        emoji: '🪪',
        title: 'Tarjeta de cada miembro',
        text: 'Toca un avatar del grupo y verás su racha, su récord, sus votos y su participación.',
      },
      {
        emoji: '🎉',
        title: 'Celebraciones y avisos',
        text: 'Una celebración al alargar la racha, un aviso antes de perderla y mensajes en el chat cuando alguien sube de nivel.',
      },
      {
        emoji: '🗨️',
        title: 'Caras en las respuestas',
        text: 'Cada respuesta libre va junto al avatar de quien la escribe.',
      },
      {
        emoji: '🔔',
        title: 'Notificaciones arregladas',
        text: 'Ahora llegan a todos los dispositivos donde las tienes permitidas.',
      },
    ],
  },
  {
    version: '1.2.1',
    date: '2026-07-16',
    title: 'Retoques',
    items: [
      {
        emoji: '⏰',
        title: 'Cambios de hora claros',
        text: 'Si el admin cambia la hora del grupo con la pregunta ya en marcha, la app avisa de que el cambio cuenta desde el día siguiente.',
      },
      {
        emoji: '✨',
        title: 'Entrada renovada',
        text: 'Las pantallas de entrar y registrarse enseñan de un vistazo de qué va el juego.',
      },
    ],
  },
  {
    version: '1.2.0',
    date: '2026-06-30',
    title: 'Packs temáticos',
    items: [
      {
        emoji: '📦',
        title: '14 packs de preguntas',
        text: 'Humor negro, Dilemas, Comida, Supervivencia… El admin decide cuáles usa su grupo.',
      },
      {
        emoji: '🔎',
        title: 'Buscar pregunta es más fácil',
        text: 'Al elegir la pregunta de mañana puedes filtrar por tipo y por pack.',
      },
      {
        emoji: '🔁',
        title: 'Sin repeticiones',
        text: 'Las preguntas de los packs no se repiten hasta que el grupo las ha gastado todas.',
      },
      {
        emoji: '📊',
        title: 'Resultados más legibles',
        text: 'Los nombres de las barras se leen siempre, y la pregunta nueva aparece sola al llegar la hora.',
      },
      {
        emoji: '💻',
        title: 'Varios dispositivos',
        text: 'Puedes tener la sesión abierta en el móvil y en el ordenador a la vez.',
      },
    ],
  },
  {
    version: '1.1.0',
    date: '2026-06-29',
    title: 'Instálala y entérate',
    items: [
      {
        emoji: '📲',
        title: 'Como una app más',
        text: 'Instala Vaya Preguntita en la pantalla de inicio del móvil.',
      },
      {
        emoji: '🔔',
        title: 'Notificaciones',
        text: 'Pregunta nueva, te toca elegir, alguien ha votado y mensajes en el debate. Las configuras por tipo y puedes silenciar cada grupo.',
      },
    ],
  },
  {
    version: '1.0.0',
    date: '2026-06-24',
    title: 'Primera versión',
    items: [
      {
        emoji: '👥',
        title: 'Grupos',
        text: 'Crea un grupo e invita con un código o un enlace.',
      },
      {
        emoji: '❓',
        title: 'Una pregunta al día',
        text: 'Sale a la hora del grupo y hay 24 horas para votar.',
      },
      {
        emoji: '🎲',
        title: 'Turnos para elegir',
        text: 'Cada día le toca a alguien elegir la pregunta de mañana: una del grupo, una del pack Base o una nueva.',
      },
      {
        emoji: '🗳️',
        title: 'Seis tipos de pregunta',
        text: 'Encuesta, superlativo, deathmatch, escala, pareja secreta y respuesta abierta.',
      },
      {
        emoji: '👀',
        title: 'Sin votos secretos',
        text: 'Los resultados dicen quién ha votado qué. Además, historial de todos los días y debate en cada pregunta.',
      },
    ],
  },
];

export const LATEST_RELEASE: Release = RELEASES[0];
export const APP_VERSION = LATEST_RELEASE.version;

// Semver comparison for plain MAJOR.MINOR.PATCH strings: < 0 when a is older than b.
export function compareVersions(a: string, b: string): number {
  const pa = a.split('.').map(Number);
  const pb = b.split('.').map(Number);
  for (let i = 0; i < 3; i++) {
    const diff = (pa[i] ?? 0) - (pb[i] ?? 0);
    if (diff !== 0) return diff;
  }
  return 0;
}
