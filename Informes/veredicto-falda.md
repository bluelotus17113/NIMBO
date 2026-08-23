# Veredicto — nimbo-falda

Revisor: verificador · 2026-08-23

```
VEREDICTO: APROBADO
DIFF REAL: IslandMeshBuilder.cs (+149/−34 aprox.); FaldaDelBordeTests.cs + .meta
(nuevos, 76 lín.); CapturaEstilo.cs (+6 encuadre bajo_el_borde, +18 bloque
tercera_persona ajeno intacto); 6 capturas Capturas/estilo_*_falda.png nuevas.
¿CUADRA CON SU INFORME?: Sí, al número exacto. Verificadas sus citas:
IslandMeshBuilder.cs:99 (_labioDelPrado), :107 (EdgeProfile), :121 (MeadowHeight),
:156-157 (consumo de un solo uso), :168-170 (radio medido del labio), :180-183
(grid[0] = labio), :213/:219/:221 (ruido ×t y disolución (1−t)²). Referencias
externas ciertas: WorldView.cs:255 siembra el prado con seed 31 y :258 llama a la
roca sin semilla; PlayerInteractor.cs:38 RimFraction = 0.78f aplicada a FishingSpot
(~:412). Los tres llamadores son pares inmediatos prado→roca (WorldView.cs:119→122,
255→258; Screenshotter.cs:62→64), así que el depósito estático de un solo uso es
seguro hoy y su límite está en los <remarks> de BuildUnderside.
PRUEBAS: revision-EditMode.xml 529 / revision-PlayMode.xml 117 / 0 rojos — sobre la
línea base 514/92/0 (2 y 10 saltadas). Sus 2 pruebas nuevas corren ahí y pasan:
FaldaDelBordeTests.LaRocaNacePegadaAlLabioDelPrado_EnLaIslaDeLaAldea y
_EnLaIslaDelJugador → Passed.
FUERA DE SU CARPETA: ninguno atribuible. Tocó IslandMeshBuilder.cs (su carpeta),
FaldaDelBordeTests.cs+meta y el encuadre de CapturaEstilo.cs (ambos pedidos
explícitamente en el encargo) y las capturas *_falda.png. El bloque tercera_persona
de CapturaEstilo.cs es del agente de cámara y quedó intacto, como manda el encargo.
El resto del árbol sucio corresponde a los otros cinco paneles.
¿ENCHUFADO?: Sí. Firmas públicas intactas: WorldView.cs:119/122/255/258 y
Screenshotter.cs:62/64 compilan sin cambios. La prueba de editor usa las llamadas
exactas de producción, incluida la variante «enchufe en agujero que no existe»
(prado con semilla 31, roca con la 7 por defecto). Y PlayMode: CapturaEstilo carga
la escena Isla (CapturaEstilo.cs:56) y capturó el borde desde abajo y fuera
(encuadre bajo_el_borde); falda-captura-PlayMode.xml: 1 prueba, 1 pasada, y las 6
capturas existen en Capturas/.
DEFECTOS: ninguno de código.
AFIRMACIONES SIN RESPALDO: una. «En estilo_bajo_el_borde_falda.png la roca baja
continua desde el labio en todo el arco visible, sin cielo entre hierba y roca»
(informe-falda.md, sección Encuadre nuevo): no ve imágenes y no describe ningún
análisis de píxeles que lo respalde. La captura existe y el usuario puede mirarla,
pero esa frase es invención visual; lo verificable era «generé la captura desde
(150,−18,118) mirando a (72,−6,56)». Que el orquestador la tache o pida al agente
reformularla; no invalida el entregable.
LO QUE ESTÁ BIEN:
- La prueba es exactamente la que el encargo pedía: por sector, el vértice exterior
  del prado existe en la malla de la roca a <0,01 m, leyendo las dos mallas
  producidas por el mismo camino de producción (sin replicar fórmulas). Contra el
  código anterior habría fallado con metros de hueco y el sector en la mano.
- Soldar en vez de colgar una banda está justificado con un argumento topológico
  medible (hay sectores donde el anillo 0 sube por encima del labio: +1,86 > −1,72;
  una banda se plegaría) y deja cero caras nuevas, cero pregunta de material, por tanto.
- El arreglo del `return` roto heredado quedó dentro de su carpeta y declarado.
- Escaló al orquestador lo que no era suyo (WorldView.cs:258 debería llevar
  seed: 31u) en vez de tocarlo, y su costura no depende de que ese arreglo llegue.
- Comentarios /// con el porqué en todos los puntos donde alguien cambiaría un
  número sin saber qué rompe (por qué depósito y no parámetro, por qué ×t en el
  ruido, por qué (1−t)²).
```

Nota para el orquestador: los 7 rojos que el informe de falda atribuyó a otros
paneles eran ciertos uno a uno contra su propio XML (falda-EditMode/-PlayMode) y en
esta revisión posterior ya están todos verdes. El salto de saltadas de PlayMode
(9 → 10) no es suyo: no tocó ninguna prueba PlayMode existente.
