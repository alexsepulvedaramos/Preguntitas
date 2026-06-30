# Pack content — working draft (rama `fix/question-pool-and-permissions`, §F)

> **Scratch/working file.** Durable record of questions approved one-by-one with the developer
> during the §F content pass, so nothing is lost if the chat context is compacted. Once all
> packs are finalized this is translated into `Data/PackSeeder.cs` and **this file is deleted**.
>
> Config legend: CustomPoll defaults to single-select (Min=Max=1) unless noted "multi" with a
> max. `AllowOther` = "Otro" free-text option. Superlative defaults `AllowNobody=false`.
> Scale is 1–10, `TargetUserId=null` (subjective topic, not a person). Deathmatch teams + Scale
> target are auto-resolved per group at clone time.
>
> Style: prefer playful/cheeky Spanish (lenguaje picaresco) whenever it keeps the meaning equally
> clear — e.g. "que haga mu" (raw) / "como la suela de un zapato" (overcooked) instead of
> "cruda/muy hecha". Applies to wave-2+ content; closed wave-1 packs can get a picaresque polish
> pass at seeder-writing time.

## Status

| # | Pack | State |
|---|---|---|
| 1 | Humor negro | ✅ closed — 16 |
| 2 | Supervivencia y apocalipsis | ✅ closed — 15 |
| 3 | Vida nocturna y resaca | ✅ closed — 16 |
| 4 | Dilemas (filosóficos y éticos) | ✅ closed — 16 |
| 5 | Citas, relaciones y red flags | ✅ closed — 20 |
| 6 | Crimen y misterio | ✅ closed — 15 |
| 7 | Famosos y celebridades | ❌ dropped (sin sentido, no se construye) |
| 8 | Comida | ✅ closed — 17 |
| 9 | Deportes y competencia | ✅ closed — 18 |
| 10 | Polémicas y bandos | ✅ closed — 16 |
| 11 | Confesiones y vergüenzas | ✅ closed — 16 (CI-6 placed) |
| 12 | Nostalgia y cringe | ✅ closed — 15 |
| 13 | Hipotéticos: ¿qué harías? | ✅ closed — 16 |
| 14 | El reparto del grupo | 🔄 in progress |
| 15 | Guarradas y dilemas asquerosos | ✅ closed — 15 |
| Base | Base pack (+~15 templates) | ⬜ pending |

First wave (per agreed scope): packs 1, 10, 13, 5, 11.

## Parking lot (approved, auto-placed — do NOT re-ask the developer)

- **La pregunta que rompe amistades: ¿piña en la pizza?** — Sí, y está buenísima / Es un crimen contra la humanidad (CustomPoll, 1) → place in Comida (8).
- **¿Un hotdog es un sándwich?** — Sí, pan + relleno = sándwich / Ni de coña (CustomPoll, 1) → place in Comida (8).
- _(Placed: cielo/infierno eternidad + reencarnación → Pack 13 Hipotéticos.)_

## Reserve ideas (developer-flagged, slot into the right pack when built)

- _(Placed: sudar mayonesa & gross-out absurds → new Pack 15; CI-6 → Confesiones.)_

---

## Pack 1 — Humor negro ✅ (16)

Crudo, morboso, gallows humor; siempre en terreno hipotético/juego.

### CustomPoll (7)

1. **Te obligan a elegir cómo palmarla. ¿Qué prefieres?** — Quemado vivo · Enterrado vivo · Congelado poco a poco · Devorado por tiburones _(elige 1)_
2. **Por 1 millón de euros, ¿qué estarías dispuesto a hacer?** — Noche entera en una morgue · No volver a hablar con tu mejor amigo · Donar el meñique · Comer algo asqueroso a diario durante un año _(multi, max 4)_
3. **Apocalipsis y hay hambre. ¿Cuál cruzas antes?** — Comerte a tu mascota · Comerte a tu vecino _(elige 1)_
4. **En tu funeral, ¿qué prefieres provocar?** — Que todos lloren a mares · Que todos se partan de risa recordándote _(elige 1)_
5. **Te toca un superpoder maldito. ¿Cuál aguantas?** — Resucitar siempre, pero con un dolor atroz cada vez · Ser inmortal viendo morir a todos los que quieres _(elige 1)_
6. **¿Qué serías capaz de hacer con tal de no morir?** — Comer carne humana · Cortarte un brazo · Matar a un desconocido · Pasar el resto de tu vida en una celda _(multi, max 4)_
7. **Si pudieras presenciar tu propia muerte como mero espectador (sin dolor), ¿lo harías?** — Sí · No _(elige 1)_

