# Pack content — working draft (rama `fix/question-pool-and-permissions`, §F)

> **Scratch/working file.** Durable record of questions approved one-by-one with the developer
> during the §F content pass, so nothing is lost if the chat context is compacted. Once all
> packs are finalized this is translated into `Data/PackSeeder.cs` and **this file is deleted**.
>
> Config legend: CustomPoll defaults to single-select (Min=Max=1) unless noted "multi" with a
> max. `AllowOther` = "Otro" free-text option. Superlative defaults `AllowNobody=false`.
> Scale is 1–10, `TargetUserId=null` (subjective topic, not a person). Deathmatch teams + Scale
> target are auto-resolved per group at clone time.

## Status

| # | Pack | State |
|---|---|---|
| 1 | Humor negro | ✅ closed — 14 |
| 3 | Vida nocturna y resaca | ⬜ pending |
| 4 | Dilemas (éticos, filosóficos y tontos) | ⬜ pending |
| 5 | Citas, relaciones y red flags | ✅ closed — 20 |
| 6 | Crimen y misterio | ⬜ pending |
| 7 | Famosos y celebridades | ⬜ pending |
| 8 | Comida | ⬜ pending (2 parked in) |
| 9 | Deportes y competencia | ⬜ pending |
| 10 | Polémicas y bandos | ✅ closed — 11 |
| 11 | Confesiones y vergüenzas | ✅ closed — 16 (CI-6 placed) |
| 12 | Nostalgia y cringe | ⬜ pending |
| 13 | Hipotéticos: ¿qué harías? | ✅ closed — 16 |
| 14 | El reparto del grupo | ⬜ pending |
| Base | Base pack (+~15 templates) | ⬜ pending |

First wave (per agreed scope): packs 1, 10, 13, 5, 11.

## Parking lot (approved, auto-placed — do NOT re-ask the developer)

- **La pregunta que rompe amistades: ¿piña en la pizza?** — Sí, y está buenísima / Es un crimen contra la humanidad (CustomPoll, 1) → place in Comida (8).
- **¿Un hotdog es un sándwich?** — Sí, pan + relleno = sándwich / Ni de coña (CustomPoll, 1) → place in Comida (8).
- _(Placed: cielo/infierno eternidad + reencarnación → Pack 13 Hipotéticos.)_

## Reserve ideas (developer-flagged, slot into the right pack when built)

- **Sudar mayonesa / llorar kétchup** (gross-out absurd) → Dilemas (4), the silly/gross subset.
- **(Superlative) ¿Quién tarda tres días en contestar un mensaje?** → place in Confesiones (11).

---

## Pack 1 — Humor negro ✅ (14)

Crudo, morboso, gallows humor; siempre en terreno hipotético/juego.

### CustomPoll (6)

1. **Te obligan a elegir cómo palmarla. ¿Qué prefieres?** — Quemado vivo · Enterrado vivo · Congelado poco a poco · Devorado por tiburones _(elige 1)_
2. **Por 1 millón de euros, ¿qué estarías dispuesto a hacer?** — Noche entera en una morgue · No volver a hablar con tu mejor amigo · Donar el meñique · Comer algo asqueroso a diario durante un año _(multi, max 4)_
3. **Apocalipsis y hay hambre. ¿Cuál cruzas antes?** — Comerte a tu mascota · Comerte a tu vecino _(elige 1)_
4. **En tu funeral, ¿qué prefieres provocar?** — Que todos lloren a mares · Que todos se partan de risa recordándote _(elige 1)_
5. **Te toca un superpoder maldito. ¿Cuál aguantas?** — Resucitar siempre, pero con un dolor atroz cada vez · Ser inmortal viendo morir a todos los que quieres _(elige 1)_
6. **¿Qué serías capaz de hacer con tal de no morir?** — Comer carne humana · Cortarte un brazo · Matar a un desconocido · Pasar el resto de tu vida en una celda _(multi, max 4)_

### Superlative (4)

7. **¿Quién recurriría al canibalismo primero si el avión se estrellara en los Andes?**
8. **¿Quién fingiría su propia muerte para librarse de un marrón?**
9. **¿A quién le darías un puñetazo por un millón de euros?**
10. **¿Quién se reiría sin poder parar en pleno funeral?**

### SecretPairing (2)

11. **¿Qué dos personas esconderían un cadáver juntas sin que les pillaran?**
12. **¿Qué dos personas se comerían la una a la otra primero en un naufragio?**

### Scale 1–10 (1)

13. **Del 1 al 10, ¿qué probabilidad hay de que acabes en el infierno?**

### Deathmatch (1)

14. **Os toca limpiar la escena de un crimen. ¿Qué equipo no deja ni una prueba?**

---

## Pack 10 — Polémicas y bandos ✅ (11)

Pique puro: temas intrascendentes que se defienden a muerte. Sin respuesta correcta. (Las
polémicas *de comida* van al pack 8 — ver parking lot.)

### CustomPoll (9)

1. **El papel higiénico se coloca…** — Con la hoja por delante · Con la hoja por detrás _(elige 1)_
2. **Marca TODAS las posturas que defenderías a muerte:** — El finde empieza el viernes · El verano es mejor que el invierno · El café sin azúcar está mejor · Madrugar el finde es de locos _(multi, max 4)_
3. **La peor de estas costumbres ajenas es:** — Hablar en el cine · Masticar con la boca abierta · Poner el manos libres en público · Dejar el carrito en mitad del súper _(elige 1, + "Otro")_
4. **Para vacacionar de verdad:** — Playa · Montaña _(elige 1)_
5. **Para ver una serie o peli extranjera:** — Versión original subtitulada · Doblada a tu idioma _(elige 1)_
6. **En una casa compartida, la tapa del váter:** — Siempre bajada · Da igual, se deja como esté _(elige 1)_
7. **A la hora de dormir:** — Con calcetines · Jamás con calcetines _(elige 1)_
8. **Nada más levantarte:** — Hago la cama siempre · ¿Para qué, si me vuelvo a meter? _(elige 1)_
9. **Mandar audios de 3 minutos por WhatsApp:** — Cómodo y normal · Una falta de respeto _(elige 1)_

### Superlative (2)

10. **¿Quién es más capaz de discutir una hora por una tontería sin dar su brazo a torcer?**
11. **¿Quién defiende las opiniones más impopulares solo por llevar la contraria?**

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
