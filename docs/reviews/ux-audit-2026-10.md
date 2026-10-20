# Auditoría UX/UI — octubre 2026

> Base: rama `feat/brand-glyphs` (1.23.0, todavía sin commitear en el checkout principal; se aplicó tal cual sobre `master` para esta revisión). Solo es un informe: no se ha cambiado código de producto.
> Objetivo: acordar el alcance de la rama **«Visual review: more personality»** (spec §16).

## Cómo se ha hecho

- El cliente Angular se ejecutó en local (`ng serve`). **La API no se levantó**: los user-secrets locales apuntan a Supabase y no a una base de datos local, así que arrancarla habría puesto el ciclo diario a trabajar sobre datos reales. Las pantallas con sesión se capturaron con Chrome headless (puppeteer-core), con un token falso y respuestas de la API simuladas: 5 miembros, cosméticos equipados, una pregunta flash, un resumen mensual e historial. Las respuestas siguen los modelos del cliente.
- Cada pantalla se revisó en claro y en oscuro, a 390 px y a 1280 px, más dos temas de la tienda (`ocean` y `grape`).
- Se completó con una revisión del código de las plantillas y de `styles-lime.css`: colores, tamaños, radios y primitivas de Spartan.
- **Capturas** (fuera del repo, para no inflarlo): carpeta compartida de la sesión, `…/side-session-notes/local_89f7533e-…/ux-audit-shots/`. Se nombran `NN-pantalla-{mobile|desktop}-{light|dark}[-tema].png`. En el texto se citan como `[05]`, `[16]`, etc.

- **Fuera de esta auditoría (ya resuelto en `feat/brand-glyphs`):** emoji de la interfaz → `GlyphComponent`, moneda → `CoinComponent`, botón «Sugerir pregunta» sage sólido, textos de notificaciones push sin emoji e iconos PWA a sangre. No se listan como hallazgos.
- **Pendiente conocido (no es hallazgo nuevo):** los mensajes de sistema del chat todavía llevan emoji.

Escala: **Severidad** alta (rompe o confunde, o falla en accesibilidad) / media (inconsistencia visible) / baja (pulido). **Esfuerzo** S (< ½ día) / M (1–2 días) / L (> 2 días).

---

## 1. Hallazgos transversales (afectan a varias pantallas)

| # | Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|---|
| T1 | **Colores fuera de los tokens.** Ámbar/naranja/verde/neutral de Tailwind escritos a mano en `group-detail` (tarjeta, chip y pestaña flash: `amber-400`, `neutral-950`), `group-detail.component.ts:242-244`, `history` (borde y chip flash), `create-question` (botón «Usar» flash: `amber-400/500`), `my-streak` y `group-card` (racha en peligro: `orange-500/700`) y `profile-page` (mensajes de guardado: `green-600`). Los colores de marco del perfil son hex fijos (`#8b5cf6`, …). Nada de esto cambia con los temas de la tienda: en `grape` la tarjeta flash sigue ámbar y los mensajes, verdes [25][27]. | alta | M | Crear tokens semánticos con su par claro/oscuro y su variante por tema: `--flash` / `--flash-foreground` (derivado de `sand` o de un ámbar del sistema), `--warning` (racha en peligro) y usar `success` para «guardado». Prohibir `*-amber-*`/`*-orange-*` en el lint de plantillas. |
| T2 | **Barras de resultados «semáforo».** `--chart-1..6` = verde, amarillo, rojo, morado, azul y lima. En una superlativa, el 3.º sale **rojo**, que se lee como «mal / error» [16][23]. Los temas tampoco redefinen `--chart-*`: en `grape` la barra ganadora sigue verde [27]. El historial y la página «Cómo funciona» también pintan cada tipo de pregunta de un color (amarillo, azul, rojo, verde y lima) [10][14]. | alta | M | Paleta de resultados derivada de la marca: el ganador en `primary`, el resto en tonos de `primary`/`sand`/`frost` con luminosidad decreciente, sin rojo. Redefinir `--chart-*` en cada `data-vp-theme`. Chips de tipo en un solo estilo neutro (`secondary`) con su glifo, en vez de un color por tipo. |
| T3 | **La jerarquía de botones cambia con el modo.** «Crear nuevo grupo» usa `bg-accent-text` y sale **sage en claro y lima en oscuro** [05]. «Sugerir pregunta» y el CTA de la landing hacen lo mismo con `primary`: en oscuro `--primary` es un sage claro casi lima (`oklch(0.8 0.1 145)`). La regla «lima = te toca, sage = secundario» desaparece en oscuro. | alta | S | Usar el mismo token en los dos modos (`bg-primary` para los CTA secundarios) y oscurecer el `--primary` oscuro, o desaturarlo, para que no compita con `--accent`. Documentar la regla en la spec §14. |
| T4 | **Objetivos táctiles pequeños.** El `hlmBtn` por defecto mide 36 px (`h-9`), `sm` 32 px, `icon-sm` 32 px y `xs`/`icon-xs` 24 px. Hay 83 usos de `sm`/`xs`/`icon-sm` en plantillas. Ejemplos: la barra superior del grupo (4 iconos de 36 px muy juntos) [06], los iconos de administrar/expulsar miembro [11] y el botón «Invitar» de las tarjetas [05]. | media | M | En móvil, mínimo 44 px de zona táctil (`size-11` o padding invisible con `after:absolute after:-inset-1`). Ajustar el `size.default` de `hlm-button` a `h-10` y revisar los `sm` de acciones principales. |
| T5 | **Tres patrones de cabecera de página.** (a) El grupo: flecha + título display italic + iconos [06]. (b) Historial: «← Volver» en texto + «Historial» en sans negrita [10]. (c) Ajustes: «← Volver» arriba + «Ajustes» como h1 display grande debajo [11]. Tienda, perfil, «Cómo funciona» y Novedades usan una cuarta variante: barra pegajosa con flecha + título display pequeño [31][13]. | media | S | Un único componente `PageHeader` (flecha `ghost icon` + título `font-display italic text-lg` + acciones a la derecha, pegajoso) para todas las subpáginas. |
| T6 | **Escala tipográfica y radios sin disciplina.** Hay tamaños arbitrarios (`text-[9px]`, `[10px]`, `[11px]`). Los títulos de sección alternan entre display italic («Objetos», «Marco»), sans semibold («Skins de anillo») y versalitas con tracking («INFORMACIÓN DEL GRUPO», «TUS GRUPOS») [31][32][11][05]. Radios: 46 usos de `rounded-xl/2xl/3xl/lg/md` frente a 16 de `rounded-default`, más píldoras `rounded-full` en opciones y botones. | media | M | Definir 3 niveles de título: página = display `text-2xl`, sección = display `text-lg`, eyebrow = versalitas `text-xs tracking-widest` solo para metadatos. Prohibir tamaños arbitrarios. Fijar dos radios (`rounded-default` para superficies, `rounded-full` para píldoras/chips) y documentarlos. |
| T7 | **Anillo de foco visible al abrir diálogos y cajones.** Al abrir el diálogo de seleccionar pregunta, el menú de usuario, el resumen mensual, crear grupo o el cajón de racha, el primer elemento (normalmente la ✕) aparece con el anillo lima de `focus-visible` aunque se haya abierto con el ratón o un toque [19][22][18][30][17]. Visualmente parece un estado «seleccionado». | media | S | Llevar el autofocus inicial al contenedor del diálogo (`tabindex=-1`) o al título, en lugar de a la ✕; o usar `cdkFocusInitial` en el campo principal (como ya hace «Crear grupo»). |
| T8 | **Accesorios de la tienda sobre avatares sin foto.** El arte del accesorio (p. ej., gafas de sol) tapa las iniciales: «marta» se ve como unas gafas sobre un círculo vacío [16][21]. En los mini-avatares de votantes (24 px) queda un borrón negro, y en oscuro el SVG casi no se ve [16 dark][27]. | alta | M | Con iniciales, escalar o desplazar el accesorio para que no tape el texto (o mostrarlo solo a partir de cierto tamaño, ≥ 40 px). En los mini-avatares, no mostrar accesorios. Añadir un contorno claro al arte en modo oscuro. |
| T9 | **El banner «Instala la app» tapa la acción principal.** En móvil es una tarjeta fija abajo que se pone encima de las opciones y del botón «Votar» [06b]. No hay ninguna regla de convivencia con el compositor del chat, que también va fijo abajo. | alta | S | No mostrarlo en el detalle del grupo mientras haya voto pendiente: mostrarlo en la lista de grupos o tras votar. O desplazar el contenido con `padding-bottom` cuando está visible. |
| T10 | **La spec no coincide con el código.** La §14 dice que los tokens están en `client/src/styles.css` y que la fuente de UI es `DM Sans`. En realidad viven en `styles-lime.css` y la fuente es **Quicksand** (`index.html`). | baja | S | Corregir la §14 dentro de la rama de la revisión visual. |
| T11 | **Mucho «shadcn por defecto».** Inputs grises planos, checkboxes nativos (`type="checkbox"` en `superlative-create`, `scale-create` y `custom-poll-create`, cuando Spartan tiene `hlm-checkbox`), selects nativos (`hlm-native-select` en el selector de tipo y los filtros), interruptor muy pálido en reposo [11] y botones deshabilitados lavados [03][06]. | media | M | Usar `hlm-checkbox` y `hlm-select`/`hlm-combobox` donde ya existen, y dar más contraste al interruptor apagado (`bg-input` → `bg-muted-foreground/30`). Estado deshabilitado del CTA: mantener el color y usar `opacity-60` + texto de ayuda («Elige una opción»), en lugar de un verde lavado. |
| T12 | **El tono de los textos se desvía en detalles.** El pie de página dice «Todos los derechos reservados» en la app y no en la landing [05][01]. El perfil pone «Email» y el registro «Correo electrónico» [13][04]. En la tienda aparece el anglicismo «Skins de anillo» [32]. La cuenta atrás de la demo usa «22h 25m 5s» y la del grupo «Quedan 4 h 59 m» [28][06]. | baja | S | Repasar `es.ts` (y en/de) con un glosario corto: correo, anillo/aro, formato de tiempo único (`4 h 59 min`), mismo pie en todas partes. |