### Superlative (5)

8. **¿Quién recurriría al canibalismo primero si el avión se estrellara en los Andes?**
9. **¿Quién fingiría su propia muerte para librarse de un marrón?**
10. **¿A quién le darías un puñetazo por un millón de euros?**
11. **¿Quién se reiría sin poder parar en pleno funeral?**
12. **¿Quién sobreviviría más tiempo en *La Purga*?**

### SecretPairing (2)

13. **¿Qué dos personas esconderían un cadáver juntas sin que les pillaran?**
14. **¿Qué dos personas se comerían la una a la otra primero en un naufragio?**

### Scale 1–10 (1)

15. **Del 1 al 10, ¿qué probabilidad hay de que acabes en el infierno?**

### Deathmatch (1)

16. **Os toca limpiar la escena de un crimen. ¿Qué equipo no deja ni una prueba?**

---

## Pack 10 — Polémicas y bandos ✅ (16)

Pique puro: temas intrascendentes que se defienden a muerte. Sin respuesta correcta. (Las
polémicas *de comida* van al pack 8 — ver parking lot.)

### CustomPoll (13)

1. **El papel higiénico se coloca…** — Con la hoja por delante · Con la hoja por detrás _(elige 1)_
2. **Marca TODAS las posturas que defenderías a muerte:** — El finde empieza el viernes · El verano es mejor que el invierno · El café sin azúcar está mejor · Madrugar el finde es de locos _(multi, max 4)_
3. **La peor de estas costumbres ajenas es:** — Hablar en el cine · Masticar con la boca abierta · Poner el manos libres en público · Dejar el carrito en mitad del súper _(elige 1, + "Otro")_
4. **Para vacacionar de verdad:** — Playa · Montaña _(elige 1)_
5. **Para ver una serie o peli extranjera:** — Versión original subtitulada · Doblada a tu idioma _(elige 1)_
6. **En una casa compartida, la tapa del váter:** — Siempre bajada · Da igual, se deja como esté _(elige 1)_
7. **A la hora de dormir:** — Con calcetines · Jamás con calcetines _(elige 1)_
8. **Nada más levantarte:** — Hago la cama siempre · ¿Para qué, si me vuelvo a meter? _(elige 1)_
9. **Mandar audios de 3 minutos por WhatsApp:** — Cómodo y normal · Una falta de respeto _(elige 1)_
10. **En el avión, reclinar tu asiento hacia atrás:** — Para eso está, es tu derecho · Es de egoístas _(elige 1)_
11. **Sandalias con calcetines:** — Comodidad ante todo · Crimen de moda imperdonable _(elige 1)_
12. **Llegar a una fiesta en casa de alguien:** — A la hora exacta que dijeron · 15-30 min tarde es lo correcto _(elige 1)_
13. **El doble check azul de WhatsApp:** — Activado, no escondo nada · Desactivado, mi vida es mía _(elige 1)_
14. **¿Cada cuánto lavas la toalla de baño?** — Aguanta semanas, total te secas limpio · Cada pocos usos, por higiene _(elige 1)_

### Superlative (2)

15. **¿Quién es más capaz de discutir una hora por una tontería sin dar su brazo a torcer?**
16. **¿Quién defiende las opiniones más impopulares solo por llevar la contraria?**

---

## Pack 13 — Hipotéticos: ¿qué harías? ✅ (16)

Superpoderes, golpes de dinero, trueques de vida. Mayoría CustomPoll binario (would-you-rather).

### CustomPoll (13)

