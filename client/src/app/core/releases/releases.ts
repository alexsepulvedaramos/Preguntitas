import { Lang } from '../i18n/languages';

// Player-facing release notes (rama 26, spec §14 "Releases"). Newest first: the first
// entry is the current app version. Adding a release = a new entry here, written in every
// UI language (rama 23), + the same version in package.json + a `vX.Y.Z` tag on master
// once merged.

export type Localized = Record<Lang, string>;

export interface ReleaseItem {
  emoji: string;
  title: Localized;
  text: Localized;
}

export interface Release {
  version: string;
  // ISO date (YYYY-MM-DD) the release reached production.
  date: string;
  title: Localized;
  items: ReleaseItem[];
}

export const RELEASES: readonly Release[] = [
  {
    version: '1.10.0',
    date: '2026-10-12',
    title: {
      es: 'Juega en inglés y en alemán',
      en: 'Play in English and German',
      de: 'Spiel auf Englisch und Deutsch',
    },
    items: [
      {
        emoji: '🌍',
        title: {
          es: 'Tres idiomas',
          en: 'Three languages',
          de: 'Drei Sprachen',
        },
        text: {
          es: 'La app ya está en español, inglés y alemán. La primera vez sale en el idioma de tu móvil, y lo cambias cuando quieras desde tu menú, tu perfil o la pantalla de entrar.',
          en: 'The app now speaks Spanish, English and German. The first time it uses your phone’s language, and you can switch whenever you like from your menu, your profile or the sign-in screen.',
          de: 'Die App gibt es jetzt auf Spanisch, Englisch und Deutsch. Beim ersten Mal startet sie in der Sprache deines Handys, und du kannst sie jederzeit in deinem Menü, deinem Profil oder auf der Anmeldeseite ändern.',
        },
      },
      {
        emoji: '🔔',
        title: {
          es: 'Avisos en tu idioma',
          en: 'Notifications in your language',
          de: 'Benachrichtigungen in deiner Sprache',
        },
        text: {
          es: 'Las notificaciones y los mensajes de error te llegan en el idioma que elijas, aunque tus amigos jueguen en otro.',
          en: 'Notifications and error messages reach you in the language you pick, even if your friends play in another one.',
          de: 'Benachrichtigungen und Fehlermeldungen bekommst du in der Sprache, die du wählst – auch wenn deine Freunde in einer anderen spielen.',
        },
      },
      {
        emoji: '📝',
        title: {
          es: 'Las preguntas, de momento en español',
          en: 'Questions in Spanish, for now',
          de: 'Die Fragen vorerst auf Spanisch',
        },
        text: {
          es: 'Las preguntas de los packs y los títulos de las rachas siguen en español: los estamos adaptando para que hagan la misma gracia en cada idioma.',
          en: 'Pack questions and streak titles are still in Spanish: we’re adapting them so they’re just as funny in every language.',
          de: 'Die Pack-Fragen und die Serien-Titel sind noch auf Spanisch: Wir passen sie gerade so an, dass sie in jeder Sprache genauso lustig sind.',
        },
      },
    ],
  },
  {
    version: '1.9.1',
    date: '2026-10-12',
    title: {
      es: 'Escalas del 1 al 10',
      en: 'Scales from 1 to 10',
      de: 'Skalen von 1 bis 10',
    },
    items: [
      {
        emoji: '🔟',
        title: {
          es: 'Todas las escalas, del 1 al 10',
          en: 'Every scale goes from 1 to 10',
          de: 'Alle Skalen von 1 bis 10',
        },
        text: {
          es: 'Las preguntas de puntuar ya no dejan elegir el rango: siempre se vota del 1 al 10, así todas se leen igual. Si alguna pregunta antigua usaba otro rango, sus votos se han pasado a la escala del 1 al 10 manteniendo la proporción.',
          en: 'Rating questions no longer let you choose the range: you always vote from 1 to 10, so they all read the same way. If an older question used another range, its votes have been converted to the 1–10 scale, keeping their proportions.',
          de: 'Bei Bewertungsfragen kann man den Bereich nicht mehr wählen: Abgestimmt wird immer von 1 bis 10, so lesen sich alle gleich. Falls eine ältere Frage einen anderen Bereich hatte, wurden ihre Stimmen anteilig auf die Skala von 1 bis 10 umgerechnet.',
        },
      },
    ],
  },
  {
    version: '1.9.0',
    date: '2026-10-11',
    title: {
      es: 'Más pantalla para jugar',
      en: 'More screen to play with',
      de: 'Mehr Platz zum Spielen',
    },
    items: [
      {
        emoji: '📱',
        title: {
          es: 'Una sola barra dentro del grupo',
          en: 'A single bar inside the group',
          de: 'Nur noch eine Leiste in der Gruppe',
        },
        text: {
          es: 'En el móvil, al entrar en un grupo desaparece la barra de arriba con el logo: solo queda la del grupo (volver, historial y ajustes), que se queda fija al hacer scroll. Más sitio para la pregunta del día.',
          en: 'On your phone, the top bar with the logo disappears when you open a group: only the group’s own bar remains (back, history and settings), and it stays put while you scroll. More room for the question of the day.',
          de: 'Auf dem Handy verschwindet in einer Gruppe die obere Leiste mit dem Logo: Es bleibt nur die Leiste der Gruppe (Zurück, Verlauf und Einstellungen), die beim Scrollen stehen bleibt. Mehr Platz für die Frage des Tages.',
        },
      },
      {
        emoji: '🌗',
        title: {
          es: 'El tema, en tu menú',
          en: 'Theme, now in your menu',
          de: 'Das Design in deinem Menü',
        },
        text: {
          es: 'El cambio de modo claro u oscuro está ahora en tu menú de usuario, y hay una opción nueva, «Sistema», que sigue lo que tengas puesto en el móvil.',
          en: 'Switching between light and dark mode now lives in your user menu, and there’s a new “System” option that follows your phone’s setting.',
          de: 'Der Wechsel zwischen hellem und dunklem Modus ist jetzt in deinem Benutzermenü, und es gibt eine neue Option „System“, die der Einstellung deines Handys folgt.',
        },
      },
    ],
  },
  {
    version: '1.8.0',
    date: '2026-10-11',
    title: {
      es: 'Notificaciones más ordenadas',
      en: 'Tidier notifications',
      de: 'Aufgeräumtere Benachrichtigungen',
    },
    items: [
      {
        emoji: '🔔',
        title: {
          es: 'Una notificación por grupo',
          en: 'One notification per group',
          de: 'Eine Benachrichtigung pro Gruppe',
        },
        text: {
          es: 'Si en un grupo pasan varias cosas mientras no miras, te llega una sola notificación que lo resume («Ana, Luis y 2 más han votado») en vez de una por cada voto o mensaje. Al abrir la app se quitan todas, y mientras la tienes abierta ya no te salta ninguna.',
          en: 'If several things happen in a group while you’re away, you get a single notification that sums it up (“Ana, Luis and 2 more voted”) instead of one per vote or message. Opening the app clears them all, and none pop up while you have it open.',
          de: 'Wenn in einer Gruppe mehreres passiert, während du nicht hinschaust, bekommst du eine einzige Benachrichtigung mit einer Zusammenfassung („Ana, Luis und 2 weitere haben abgestimmt“) statt einer pro Stimme oder Nachricht. Beim Öffnen der App verschwinden alle, und solange sie offen ist, kommt keine mehr.',
        },
      },
      {
        emoji: '🔥',
        title: {
          es: 'Que no se te escape la racha',
          en: 'Don’t let your streak slip',
          de: 'Lass dir deine Serie nicht entgehen',
        },
        text: {
          es: 'En tu lista de grupos, junto a «Vota ahora», verás la racha que te juegas si no votas hoy.',
          en: 'In your group list, next to “Vote now”, you’ll see the streak you’ll lose if you don’t vote today.',
          de: 'In deiner Gruppenliste siehst du neben „Jetzt abstimmen“, welche Serie auf dem Spiel steht, wenn du heute nicht abstimmst.',
        },
      },
      {
        emoji: '🔗',
        title: {
          es: 'Invitaciones a la primera',
          en: 'Invitations that just work',
          de: 'Einladungen auf Anhieb',
        },
        text: {
          es: 'Si te invitan con un enlace y aún no tienes cuenta, al registrarte entras directamente en el grupo, sin tener que volver a abrir el enlace.',
          en: 'If you’re invited with a link and don’t have an account yet, signing up takes you straight into the group — no need to open the link again.',
          de: 'Wenn du per Link eingeladen wirst und noch kein Konto hast, landest du nach der Registrierung direkt in der Gruppe, ohne den Link noch einmal öffnen zu müssen.',
        },
      },
      {
        emoji: '🎨',
        title: {
          es: 'Pequeños arreglos',
          en: 'Small fixes',
          de: 'Kleine Verbesserungen',
        },
        text: {
          es: 'Los avatares de «Garabatos» ya se ven bien en modo oscuro, al cerrar sesión ese móvil deja de recibir avisos de tu cuenta, y las notificaciones aguantan más si tienes el móvil apagado un rato.',
          en: '“Doodle” avatars now look right in dark mode, signing out stops that phone from receiving your account’s notifications, and notifications last longer if your phone is off for a while.',
          de: '„Kritzel“-Avatare sehen jetzt auch im dunklen Modus gut aus, nach dem Abmelden bekommt das Handy keine Benachrichtigungen deines Kontos mehr, und Benachrichtigungen halten länger, wenn dein Handy eine Weile aus ist.',
        },
      },
    ],
  },
  {
    version: '1.7.0',
    date: '2026-10-11',
    title: {
      es: 'Pruébalo sin cuenta',
      en: 'Try it without an account',
      de: 'Ausprobieren ohne Konto',
    },
    items: [
      {
        emoji: '🎮',
        title: {
          es: 'Un día de prueba',
          en: 'A trial day',
          de: 'Ein Probetag',
        },
        text: {
          es: 'Ahora puedes jugar un día entero en un grupo de mentira sin registrarte: votas, ves quién ha votado a quién, eliges la pregunta de mañana y estrenas tu primer marco. Está en la portada, en «Cómo funciona» y, si aún no tienes grupo, en tu lista de grupos.',
          en: 'You can now play a whole day in a pretend group without signing up: you vote, see who voted for whom, pick tomorrow’s question and get your first frame. It’s on the home page, in “How it works” and, if you don’t have a group yet, in your group list.',
          de: 'Du kannst jetzt einen ganzen Tag in einer Fantasiegruppe spielen, ohne dich zu registrieren: abstimmen, sehen, wer wen gewählt hat, die Frage für morgen auswählen und deinen ersten Rahmen bekommen. Du findest ihn auf der Startseite, unter „So funktioniert’s“ und – falls du noch keine Gruppe hast – in deiner Gruppenliste.',
        },
      },
      {
        emoji: '📲',
        title: {
          es: 'Para convencer a tus amigos',
          en: 'To win your friends over',
          de: 'Um deine Freunde zu überzeugen',
        },
        text: {
          es: 'Si alguien no acaba de pillar de qué va, mándale la demo: en menos de un minuto lo entiende.',
          en: 'If someone doesn’t quite get what it’s about, send them the demo: they’ll get it in under a minute.',
          de: 'Wenn jemand nicht ganz versteht, worum es geht, schick ihm die Demo: In weniger als einer Minute ist alles klar.',
        },
      },
    ],
  },
  {
    version: '1.6.1',
    date: '2026-10-11',
    title: {
      es: 'Actualizaciones al momento',
      en: 'Instant updates',
      de: 'Updates sofort',
    },
    items: [
      {
        emoji: '✨',
        title: {
          es: 'Aviso de versión nueva',
          en: 'New-version notice',
          de: 'Hinweis auf neue Versionen',
        },
        text: {
          es: 'Cuando haya una versión nueva, la app te avisa y se actualiza con un toque. Ya no hace falta cerrarla del todo para ver las novedades.',
          en: 'When there’s a new version, the app lets you know and updates with a single tap. No need to fully close it to see what’s new.',
          de: 'Wenn es eine neue Version gibt, sagt dir die App Bescheid und aktualisiert sich mit einem Tipp. Du musst sie nicht mehr ganz schließen, um die Neuigkeiten zu sehen.',
        },
      },
    ],
  },
  {
    version: '1.6.0',
    date: '2026-10-11',
    title: {
      es: 'Una portada para los nuevos',
      en: 'A home page for newcomers',
      de: 'Eine Startseite für Neue',
    },
    items: [
      {
        emoji: '🏠',
        title: {
          es: 'Portada nueva',
          en: 'New home page',
          de: 'Neue Startseite',
        },
        text: {
          es: 'Quien entre en la web sin cuenta ve primero de qué va el juego, en tres pasos y con ejemplos, antes de registrarse. Ideal para mandársela a quien quieras invitar.',
          en: 'Anyone visiting without an account first sees what the game is about, in three steps and with examples, before signing up. Perfect for sending to whoever you want to invite.',
          de: 'Wer die Seite ohne Konto öffnet, sieht zuerst in drei Schritten und mit Beispielen, worum es geht, bevor er sich registriert. Ideal, um sie an alle zu schicken, die du einladen willst.',
        },
      },
      {
        emoji: '👋',
        title: {
          es: 'Más fácil empezar',
          en: 'Easier to get started',
          de: 'Leichter loslegen',
        },
        text: {
          es: 'Si aún no tienes grupo te explicamos cómo arrancar, y un grupo con una sola persona te deja compartir la invitación con un toque: el juego empieza en cuanto sois dos.',
          en: 'If you don’t have a group yet we explain how to get going, and a group with just one person lets you share the invitation with a tap: the game starts as soon as there are two of you.',
          de: 'Wenn du noch keine Gruppe hast, erklären wir dir, wie es losgeht, und in einer Gruppe mit nur einer Person kannst du die Einladung mit einem Tipp teilen: Das Spiel beginnt, sobald ihr zu zweit seid.',
        },
      },
      {
        emoji: '🔗',
        title: {
          es: 'Invitaciones en español',
          en: 'Invitations in Spanish',
          de: 'Einladungen auf Spanisch',
        },
        text: {
          es: 'El mensaje al compartir la invitación de un grupo ya no sale en inglés.',
          en: 'The message you share with a group invitation no longer comes out in English.',
          de: 'Die Nachricht beim Teilen einer Gruppeneinladung erscheint nicht mehr auf Englisch.',
        },
      },
    ],
  },
  {
    version: '1.5.0',
    date: '2026-10-11',
    title: {
      es: 'Cómo funciona y novedades',
      en: 'How it works and what’s new',
      de: 'So funktioniert’s und Neuigkeiten',
    },
    items: [
      {
        emoji: '📖',
        title: {
          es: 'Nueva sección «Cómo funciona»',
          en: 'New “How it works” section',
          de: 'Neuer Bereich „So funktioniert’s“',
        },
        text: {
          es: 'El ciclo diario, los seis tipos de pregunta, los packs y las rachas, explicados con ejemplos. La tienes en el menú de tu avatar.',
          en: 'The daily cycle, the six question types, the packs and the streaks, explained with examples. You’ll find it in your avatar’s menu.',
          de: 'Der Tagesablauf, die sechs Fragetypen, die Packs und die Serien – mit Beispielen erklärt. Du findest ihn im Menü deines Avatars.',
        },
      },
      {
        emoji: '📰',
        title: {
          es: 'Esta página',
          en: 'This page',
          de: 'Diese Seite',
        },
        text: {
          es: 'Cada versión de la app con lo que ha cambiado. Cuando haya algo nuevo verás un punto en tu avatar.',
          en: 'Every version of the app and what changed. When there’s something new, you’ll see a dot on your avatar.',
          de: 'Jede Version der App mit ihren Änderungen. Wenn es etwas Neues gibt, siehst du einen Punkt an deinem Avatar.',
        },
      },
    ],
  },
  {
    version: '1.4.0',
    date: '2026-10-08',
    title: {
      es: 'Más preguntas, y mejores',
      en: 'More questions, and better ones',
      de: 'Mehr Fragen – und bessere',
    },
    items: [
      {
        emoji: '🆕',
        title: {
          es: 'Tres packs nuevos',
          en: 'Three new packs',
          de: 'Drei neue Packs',
        },
        text: {
          es: '«Qué prefieres: absurdos», «Talentos del grupo» y «Viajes y planes».',
          en: '“Would you rather: absurd”, “Group talents” and “Trips and plans”.',
          de: '„Was wäre dir lieber: absurd“, „Talente der Gruppe“ und „Reisen und Pläne“.',
        },
      },
      {
        emoji: '➕',
        title: {
          es: '74 preguntas nuevas',
          en: '74 new questions',
          de: '74 neue Fragen',
        },
        text: {
          es: 'Repartidas entre los packs nuevos y Base, Comida, Confesiones, Deportes, Polémicas y Vida nocturna.',
          en: 'Spread across the new packs and Base, Food, Confessions, Sports, Hot takes and Nightlife.',
          de: 'Verteilt auf die neuen Packs sowie Basis, Essen, Geständnisse, Sport, Streitfragen und Nachtleben.',
        },
      },
      {
        emoji: '🧹',
        title: {
          es: 'Limpieza',
          en: 'Clean-up',
          de: 'Aufgeräumt',
        },
        text: {
          es: 'Fuera 43 preguntas repetidas o flojas. Otras 9 se han reescrito para responderse en una frase o servir a más grupos.',
          en: '43 repeated or weak questions are gone. Another 9 have been rewritten so they can be answered in one sentence or suit more groups.',
          de: '43 doppelte oder schwache Fragen sind raus. 9 weitere wurden umgeschrieben, damit man sie in einem Satz beantworten kann oder sie zu mehr Gruppen passen.',
        },
      },
      {
        emoji: '📏',
        title: {
          es: 'Escalas sin víctima al azar',
          en: 'Scales without a random victim',
          de: 'Skalen ohne zufälliges Opfer',
        },
        text: {
          es: 'Las preguntas de escala de los packs ya no van «sobre» alguien elegido al azar: cada uno se puntúa a sí mismo o da su opinión.',
          en: 'Pack scale questions are no longer “about” someone picked at random: everyone rates themselves or gives their opinion.',
          de: 'Skalenfragen aus den Packs gehen nicht mehr „über“ eine zufällig gewählte Person: Alle bewerten sich selbst oder sagen ihre Meinung.',
        },
      },
      {
        emoji: '✍️',
        title: {
          es: 'Vuelve el «Otro»',
          en: '“Other” is back',
          de: '„Sonstiges“ ist zurück',
        },
        text: {
          es: 'Las encuestas de los packs que lo tenían previsto ya dejan escribir tu propia respuesta.',
          en: 'Pack polls that were meant to have it now let you write your own answer again.',
          de: 'Umfragen aus den Packs, bei denen es vorgesehen war, lassen dich wieder deine eigene Antwort schreiben.',
        },
      },
    ],
  },
  {
    version: '1.3.1',
    date: '2026-10-07',
    title: {
      es: 'Adiós a los cierres de sesión',
      en: 'Goodbye, surprise sign-outs',
      de: 'Schluss mit ungewollten Abmeldungen',
    },
    items: [
      {
        emoji: '🔐',
        title: {
          es: 'No más expulsiones',
          en: 'No more getting kicked out',
          de: 'Kein Rauswurf mehr',
        },
        text: {
          es: 'La app ya no te saca de tu cuenta al abrirla sin conexión o mientras el servidor se actualiza.',
          en: 'The app no longer signs you out when you open it offline or while the server is updating.',
          de: 'Die App meldet dich nicht mehr ab, wenn du sie offline öffnest oder während der Server aktualisiert wird.',
        },
      },
      {
        emoji: '📅',
        title: {
          es: 'Sesiones de un año',
          en: 'Year-long sessions',
          de: 'Sitzungen für ein Jahr',
        },
        text: {
          es: 'Tu sesión dura un año desde la última vez que entras (antes eran 90 días).',
          en: 'Your session lasts a year from the last time you open the app (it used to be 90 days).',
          de: 'Deine Sitzung hält ein Jahr ab deinem letzten Besuch (vorher waren es 90 Tage).',
        },
      },
    ],
  },
  {
    version: '1.3.0',
    date: '2026-10-06',
    title: {
      es: 'Llegan las rachas',
      en: 'Streaks are here',
      de: 'Die Serien sind da',
    },
    items: [
      {
        emoji: '🔥',
        title: {
          es: 'Una racha en cada grupo',
          en: 'A streak in every group',
          de: 'Eine Serie in jeder Gruppe',
        },
        text: {
          es: 'Cada día que votas la pregunta, tu racha sube uno. Si hay pregunta y no votas, vuelve a cero.',
          en: 'Every day you vote on the question, your streak goes up by one. If there’s a question and you don’t vote, it goes back to zero.',
          de: 'Jeden Tag, an dem du abstimmst, wächst deine Serie um eins. Wenn es eine Frage gibt und du nicht abstimmst, fällt sie auf null.',
        },
      },
      {
        emoji: '🖼️',
        title: {
          es: 'Siete marcos, siete títulos',
          en: 'Seven frames, seven titles',
          de: 'Sieben Rahmen, sieben Titel',
        },
        text: {
          es: 'Desde el tercer día tu avatar estrena marco: madera, piedra, bronce, plata, oro, zafiro y amatista. Cada uno trae un título que es tuyo para siempre.',
          en: 'From day three your avatar gets a frame: wood, stone, bronze, silver, gold, sapphire and amethyst. Each one comes with a title that’s yours forever.',
          de: 'Ab dem dritten Tag bekommt dein Avatar einen Rahmen: Holz, Stein, Bronze, Silber, Gold, Saphir und Amethyst. Zu jedem gibt es einen Titel, der für immer dir gehört.',
        },
      },
      {
        emoji: '👑',
        title: {
          es: 'La corona',
          en: 'The crown',
          de: 'Die Krone',
        },
        text: {
          es: 'Para quien lleve la racha más larga del grupo.',
          en: 'For whoever has the longest streak in the group.',
          de: 'Für die Person mit der längsten Serie der Gruppe.',
        },
      },
      {
        emoji: '🪪',
        title: {
          es: 'Tarjeta de cada miembro',
          en: 'A card for every member',
          de: 'Eine Karte für jedes Mitglied',
        },
        text: {
          es: 'Toca un avatar del grupo y verás su racha, su récord, sus votos y su participación.',
          en: 'Tap an avatar in the group to see their streak, their record, their votes and how often they take part.',
          de: 'Tippe auf einen Avatar in der Gruppe und du siehst Serie, Rekord, Stimmen und Beteiligung.',
        },
      },
      {
        emoji: '🎉',
        title: {
          es: 'Celebraciones y avisos',
          en: 'Celebrations and reminders',
          de: 'Feiern und Erinnerungen',
        },
        text: {
          es: 'Una celebración al alargar la racha, un aviso antes de perderla y mensajes en el chat cuando alguien sube de nivel.',
          en: 'A celebration when your streak grows, a reminder before you lose it and chat messages when someone levels up.',
          de: 'Eine Feier, wenn deine Serie wächst, eine Erinnerung, bevor du sie verlierst, und Chatnachrichten, wenn jemand aufsteigt.',
        },
      },
      {
        emoji: '🗨️',
        title: {
          es: 'Caras en las respuestas',
          en: 'Faces next to answers',
          de: 'Gesichter bei den Antworten',
        },
        text: {
          es: 'Cada respuesta libre va junto al avatar de quien la escribe.',
          en: 'Every free-text answer appears next to the avatar of whoever wrote it.',
          de: 'Jede freie Antwort steht neben dem Avatar der Person, die sie geschrieben hat.',
        },
      },
      {
        emoji: '🔔',
        title: {
          es: 'Notificaciones arregladas',
          en: 'Notifications fixed',
          de: 'Benachrichtigungen repariert',
        },
        text: {
          es: 'Ahora llegan a todos los dispositivos donde las tienes permitidas.',
          en: 'They now reach every device where you’ve allowed them.',
          de: 'Sie kommen jetzt auf allen Geräten an, auf denen du sie erlaubt hast.',
        },
      },
    ],
  },
  {
    version: '1.2.1',
    date: '2026-07-16',
    title: {
      es: 'Retoques',
      en: 'Touch-ups',
      de: 'Feinschliff',
    },
    items: [
      {
        emoji: '⏰',
        title: {
          es: 'Cambios de hora claros',
          en: 'Clear time changes',
          de: 'Klare Zeitänderungen',
        },
        text: {
          es: 'Si el admin cambia la hora del grupo con la pregunta ya en marcha, la app avisa de que el cambio cuenta desde el día siguiente.',
          en: 'If the admin changes the group’s time while a question is already running, the app explains that the change applies from the next day.',
          de: 'Wenn der Admin die Uhrzeit der Gruppe ändert, während schon eine Frage läuft, weist die App darauf hin, dass die Änderung ab dem nächsten Tag gilt.',
        },
      },
      {
        emoji: '✨',
        title: {
          es: 'Entrada renovada',
          en: 'A fresh sign-in',
          de: 'Neue Anmeldeseite',
        },
        text: {
          es: 'Las pantallas de entrar y registrarse enseñan de un vistazo de qué va el juego.',
          en: 'The sign-in and sign-up screens show what the game is about at a glance.',
          de: 'Die Anmelde- und Registrierungsseiten zeigen auf einen Blick, worum es im Spiel geht.',
        },
      },
    ],
  },
  {
    version: '1.2.0',
    date: '2026-06-30',
    title: {
      es: 'Packs temáticos',
      en: 'Themed packs',
      de: 'Themen-Packs',
    },
    items: [
      {
        emoji: '📦',
        title: {
          es: '14 packs de preguntas',
          en: '14 question packs',
          de: '14 Fragen-Packs',
        },
        text: {
          es: 'Humor negro, Dilemas, Comida, Supervivencia… El admin decide cuáles usa su grupo.',
          en: 'Dark humour, Dilemmas, Food, Survival… The admin decides which ones their group uses.',
          de: 'Schwarzer Humor, Dilemmas, Essen, Überleben… Der Admin entscheidet, welche seine Gruppe nutzt.',
        },
      },
      {
        emoji: '🔎',
        title: {
          es: 'Buscar pregunta es más fácil',
          en: 'Finding a question is easier',
          de: 'Fragen finden ist leichter',
        },
        text: {
          es: 'Al elegir la pregunta de mañana puedes filtrar por tipo y por pack.',
          en: 'When picking tomorrow’s question you can filter by type and by pack.',
          de: 'Bei der Auswahl der Frage für morgen kannst du nach Typ und Pack filtern.',
        },
      },
      {
        emoji: '🔁',
        title: {
          es: 'Sin repeticiones',
          en: 'No repeats',
          de: 'Keine Wiederholungen',
        },
        text: {
          es: 'Las preguntas de los packs no se repiten hasta que el grupo las ha gastado todas.',
          en: 'Pack questions don’t repeat until the group has used them all.',
          de: 'Pack-Fragen wiederholen sich erst, wenn die Gruppe alle durch hat.',
        },
      },
      {
        emoji: '📊',
        title: {
          es: 'Resultados más legibles',
          en: 'Easier-to-read results',
          de: 'Besser lesbare Ergebnisse',
        },
        text: {
          es: 'Los nombres de las barras se leen siempre, y la pregunta nueva aparece sola al llegar la hora.',
          en: 'The names on the bars are always readable, and the new question shows up by itself when it’s time.',
          de: 'Die Namen auf den Balken sind immer lesbar, und die neue Frage erscheint von selbst, wenn es so weit ist.',
        },
      },
      {
        emoji: '💻',
        title: {
          es: 'Varios dispositivos',
          en: 'Several devices',
          de: 'Mehrere Geräte',
        },
        text: {
          es: 'Puedes tener la sesión abierta en el móvil y en el ordenador a la vez.',
          en: 'You can stay signed in on your phone and your computer at the same time.',
          de: 'Du kannst gleichzeitig auf dem Handy und am Computer angemeldet sein.',
        },
      },
    ],
  },
  {
    version: '1.1.0',
    date: '2026-06-29',
    title: {
      es: 'Instálala y entérate',
      en: 'Install it and stay in the loop',
      de: 'Installieren und nichts verpassen',
    },
    items: [
      {
        emoji: '📲',
        title: {
          es: 'Como una app más',
          en: 'Just like any other app',
          de: 'Wie jede andere App',
        },
        text: {
          es: 'Instala Vaya Preguntita en la pantalla de inicio del móvil.',
          en: 'Install Vaya Preguntita on your phone’s home screen.',
          de: 'Installiere Vaya Preguntita auf dem Startbildschirm deines Handys.',
        },
      },
      {
        emoji: '🔔',
        title: {
          es: 'Notificaciones',
          en: 'Notifications',
          de: 'Benachrichtigungen',
        },
        text: {
          es: 'Pregunta nueva, te toca elegir, alguien ha votado y mensajes en el debate. Las configuras por tipo y puedes silenciar cada grupo.',
          en: 'New question, your turn to pick, someone voted and messages in the debate. You set them up by type and can mute each group.',
          de: 'Neue Frage, du bist mit Auswählen dran, jemand hat abgestimmt und Nachrichten in der Diskussion. Du stellst sie pro Typ ein und kannst jede Gruppe stummschalten.',
        },
      },
    ],
  },
  {
    version: '1.0.0',
    date: '2026-06-24',
    title: {
      es: 'Primera versión',
      en: 'First version',
      de: 'Erste Version',
    },
    items: [
      {
        emoji: '👥',
        title: {
          es: 'Grupos',
          en: 'Groups',
          de: 'Gruppen',
        },
        text: {
          es: 'Crea un grupo e invita con un código o un enlace.',
          en: 'Create a group and invite people with a code or a link.',
          de: 'Gründe eine Gruppe und lade mit einem Code oder Link ein.',
        },
      },
      {
        emoji: '❓',
        title: {
          es: 'Una pregunta al día',
          en: 'One question a day',
          de: 'Eine Frage pro Tag',
        },
        text: {
          es: 'Sale a la hora del grupo y hay 24 horas para votar.',
          en: 'It comes out at the group’s time and there are 24 hours to vote.',
          de: 'Sie erscheint zur Uhrzeit der Gruppe, und ihr habt 24 Stunden zum Abstimmen.',
        },
      },
      {
        emoji: '🎲',
        title: {
          es: 'Turnos para elegir',
          en: 'Taking turns to pick',
          de: 'Abwechselnd auswählen',
        },
        text: {
          es: 'Cada día le toca a alguien elegir la pregunta de mañana: una del grupo, una del pack Base o una nueva.',
          en: 'Each day someone gets to pick tomorrow’s question: one of the group’s, one from the Base pack or a brand-new one.',
          de: 'Jeden Tag wählt jemand die Frage für morgen aus: eine aus der Gruppe, eine aus dem Basis-Pack oder eine ganz neue.',
        },
      },
      {
        emoji: '🗳️',
        title: {
          es: 'Seis tipos de pregunta',
          en: 'Six question types',
          de: 'Sechs Fragetypen',
        },
        text: {
          es: 'Encuesta, superlativo, deathmatch, escala, pareja secreta y respuesta abierta.',
          en: 'Poll, superlative, deathmatch, scale, secret pairing and open answer.',
          de: 'Umfrage, Superlativ, Deathmatch, Skala, geheimes Paar und freie Antwort.',
        },
      },
      {
        emoji: '👀',
        title: {
          es: 'Sin votos secretos',
          en: 'No secret votes',
          de: 'Keine geheimen Stimmen',
        },
        text: {
          es: 'Los resultados dicen quién ha votado qué. Además, historial de todos los días y debate en cada pregunta.',
          en: 'The results show who voted for what. Plus a history of every day and a debate on every question.',
          de: 'Die Ergebnisse zeigen, wer was gewählt hat. Dazu ein Verlauf aller Tage und eine Diskussion zu jeder Frage.',
        },
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
