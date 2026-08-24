# Veredicto — nimbo-ropa (segunda vuelta)

Verificador: nimbo-verificador · 2026-08-23 · Revisión de la corrección del
RECHAZADO anterior. El trabajo vive commiteado en `835cba2` (la primera vuelta nunca
se commiteó sola), así que esta revisión es contra los ficheros actuales punto por
punto contra mi veredicto previo, más las suites frescas `arbolhook2-EditMode.xml`
y `retomo-PlayMode.xml`.

```
VEREDICTO: APROBADO

DIFF REAL:
  Segunda vuelta (sobre lo ya revisado):
    OutfitLook.cs:69-77        caso "hat" reescrito (+5 netas): HasGarment=false,
                               color solo en PieceColor, comentario con el porqué
    Tests/RopaDiariaTests.cs   nuevo, 116 líneas, 3 pruebas + .meta
    Tests/RopaLookTests.cs     +1 prueba (UnSombreroNoTineLaRopaDelCuerpo, :83-104)
    Tests/PlayMode/RopaEnLaIslaTests.cs   ampliada UnSombreroCuelgaUnaPiezaNuevaDelCuerpo
                                          (:143-177)
  Fuera de carpeta: sin cambios esta vuelta; acumulado = los tres ya declarados
  (IEconomyService.cs, InventoryTests.cs, Nimbo.PlayTests.asmdef).

¿CUADRA CON SU INFORME?: Sí, salvo una cifra de recuento optimista: dice «añade
  116 líneas entre cuatro ficheros», pero RopaDiariaTests.cs tiene 116 líneas él
  solo; sumando la prueba nueva de RopaLookTests (~23), la ampliación de juego
  (~35) y el caso "hat" (~5), la vuelta ronda las 180. Nada oculto ni inventado:
  todo lo declarado existe y es exactamente lo que hay. Anotado como descuido,
  no como defecto bloqueante.

PRUEBAS: Suite fresca del orquestador — EditMode 563 / 561 pasadas / 0 rojas /
  2 saltadas (base 514 ✓); PlayMode 133 / 123 / 0 rojas / 10 saltadas (base 92 ✓;
  hasta la roja ajena del Árbol ya está verde). Las 16 pruebas de ropa están en
  las suites frescas y pasan una a una: 9 de RopaLookTests + 3 de RopaDiariaTests
  en arbolhook2, 4 de RopaEnLaIslaTests en retomo. La diferencia entre su corrida
  (569) y la fresca (563) NO es ropa: es ArbolNimboTests 9→3, consolidación de
  nimbo-arbol entre las 12:42 y las 13:06. Sus XML propios (569/567/0/2 y
  133/122/1/10) cuadran al dígito, y la única roja de su PlayMode era
  ArbolEnLaIslaTests.DelanteDelArbolElCartelOfreceHablarle —ajena—, tal como
  declaró.

FUERA DE SU CARPETA: Los tres ya conocidos y declarados, sin cambios esta vuelta.
  Verificado además el motivo del defecto 3: ArbolEnLaIslaTests.cs:7 hace
  `using Nimbo.Island` y el asmdef lleva overrideReferences:true, así que quitar
  la referencia rompería la compilación de toda la suite de juego. Declararla era
  la opción correcta de las dos que di.

DEFECTOS CORREGIDOS (verificados uno a uno):
  1. Sombrero que tiñe el cuerpo → CORREGIDO. OutfitLook.cs:69-77 devuelve
     HasGarment=false con el color en PieceColor. Coherente en los dos puntos de
     pintado: IslanderView.cs:77-79 y :126 usan `look.HasGarment ? look.Garment :
     OutfitColor(...)`, así que quien lleva gorra conserva su ropa de siempre
     (s∈[0.35,0.62], v∈[0.78,0.96] en IslanderView.cs:101 — imposible que
     coincida con el #3366AA de la gorra, s=0.70 v=0.667: la aserción no puede
     pasar por casualidad). La pieza sigue colgando porque ChibiMeshBuilder.cs:254
     conmuta por look.Piece, no por HasGarment, y el guardia de pantalón (:135-137)
     sigue blindando el cuerpo. Fijado dos veces: editor (RopaLookTests.cs:83-104,
     gorra Y gorro, pieza intacta, color viajando en PieceColor) y juego
     (RopaEnLaIslaTests.cs:143-177: deja al vecino SIN prenda, equipa
     cloth_gorra_nimbo_clasica por Receive —el camino real—, exige pieza colgada
     y _BaseColor de «ropa» ≠ azul). Con el código anterior esa aserción sale roja.
  2. OnDayPassed sin certificado → CORREGIDO. RopaDiariaTests.cs publica DayPassed
     de verdad contra el manejador real: cambio con ≥2 prendas en 60 días y nunca
     fuera del armario (:66), guardia de una sola prenda (:87, cubre
     WardrobeService.cs:72), y Dispose desengancha del calendario (:100, mismo
     vecino y mismos días que la primera, así que la diferencia es el Dispose y no
     suerte). Determinista por la semilla {id}|cambio-ropa|{día} que yo mismo vi
     en WardrobeService.cs:75. Las tres verdes en la suite fresca.
  3. Referencia Nimbo.Island → DECLARADA CON MOTIVO, y el motivo es cierto (ver
     FUERA DE SU CARPETA). Admite además que su informe anterior la contó mal.

¿ENCHUFADO?: Sí. La mitad principal (prenda visible en el muñeco) sigue certificada
  por RopaEnLaIslaTests sobre la escena Isla con malla real, y la mitad que faltaba
  (cambio diario) ahora tiene certificado propio a nivel de evento real. Las cuatro
  pruebas de juego de ropa pasan en retomo-PlayMode.xml (20:52, la más fresca).

¿ROMPIÓ LO QUE ESTABA BIEN?: No. Comprobado en el árbol actual: IEconomyService.cs
  :84-90 conserva Slot/Style/Palette; InventoryTests.cs:59-61 el FakeItemDef con las
  tres propiedades; WardrobeService mantiene suscripción, semilla y Dispose;
  IslanderView conserva SwapMesh (:123/:125/:140) y OnDestroy (:252-261) soltando las
  tres mallas y la cara. Ninguna línea de lo aprobado fue retocada salvo el caso
  "hat" que yo mismo mandé cambiar.

AFIRMACIONES SIN RESPALDO: Ninguna nueva. Verifiqué al dígito: cifras de los cuatro
  XML, nombre de la roja ajena, HSV de la gorra (#3366AA → s=0.70, v=0.667, exacto),
  using Nimbo.Island en ArbolEnLaIslaTests.cs:7, y las 12+4 pruebas de ropa verdes
  en las suites frescas. Única imprecisión: el recuento de líneas del informe (arriba).

LO QUE ESTÁ BIEN (que no lo deshaga la siguiente vuelta):
  - La corrección es mínima: un caso de switch y cinco líneas de comentario, sin
    banderas ni abstracciones nuevas para un fix de una línea de comportamiento.
  - Fijó el defecto por DOS caminos independientes (contrato puro en editor +
    escena Isla en juego), que es lo que evita que una refactorización futura
    vuelva a teñir el cuerpo sin que nadie lo delate.
  - La prueba de Dispose compara contra sí misma (mismo vecino, mismos días que la
    prueba viva): diseño que elimina la explicación cómoda de «sería suerte».
  - Honestidad de informe: declara el error de recuento anterior, mantiene el
    PENDIENTE del Dispose sin llamador en GameBootstrap.OnDestroy (:567-583) para
    el orquestador en vez de tocar el fichero, y no reclama ninguna mejora visual
    que no pueda medir.
```

Pendiente para el orquestador (sin cambios): llamar `WardrobeService.Dispose()`
desde `GameBootstrap.OnDestroy`; cada descarga de escena deja hoy una suscripción
viva al calendario.