1. **El superpoder definitivo:** — Volar · Ser invisible
2. **Si pudieras elegir un poder:** — Teletransportarte a cualquier sitio · Viajar en el tiempo
3. **¿Cómo prefieres el dinero?** — 1 millón de golpe ahora mismo · 4.000 € cada mes el resto de tu vida
4. **Trato de paciencia:** — 1 millón mañana · 10 millones dentro de 10 años
5. **Tu vida laboral ideal:** — Forrado haciendo algo que odias · Justo a fin de mes amando lo que haces
6. **¿Qué vida prefieres? (sano y en forma todo el tiempo)** — Una sola vida de 800 años · 8 vidas de 100 años
7. **Te dan un mando para tu vida. ¿Cuál?** — Botón de pausa · Botón de rebobinar
8. **Te toca la eternidad en uno. ¿Cuál eliges?** — Un cielo perfecto pero aburridísimo para siempre · Un infierno entretenido pero con dolor constante
9. **Te reencarnas en lo que peor te caiga. ¿Qué prefieres ser?** — Cucaracha indestructible · Mosquito que vive un día molestando · Pez con 3 s de memoria
10. **¿Qué prefieres tener?** — Más tiempo libre · Más dinero
11. **Pacto con el universo:** — Cancelar todas tus deudas hoy · Duplicar tu sueldo para siempre
12. **Una llamada imposible:** — 1 minuto con tu yo del pasado · 1 minuto con tu yo del futuro
13. **Si tu cuerpo no necesitara una cosa:** — No volver a dormir nunca · No volver a comer nunca

### Superlative (3)

14. **¿Quién usaría sus superpoderes para el mal sin pensárselo?**
15. **¿Quién se gastaría un millón de euros en menos de un mes?**
16. **¿Quién malgastaría un deseo mágico en una absoluta chorrada?**

---

## Pack 5 — Citas, relaciones y red flags ✅ (20)

Sin asumir "tu pareja actual" (no funciona en grupo). SecretPairing + Superlative de conducta de
ligue + CustomPoll de red/green flags universales.

### SecretPairing (7)

1. **¿Qué dos personas del grupo harían la mejor pareja?**
2. **¿Qué dos personas acabarían liándose en una boda?**
3. **¿Qué dos personas serían la pareja más tóxica?**
4. **¿Qué dos personas ligan fatal por separado pero juntas serían perfectas?**
5. **¿Qué dos personas seguro que se han mandado un mensajito subido de tono alguna vez?**
6. **¿Qué dos personas discutirían a diario pero no podrían vivir la una sin la otra?**
7. **¿Qué dos personas tendrían el romance de verano más intenso?**

### Superlative (6)

8. **¿Quién volvería con su ex por enésima vez jurando que esta vez sí?**
9. **¿Quién tiene el peor gusto eligiendo con quién sale?**
10. **¿Quién stalkea más a su crush en redes?**
11. **¿Quién ligaría más en una noche de fiesta?**
12. **¿Quién se pillaría a las dos semanas de conocer a alguien?**
13. **¿Quién sería el más celoso en una relación?**

### CustomPoll (7)

14. **La red flag más imperdonable en una primera cita:** — Llega 40 min tarde sin avisar · Habla solo de su ex · Es borde con el camarero · No suelta el móvil _(elige 1, + "Otro")_
15. **¿Qué prefieres?** — Una mala cita y que no pase de ahí · Tres citas geniales y que acabe en ghosting
16. **¿Quién crea tu próximo perfil de citas?** — Tu madre · Tu ex
17. **¿Con quién sales?** — Alguien que odia a los animales · Alguien con siete perros y que los antepone a todo
18. **¿Qué duele más?** — Que te dejen por un mensaje · Que te hagan ghosting sin explicación
19. **El green flag definitivo:** — Trata bien al camarero · Se lleva genial con sus amigos · Contesta rápido los mensajes · Le va bien estando solo _(elige 1, + "Otro")_
20. **¿Qué prefieres?** — Dejar tú · Que te dejen a ti

---

## Pack 11 — Confesiones y vergüenzas ✅ (16)

Guilty pleasures suaves, manías, cringe. Nada turbio. ("Yo nunca" = CustomPoll de 2 opciones Sí/No.)

### Superlative (6)