---

## 2. Hallazgos por pantalla

### Landing — [01]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| En móvil, los puntos del logotipo («…») se van a una segunda línea debajo de «Preguntita» porque la cabecera se queda sin sitio con idioma + tema + «Entrar» [01-mobile][02][04]. | media | S | `whitespace-nowrap` en `LogoWordmarkComponent` y ocultar la etiqueta «ES» por debajo de `sm` (icono solo). |
| La barra de «Dani» del ejemplo es amarilla saturada y la de «Lucía», lima: los mismos colores del semáforo de T2. | media | S | Se corrige con T2 (`results-showcase.component.ts`). |
| En escritorio oscuro, los rayos del marco de «1 año» tapan la etiqueta «1 AÑO». | baja | S | Bajar el `z-index` del brillo o separar la etiqueta. |
| «Ya tengo cuenta» es un enlace de texto muy poco visible bajo dos botones; en escritorio queda descolgado a la izquierda. | baja | S | Moverlo a la cabecera («Entrar» ya existe) o darle `variant="link"`. |

### Demo — [02][28]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Reutiliza bien el flujo real. El contador «Siguiente en 22h 25m 5s» tiene otro formato (T12) y el chip verde no se parece al temporizador real (barra + «Quedan…»). | baja | S | Reutilizar `CountdownComponent`. |
| Las opciones de la superlativa son solo nombres, sin avatar (igual que en la app; ver «Votar»). | media | M | Ver «Detalle del grupo · votar». |

