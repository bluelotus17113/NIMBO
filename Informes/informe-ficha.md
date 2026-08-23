# Informe — ficha de habitante (nimbo-ficha)

**Carpeta:** `Assets/_Project/Scripts/UI/Islander/**`
**Verificación:** `Informes/pruebas/ficha-PlayMode.xml` → **7 pruebas · 7 pasadas · 0 fallos · 0 saltadas** (filtro `Nimbo.PlayTests.FichaEnLaIslaTests`, escena `Isla` real).
**Árbol entero, medido en las pasadas completas de los otros agentes sobre el mismo código:** EditMode 529 pruebas (falda 04:16: 526 ✓ / 1 ✗), PlayMode 117 (tiendas 04:17 y falda 04:22). **Ningún fallo es de mi carpeta**; detalle en «El árbol ahora mismo».

---

## Cómo empezó esta pasada

El reinicio dejó el trabajo de la pasada anterior escrito pero sin verificar. `git status`/`git diff` de mi carpeta mostraban los tres arreglos ya codificados (`IslanderPanel`, `SocialSection`, `JobSection`, `HomeSection` modificados; `IslandSocialCard`, `SocialLabels`, `TastesSection`, `AssemblyInfo` nuevos con sus `.meta`) más las 7 pruebas PlayMode. Lo que faltaba era justo lo que no pude hacer antes: compilar y medir.

**Costura ajena que bloqueaba todo:** el árbol no compilaba por `Assets/_Project/Scripts/Art/World/IslandMeshBuilder.cs:84` — error CS0127, un `return Build(...)` dentro del nuevo `FillSurface`, que es `void`. Era la cola sin borrar de una extracción a medias (`BuildSurface` ya hace ese `return` en la línea 26 tras llamar a `FillSurface`). No es mi fichero: borré esa única línea porque sin ella nadie de este proyecto compila nada, y lo dejo señalado para quien lleve la falda.

## Fallo 1 — La ficha no cabía y lo que se perdía eran las relaciones

Antes: seis tarjetas apiladas directo en un `VisualElement` de 380 px, sin scroll — el único panel de lista del juego sin él. Estimado 1.150–1.400 px contra ~780 útiles a 1080p, y lo que caía por abajo era la tarjeta «Con quién anda».

Ahora: todo cuelga de un `ScrollView` (`IslanderPanel.cs:63-65`, `flexGrow = 1`; tarjetas añadidas a `_scroll` en `:92, :98, :104-107, :113, :117`). Mismo patrón que Craft:55, Board:56, Chronicle:53 y el resto.

## Fallo 2 — El refresco borraba todo lo que decía

Causa raíz, fuera de mi carpeta: `UiRoot.cs:29` fija `_refreshInterval = 0.4f` y `UiRoot.cs:681` llama a `_panel.Refresh()` en cada ciclo. El arreglo va en mi lado, como sugería el encargo: **`Refresh` ya no destruye lo que no cambió**. `UiRoot.cs` no se ha tocado y no hace falta tocarlo.

- **`SocialSection`**: los botones se levantan solo si cambia la firma — etapa de romance, veredictos de cortejo/petición de mano y las cuatro puertas (`SocialSection.cs:121-127`, early return en `:92`). El aviso («Por hoy ya está bien», «Te ha traído X», «Ya está dicho») solo se borra al cambiar de persona o de día (`:94-98`); `Say()` es `internal` (`:115`) para que la prueba clave el contrato sin simular clics.
- **Peticiones** (`IslanderPanel.cs:191-224`): una pasada lee, otra pinta; entre medias compara la firma (id, kind, prioridad, recompensa, línea, demanda y opciones de cada petición). `OpenFor` itera `_open` en orden de inserción (`RequestService.cs`), así que la firma no aletea.
- **Relaciones** (`IslanderPanel.cs:231-246`): misma técnica.
- **Trabajo** (`JobSection.cs:80-108`): firma con puerta, catálogo, oficio actual y veredicto/afinidad por oficio.
- **Casa** (`HomeSection.cs:93-105`): la firma incluye **lo que te falta**, así que recoger madera con la ficha abierta actualiza la fila aunque el veredicto no cambie.

Las barras y los textos siguen tocándose en cada pasada: escribir texto no derriba nada bajo el cursor.

## Fallo 3 — No había forma de ver quién anda con quién

Nuevo `IslandSocialCard.cs`, enganchado al final de la ficha (`IslanderPanel.cs:117`): **«Quién anda con quién»**, el estado de hoy de un vistazo — parejas, prometidos, casados, flechazos y riñas abiertas.

- **No calcula nada.** Lee las agendas que ya escribe el servicio social (`Collect`, `IslandSocialCard.cs:108-158`) y nombra cada estado con `SocialLabels`. Ni una fórmula de compatibilidad: aquí solo se mira lo que hoy es verdad.
- Un flechazo es de uno hacia otro y sale direccional (`A → B`, clave `a>b`, `:134-138`); lo demás es estado de pareja o de bronca, de dos, con clave ordenada (`PairKey`, `:160-161`) para no contar dos veces. Mismo criterio de deduplicación que el tablón de noticias.
- Orden de lectura con `SocialLabels.RankFor`: bodas arriba, riñas debajo (`SocialLabels.cs:71-86`).
- Vacío con voz: «Todavía nadie anda con nadie. La isla acaba de empezar.» (`:67-72`).

