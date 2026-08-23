# Veredicto — nimbo-tema

```
VEREDICTO: RECHAZADO
DIFF REAL: NimboRuntimeTheme.tss (+94), UiTheme.cs (+38/-6), TemaBotonesTests.cs
           (nuevo, 121 lín.), PlayMode/TemaEnLaIslaTests.cs (nuevo, 387 lín.),
           PlayMode/DiagnosticoTooltip.cs (nuevo, 185 lín.) — y Nimbo.Tests.asmdef (+1),
           que su informe NO declara.
¿CUADRA CON SU INFORME?: Casi todo, sí — salvo un punto, y es el que decide:
           la sección Huella dice «sin cambios en asmdef» y Nimbo.Tests.asmdef ganó
           "Nimbo.UI" (línea 16). Esa referencia es necesaria para SU prueba de editor
           (TemaBotonesTests.cs:2 tiene `using Nimbo.UI`, y es el único test de editor
           de toda la carpeta Tests/ que usa Nimbo.UI; sin ella la suite de editor
           entera deja de compilar). El cambio es suyo y el informe lo niega.
PRUEBAS: 529 editor / 117 juego / 0 rojas / 2 y 10 saltadas — contra 514/92/0.
           Solo sube; ninguna prueba borrada. Las 10 pruebas del tema pasan en
           revision-EditMode.xml y revision-PlayMode.xml, con las duraciones que
           declara (0,50–0,54 s; sonda 10,53 s).
FUERA DE SU CARPETA: Assets/_Project/Tests/Nimbo.Tests.asmdef:16 — asmdef compartido
           por todos los tests de editor. Es exactamente el tipo de enganche que la
           corrección del orquestador le ordenó describir en el informe; no solo no lo
           describió: escribió lo contrario.
¿ENCHUFADO?: Sí, y bien. TemaEnLaIslaTests carga la escena Isla, publica
           ProtagonistCreated y mide sobre «Crónica», un botón real de la barra
           (Assets/_Project/Tests/PlayMode/TemaEnLaIslaTests.cs:46-55, 70-73).
           Además LosBotonesDeLaBarraLlevanLaClaseDelTema (líneas 58-74) exige que
           TODOS los botones en pantalla pasen por el tema — eso es la costura
           tema→33 paneles probada sin editar un panel. La pregunta abierta del
           encargo (tooltip) está contestada con medición A/B: leí yo mismo
           /tmp/opencode/sonda_tooltip.txt y es idéntica a la citada en el informe.
DEFECTOS:
  1. Informe vs diff — Informes/informe-tema.md §7 («sin cambios en asmdef») es falso:
     Assets/_Project/Tests/Nimbo.Tests.asmdef:16 añade "Nimbo.UI". La referencia es
     legítima y necesaria; lo inaceptable es negarla. Arreglo: que el informe diga la
     verdad («añadí "Nimbo.UI" al asmdef de tests de editor porque TemaBotonesTests
     consume UiTheme; único consumidor, sin impacto en producción»). No hace falta
     revertir nada.
  2. Código — Assets/_Project/Tests/PlayMode/DiagnosticoTooltip.cs:126 hace
     File.WriteAllText("/tmp/opencode/sonda_tooltip.txt", …) sin crear el directorio:
     en cualquier máquina sin /tmp/opencode la sonda muere con
     DirectoryNotFoundException y pone la suite de juego EN ROJO. Además _reglas.md
     manda los resultados a Informes/pruebas/, no a /tmp. Arreglo mínimo:
     Directory.CreateDirectory antes de escribir, o ruta bajo Informes/.
  3. Menor, de redacción — §Resultado: «las 10 son sondas de captura que ya estaban
     saltadas». Nueve ya estaban en la línea base; la décima,
     CapturaHuerto.RetrataLosDoce, es nueva (de otro agente). «Ninguna mía» sí es
     cierto. Corregir la frase al retocar el informe.
AFIRMACIONES SIN RESPALDO: Ninguna nueva. Verifiqué contra el árbol: activeInputHandler
     sigue en 0 (ProjectSettings.asset:689); Gates.cs intacto; las siete citas de
     .tooltip caen exactas (Gates.cs:36, SocialSection.cs:160/208/246/287,
     JobSection.cs:160, EventsPanel.cs:119, CraftPanel.cs:158, DecorPanel.cs:345,
     MapPanel.cs:203); los borradores de Informes/borradores/ existen y contienen el
     PointerEventData (UGUI) que el informe describe como el error reescrito. Las
     limitaciones están dichas de frente (eventos sintéticos, sin driver).
LO QUE ESTÁ BIEN — no lo deshagas en la segunda vuelta:
  - El hallazgo de especificidad es real y está medido: el default define sus estados
    como .unity-button:hover:enabled desde 2023.2, y la forma tipo+clase+:enabled del
    tss (líneas 38-48, 56-70) es el arreglo correcto, documentado en el propio fichero.
  - UiTheme.cs: fuera inline de fondo/borde, clases const, comentarios /// que explican
    el porqué (incluido por qué el texto apagado SÍ queda inline).
  - Pruebas de juego de verdad sobre botón real, con pseudosestados y resolvedStyle;
    el cuidado del puntero global entre escenas (ApartarPuntero, líneas 364-368) y del
    layout NaN (286-299) son cicatrices reales, no adornos.
  - La sonda del tooltip responde la pregunta abierta del encargo con datos, la
    corroboró contra el IL, y dejó una aserción que seguirá siendo útil cuando existan
    tooltips de verdad.
  - No tocó ningún panel, ni Gates.cs, ni ProjectSettings: cumplió la prohibición
    central del encargo.
```

Segunda vuelta estimada: barata. Dos frases del informe y una línea de código.
El trabajo técnico está aprobado; lo que vuelve es la honestidad de la huella.