### Login / registro — [03][04]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| El título de la tarjeta («Inicia sesión», «Crea tu cuenta») va en sans negrita mientras toda la marca usa display italic. Login enseña una pregunta de reclamo y registro una encuesta: dos héroes distintos para la misma pantalla. | media | S | Título en `font-display italic` y el mismo héroe en las dos (o alternarlos a propósito). |
| El botón deshabilitado hasta rellenar el formulario sale verde lavado, con poco contraste [03]. | media | S | T11. |
| La sombra de la tarjeta es fuerte en claro (estilo plantilla) y no aparece en oscuro. | baja | S | Borde + `shadow-sm` coherente con el resto de tarjetas. |

### Lista de grupos — [05][05b][24][29][30]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| El chip naranja «No pierdas tu racha de 12 días» rompe la paleta y, en móvil, salta a 2 líneas y **cambia la altura de la tarjeta** [05-mobile]. Ya está en el backlog §16 («Calmer streak-danger hint»). | alta | S | Icono de racha con un pequeño indicador `--warning` junto al nombre, de tamaño fijo. |
| Los avatares de grupo son una letra sobre verdes de distinto tono; el teal de «P» se sale de la gama [05]. | baja | S | Tonos derivados de `primary`/`sand`/`frost` (las mismas cuatro familias de `GlyphComponent`). |
| El separador «o» entre «Crear nuevo grupo» y «Unirse a un grupo existente» flota sin líneas, y «Unirse» es un enlace subrayado: parece texto, no una acción equivalente [05]. | media | S | Dos botones: «Crear grupo» (primario) y «Unirme con código» (`outline`), o el mismo separador con líneas que «o elige una ya existente» del diálogo [19]. |
| Jerarquía cambiada en oscuro (T3). | alta | S | T3. |
| El estado vacío funciona bien (3 pasos + demo) [05b], pero no usa la primitiva `hlm-empty` de Spartan. | baja | S | Opcional: migrarlo a `hlm-empty` para unificarlo con otros vacíos. |

### Detalle del grupo · votar — [06][09][25]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| **Las opciones de la superlativa son píldoras de texto con el nombre de usuario**, sin avatar, anillo ni cosméticos. Es la pantalla más vista y la que más «formulario genérico» parece. | alta | M | Opción = avatar (con anillo) + nombre, con la opción elegida marcada en `accent`. Es el sitio natural para la personalidad (los cosméticos se ven donde importan). |
| La barra superior acumula racha + resumen + historial + ajustes (+ refrescar en escritorio) a 36 px cada uno, y el nombre del grupo se trunca pronto [06]. En escritorio se suma a la cabecera global y salen dos barras seguidas [08-desktop]. | media | M | Llevar historial y resumen a un menú «⋯» o a pestañas bajo el título; en escritorio fusionar las dos barras. |
| El chip de tipo («Superlativo») es amarillo (T2). | media | S | T2. |
| En escritorio, `.vp-question-text` pasa a `md:text-5xl` dentro de una columna `max-w-lg`: la pregunta ocupa 3–4 líneas enormes [08-desktop][16-desktop]. | media | S | Limitar a `md:text-4xl` o ensanchar la columna en `lg` (`max-w-2xl`). |
| El banner «Ya está el resumen del mes» compite con «Te toca elegir»: dos tarjetas verdes casi iguales seguidas [08][25]. | baja | S | El resumen como chip/punto en el icono de la barra (ya existe) y que el banner solo aparezca el primer día. |
| Botón «Votar» deshabilitado verde lavado (T11). | media | S | T11. |

### Detalle del grupo · resultados + adivinar autor — [16][27]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Barras semáforo (T2) y accesorios que tapan las iniciales (T8). | alta | M | T2, T8. |
| Los mini-avatares de votantes se pegan al final de la barra y se solapan; el porcentaje queda fuera, separado de su barra [16]. | media | S | Votantes en una fila bajo la barra (o un `+n`) y el porcentaje dentro de la barra cuando cabe. |
| En «¿Quién la escribió?», «Adivinar» deshabilitado + «Saltar y ver quién fue» como texto + «Pista · 20» en una segunda línea: tres pesos visuales sin orden claro. Los nombres largos se cortan («carla_l…») [16]. | media | S | Fila de acciones: «Adivinar» (accent, se activa al elegir), «Pista» (outline con moneda) y «Saltar» (link). Nombres a 2 líneas o tooltip. |
| Tras votar, la tarjeta salta sola a la flash pendiente [07]: el usuario no ve el resultado que acaba de desbloquear. | media | S | Quedarse en «Hoy» y mostrar el aviso «Te queda 1 pregunta por responder» (ya existe) [16]. |
| La tarjeta flash es ámbar, con sombra y borde de 2 px (T1). Es la única superficie con ese tratamiento y no sigue el tema [25]. | media | S | T1 (`--flash`). |

