# Informe — nimbo-rasdesuelo (segunda vuelta)

*23/08/2026. Primera entrega rechazada (`Informes/veredicto-rasdesuelo.md`); esta vuelta
corrige SOLO los dos defectos listados y no toca nada de lo aprobado.*

## Qué corregí

### Defecto 2 — el superlativo falso (el código sí, esta vez)

`CapturaEstilo.cs:39-51` decía que el centro del encuadre `flores_a_ras` era «el pico
más alto del campo de ruido que siembra las flores». Es falso. Antes de tocar el
comentario repliqué yo mismo la medida del verificador — script en
`Informes/rasdesuelo-replica-ruido.py`, float32 operación a operación sobre
`ValueNoise.cs:24-62`, escala 11 como `Meadow.cs:442`, offset cero como
`WorldView.cs:170-172`—:

| medida | mi réplica | veredicto |
|---|---|---|
| pico global del campo | 0.9907 en (−65.25, 0.00) | ≈0.991 en (−65.3, 0.0) ✓ |
| valor en el centro elegido (54.5, −11.5) | 0.9738 | 0.974 ✓ |
| círculo 5 m del elegido ≥ 0.70 / media | 86.9 % / 0.849 | 86.9 % / 0.849 ✓ |
| círculo 5 m del pico ≥ 0.70 / media | 76.3 % / 0.790 | — |

Con la medida delante, el comentario nuevo dice la verdad completa: el sitio elegido **no**
es el pico —ese cae en la pradera oeste, a ~19,3 m de Residencial B
(`IslandLayout.cs:48-50`: centro (−52, −14), radio 14; √(13.25²+14²))— pero su vecindad
es **más espesa** que la del pico (87 % frente al 76 % del círculo de 5 m por encima del
umbral): el pico es un máximo puntual, no el mejor macizo.

**No reapunté la cámara al pico verdadero**, que era la otra opción del veredicto: mover
el encuadre invalidaría los PNG ya renderizados y aprobaría una foto nueva sin haberla
visto nadie. El cambio es solo el comentario; la posición, intacta.

### Defecto 1 — la escena (nada que hacer por mí)

El veredicto asigna al orquestador ratificar `Assets/_Project/Scenes/Isla.unity:21-22`
(`m_LinearFogStart` 60→30, `m_LinearFogEnd` 280→260) y me prohíbe volver a tocarla. No la
he tocado: `git diff HEAD` sobre la escena sigue siendo exactamente esas dos líneas, ni
una más. Queda descrito aquí para el orquestador, que decide.

## Qué NO toqué (aprobado por el veredicto, «no deshacer»)

- El rango de inclinación 55–85° de las flores (`Meadow.cs:326`) y su comentario nuevo.
- La niebla 30/260 en C# (`IslandLighting.cs:70-71`) y su compromiso documentado.
- Densidad y tamaño de flor: sin cambios, como advirtió el encargo (overdraw sigue sin
  medirse; sigo sin subir nada).

## Pruebas

`Tools/agentes/unity.sh PlayMode rasdesuelo` (suite completa, sin filtro):
**132 pruebas · 121 pasadas · 1 fallo · 10 saltadas.** La única roja es
`DelanteDelArbolElCartelOfreceHablarle`, declarada ajena en el propio veredicto
(«enganche pendiente del orquestador»). Mismos números que la corrida ola34 que el
verificador dio por buenas.

No corrí EditMode esta vuelta: mi delta desde el estado verificado son comentarios en un
fichero del ensamblado de juego (`Nimbo.PlayTests`) y este informe. La corrida de juego
compila el proyecto entero antes de ejecutar —si hubiera roto compilación, no habría XML—,
y un comentario no puede cambiar el resultado de ninguna prueba de editor: los 565/563/0/2
de ola34 siguen siendo el número de editor.

## Capturas que hay que mirar (`Capturas/`, sufijo `_estilo` = 11:32 de hoy)

La captura `flores_a_ras` se renderizó ANTES de este arreglo de comentario; el arreglo no
cambia geometría ni cámara, así que el PNG sigue siendo evidencia válida.

1. **`estilo_flores_a_ras_estilo.png`** — la nueva de esta entrega. A 12,5 m y casi
   horizontal (desde (44, 1.34, −9.5) hacia (56, 0.23, −13)): las rosetas deben enseñar
   la cara llena, no un hilo; si se vieran de canto, el rango 55–85° está mal. Detrás,
   ~45 m de prado cayendo hacia la orilla este: la bruma debe empezar a destiñirlo.
   Gradiente medido ahora sobre este PNG con método declarado (tercios de la imagen
   1280×720, media de B−R por banda): **tercio inferior −35.5** (prado cálido),
   **franja media +8.3**, **tercio superior +51.7** (cielo azul). Sustituye a las cifras
   sin banda declarada de la primera entrega, que el verificador no pudo reproducir.
2. **`estilo_panorama_estilo.png` contra `estilo_panorama_antes.png`** — la isla entera
   desde el sureste alto: la de después debe salir con más bruma en el centro (~47 %
   frente al 36 % de destiñe a 139 m). El verificador reprodujo mis B−R exactos:
   19,8 → 24,2.
3. **`estilo_prado_estilo.png`** — a ras por el prado: la hierba llena el encuadre hasta
   arriba y el borde lejano debe perder nitidez por la bruma (antes llegaba nítido).
4. **`estilo_tercera_persona_estilo.png`** — lo que ve el jugador de verdad: flores de
   lado legibles en el cuadro medio y bruma en el fondo, sin tapar la referencia.
5. Las otras cuatro (`arbol`, `linde`, `bajo_el_borde`, `tras_el_arbol`) están intactas;
   `tras_el_arbol` es del agente de cámara, no mía.

## Cabos para el orquestador

- **Ratificar la escena** (defecto 1): las dos líneas de niebla en
  `Assets/_Project/Scenes/Isla.unity:21-22`. Sin ellas el cambio de C# no llega al juego:
  nadie llama `IslandLighting.Apply` en runtime —solo `Editor/SceneBuilder.cs:182` y
  `Tests/PlayMode/Estudio.cs:47`, medido por el verificador— así que los RenderSettings
  serializados SON el camino de entrega.
- Si la niebla quisiera fuente única en C#, la llamada a `IslandLighting.ApplyHaze()` en
  el arranque de `WorldView` es de quien tenga ese fichero (no es el mío).
- Pendiente de antes: overdraw del prado a 5 m sin medir; mi estimación de ~500–700
  flores visibles en el encuadre sigue siendo estimación, no medida.
