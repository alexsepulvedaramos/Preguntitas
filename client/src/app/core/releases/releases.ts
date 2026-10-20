import { Lang } from '../i18n/languages';
import { GlyphName } from '../../shared/components/glyph/glyphs';

// Player-facing release notes (rama 26, spec §14 "Releases"). Newest first: the first
// entry is the current app version. Adding a release = a new entry here, written in every
// UI language (rama 23), + the same version in package.json + a `vX.Y.Z` tag on master
// once merged.

export type Localized = Record<Lang, string>;

export interface ReleaseItem {
  glyph: GlyphName;
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
    version: '1.23.0',
    date: '2026-10-20',
    title: {
      es: 'Un aire más propio',
      en: 'A look of its own',
      de: 'Ein eigener Look',
    },
    items: [
      {
        glyph: 'sparkles',
        title: {
          es: 'Iconos con personalidad',
          en: 'Icons with personality',
          de: 'Icons mit Persönlichkeit',
        },
        text: {
          es: 'Los emojis dejan paso a un juego de iconos propio, con los colores de la app y de tu tema: racha, congelaciones, pistas, preguntas flash, premios del mes y más.',
          en: 'Emoji make way for a set of icons of our own, in the app’s colours and your theme’s: streaks, freezes, hints, flash questions, monthly awards and more.',
          de: 'Emojis machen Platz für eigene Icons in den Farben der App und deines Themes: Serie, Serienschutz, Tipps, Blitzfragen, Monatspreise und mehr.',
        },
      },
      {
        glyph: 'wallet',
        title: {
          es: 'Una moneda nuestra',
          en: 'A coin of our own',
          de: 'Eine eigene Münze',
        },
        text: {
          es: 'Las monedas llevan ahora la interrogación de Vaya Preguntita, en la cartera, la tienda, los precios y los avisos.',
          en: 'Coins now carry the Vaya Preguntita question mark: in your wallet, the shop, prices and notifications.',
          de: 'Münzen tragen jetzt das Fragezeichen von Vaya Preguntita: im Geldbeutel, im Shop, bei Preisen und Hinweisen.',
        },
      },
      {
        glyph: 'question',
        title: {
          es: '«Sugerir pregunta» se ve mejor',
          en: '“Suggest a question” stands out',
          de: '„Frage vorschlagen“ fällt besser auf',
        },
        text: {
          es: 'El botón para sugerir una pregunta ahora es un botón de verdad, en el verde de la app.',
          en: 'The button to suggest a question is now a proper button, in the app’s green.',
          de: 'Der Knopf zum Vorschlagen einer Frage ist jetzt ein richtiger Knopf, im Grün der App.',
        },
      },
      {
        glyph: 'bell',
        title: {
          es: 'Notificaciones con la interrogación',
          en: 'Notifications with the question mark',
          de: 'Benachrichtigungen mit dem Fragezeichen',
        },
        text: {
          es: 'En Android, las notificaciones muestran en la barra de estado la interrogación de la app.',
          en: 'On Android, notifications show the app’s question mark in the status bar.',
          de: 'Auf Android zeigen Benachrichtigungen das Fragezeichen der App in der Statusleiste.',
        },
      },
      {
        glyph: 'phone',
        title: {
          es: 'Icono limpio en iPhone',
          en: 'A clean icon on iPhone',
          de: 'Sauberes Icon auf dem iPhone',
        },
        text: {
          es: 'El icono de la app en la pantalla de inicio ya no tiene esas esquinas grises. Si la tienes instalada en iPhone, quítala y vuelve a añadirla para ver el nuevo.',
          en: 'The app icon on your home screen no longer has those grey corners. If you installed it on an iPhone, remove it and add it again to get the new one.',
          de: 'Das App-Icon auf dem Startbildschirm hat keine grauen Ecken mehr. Wenn du die App auf dem iPhone installiert hast, entferne sie und füge sie erneut hinzu, um das neue zu sehen.',
        },
      },
    ],
  },
  {
    version: '1.22.2',
    date: '2026-10-20',
    title: {
      es: 'Regalos con confirmación',
      en: 'Gifts ask first',
      de: 'Geschenke mit Bestätigung',
    },
    items: [
      {
        glyph: 'gift',
        title: {
          es: 'Antes de regalar, te pregunta',
          en: 'A check before you gift',
          de: 'Erst fragen, dann schenken',
        },
        text: {
          es: 'Regalar una congelación ahora te pide confirmarlo antes de gastar tus monedas. Y en tu propia tarjeta ya no aparece el botón de regalar: no puedes regalarte una a ti mismo.',
          en: 'Gifting a freeze now asks you to confirm before spending your coins. And the gift button no longer shows on your own card: you can’t gift one to yourself.',
          de: 'Einen Serienschutz zu verschenken fragt jetzt erst nach, bevor deine Münzen ausgegeben werden. Und auf deiner eigenen Karte gibt es den Schenken-Knopf nicht mehr: Du kannst dir selbst keinen schenken.',
        },
      },
    ],
  },
  {
    version: '1.22.1',
    date: '2026-10-16',
    title: {
      es: 'Sin cierres de sesión inesperados',
      en: 'No more surprise logouts',
      de: 'Keine überraschenden Abmeldungen mehr',
    },
    items: [
      {
        glyph: 'lock',
        title: {
          es: 'Tu sesión aguanta los cortes',
          en: 'Your session survives hiccups',
          de: 'Deine Sitzung übersteht Aussetzer',
        },
        text: {
          es: 'Si cerrabas la app o se cortaba la conexión justo mientras renovaba tu sesión, a veces te echaba y tenías que volver a entrar. Ya no: aunque la abras días después, sigues dentro.',
          en: 'If you closed the app or lost connection right while it was renewing your session, it could log you out and make you sign in again. Not any more: even if you open it days later, you stay signed in.',
          de: 'Wenn du die App geschlossen hast oder die Verbindung genau beim Erneuern deiner Sitzung abbrach, wurdest du manchmal abgemeldet. Jetzt nicht mehr: Selbst wenn du sie erst Tage später öffnest, bleibst du angemeldet.',
        },
      },
    ],
  },
  {
    version: '1.22.0',
    date: '2026-10-13',
    title: {
      es: 'La tienda de estilo',
      en: 'The style shop',
      de: 'Der Style-Shop',
    },
    items: [
      {
        glyph: 'shop',
        title: {
          es: 'Gasta tus monedas en tu estilo',
          en: 'Spend your coins on your style',
          de: 'Gib deine Münzen für deinen Style aus',
        },
        text: {
          es: 'La tienda estrena «Tu estilo»: skins para tu anillo de racha (neón, píxel, hielo y llamas, que siguen marcando tu nivel), accesorios para tu avatar, color para tu nombre, una insignia emoji y títulos de broma. Se compran una vez, son tuyos para siempre y te los pruebas antes de comprarlos.',
          en: 'The shop opens "Your style": skins for your streak ring (neon, pixel, ice and flames, still showing your level), accessories for your avatar, a colour for your name, an emoji badge and joke titles. Buy them once, keep them forever, and try them on before you buy.',
          de: 'Im Shop gibt es jetzt „Dein Style“: Skins für deinen Serienring (Neon, Pixel, Eis und Flammen, die weiter deine Stufe zeigen), Accessoires für deinen Avatar, eine Farbe für deinen Namen, ein Emoji-Abzeichen und Spaßtitel. Einmal kaufen, für immer behalten – und vorher anprobieren.',
        },
      },
      {
        glyph: 'palette',
        title: {
          es: 'Temas para toda la app',
          en: 'Themes for the whole app',
          de: 'Themes für die ganze App',
        },
        text: {
          es: 'Océano, Fresa, Uva o Mandarina: cambia la paleta de colores de toda la app, en modo claro y oscuro. Solo la ves tú.',
          en: 'Ocean, Strawberry, Grape or Tangerine: change the colour palette of the whole app, light and dark. Only you see it.',
          de: 'Ozean, Erdbeere, Traube oder Mandarine: Ändere die Farbpalette der ganzen App, hell und dunkel. Nur du siehst sie.',
        },
      },
      {
        glyph: 'calendar',
        title: {
          es: 'Marco de antigüedad, gratis',
          en: 'A free seniority frame',
          de: 'Ein kostenloser Dabeisein-Rahmen',
        },
        text: {
          es: 'Un nuevo marco que crece con tu tiempo en el grupo: al mes, a los 6 meses, al año y a los 2 años. Elígelo en tu perfil en lugar del de racha.',
          en: 'A new frame that grows with your time in the group: at 1 month, 6 months, 1 year and 2 years. Pick it in your profile instead of the streak one.',
          de: 'Ein neuer Rahmen, der mit deiner Zeit in der Gruppe wächst: nach 1 Monat, 6 Monaten, 1 Jahr und 2 Jahren. Wähl ihn im Profil statt des Serienrahmens.',
        },
      },
    ],
  },
  {
    version: '1.21.0',
    date: '2026-10-13',
    title: {
      es: 'El resumen del mes',
      en: 'Your month in review',
      de: 'Der Monatsrückblick',
    },
    items: [
      {
        glyph: 'celebrate',
        title: {
          es: 'Cada mes, vuestro resumen',
          en: 'A summary every month',
          de: 'Jeden Monat ein Rückblick',
        },
        text: {
          es: 'El día 1 llega el resumen del mes anterior de cada grupo, en historias a pantalla completa: cuántas preguntas jugasteis, la participación del grupo y las rachas más largas. Lo abres desde el grupo y puedes volver a los de los últimos 12 meses.',
          en: "On the 1st, each group gets a summary of the month before as full-screen stories: how many questions you played, the group's participation and the longest runs. Open it from the group and look back at the last 12 months.",
          de: 'Am 1. bekommt jede Gruppe einen Rückblick auf den Vormonat als Vollbild-Storys: wie viele Fragen ihr gespielt habt, die Beteiligung der Gruppe und die längsten Serien. Du öffnest ihn in der Gruppe und kannst die letzten 12 Monate nachschauen.',
        },
      },
      {
        glyph: 'trophy',
        title: {
          es: 'Premios del mes',
          en: 'Monthly awards',
          de: 'Auszeichnungen des Monats',
        },
        text: {
          es: 'El más votado en superlativos, el selector más activo, el mejor detective de autores, el más constante y el autor estrella, cuya pregunta tuvo más respuestas. Si hay empate, el premio se comparte.',
          en: 'Most voted in superlatives, most active picker, best author detective, most consistent, and the star author whose question got the most answers. Ties share the award.',
          de: 'Meistgewählt bei Superlativen, aktivste Auswahl, bester Autoren-Detektiv, am beständigsten und der Star-Autor, dessen Frage die meisten Antworten bekam. Bei Gleichstand wird die Auszeichnung geteilt.',
        },
      },
      {
        glyph: 'match',
        title: {
          es: 'Tu match',
          en: 'Your match',
          de: 'Dein Match',
        },
        text: {
          es: 'La persona con la que más os habéis votado mutuamente en superlativos y parejas secretas. Solo lo ves tú.',
          en: 'The person you and they picked each other most in superlatives and secret pairings. Only you see yours.',
          de: 'Die Person, mit der ihr euch bei Superlativen und geheimen Paaren am häufigsten gegenseitig gewählt habt. Nur du siehst deins.',
        },
      },
    ],
  },
  {
    version: '1.20.0',
    date: '2026-10-13',
    title: {
      es: 'Tu inventario',
      en: 'Your inventory',
      de: 'Dein Inventar',
    },
    items: [
      {
        glyph: 'backpack',
        title: {
          es: 'Compra ahora, úsalo cuando quieras',
          en: 'Buy now, use it whenever',
          de: 'Jetzt kaufen, später benutzen',
        },
        text: {
          es: 'En la tienda ya puedes comprar por adelantado congelaciones, reparaciones de racha, pistas de autor y preguntas flash. Se guardan en tu inventario y sirven en cualquier grupo.',
          en: 'In the shop you can now buy streak freezes, streak repairs, author hints and flash questions ahead of time. They go to your inventory and work in any group.',
          de: 'Im Shop kannst du jetzt Serienschutz, Serienreparaturen, Tipps zur Autorschaft und Blitzfragen im Voraus kaufen. Sie landen in deinem Inventar und gelten in jeder Gruppe.',
        },
      },
      {
        glyph: 'sparkles',
        title: {
          es: '«Usar · tienes N»',
          en: '“Use · you have N”',
          de: '„Benutzen · du hast N“',
        },
        text: {
          es: 'Cuando tienes alguno, su botón de siempre cambia a «Usar» y no te cobra nada: en tu racha del grupo, al regalar una congelación, en «¿Quién la escribió?» y al enviar una pregunta flash. Si no te queda ninguno, pagas en el momento como antes.',
          en: 'When you have one, its usual button turns into “Use” and costs you nothing: on your streak in the group, when gifting a freeze, in “Who wrote it?” and when sending a flash question. If you have none left, you pay on the spot as before.',
          de: 'Wenn du einen hast, wird sein gewohnter Knopf zu „Benutzen“ und kostet nichts: bei deiner Serie in der Gruppe, beim Verschenken von Serienschutz, bei „Wer hat sie geschrieben?“ und beim Senden einer Blitzfrage. Hast du keinen mehr, zahlst du wie bisher direkt.',
        },
      },
    ],
  },
  {
    version: '1.19.0',
    date: '2026-10-13',
    title: {
      es: 'Preguntas flash',
      en: 'Flash questions',
      de: 'Blitzfragen',
    },
    items: [
      {
        glyph: 'flash',
        title: {
          es: 'Lanza tu pregunta ya',
          en: 'Launch your question now',
          de: 'Stell deine Frage sofort',
        },
        text: {
          es: '¿No puedes esperar a que te toque? Al sugerir una pregunta, pulsa «Enviar ahora» y por 500 🪙 sale al momento para todo el grupo como pregunta flash, junto a la del día. Se puede votar durante 24 h y, como siempre, los resultados se ven al votar. Una por persona y día en cada grupo.',
          en: 'Can’t wait for your turn? When you suggest a question, tap “Send now” and for 500 🪙 it goes out to the whole group right away as a flash question, next to today’s. It can be voted for 24 h and, as always, you see the results once you vote. One per person per day in each group.',
          de: 'Du kannst nicht warten, bis du dran bist? Wenn du eine Frage vorschlägst, tipp auf „Jetzt senden“: Für 500 🪙 geht sie sofort als Blitzfrage an die ganze Gruppe, neben der Tagesfrage. Man kann 24 Std. lang abstimmen, und wie immer siehst du die Ergebnisse, sobald du abstimmst. Eine pro Person und Tag in jeder Gruppe.',
        },
      },
      {
        glyph: 'cards',
        title: {
          es: 'Todas las preguntas abiertas, a mano',
          en: 'All open questions at hand',
          de: 'Alle offenen Fragen griffbereit',
        },
        text: {
          es: 'Cuando hay más de una pregunta abierta, pasas de una a otra con las pestañas de arriba. Las que te quedan por responder llevan un punto rojo y, al votar una, saltas a la siguiente. Las flash también quedan en el historial, marcadas con un rayo. Puedes desactivar su aviso en tu perfil.',
          en: 'When more than one question is open, switch between them with the tabs at the top. The ones you haven’t answered carry a red dot, and voting one takes you to the next. Flash questions stay in the history too, marked with a bolt. You can turn their notification off in your profile.',
          de: 'Wenn mehr als eine Frage offen ist, wechselst du oben über die Tabs zwischen ihnen. Unbeantwortete tragen einen roten Punkt, und nach dem Abstimmen geht es zur nächsten. Blitzfragen bleiben auch im Verlauf, mit einem Blitz markiert. Ihre Benachrichtigung kannst du in deinem Profil ausschalten.',
        },
      },
    ],
  },
  {
    version: '1.18.0',
    date: '2026-10-12',
    title: {
      es: 'Elige tú',
      en: 'Your pick',
      de: 'Du wählst',
    },
    items: [
      {
        glyph: 'race',
        title: {
          es: 'La carrera por elegir',
          en: 'The race to pick',
          de: 'Das Rennen ums Auswählen',
        },
        text: {
          es: 'Si a 10 minutos de que salga la pregunta nadie la ha elegido, avisamos a quienes ya han votado la de hoy: el primero que la elija (o que diga «Me vale esta») se queda el turno y gana 10 🪙. Puedes desactivar este aviso en tu perfil.',
          en: 'If nobody has chosen the question 10 minutes before it goes out, everyone who has already voted today gets a heads-up: the first to choose it (or tap “This one works”) takes the turn and wins 10 🪙. You can turn this notification off in your profile.',
          de: 'Hat 10 Minuten vor dem Start noch niemand die Frage gewählt, bekommen alle Bescheid, die heute schon abgestimmt haben: Wer zuerst wählt (oder „Passt so“ tippt), übernimmt die Runde und bekommt 10 🪙. Du kannst diese Benachrichtigung in deinem Profil ausschalten.',
        },
      },
    ],
  },
  {
    version: '1.17.0',
    date: '2026-10-12',
    title: {
      es: 'Vota para verlo',
      en: 'Vote to see it',
      de: 'Abstimmen, um es zu sehen',
    },
    items: [
      {
        glyph: 'lock',
        title: {
          es: 'El historial, solo para quien vota',
          en: 'The history, only for voters',
          de: 'Der Verlauf nur für alle, die abstimmen',
        },
        text: {
          es: 'Los resultados de cada día del historial ahora solo se ven si votaste esa pregunta. ¿Te la perdiste? Vota desde el historial y descúbrelos: tu voto cuenta en el recuento, aunque ya no suma racha ni monedas. Vale para todas las preguntas del grupo, también las de antes de que entraras.',
          en: 'Each day’s results in the history now only show if you voted that question. Missed it? Vote from the history to find out: your vote counts in the tally, though it no longer adds to your streak or coins. It works for every question in the group, even the ones from before you joined.',
          de: 'Die Ergebnisse eines Tages im Verlauf siehst du jetzt nur, wenn du bei der Frage abgestimmt hast. Verpasst? Stimm im Verlauf ab und finde es heraus: Deine Stimme zählt im Ergebnis mit, bringt aber keine Serie und keine Münzen mehr. Das gilt für alle Fragen der Gruppe, auch für die von vor deinem Beitritt.',
        },
      },
      {
        glyph: 'sparkles',
        title: {
          es: 'Pequeños retoques',
          en: 'Small touches',
          de: 'Kleine Verbesserungen',
        },
        text: {
          es: 'La línea del tiempo que queda lleva ahora un reloj delante, para que se entienda de un vistazo. Y el botón de sugerir pregunta por fin parece un botón.',
          en: 'The time-left line now has a clock in front of it, so it reads at a glance. And the suggest-a-question button finally looks like a button.',
          de: 'Die Linie mit der verbleibenden Zeit hat jetzt eine Uhr davor, damit man sie auf einen Blick versteht. Und der Knopf zum Vorschlagen einer Frage sieht endlich wie ein Knopf aus.',
        },
      },
    ],
  },
  {
    version: '1.16.0',
    date: '2026-10-12',
    title: {
      es: '¿Quién la escribió?',
      en: 'Who wrote it?',
      de: 'Wer hat sie geschrieben?',
    },
    items: [
      {
        glyph: 'guess',
        title: {
          es: 'Adivina quién escribió la pregunta',
          en: 'Guess who wrote the question',
          de: 'Rate, wer die Frage geschrieben hat',
        },
        text: {
          es: 'Cuando la pregunta del día la ha escrito alguien del grupo, después de votar puedes adivinar quién fue. Tienes un intento y la respuesta sale al momento: si aciertas, ganas 5 🪙. Si prefieres no arriesgarte, salta y descubre quién fue. Y si la escribiste tú, verás cuántos te han pillado.',
          en: 'When today’s question was written by someone in the group, you can guess who after voting. You get one try and the answer shows right away: guess right and win 5 🪙. Rather not risk it? Skip and find out who it was. And if you wrote it, you’ll see how many people caught you.',
          de: 'Wenn jemand aus der Gruppe die Frage des Tages geschrieben hat, kannst du nach dem Abstimmen raten, wer es war. Du hast einen Versuch und die Antwort kommt sofort: Liegst du richtig, bekommst du 5 🪙. Kein Risiko? Überspring es und sieh nach, wer es war. Und wenn die Frage von dir ist, siehst du, wie viele dich erwischt haben.',
        },
      },
      {
        glyph: 'hint',
        title: {
          es: 'Pista de autor',
          en: 'Author hint',
          de: 'Tipp zur Autorschaft',
        },
        text: {
          es: '¿Dudas? Por 20 🪙 la pista quita la mitad de los sospechosos que no son. Si aciertas después, ganas tus 5 🪙 igualmente.',
          en: 'Not sure? For 20 🪙 the hint rules out half of the wrong suspects. Guess right afterwards and you still win your 5 🪙.',
          de: 'Unsicher? Für 20 🪙 streicht der Tipp die Hälfte der falschen Verdächtigen. Rätst du danach richtig, bekommst du trotzdem deine 5 🪙.',
        },
      },
      {
        glyph: 'sparkles',
        title: {
          es: 'Una pantalla de grupo más limpia',
          en: 'A cleaner group screen',
          de: 'Ein aufgeräumter Gruppenbildschirm',
        },
        text: {
          es: 'La pregunta manda. Tu racha es ahora una pequeña llama arriba que se pone naranja si está en peligro; tócalo para ver tus congelaciones. El tiempo que queda es una línea fina, y al lado ves quién ha votado ya. Los avisos importantes, como que te toca elegir, siguen saliendo encima de la pregunta.',
          en: 'The question comes first. Your streak is now a small flame at the top that turns orange when it’s at risk; tap it to see your freezes. Time left is a thin line, with who has already voted right next to it. Important notices, like your turn to pick, still show above the question.',
          de: 'Die Frage steht im Mittelpunkt. Deine Serie ist jetzt eine kleine Flamme oben, das orange wird, wenn sie in Gefahr ist; tipp darauf für deinen Serienschutz. Die verbleibende Zeit ist eine dünne Linie, daneben siehst du, wer schon abgestimmt hat. Wichtige Hinweise, etwa dass du mit Auswählen dran bist, stehen weiterhin über der Frage.',
        },
      },
      {
        glyph: 'history',
        title: {
          es: 'Autores en el historial',
          en: 'Authors in the history',
          de: 'Autor:innen im Verlauf',
        },
        text: {
          es: 'El historial dice quién escribió cada pregunta y si lo adivinaste. Tus aciertos también salen en tus estadísticas del grupo.',
          en: 'The history shows who wrote each question and whether you guessed it. Your right guesses also show in your group stats.',
          de: 'Im Verlauf steht, wer welche Frage geschrieben hat und ob du richtig geraten hast. Deine Treffer stehen auch in deinen Gruppenstatistiken.',
        },
      },
      {
        glyph: 'tools',
        title: {
          es: 'Avisos que ya no se esconden',
          en: 'Notices no longer hide',
          de: 'Hinweise verstecken sich nicht mehr',
        },
        text: {
          es: 'Los avisos que salen con una ventana abierta, como «no tienes monedas suficientes» al regalar una congelación, ya se ven por encima.',
          en: 'Notices that pop up while a window is open, like “you don’t have enough coins” when gifting a freeze, now show on top of it.',
          de: 'Hinweise, die bei geöffnetem Fenster erscheinen, etwa „nicht genug Münzen“ beim Verschenken eines Serienschutzes, liegen jetzt darüber.',
        },
      },
    ],
  },
  {
    version: '1.15.0',
    date: '2026-10-12',
    title: {
      es: 'Congela tu racha',
      en: 'Freeze your streak',
      de: 'Schütze deine Serie',
    },
    items: [
      {
        glyph: 'freeze',
        title: {
          es: 'Congelaciones de racha',
          en: 'Streak freezes',
          de: 'Serienschutz',
        },
        text: {
          es: 'Si un día no votas, una congelación se gasta sola y tu racha sigue viva. Cada mes te regalamos una en cada grupo, y puedes comprar más por 100 🪙 (hasta tener 2). Las ves junto a tu racha, en la pantalla del grupo.',
          en: 'If you miss a day, a freeze is used automatically and your streak lives on. You get a free one every month in each group, and you can buy more for 100 🪙 (up to 2 at a time). You’ll find them next to your streak on the group screen.',
          de: 'Verpasst du einen Tag, wird automatisch ein Serienschutz verbraucht und deine Serie bleibt am Leben. Jeden Monat bekommst du in jeder Gruppe einen geschenkt, und für 100 🪙 kannst du mehr kaufen (bis zu 2 gleichzeitig). Du findest sie neben deiner Serie auf dem Gruppenbildschirm.',
        },
      },
      {
        glyph: 'gift',
        title: {
          es: 'Regala una congelación',
          en: 'Gift a freeze',
          de: 'Verschenke Serienschutz',
        },
        text: {
          es: 'Toca el avatar de alguien del grupo y regálale una congelación para salvarle la racha.',
          en: 'Tap someone’s avatar in the group and gift them a freeze to save their streak.',
          de: 'Tippe in der Gruppe auf den Avatar einer Person und schenk ihr einen Serienschutz, um ihre Serie zu retten.',
        },
      },
      {
        glyph: 'repair',
        title: {
          es: 'Repara una racha perdida',
          en: 'Repair a lost streak',
          de: 'Repariere eine verlorene Serie',
        },
        text: {
          es: '¿Se te escapó la racha sin congelación? Recupérala por 300 🪙 antes de que cierre la siguiente pregunta (una vez al mes por grupo).',
          en: 'Lost your streak without a freeze? Win it back for 300 🪙 before the next question closes (once a month per group).',
          de: 'Serie ohne Schutz verloren? Hol sie dir für 300 🪙 zurück, bevor die nächste Frage schließt (einmal im Monat pro Gruppe).',
        },
      },
    ],
  },
  {
    version: '1.14.0',
    date: '2026-10-12',
    title: {
      es: 'Llegan las monedas',
      en: 'Coins are here',
      de: 'Münzen sind da',
    },
    items: [
      {
        glyph: 'wallet',
        title: {
          es: 'Gana monedas jugando',
          en: 'Earn coins as you play',
          de: 'Verdiene Münzen beim Spielen',
        },
        text: {
          es: 'Cada voto te da monedas, y si eres el primero en votar, unas cuantas más. También ganas al elegir la pregunta cuando te toca (aunque sea quedándote la que salió sola) y aún más si la escribes tú.',
          en: 'Every vote earns you coins, and a few more if you’re the first to vote. You also earn some by picking the question when it’s your turn (even if you keep the one picked for you), and more if you write it yourself.',
          de: 'Jede Stimme bringt dir Münzen, und ein paar mehr, wenn du als Erstes abstimmst. Auch fürs Auswählen der Frage gibt’s welche, wenn du dran bist (selbst wenn du die automatisch gewählte behältst), und noch mehr, wenn du sie selbst schreibst.',
        },
      },
      {
        glyph: 'streak',
        title: {
          es: 'Tus rachas también pagan',
          en: 'Your streaks pay too',
          de: 'Auch deine Serien zahlen sich aus',
        },
        text: {
          es: 'Cada nivel y cada hito de racha trae su premio, y cuanto más larga la racha, más gordo. Votar y elegir dan hasta 40 monedas al día; las rachas no tienen tope.',
          en: 'Every streak level and milestone comes with a reward, and the longer the streak, the bigger it gets. Voting and picking earn up to 40 coins a day; streaks have no cap.',
          de: 'Jede Serienstufe und jeder Meilenstein bringt eine Belohnung, und je länger die Serie, desto größer. Abstimmen und Auswählen bringen bis zu 40 Münzen am Tag; Serien haben kein Limit.',
        },
      },
      {
        glyph: 'wallet',
        title: {
          es: 'Tu cartera',
          en: 'Your wallet',
          de: 'Dein Geldbeutel',
        },
        text: {
          es: 'En el menú de usuario, «Monedas y tienda» te enseña cuántas tienes y de dónde ha salido cada una. La tienda para gastarlas abre muy pronto.',
          en: 'In the user menu, “Coins & shop” shows how many you have and where each one came from. The shop to spend them opens very soon.',
          de: 'Im Benutzermenü zeigt dir „Münzen & Shop“, wie viele du hast und woher jede einzelne kommt. Der Shop zum Ausgeben öffnet sehr bald.',
        },
      },
    ],
  },
  {
    version: '1.13.0',
    date: '2026-10-12',
    title: {
      es: 'Turnos sorteados',
      en: 'Turns drawn at random',
      de: 'Ausgeloste Runden',
    },
    items: [
      {
        glyph: 'dice',
        title: {
          es: 'Nadie sabe a quién le toca',
          en: 'Nobody knows who’s next',
          de: 'Niemand weiß, wer als Nächstes dran ist',
        },
        text: {
          es: 'El turno para elegir la pregunta ya no sigue un orden fijo: se sortea cada día entre quienes aún no han elegido en esta ronda. Todos eligen las mismas veces, pero ya no se puede adivinar quién viene después. Y nadie repite dos días seguidos.',
          en: 'Picking the question no longer follows a fixed order: each day’s turn is drawn among those who haven’t picked yet this round. Everyone picks the same number of times, but you can’t guess who’s next any more. And nobody gets two days in a row.',
          de: 'Wer die Frage auswählt, folgt keiner festen Reihenfolge mehr: Jeden Tag wird unter denen gelost, die in dieser Runde noch nicht dran waren. Alle sind gleich oft dran, aber man kann nicht mehr erraten, wer als Nächstes kommt. Und niemand ist zwei Tage hintereinander dran.',
        },
      },
      {
        glyph: 'like',
        title: {
          es: '«Me vale esta»',
          en: '“This one works”',
          de: '„Passt so“',
        },
        text: {
          es: 'Si te toca elegir y te gusta la pregunta que ha salido automáticamente, ahora puedes quedártela con un toque en lugar de buscar otra.',
          en: 'If it’s your turn and you like the question that was picked automatically, you can now keep it with one tap instead of looking for another.',
          de: 'Wenn du dran bist und dir die automatisch ausgewählte Frage gefällt, kannst du sie jetzt mit einem Tipp behalten, statt eine andere zu suchen.',
        },
      },
    ],
  },
  {
    version: '1.12.0',
    date: '2026-10-12',
    title: {
      es: 'Invita con un QR',
      en: 'Invite with a QR code',
      de: 'Einladen per QR-Code',
    },
    items: [
      {
        glyph: 'phone',
        title: {
          es: 'Código QR para unirse',
          en: 'A QR code to join',
          de: 'Ein QR-Code zum Beitreten',
        },
        text: {
          es: 'Cada grupo tiene ahora su código QR: tu amigo lo escanea con la cámara del móvil y entra directo al grupo. Lo encuentras al invitar desde la lista de grupos, en el aviso para invitar cuando el grupo aún no ha empezado y en los ajustes del grupo.',
          en: 'Every group now has its own QR code: your friend scans it with their phone camera and lands straight in the group. You’ll find it when inviting from the group list, in the invite prompt while the group hasn’t started yet, and in the group settings.',
          de: 'Jede Gruppe hat jetzt ihren eigenen QR-Code: Deine Freunde scannen ihn mit der Handykamera und sind direkt in der Gruppe. Du findest ihn beim Einladen in der Gruppenliste, im Einladungshinweis, solange die Gruppe noch nicht gestartet ist, und in den Gruppeneinstellungen.',
        },
      },
      {
        glyph: 'search',
        title: {
          es: 'A pantalla completa',
          en: 'Full screen',
          de: 'Im Vollbild',
        },
        text: {
          es: 'Toca el QR para verlo en grande y que lo escaneen desde tu móvil; la pantalla no se apaga mientras lo enseñas. Y el diálogo de invitar se ha puesto guapo: ahora se ve claro a qué grupo estás invitando.',
          en: 'Tap the QR to show it big so friends can scan it off your phone; the screen stays on while you show it. The invite dialog got a makeover too: it’s now clear which group you’re inviting to.',
          de: 'Tippe auf den QR-Code, um ihn groß anzuzeigen, damit andere ihn von deinem Handy scannen können; der Bildschirm bleibt dabei an. Auch der Einladungsdialog ist hübscher geworden: Man sieht jetzt klar, in welche Gruppe du einlädst.',
        },
      },
    ],
  },
  {
    version: '1.11.1',
    date: '2026-10-12',
    title: {
      es: 'El chat y los resultados se actualizan solos',
      en: 'Chat and results update by themselves',
      de: 'Chat und Ergebnisse aktualisieren sich von selbst',
    },
    items: [
      {
        glyph: 'refresh',
        title: {
          es: 'Sin tocar el botón de refrescar',
          en: 'No more tapping refresh',
          de: 'Kein Aktualisieren-Knopf mehr nötig',
        },
        text: {
          es: 'Mientras tienes el grupo abierto, los mensajes del chat y los votos de la pregunta del día aparecen solos, sin recargar. Si dejas la app en segundo plano se pausa y, al volver, se pone al día al instante.',
          en: 'While you have the group open, chat messages and votes on the daily question show up on their own, no reload needed. If you leave the app in the background it pauses, and catches up the moment you come back.',
          de: 'Solange die Gruppe geöffnet ist, erscheinen Chat-Nachrichten und Stimmen zur Tagesfrage von selbst, ganz ohne Neuladen. Läuft die App im Hintergrund, pausiert die Aktualisierung und holt beim Zurückkommen sofort alles nach.',
        },
      },
    ],
  },
  {
    version: '1.11.0',
    date: '2026-10-12',
    title: {
      es: 'Las preguntas, también en tu idioma',
      en: 'Questions in your language too',
      de: 'Die Fragen jetzt auch in deiner Sprache',
    },
    items: [
      {
        glyph: 'pack',
        title: {
          es: 'Packs en inglés y en alemán',
          en: 'Packs in English and German',
          de: 'Packs auf Englisch und Deutsch',
        },
        text: {
          es: 'Todas las preguntas de los packs están en inglés y alemán, adaptadas para que hagan la misma gracia, no traducidas palabra por palabra. Cada uno ve la pregunta del día en su idioma, aunque el grupo juegue mezclado; las preguntas que escribís vosotros salen tal cual las escribisteis.',
          en: 'Every pack question is now in English and German, adapted to be just as funny rather than translated word for word. Everyone sees the daily question in their own language, even in a mixed group; questions you write yourselves show exactly as you wrote them.',
          de: 'Alle Pack-Fragen gibt es jetzt auf Englisch und Deutsch – angepasst, damit sie genauso lustig sind, nicht Wort für Wort übersetzt. Alle sehen die Tagesfrage in ihrer eigenen Sprache, auch in gemischten Gruppen; eure eigenen Fragen erscheinen genau so, wie ihr sie geschrieben habt.',
        },
      },
      {
        glyph: 'medal',
        title: {
          es: 'Títulos de racha con su gracia',
          en: 'Streak titles with a local twist',
          de: 'Serien-Titel mit Lokalkolorit',
        },
        text: {
          es: 'Los títulos de racha y los mensajes automáticos del chat también llegan en tu idioma. En inglés, la Vieja del Visillo es «The Curtain Twitcher»; en alemán, «Die Gardinenspionin».',
          en: 'Streak titles and the automatic chat messages now come in your language too. At the top of the ladder, you become «The Curtain Twitcher».',
          de: 'Serien-Titel und die automatischen Chat-Nachrichten kommen jetzt auch in deiner Sprache. Ganz oben wirst du zur «Gardinenspionin».',
        },
      },
    ],
  },
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
        glyph: 'globe',
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
        glyph: 'bell',
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
        glyph: 'note',
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
        glyph: 'scale',
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
        glyph: 'phone',
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
        glyph: 'theme',
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
        glyph: 'bell',
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
        glyph: 'streak',
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
        glyph: 'link',
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
        glyph: 'palette',
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
        glyph: 'game',
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
        glyph: 'install',
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
        glyph: 'sparkles',
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
        glyph: 'home',
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
        glyph: 'hello',
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
        glyph: 'link',
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
        glyph: 'book',
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
        glyph: 'news',
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
        glyph: 'pack',
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
        glyph: 'plus',
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
        glyph: 'cleanup',
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
        glyph: 'ruler',
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
        glyph: 'write',
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
        glyph: 'lock',
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
        glyph: 'calendar',
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
        glyph: 'streak',
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
        glyph: 'image',
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
        glyph: 'crown',
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
        glyph: 'profile',
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
        glyph: 'celebrate',
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
        glyph: 'chat',
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
        glyph: 'bell',
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
        glyph: 'clock',
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
        glyph: 'sparkles',
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
        glyph: 'pack',
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
        glyph: 'search',
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
        glyph: 'repeat',
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
        glyph: 'chart',
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
        glyph: 'desktop',
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
        glyph: 'install',
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
        glyph: 'bell',
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
        glyph: 'group',
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
        glyph: 'question',
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
        glyph: 'dice',
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
        glyph: 'vote',
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
        glyph: 'eye',
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