### Cajón de racha (my-streak) — [17]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| El número de la racha («12») va en sans negrita; es el momento más «trofeo» de la app y no usa la tipografía de marca. | baja | S | `font-display italic text-5xl` + el anillo del nivel actual alrededor. |
| El estado «en peligro» usa `orange-*` (T1). | media | S | T1 (`--warning`). |
| El CTA «Usar · tienes 2» abre con el anillo de foco lima (T7). | media | S | T7. |

### Diálogo «Elige la pregunta de mañana» — [19]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Buena jerarquía («Me vale esta» lima, «Crear pregunta nueva» sage). Pero cada pregunta propia lleva una papelera roja siempre visible: demasiado peso destructivo en una lista de elección. | media | S | Papelera en `ghost` gris (rojo solo al pasar el ratón) o borrar con swipe/menú «⋯». |
| Filtros con select nativo (T11); la «tarjeta» de pregunta usa píldora `rounded-3xl` en vez de `rounded-default`. | baja | S | T6, T11. |

### Crear pregunta — [20]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| El bloque flash «Usar · tienes 1» es ámbar fijo (`amber-400/500`): el caso citado en el encargo (T1). | media | S | `--flash` (T1). |
| Selector de tipo nativo y checkboxes nativos (T11). El *placeholder* («p. ej. ¿Quién acabaría antes en la cárcel?») se corta a 390 px. | media | S | `hlm-select` con glifo por tipo; *placeholder* más corto o `textarea` de 2 líneas. |
| El tipo de pregunta podría elegirse con tarjetas con glifo (ya existen los glifos por tipo) en lugar de un desplegable. | baja | M | Grid de 6 tarjetas con el glifo y una línea de ejemplo (personalidad). |

### Historial — [10][23]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Cabecera «← Volver · Historial» distinta del resto (T5). | media | S | T5. |
| Chips de tipo de cinco colores (T2); ✓/✗ de adivinanza en verde y rojo puros, sin `aria-label`. | media | S | Chips neutros; ✓/✗ con glifo y texto accesible. |
| El detalle se abre dentro de la lista con una pregunta en `text-3xl` que domina la pantalla [23]. | baja | S | Tamaño `text-2xl` en el detalle del historial. |

### Chat (debate) — [06][16]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Correcto y limpio. Los mensajes de sistema («alex ha votado») usan `sand` y se distinguen bien. Los nombres de los demás no usan el color de nombre comprado en la tienda (tampoco su avatar). | baja | M | Avatar pequeño + `vp-name--*` en los nombres del chat. Los cosméticos ganan valor. |
| El compositor fijo abajo convive con el banner de instalación (T9). | alta | S | T9. |

### Ficha de miembro — [21]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| El accesorio tapa la cara del avatar (T8). | alta | M | T8. |
| Rejilla de 7 estadísticas: la última queda sola en su fila. Todas pesan igual. | baja | S | 2 destacadas (racha actual, participación) en grande y el resto en una lista compacta; o un número par de celdas. |

### Ajustes del grupo — [11]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Las acciones por miembro son dos iconos sin texto (escudo = hacer admin, persona− = expulsar) de unos 32 px, juntos y sin pista visual de qué hacen [11]. | alta | S | Menú «⋯» por miembro con «Hacer administrador» / «Expulsar» con texto (`hlm-dropdown-menu`). |
| En el QR hay un círculo oscuro que tapa la esquina inferior derecha [11]. | media | S | Revisar el icono de «ampliar»: ponerlo fuera del QR o como chip bajo él (ya está «Toca para ampliar»). |
| Títulos de sección en versalitas mientras el perfil los pone en display italic (T6). Interruptor «Silenciar» casi invisible en reposo (T11). | media | S | T6, T11. |
| «Guardar cambios» va a la derecha y es pequeño, mientras el perfil usa un botón a todo el ancho. | baja | S | Mismo patrón de guardar en todas las páginas de formulario. |