1. **¿Quién tarda tres días en contestar un mensaje?** _(heredada de Citas)_
2. **¿Quién ha llorado con un anuncio de la tele?**
3. **¿Quién se hace el ocupado para no saludar a alguien por la calle?**
4. **¿Quién relee sus propios mensajes riéndose de lo gracioso que es?**
5. **¿Quién finge haber leído libros o visto pelis que no ha tocado?**
6. **¿Quién tiene el historial de Spotify más vergonzoso?**

### CustomPoll — "Yo nunca" (4)

7. **Yo nunca he cantado a grito pelado pensando que no me oía nadie** — Sí / No
8. **Yo nunca he olido la ropa para decidir si ponérmela otra vez** — Sí / No
9. **Yo nunca he respondido "tú también" a un "que aproveche" o "feliz cumpleaños"** — Sí / No
10. **Yo nunca he fingido estar enfermo para librarme de un plan** — Sí / No

### CustomPoll (4)

11. **Marca tus placeres culpables:** — Realities malísimos · Canciones de Abraham Mateo o Justin Bieber · Cotillear perfiles a las 3am · Comer de pie sobre el fregadero _(multi)_
12. **Marca lo que has hecho alguna vez:** — Mirar el móvil 30 min en el baño · Aplicar la regla de los 5 segundos · Fingir una llamada para escapar de alguien · Hablar solo en voz alta _(multi)_
13. **Confesión rápida: en los planes eres…** — El que siempre llega tarde · El que nunca contesta · El que desaparece sin avisar · El que cancela en el último momento _(elige 1, + "Otro")_
14. **¿Qué te da MÁS vergüenza que te pillen haciendo?** — Hablando solo · Bailando frente al espejo · Llorando con una peli · Cantando en la ducha _(elige 1)_

### Scale (1)

15. **Del 1 al 10, ¿cómo de cotilla eres en realidad?**

### OpenText (1)

16. **Confiesa tu placer culpable más vergonzoso, sin filtro.**

---

## Pack 8 — Comida ✅ (17)

Debates gastronómicos y manías de comer. (CO-1/CO-2 heredadas del parking de Polémicas. No
duplicar la tortilla con/sin cebolla — ya está en Base.) Estilo picaresco.

### CustomPoll (11)

1. **¿Piña en la pizza?** — Sí, y está buenísima · Un crimen contra la humanidad
2. **¿Un hotdog es un sándwich?** — Sí, pan + relleno = sándwich · Ni de coña
3. **¿Desayunar pizza fría del día anterior?** — Manjar de campeones · Aberración
4. **Si solo pudieras comer un sabor el resto de tu vida:** — Dulce · Salado
5. **¿Hay que terminar siempre todo el plato?** — Sí, no se tira la comida · Paro cuando estoy lleno
6. **El cereal con leche:** — Primero leche, luego cereal · Primero cereal, luego leche
7. **Marca lo que SÍ le pondrías a una pizza aunque escandalice:** — Piña · Huevo · Patatas fritas · Kebab _(multi)_
8. **¿Tu condimento de cabecera para echarle a todo?** — Kétchup · Mostaza · Mayonesa _(elige 1, + "Otro")_
9. **¿Kétchup en la tortilla de patatas?** — Está de vicio · Es un atentado
10. **¿Cómo pides el chuletón?** — Que casi haga "mu" · En su punto, jugoso · Hecho pero tierno · Como la suela de un zapato
11. **Marca los crímenes culinarios que has cometido:** — Kétchup a la paella · Pedir la carne como una suela · Partir los espaguetis antes de cocer · Pizza con cuchillo y tenedor _(multi, + "Otro")_

### Superlative (4)

12. **¿Quién come más rápido, casi sin masticar?**
13. **¿Quién es el más tiquismiquis para comer?**
14. **¿Quién te robaría comida del plato sin pedir permiso?**
15. **¿Quién mezcla comidas raras que dan grima?**

### SecretPairing (1)

16. **¿Qué dos personas se pelearían por el último trozo de pizza?**

### Scale (1)

17. **Del 1 al 10, ¿cómo de tiquismiquis eres comiendo?**

---

## Pack 3 — Vida nocturna y resaca ✅ (16)