De paso, las etiquetas vivían duplicables dentro de `IslanderPanel`; ahora son `SocialLabels.StatusOf/StatusColor`, única fuente para ficha y mapa (`SocialLabels.cs:21-60`). Y se corrigió un agujero que la extracción destapó: un recién declarado (`RomanceStage.Confessed`) caía al bloque de amistad y salía como «se conocen»; ahora dice «pendiente de respuesta» (`SocialLabels.cs:27-29`).

## Lo de regalar — cabía

Nuevo `TastesSection.cs` («Qué le gusta»), justo detrás de «Qué le dices» (`IslanderPanel.cs:105`): nombra `LovedFoods`/`HatedFoods` vía `ItemNames.Of` (`TastesSection.cs:62-84`). Los gustos son estables a propósito, así que la frase se construye una vez por habitante (`:51`). Si el vecino no tiene gustos escritos, el vacío habla: «Nadie ha apuntado todavía qué le gusta. Regálale algo y mira la cara que pone.» — el sistema deja de ser invisible sin destrozar el juego de descubrirlo.

## Las pruebas

`Assets/_Project/Tests/PlayMode/FichaEnLaIslaTests.cs` — 7 pruebas contra la escena `Isla` de verdad (modelo `CronicaEnLaIslaTests`), todas en verde:

1. `LaFichaLlevaScroll_PorqueMideMasQueLaPantalla` — hay `ScrollView` en la ficha.
2. `LasSieteTarjetasDeLaFichaEstanDondeSeLeen` — las siete tarjetas existen, incluidas las dos nuevas.
3. `ElMapaSocialNombraParejasYRinas` — sembrada una pareja y una riña en los datos, el mapa las nombra; el flechazo direccional también.
4. `ElAvisoDeUnGestoSobreviveALosRefrescos` — tres `Refresh` seguidos no borran el aviso; cambiar de persona, sí.
5. `LosBotonesNoSeDerribanSiNoCambiaNada` — mismas instancias de `Button` tras dos refrescos, en la sección y en la ficha entera con peticiones incluidas.
6. `LosGustosDelVecinoSalenEnSuFicha` — «Le encanta…» o el vacío con voz.
7. `CadaEstadoTieneUnaPalabra` — contrato de etiquetas, incluido el Confessed que antes salía «se conocen».

Para clavar el contrato del aviso sin clics: `AssemblyInfo.cs` abre solo `SocialSection.Say` a `Nimbo.PlayTests`.

## El árbol ahora mismo (medido, no supuesto)

| Suite | Fuente | Total | ✓ | ✗ | ⊘ |
|---|---|---|---|---|---|
| PlayMode filtrado mío | `ficha-PlayMode.xml` 04:13 | 7 | 7 | 0 | 0 |
| EditMode completo | `falda-EditMode.xml` 04:16 | 529 | 526 | 1 | 2 |
| PlayMode completo | `falda-PlayMode.xml` 04:22 | 117 | 100 | 6 | 10 |

Línea base viva (514 editor + 92 juego) **superada**: 529 y 117 con mis +7. Los fallos que hay, todos de otros encargos en curso con su agente activo encima:

- `SiluetaDeCultivosTests` ×1-2 (EditMode) — nimbo-cultivos.
- `TemaEnLaIslaTests` ×3 (PlayMode) — nimbo-tema; en la pasada de tiendas aún fallaban además `TiendasEnLaIslaTests` ×2 y en la de falda `BootTests.LaCamaraEmpiezaEncuadrandoAlProtagonista` ×1, y cada pasada posterior los iba dejando verdes: están iterando.

## Para el orquestador

1. **`IslandMeshBuilder.cs:84`** — toqué un fichero fuera de mi carpeta: borré el `return Build(...)` sobrante dentro de `FillSurface` (CS0127 bloqueaba la compilación de todo el proyecto tras el reinicio). Revisarlo quien lleve la falda.
2. **`UiRoot.cs` no necesita cambios.** El arreglo del refresco vive en las secciones; `UiRoot.cs:681` puede seguir llamando a `Refresh()` cada 0,4 s.
3. **Seam con nimbo-tema:** `HomeSection.cs:132` y `JobSection.cs:172` ponen `backgroundColor` inline sobre botones de `UiTheme.Action`. Según las observaciones de UiTheme.cs, el inline gana a las pseudoclases y mata `:hover`/`:active` de esos dos botones. Es su dominio; yo no toco UiTheme ni sus decisiones.
4. **Unity sin XML en sesión fría:** la primera pasada tras el reinicio hizo import completo (`CompileScripts: 862s` en `ficha-EditMode.log`) y salió «successfully» sin correr pruebas ni escribir XML; pasó igual en el segundo intento. Con la Library caliente, la misma herramienta sí escribe XML (todas las pasadas de 03:22 en adelante). Además, una orden mía fue cortada por fuera dejando un Unity huérfano que terminó sin XML — no hubo lockfile huérfano: el proceso siguió vivo y sostuvo el lock hasta acabar.