### Resumen mensual — [18][34]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| **Fallo:** la cabecera dice «Septiembre **De** 2026». La clase `capitalize` (`monthly-summary-viewer.component.html:23` y `:178`) pone en mayúscula cada palabra. | media | S | Capitalizar solo la primera letra en TS (`label[0].toUpperCase() + …`) y quitar `capitalize`. |
| La portada de la historia es un glifo + título sobre el fondo desenfocado de la app, con mucho vacío: es la pantalla «wrapped» y debería ser la más expresiva [18]. | media | M | Fondo a sangre con el color del tema (gradiente `primary`→`accent`), número grande en display y confeti suave (ya existe `confetti.ts`). |

### Tienda — [12][26][31][32][33]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| **Página única de ~5 900 px en móvil** con más de 45 artículos [26]. Cuesta encontrar algo y volver arriba. | alta | M | `hlm-tabs` pegajosas: Objetos · Anillos · Accesorios · Nombre · Insignias · Títulos · Temas · Movimientos. |
| Las rarezas (Rara/Épica/Legendaria) son el mismo chip gris. Además, como no todas las tarjetas tienen chip, los botones «Comprar» quedan a distintas alturas [32]. | media | S | Color/borde por rareza (con tokens) y botón anclado abajo (`mt-auto`). |
| «Comprar» es sage sólido en Objetos y `secondary` gris en cosméticos: misma acción, dos estilos [31][32]. | media | S | Un estilo (outline con moneda); sólido solo para «Equipar». |
| Títulos de sección mezclados (T6). Anglicismo «Skins» (T12). | baja | S | T6, T12. |

### Perfil — [13][35]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Los colores de marco son el arcoíris de Tailwind en hex (morado, azul, verde, amarillo, naranja, rojo, rosa), fuera de la paleta y sin variante oscura. Ya está en el backlog («Adaptive frame-color palette»). | media | M | Paleta de claves con tokens claro/oscuro (lo que ya propone §16). |
| «Confirmar nueva contraseña» no tiene botón de ver, como los otros dos campos. «Email» vs. «Correo electrónico» (T12). | baja | S | Igualar los campos. |
| Mensajes de guardado en `green-600` (T1). | baja | S | `text-success`. |

### Menú de usuario — [22]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| En el grupo de botones Tema/Idioma, **la opción activa casi no se distingue**: en claro es un tinte gris muy suave y en oscuro no se ve [22-dark]. | alta | S | Opción activa con `bg-primary text-primary-foreground` (o un anillo `accent`), igual que los chips de título del perfil. |
| La ✕ abre con anillo de foco (T7). | media | S | T7. |

### «Cómo funciona» y Novedades — [14][15]
| Hallazgo | Sev. | Esf. | Propuesta |
|---|---|---|---|
| Novedades muestra todas las versiones desplegadas: ~13 000 px en móvil [15]. | media | S | Mostrar la última versión abierta y el resto plegadas (`hlm-accordion`), o «Ver anteriores». |
| «Cómo funciona» está bien estructurada, pero repite los chips de tipo de colores (T2). | baja | S | T2. |

---

## 3. Lo que ya funciona (y conviene conservar)

- La tipografía display italic en las preguntas y los títulos de diálogo tiene mucha personalidad: hay que llevarla a más sitios, no quitarla.
- `GlyphComponent` (azulejos sage/lima/arena/escarcha) y `CoinComponent` dan identidad, siguen los temas y quedan bien en oscuro.
- Los marcos de racha (piedra → amatista) y la corona son lo más «juego» de la app.
- El diálogo de selección y el cajón de racha tienen una jerarquía de botones correcta en claro.
- Los temas de la tienda recolorean bien `primary`/`accent`. Lo que no cambia con ellos son los colores escritos a mano (T1/T2).