Fiesta, borracheras, resacas, caos de madrugada. Estilo picaresco, mucho señalar al fiestero.

### Superlative (7)

1. **¿Quién es el primero en caer redondo de borracho?**
2. **¿Quién acaba bailando encima de una mesa?**
3. **¿Quién hace la "bomba de humo" y desaparece sin despedirse?**
4. **¿Quién manda los mensajes más vergonzosos a las 4 de la mañana?**
5. **¿Quién propone "la última" y acaban siendo las 7?**
6. **¿Quién se pone más moñas y cariñoso cuando bebe?**
7. **¿Quién tiene resacas de tres días y jura no beber nunca más?**

### SecretPairing (1)

8. **¿Qué dos personas se irían de after hasta ver el amanecer?**

### CustomPoll (6)

9. **La cura milagrosa de la resaca:** — Dormir hasta las 3 de la tarde · Comida grasienta de rey · Seguir bebiendo (el clavo) · Sufrir en silencio _(elige 1, + "Otro")_
10. **¿Qué resaca prefieres?** — La física, el cuerpo hecho fosfatina · La moral, la vergüenza de lo que hiciste
11. **A las 6 de la mañana eres:** — El que grita "¡una más!" · El que ya está pidiendo el taxi
12. **Plan de viernes ideal:** — Salir hasta reventar · Sofá, manta y peli
13. **La bebida que nunca te falla:** — Cerveza · Vino · Combinados · Chupitos _(elige 1, + "Otro")_
14. **Te despiertas con resacón y 20 mensajes sin leer. ¿Qué haces?** — Leerlos de golpe y asumir el daño · Apagar el móvil y vivir en negación

### Scale (1)

15. **Del 1 al 10, ¿cómo de fiestero eres en realidad?**

### Deathmatch (1)

16. **Maratón de fiesta hasta el amanecer: ¿qué equipo aguanta en pie sin caer?**

---

## Pack 2 — Supervivencia y apocalipsis ✅ (15)

Isla desierta, naufragio, zombieland. No duplicar lo de Base ("abrazar a un zombie",
"sobrevivir perdidos", "reconstrucción social"). Cada pregunta autoexplicativa. Picaresco.

### Deathmatch (1)

1. **Naufragio: ¿qué equipo monta una balsa que de verdad flote?**

### Superlative (6)

2. **¿Quién sería el primero en palmarla en un apocalipsis zombie?**
3. **¿Quién se autoproclamaría líder sin tener ni idea?**
4. **¿Quién entraría en pánico nada más empezar el caos?**
5. **¿Quién se quedaría dormido en su turno de guardia?**
6. **En un grupo de supervivientes a un apocalipsis, ¿quién sería el más inútil cuando llegara el caos?**
7. **¿Quién se vendría arriba e intentaría una heroicidad que los pondría a todos en peligro?**

### SecretPairing (2)

8. **¿Qué dos personas montarían la alianza más letal para sobrevivir?**
9. **Tras el fin del mundo, ¿qué dos personas serían la última esperanza para repoblar la Tierra?**

### CustomPoll (6)

10. **Fin del mundo en 24h, ¿qué haces?** — Fiesta sin freno · Con la familia · Saquear tiendas · Dormir tranquilo _(elige 1, + "Otro")_
11. **Marca lo que NO aguantarías sin:** — Café · Móvil · Ducha caliente · Tu serie favorita _(multi)_
12. **Solo puedes coger un arma cuerpo a cuerpo para el apocalipsis zombie. ¿Cuál?** — Bate de béisbol · Katana · Machete · Sartén de hierro
13. **Montas tu kit de supervivencia y solo cabe una cosa más en la mochila. ¿Cuál metes?** — Comida para una semana · Un buen cuchillo · Un botiquín · Un mechero
14. **En un apocalipsis, ¿qué tipo de superviviente serías?** — El que acapara recursos · El que ayuda a todos · El llanero solitario · El que se esconde y reza
15. **Si el mundo se fuera a acabar, ¿qué final prefieres?** — Meteorito instantáneo · Invasión zombie · Pandemia lenta · Guerra nuclear _(+ "Otro")_

---

