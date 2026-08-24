# Informe: memoria conversacional

Agente: ox-alpha · Carpeta asignada: `Assets/_Project/Scripts/Social/**` · Fecha: 2026-08-24

---

## 1. El hueco, medido

La vida social quedaba registrada y al hablar nadie te mencionaba nada:

- `NewsBoard` (`Scripts/Events/News/NewsBoard.cs:335-347`) escribe cada suceso social en
  `SaveGame.Chronicle` (`Scripts/Data/Save/SaveGame.cs:98`) con día y nombres puestos
  (`ChronicleEntry.cs:19-22`). Riñas, parejas, bodas, bebés, niveles y obras: todo queda.
- `SocialService.PlayerInteract` (`Scripts/Social/SocialService.cs`, método `PlayerInteract`)
  movía afinidad y cara, pero **no devolvía ni producía una sola palabra**.
- Los dos puntos donde hoy se habla con un vecino no enseñaban texto de ningún tipo:
  - `PlayerInteractor.MeetIslander` (`Scripts/Art/Player/PlayerInteractor.cs:958`) — solo
    `_social?.PlayerInteract(TargetId, SocialInteraction.Chat)` y una emoción.
  - `SocialSection.Gesture` (`Scripts/UI/Islander/SocialSection.cs:169-172`) — al charlar
    bien hace `Say("")`: borra el aviso. La conversación era muda por construcción.

## 2. Lo que he escrito yo (mi carpeta)

### 2.1 Nuevo: `Scripts/Social/Memory/ConversationRecall.cs`

**No inventa memoria nueva.** Lee la crónica que ya existe vía `IChronicleService`
(`ServiceRegistry.TryGet`, mismo camino que `PlayerAskFavour` usa para el gathering,
`SocialService.cs:549`) y decide qué es contable con tres filtros:

| Filtro | Regla | Dónde |
|---|---|---|
| Frescura | `age = hoy - entry.Day ≤ 3` (`FreshDays = 3`) | `TryPick` |
| Verdad presente | riña con `Conflict == None` ahora → no se cuenta; pareja con `Romance == Separated` → tampoco. Solo veta si la agenda dice lo contrario | `StillTrue` |
| Ya contado | bandera `recuerdo:{id}:{día}:{hash}` en `SaveGame.Flags`; más un tope de **una historia por vecino y día** (`recuerdo_dia:{id}:{día}`) | `MarkTold`/`MarkDay` |

Decisiones de diseño, con su porqué:

- **A veces, no siempre**: dado 0,20–0,40 según el eje Expresión (`ChanceFloor`/`ChanceCeiling`,
  públicos para que las pruebas lo claven). Justificación: la probabilidad solo manda en la
  primera charla del día (debajo hay tope diario); por encima de ~0,5 el vecino es un tablón
  de anuncios, por debajo de ~0,15 el sistema existe pero no se oye. Media: una de cada tres
  primeras charlas del día trae memoria.
- **Envejece**: ventana de 3 días. Lo de ayer está caliente; lo de la semana pasada ya lo sabe
  toda la isla. Además las banderas de «ya contado» se autolimpian cuando el suceso sale de la
  ventana (`CleanOldFlags`), así que el guardado no engorda para siempre.
- **Suena a esa persona — por ejes, no por tipos**: la voz sale del eje Expresión
  (`PersonalityProfile.Expression`): el reservado solo cuenta lo propio y con tono llano
  (`ReservedBank`); el expresivo cuenta además lo ajeno (cotilleo) con fanfarria
  (`GossipBank`). No son las 16 voces: son los ejes que separan *a quién le importa contar*.
  El texto del suceso va tal cual lo escribió la crónica —ya trae nombres y verbo— y el
  prefijo solo pone el tono, así que cambiar plantillas del tablón nunca rompe una frase.
- **Propio antes que ajeno, nuevo antes que viejo**: puntuación `(propio ? 100 : 0) - edad·10`.
- **Determinismo por día**: dado y prefijo salen de `Rng.FromSeed($"recuerdo…{id}:{día}")`,
  el patrón de `PlayerAskFavour` (`SocialService.cs:555`). Que hoy le apetezca contar es un
  hecho del día de ese vecino, no de cada clic.
- **Persistencia sin tocar el formato**: el «ya te lo contó» vive en `SaveGame.Flags`
  (`HasFlag/SetFlag`, `SaveGame.cs:130-135`), el mismo mecanismo que usó el Árbol Nimbo para
  su racha. Mismo pacto que triángulos y bodas: parámetro opcional, sin partida se lleva en
  memoria de sesión.

### 2.2 `Scripts/Social/SocialService.cs` (editado)

- Constructor: nuevo parámetro opcional `List<string> memoryFlags = null` tras `weddings`
  (línea ~62). Ninguna llamada existente se rompe.
- `_recall` creado en el constructor; expuesto como `Recall` y puerta única
  `RecallLine(islanderId)` (devuelve la frase o null).

## 3. Pruebas

`Tests/MemoriaConversacionalTests.cs` — 13 pruebas de editor, todas verdes:

- **Puede mencionar**: `UnVecinoPuedeMencionarUnaRinaDeAyer` fabrica «¡Bea y Mia han
  discutido!» de ayer y comprueba que Bea la saca en conversación con el texto del suceso;
  `UnaParejaNuevaTambienSeCuenta` lo mismo con una pareja nueva.