---

## 4. Alcance propuesto para «Visual review: more personality» (spec §16)

La revisión es demasiado grande para una sola rama. Propongo **3 sub-ramas** cortadas de `master` (cada una con backend sin cambios, solo cliente + spec). La lógica: primero el sistema (tokens y primitivas), después las pantallas.

### Sub-rama A — `feat/visual-tokens` · «Sistema» (PATCH o MINOR)
**Entra:** T1 (tokens `--flash`, `--warning`, `success` y fuera los ámbar/naranja/verde a mano), T2 (paleta de resultados y de tipos derivada de la marca, `--chart-*` por tema), T3 (misma jerarquía de botones en claro y oscuro), T4 (botones de 44 px en móvil), T6 (escala de títulos y radios documentada), T7 (foco inicial de diálogos), T10 (corregir §14), T11 (checkbox, select e interruptor de Spartan; estado deshabilitado), T12 (glosario de textos).
**Por qué primero:** casi todos los hallazgos de pantalla se resuelven solos con esto, y fija las reglas para las ramas siguientes.
**Esfuerzo:** M–L.

### Sub-rama B — `feat/visual-core-loop` · «Personalidad donde se juega» (MINOR)
**Entra:** opciones de voto con avatar y anillo; barras de resultados (votantes, porcentaje); acciones de «¿Quién la escribió?»; quedarse en «Hoy» tras votar; accesorios legibles sobre iniciales (T8); `PageHeader` único (T5) y la barra superior del grupo; banner de instalación sin tapar el voto (T9); aviso de racha en peligro sin cambiar la altura de la tarjeta (ya en el backlog); número de racha en display; nombres y avatares con cosméticos en el chat; pregunta a `text-4xl` en escritorio.
**Esfuerzo:** L. Es el núcleo de «más personalidad».

### Sub-rama C — `feat/visual-secondary-screens` · «Pantallas secundarias» (PATCH/MINOR)
**Entra:** pestañas de la tienda, rarezas y botones alineados; menú «⋯» por miembro y QR en ajustes; portada del resumen mensual + arreglo de «Septiembre De»; Novedades plegadas; títulos y héroes de login/registro; logotipo sin saltos en la cabecera móvil; selector de tipo con tarjetas en «Crear pregunta»; ficha de miembro.
**Esfuerzo:** M–L. Se puede partir en C1 (tienda + resumen) y C2 (el resto) si se alarga.

### Fuera de alcance (se queda en el backlog)
- **Marca cuadrada/maskable** (ya está en §16 como pendiente: es trabajo de diseño gráfico, no de UI).
- **Paleta adaptativa de colores de marco** (requiere migración EF; ya está en §16 como ítem propio). Se puede retomar justo después de A, porque reutiliza sus tokens.
- **Punto de actividad no leída en las tarjetas** (§16; necesita backend).
- Nuevos temas de tienda, animaciones nuevas o rediseño de la landing más allá de los arreglos de arriba.
- Cualquier cambio de API o modelo de datos.

### Arreglos rápidos que no necesitan esperar a la rama
Se pueden sacar como PATCH sueltos si se prefiere: «Septiembre De» (resumen), opción activa invisible en Tema/Idioma, banner de instalación encima del botón «Votar», ícono oscuro sobre el QR.

### Decisiones que hay que confirmar antes de empezar
1. ¿Las barras de resultados deben seguir siendo multicolor (con colores de marca) o pasar a un único color con intensidades?
2. ¿Rojo prohibido fuera de errores y acciones destructivas? (Recomendado: sí.)
3. ¿`--primary` en oscuro se oscurece para separarlo del lima, aunque cambie el aspecto actual de la app en oscuro?
4. ¿Se acepta subir el botón por defecto de Spartan a 40 px (44 px de zona táctil) en toda la app?

Una vez confirmadas, se pasan a la spec §14/§16 para que no se vuelvan a preguntar.