## Pack 4 — Dilemas (filosóficos y éticos) ✅ (16)

Solo dilemas con peso, estilo "dilema del tren", sin respuesta correcta. NO guarradas (esas
van al pack 15). Todo autoexplicativo. CustomPoll.

### CustomPoll (16)

- **(CustomPoll, 1)** Te dan mal el cambio a tu favor (10€ de más) y te das cuenta. ¿Lo dices? — Sí, siempre lo devuelvo · Depende: a una gran empresa me callo, a una tienda pequeña lo digo · Me callo y a ganar, sea quien sea
- **(CustomPoll, 1)** Puedes salvar a 5 desconocidos o a 1 ser querido. ¿A quién salvas? — A los 5 desconocidos · A tu ser querido
- **(CustomPoll, 1)** ¿Mentirías a un amigo para no hacerle daño? — Sí, mentira piadosa · No, la verdad por delante
- **(CustomPoll, 1)** ¿El fin justifica los medios? — Sí, si el resultado es bueno · No, nunca
- **(CustomPoll, 1)** ¿Qué vida prefieres? — Corta pero intensa y que deje huella · Larga y tranquila sin pena ni gloria
- **(CustomPoll, 1)** ¿Saber qué hay tras la muerte pero sin poder contarlo, o no saberlo jamás? — Saberlo en silencio · No saberlo nunca
- **(CustomPoll, 1)** ¿Feliz viviendo engañado o infeliz pero sabiendo toda la verdad? — Feliz pero engañado · Infeliz pero consciente
- **(CustomPoll, 1)** Un tren va a atropellar a 5 personas. Puedes desviarlo a otra vía donde solo hay 1. ¿Tiras de la palanca? — Sí, desvío (muere 1, salvo 5) · No intervengo (mueren 5)
- **(CustomPoll, 1)** Para parar ese tren y salvar a 5, tendrías que empujar tú a un desconocido a la vía. ¿Lo empujas? — Sí · No
- **(CustomPoll, 1)** Te ofrecen enchufarte para siempre a una máquina que simula una vida perfecta, sin que sepas que es mentira. ¿Entras? — Sí, dame la vida perfecta · No, prefiero la realidad aunque duela
- **(CustomPoll, 1)** Si cambias una a una todas las piezas de un barco, ¿sigue siendo el mismo barco? — Sí, es el mismo · No, ya es otro
- **(CustomPoll, 1)** Viajas al pasado y tienes delante a Hitler de bebé. ¿Lo matas? — Sí · No
- **(CustomPoll, 1)** ¿El bien y el mal son universales o los decide cada cultura? — Universales · Relativos a cada cultura
- **(CustomPoll, 1)** ¿El ser humano es bueno por naturaleza o egoísta por naturaleza? — Bueno por naturaleza · Egoísta por naturaleza
- **(CustomPoll, 1)** Al juzgar un acto, ¿qué importa más? — La intención · El resultado
- **(CustomPoll, 1)** La paradoja de la tolerancia: ¿hay que tolerar a los intolerantes? — Sí, a todos · No, a los intolerantes no

## Pack 15 — Guarradas y dilemas asquerosos ✅ (15)

"Qué prefieres" guarros/absurdos, ambas opciones igual de asquerosas. Picaresco.

### CustomPoll (15)