- **Deja de mencionarlo al envejecer**: `DejaDeMencionarloCuandoEnvejece` — a 3 días aún se
  cuenta, a 4 no. Es la prueba que evita el tablón eterno.
- No repite: ni el mismo día, ni al siguiente mientras siga fresco, ni tras «cerrar y abrir»
  (otra instancia con las mismas banderas).
- Una historia al día aunque haya dos sucesos frescos.
- Voz: la reservada no cuenta cotilleos ajeno (sí lo propio); el mismo suceso no sale igual
  en boca de la cotilla que de la interesada.
- Verdad presente: compuesta la riña (los dos lados a `None`, como hace `ClearRivalry`),
  deja de contarse.
- Dado: a 0 hay material pero no sale y no se gasta; estable dentro del día.
- Bordes: sin crónica registrada no revienta; integración completa a través de
  `SocialService.RecallLine` con banderas de partida.

Resultado del filtro: **13 pruebas · 13 pasadas · 0 fallos**
(`Informes/pruebas/memoria-EditMode.xml`).

## 4. Enganches que NO son míos (para el orquestador)

El recuerdo está construido y probado, pero **nadie lo llama desde la interfaz todavía**.
Dos enganches propuestos:

1. **Ficha (el importante)** — `Scripts/UI/Islander/SocialSection.cs:171-172`. Hoy:

   ```csharp
   if (!social.PlayerInteract(_islanderId, gesture.Interaction))
       Say("Por hoy ya está bien. Mañana más.");
   else
       Say("");
   ```

   Propuesto:

   ```csharp
   if (!social.PlayerInteract(_islanderId, gesture.Interaction))
       Say("Por hoy ya está bien. Mañana más.");
   else
       Say(social.RecallLine(_islanderId) ?? "");
   ```

   Requiere añadir a `ISocialService` (`Scripts/Core/Services/Contracts/ISocialService.cs`,
   junto a `PlayerRelationship`, línea ~98): `string RecallLine(string islanderId);` — la
   implementación ya existe en `SocialService.RecallLine`. `Nimbo.UI` no referencia
   `Nimbo.Social`, por eso la puerta tiene que ser el contrato de Core. El aviso de la ficha
   aguanta los refrescos de 0,4 s mientras no cambies de persona o de día
   (`SocialSection.cs:108-115`), así que la frase se lee sin que la pisen.

2. **Mundo (opcional)** — `Scripts/Art/Player/PlayerInteractor.cs:958`: tras
   `PlayerInteractor`'s `_social?.PlayerInteract(TargetId, SocialInteraction.Chat)` no hay
   canal de texto en el mundo (solo emociones, `_body.SetEmotion`). Si algún día hay burbujas
   de texto, ahí va la misma llamada. Hoy no lo propongo: la propia ficha documenta que «la
   vida social se lee abriendo el menú» (`SocialSection.cs:18-19`).

3. **Bootstrap (una línea)** — `Scripts/Game/Bootstrap/GameBootstrap.cs:245-246`: pasar
   `_save.Flags` como noveno argumento de `new SocialService(...)` para que el «ya te lo
   contó» sobreviva al guardado. Sin ella funciona todo, pero reabrir el juego podría repetir
   una historia fresca de la sesión anterior.

Con el enganche 1 aplicado, la prueba de costura que falta es de PlayMode siguiendo
`CronicaEnLaIslaTests.cs`: cargar `Isla`, fabricar suceso, abrir la ficha, pulsar Charlar y
comprobar que el aviso a veces trae la línea. No la escribo yo: tocaría UI, que no es mi
carpeta.

## 5. Errores ajenos vistos al compilar (anotados, no tocados)

- `CreatorMannequin.cs` (`UI/Creator`): 4 errores CS1061/CS0029 sobre `pickingMode` y
  `StyleColor`. Desaparecieron solos antes del segundo intento — otro agente los arreglaba
  mientras yo compilaba.
- `FiestaDecoradaEnLaIslaTests.cs` (`Tests/PlayMode`): faltaba `using UnityEngine.TestTools`
  (CS0246 en `UnityTest`, líneas 76/94/119/135). Bloqueó dos corridas completas; el dueño lo
  arregló a las 22:35 y después compiló limpio.

## 6. Estado de la línea base

Suite completa de editor lanzada al final para confirmar que mi cambio en `SocialService`
no rompe nada:

- Filtrada a lo mío (`-testFilter MemoriaConversacional`): **13 pruebas · 13 pasadas ·
  0 fallos**.
- Suite entera de editor: **588 pruebas · 586 pasadas · 0 FALLOS · 2 saltadas**
  (`Informes/pruebas/memoria-EditMode.xml`). La línea base viva era 514 editor con 2
  saltadas y cero en rojo; hoy hay 588 porque entre medias otros agentes han sumado sus
  suites — lo importante se mantiene: **cero en rojo**, las mismas 2 saltadas.
- No he corrido PlayMode: no toqué nada de escena ni de mundo. Mi único cambio con
  alcance es el parámetro opcional del constructor de `SocialService`, cubierto por las
  588 de editor.

El árbol queda sucio a propósito, sin commits: `ConversationRecall.cs` y
`MemoriaConversacionalTests.cs` nuevos (con sus `.meta` generados por Unity),
`SocialService.cs` e `informe-memoria.md` editados.
