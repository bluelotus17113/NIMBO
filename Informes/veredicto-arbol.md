# Veredicto: nimbo-arbol

Verificador: ox-alpha · Fecha: 2026-08-23 · Encargo: `Informes/encargos/nimbo-arbol.md`

```
VEREDICTO: RECHAZADO
DIFF REAL: Assets/_Project/Scripts/Island/NimboTree.cs (+127 netas),
           Assets/_Project/Tests/ArbolNimboTests.cs (nuevo, 272 l),
           Assets/_Project/Tests/PlayMode/ArbolEnLaIslaTests.cs (nuevo, 167 l),
           Assets/_Project/Tests/PlayMode/Nimbo.PlayTests.asmdef (+2: Island, Simulation)
¿CUADRA CON SU INFORME?: sí, con una salvedad — el informe declara solo
    «Nimbo.Island» en el asmdef; el diff lleva también «Nimbo.Simulation», que es
    del agente de ropa (RopaEnLaIslaTests necesita WardrobeService). Costura de dos
    agentes sobre el mismo fichero, no atribuible al árbol.
PRUEBAS: 565 editor / 132 juego / 1 roja — contra base 514/92/0.
    Editor: 563 pasadas, 0 rojas, 2 saltadas. Sube (+51 en la ola).
    Juego: 121 pasadas, 1 roja, 10 saltadas. La roja es exactamente
    DelanteDelArbolElCartelOfreceHablarle, declarada deliberada por el orquestador,
    con el mensaje que señala el enganche D — idéntico al citado en el informe.
FUERA DE SU CARPETA: Nimbo.PlayTests.asmdef (fichero compartido). Ver nota abajo.
¿ENCHUFADO?: parcialmente por diseño. ArbolEnLaIslaTests.cs carga la escena Isla
    de verdad: ElArbolEstaRegistradoYEnPieEnElCentro y HablarleEnLaIslaCargadaCierraElDiaYEntrega
    pasan hoy; DelanteDelArbolElCartelOfreceHablarle verifica cartel+tecla+servicio
    en cuanto el orquestador aplique el enganche D. Es el modelo CronicaEnLaIsla:
    teletransporta al protagonista (CharacterController apagado, precedentemente en
    MinijuegosEnLaIslaTests.cs:142), aparta a los vecinos de la plaza y compara el
    enum por nombre para compilar antes del enganche. Correcto.
DEFECTOS: ver abajo, 3.
AFIRMACIONES SIN RESPALDO: las de los defectos 1-3. El resto verificadas:
    GameBootstrap.cs:335/367, PlayerInteractor.cs:173-174/459/853, WorldView.cs:134/136,
    UiRoot.cs:214/412, SaveGame.cs:113, Nimbo.Tests.asmdef:13, AchievementToast.cs:22
    (existe, en Scripts/UI/Achievements/) y _toast.Push con ese patrón en UiRoot.cs:335 —
    todas ciertas. ITreeService.cs no existe y los seis enganches no están aplicados:
    no se saltó su carril.
LO QUE ESTÁ BIEN: no deshacer esto en la siguiente vuelta —
    - Honestidad de las pruebas: el informe dice «no puedo decir todavía que pasaron»
      y da cifras medidas que cuadran con el XML al dígito, incluido el mensaje de la roja.
    - La racha sobre banderas existentes de SaveGame.Flags (SaveGame.cs:113 ya las
      documenta) en vez de serializar un campo nuevo; CleanOldFlags conservando ayer
      con el porqué escrito (sin eso la racha es imposible).
    - Semilla diaria Rng.FromSeed("arbol_"+Day): hace afirmable el contenido y elimina
      la partida-dependencia del regalo.
    - HablarEntregaLasMonedasQueDice documenta y cubre el bug de doble entrega real
      («dijo 22, la cartera oyó 44») buscando el día de monedas SIN economía registrada.
    - La desviación del encargo (escalar el árbol entero, no solo la copa) está
      declarada como tal y argumentada técnicamente: así se desvía.
```

## Los tres defectos (todos de comentarios; el código pasa las pruebas)

1. **El comentario-propuesto promete una prueba que no existe.**
   `Informes/informe-arbol.md` §E, bloque destinado a `WorldView.cs`: «ArbolNimboTests
   vigila que las dos partes no se separen». Falso medido: `rg 'GrowthScale|0\.35f'
   Assets/_Project/Tests` da cero resultados — ninguna prueba toca esa fórmula. El
   orquestador pegará ese comentario tal cual en un monolito, dejando escrita una red
   de seguridad imaginaria: quien cambie la constante creyendo que una prueba lo
   avisará, no tendrá aviso. Arreglo: o una prueba que replique la fórmula esperada y
   la compare contra `NimboTree.GrowthScale` (NimboTree.cs:79) para niveles 1..10, o
   borrar la frase.

2. **El techo de la racha está mal contado.**
   `NimboTree.cs:93` (comentario de `StreakMultiplier`): «techo en ×1,5 al quinto».
   La fórmula es `1 + Clamp(Streak-1, 0, 5)*0,1`: con racha 5 paga ×1,4; el techo
   ×1,5 llega con racha **6**. El informe repite el error en §5.4 («hasta el techo del
   quinto día»). Es justo el número que alguien cambiará sin saber qué rompe.
   Arreglo: «a partir del sexto día de racha» en ambos sitios.

3. **La probabilidad de los chistes está mal contada.**
   Comentario de `JokeOfTheDay` (`NimboTree.cs`, sobre línea 202): «al tercer chiste
   ya es más probable repetir que estrenar». Con 6 opciones, P(repetir en la tirada
   k) = (k−1)/6: en la tercera es 2/6 ≈ 33%; no supera al estreno hasta la quinta
   (4/6). La rotación sigue defendible, pero el argumento numérico escrito es falso.
   Arreglo: mover la cifra a la quinta tirada o reformular sin número falso.

## Nota de costura (no es defecto del árbol)

`Nimbo.PlayTests.asmdef` es fichero compartido y la regla manda describir el cambio,
no aplicarlo. El agente lo aplicó — pero lo declara en §2.2 con línea exacta, es
aditivo de una línea, era imprescindible para compilar las pruebas que el encargo
exige, y convive sin choque con la línea de ropa. El orquestador debe quedarse con
«Nimbo.Island» al integrar. No lo elevo a rechazo por esto: la alternativa
(describirlo y esperar) habría dejado sus pruebas sin compilar otra vuelta entera.

## Por qué RECHAZADO y no APROBADO

Los tres defectos son pequeños y de arreglo trivial, pero dos ya viven en el árbol
entregado y el tercero está destinado a pegarse en `WorldView.cs`. Este proyecto
aprendió a golpes que un comentario con un número falso es un bug futuro: quien venga
después a ajustar el techo o la rotación lo hará guiándose por esos números. Otra
vuelta cuesta menos que eso.