- **(CustomPoll, 1)** ¿Qué prefieres? — Sudar mayonesa · Llorar kétchup
- **(CustomPoll, 1)** ¿Qué lames? — El suelo del metro · El pasamanos de una escalera mecánica
- **(CustomPoll, 1)** ¿Qué prefieres tener? — Dedos de salchicha · Pelo de espagueti
- **(CustomPoll, 1)** ¿Beberte de un trago un vaso de aceite o uno de vinagre? — Aceite · Vinagre
- **(CustomPoll, 1)** ¿Qué prefieres que te hagan encima? — Que te caguen · Que te vomiten
- **(CustomPoll, 1)** ¿Con qué cargas para siempre? — Mal aliento permanente · Sudar a chorros sin parar
- **(CustomPoll, 1)** ¿Qué te tragas? — Una uña del pie · Un mechón de pelo
- **(CustomPoll, 1)** ¿Qué bocata te comes? — De uñas · De pelos
- **(CustomPoll, 1)** ¿Qué bebes? — El agua de fregar los platos · El agua de una bañera ajena
- **(CustomPoll, 1)** ¿Qué prefieres tener siempre? — La piel pegajosa · Mocos colgando
- **(CustomPoll, 1)** ¿Beber leche caducada en grumos o comer huevos podridos? — Leche en grumos · Huevos podridos
- **(CustomPoll, 1)** ¿Limpiarte con papel higiénico ya usado o no usar nada? — Papel usado · Nada
- **(CustomPoll, 1)** ¿Que se te meta una cucaracha en la oreja o una araña por la nariz? — Cucaracha en la oreja · Araña por la nariz
- **(CustomPoll, 1)** ¿Comer un plato de mocos o beber un vaso de babas? — Plato de mocos · Vaso de babas
- **(CustomPoll, 1)** ¿Encontrarte siempre un pelo en cada plato o una uña de vez en cuando? — Un pelo siempre · Una uña a veces

---

## Pack 6 — Crimen y misterio ✅ (15)

Crimen perfecto, sospechosos, bandas, misterios. No duplicar Base ("atraco perfecto",
"poli bueno/malo"). Picaresco, autoexplicativo.

### Superlative (7)

1. **¿Quién cometería el crimen perfecto y no lo pillarían jamás?**
2. **¿Quién confesaría entre lágrimas al primer interrogatorio?**
3. **¿Quién sería el cerebro de una banda criminal?**
4. **¿Quién acabaría detenido por la tontería más absurda?**
5. **¿Quién parece el más inocente pero esconde el lado más oscuro?**
6. **¿A quién acusarían primero si apareciera un cadáver en una cena del grupo?**
7. **¿Quién se libraría de una multa hablándole bien al policía?**

### SecretPairing (2)

8. **¿Qué dos personas formarían el dúo criminal más temido?**
9. **¿Qué dos personas se delatarían mutuamente en cuanto les apretaran un poco?**

### CustomPoll (3)

10. **¿Qué clase de criminal serías?** — El cerebro · El músculo · El de los contactos · El que conduce y poco más
11. **Si tuvieras que cometer un crimen y salir impune, ¿cuál?** — Atraco a mano armada a un banco · Hackeo millonario desde el sofá · Robo de una obra de arte en un museo · Una estafa piramidal de las gordas
12. **¿Qué gran misterio te gustaría resolver de verdad?** — Quién mató a JFK · Qué hay en el Triángulo de las Bermudas · Si estamos solos en el universo · Qué pasó de verdad con el avión MH370

### Scale (1)

13. **Del 1 al 10, ¿cómo de bien mientes bajo presión?**

### OpenText (2)

14. **Confiesa: ¿qué es lo más caro que has mangado alguna vez y de dónde?**
15. **En teoría, eh... ¿cuál sería la mejor manera de deshacerte de un cadáver?**

---

## Pack 9 — Deportes y competencia ✅ (18)

Mucho Deathmatch de equipos + señalar al competitivo. No duplicar Base (tira y afloja,
batalla de gallos, mus, karaoke). Picaresco.

### Deathmatch (4)

1. **Partido de fútbol a muerte: ¿qué equipo gana?**
2. **Gymkana imposible por la ciudad: ¿qué equipo llega primero a la meta?**
3. **Concurso de cultura general: ¿qué equipo sabe más?**
4. **Búsqueda del tesoro: ¿qué equipo lo encuentra antes?**

### Superlative (6)

5. **¿Quién es el peor perdedor del grupo?**
6. **¿Quién hace trampas en cuanto te despistas?**
7. **¿Quién se pone competitivo hasta en el Parchís?**
8. **¿Quién celebra una victoria como si hubiera ganado el Mundial?**
9. **¿Quién culpa siempre al árbitro o a la mala suerte cuando pierde?**
10. **¿Quién es el más patoso para cualquier deporte?**

### SecretPairing (2)

11. **¿Qué dos personas formarían la pareja imbatible en un torneo de pádel?**
12. **¿Qué dos personas acabarían enfadadas por un simple partido amistoso?**

### CustomPoll (3)

13. **¿Prefieres ganar haciendo trampas o perder limpiamente?** — Ganar tramposo · Perder limpio
14. **¿Qué prefieres ser?** — El mejor de un equipo malísimo · El peor de un equipo campeón
15. **En un juego de mesa, ¿qué eres?** — El estratega · El tramposo · El que se enfada · El que va a su bola

### Scale (1)

16. **Del 1 al 10, ¿cómo de competitivo eres en realidad?**

### OpenText (2)

17. **Cuéntanos tu mayor momento de gloria deportiva.**
18. **Cuéntanos tu momento más patético haciendo deporte.**

---

## Pack 12 — Nostalgia y cringe ✅ (15)

Fases vergonzosas, referencias generacionales, infancia. Nido de OpenText. Picaresco.

### Superlative (5)

1. **¿Quién tuvo la fase más vergonzosa en la adolescencia?**
2. **¿Quién era el típico empollón del colegio?**
3. **¿Quién era el más gamberro de clase?**
4. **¿Quién tenía el corte de pelo más cuestionable de joven?**
5. **¿Quién ha pegado el mayor cambio (glow up) con los años?**

### CustomPoll (5)

6. **¿Qué época era mejor?** — Cuando no había móviles · Ahora con todo a un clic
7. **Si pudieras volver a una etapa, ¿a cuál?** — La infancia sin preocupaciones · La adolescencia rebelde · Los años locos de fiesta · Ninguna, mejor el presente
8. **¿Qué reliquia tecnológica echas de menos?** — La videoconsola de tu infancia · El móvil de tapa y teclas · Los CDs y casetes · El MSN Messenger _(+ "Otro")_
9. **¿Qué recuerdas con más cariño de pequeño?** — Los dibujos del sábado por la mañana · Jugar en la calle hasta que anochecía · Las meriendas en casa de un amigo · Los veranos que no acababan nunca
10. **Marca tu(s) tribu(s) de adolescente:** — Pijo · Friki · Rebelde/macarra · Hippie/alternativo · Skater · El normal que pasaba desapercibido _(multi)_

### SecretPairing (1)

11. **¿Qué dos personas habrían sido los pesados de la clase juntos?**

### Scale (1)

12. **Del 1 al 10, ¿cómo de cringe era tu yo adolescente?**

### OpenText (3)

13. **Cuéntanos tu momento más cringe de la adolescencia.**
14. **¿Cuál era tu grupo o canción favorita de joven que ahora te da vergüenza?**
15. **¿Cuál era tu sueño de pequeño que ahora te da ternura (o risa)?**

---

## Pack 14 — El reparto del grupo 🔄 (in progress — 18 confirmed, + female chars pending)

Todo Superlative: "¿quién sería [personaje]?". Personajes icónicos eternos + arquetipos
narrativos. (El tipo "reparto" agrupado = rama futura, ver §13/§16 del spec.)

### Confirmed (18) — todos Superlative

1. ¿Quién del grupo sería el Joker (puro caos)?
2. ¿Quién sería Batman (oscuro y va por libre)?
3. ¿Quién sería Homer Simpson (un desastre adorable)?
4. ¿Quién sería Gandalf (el sabio que guía a todos)?
5. ¿Quién sería Yoda (suelta perlas de sabiduría rara)?
6. ¿Quién sería James Bond (elegante y ligón)?
7. ¿Quién sería Sherlock Holmes (lo deduce todo)?
8. ¿Quién sería Mr. Bean (el desastre silencioso)?
9. ¿Quién sería el Grinch (odia la diversión)?
10. ¿Quién sería Peter Pan (se niega a madurar)?
11. ¿Quién sería el héroe que salva el día?
12. ¿Quién sería el villano de la película?
13. ¿Quién sería el alivio cómico del grupo?
14. ¿Quién sería el que muere en el primer capítulo?
15. ¿Quién sería el villano secreto que nadie ve venir?
16. ¿Quién sería el protagonista absoluto de la serie?
17. ¿Quién sería el secundario que se roba todas las escenas?
18. ¿Quién sería el cerebro malvado detrás de todo?
