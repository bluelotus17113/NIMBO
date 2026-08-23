# Veredicto — revisión de nimbo-rasdesuelo

*Verificador de Isla Nimbo, 23/08/2026. Todas las medidas son propias; el método de
replicación del ruido está indicado donde se usa.*

```
VEREDICTO: RECHAZADO
DIFF REAL: Meadow.cs (+9/−3 aprox: rango de inclinación 4–22°→55–85° y comentarios),
           IslandLighting.cs (+13/−4: niebla 60/280→30/260 y comentarios),
           Tests/PlayMode/CapturaEstilo.cs (+11: entrada flores_a_ras y una palabra
           de cabecera), Scenes/Isla.unity (2 líneas: m_LinearFogStart 60→30,
           m_LinearFogEnd 280→260), Informes/informe-rasdesuelo.md, 2 PNG en Capturas/,
           logs. El resto del árbol sucio es de otros agentes de la ola.
¿CUADRA CON SU INFORME?: Casi todo, con una excepción (defecto 2). La tabla de niebla
           es aritmética correcta (100 m: (100−30)/230 = 30 % ✓; 210 m: 78 % ✓;
           panorama 139 m: 47 % frente a 36 % ✓). IslandCamera.cs:40-41 dice 5,5/18 ✓.
           Archipelago.cs:31,34,50-51: radio 100, vecina a −165 con radio 45, puente
           −80/−132 ✓. Las estadísticas locales del ruido se reproducen EXACTAS con mi
           réplica en float32 de ValueNoise.cs:24-62 + escala 11 de Meadow.cs:442:
           86,9 % del círculo de 5 m con Variation ≥ 0,70, media 0,849. Los B−R de
           panorama los reproduzco idénticos: 19,8 → 24,2. «Solo quité la palabra
           cuatro» cuadra con el diff. La atribución de tras_el_arbol a otro agente
           también. Lo que NO cuadra: «el pico más alto del campo de ruido» (defecto 2).
PRUEBAS: ola34 EditMode 565 / 563 verdes / 0 rojas / 2 saltadas. PlayMode 132 / 121 /
           1 roja / 10 saltadas — la roja es DelanteDelArbolElCartelOfreceHablarle
           (ArbolEnLaIslaTests), declarada a propósito, enganche pendiente del
           orquestador: ajena a este agente. Por encima de la línea base 514/92 sin
           rojas inesperadas. Su corrida filtrada rasdesuelo-PlayMode.xml: 1/1.
FUERA DE SU CARPETA: Assets/_Project/Scenes/Isla.unity — no estaba en su encargo
           («Tu carpeta: Meadow.cs e IslandLighting.cs»; CapturaEstilo.cs sí lo autoriza
           el texto del encargo). Lo declara a bombo y plato en su informe §3, pero la
           regla de la casa es describirlo para el orquestador, no hacerlo: otro agente
           de esta misma ola siguió ese protocolo con su enganche de escena y por eso su
           prueba está en rojo esperando. Dos agentes editando la escena en paralelo es
           exactamente la colisión que la regla existe para evitar.
¿ENCHUFADO?: Sí, y es lo mejor del trabajo. Verificado: nadie llama ApplyHaze en
           runtime — los únicos invocadores de IslandLighting.Apply son
           Editor/SceneBuilder.cs:182 y Tests/PlayMode/Estudio.cs:47 — así que los
           RenderSettings serializados de Isla.unity SON el camino de entrega, y sin
           editarlos el cambio de C# no habría llegado jamás al juego. Los PNG renderizados
           de la escena real lo confirman (B−R de panorama +4,4, reproducido). El
           encargo pedía además el encuadre nuevo en CapturaEstilo y está, cargando la
           escena Isla de verdad; los cinco encuadres previos y el de otro agente están
           intactos.
DEFECTOS:
  1. Scenes/Isla.unity:21-22 tocado fuera de su carpeta. Acción para el orquestador:
     RATIFICA la edición (los valores 30/260 son correctos y necesarios — reverla sería
     desentregar la niebla) y deja escrito que la escena es territorio del orquestador.
     El agente no debe volver a tocarla; si la niebla necesita runtime, la llamada a
     Apply en el arranque de WorldView es de quien tenga ese fichero, como él mismo
     apunta en su cabo suelto.
  2. Afirmación falsa y durable en CapturaEstilo.cs:39-40 («es el pico más alto del
     campo de ruido que siembra las flores») e informe-rasdesuelo.md §4 y «Lo que no
     sé». Medido con réplica float32 del algoritmo exacto: el máximo global de
     Variation sobre la isla-aldea es ≈0,991 en (−65.3, 0.0) —pradera abierta, fuera de
     todos los keep-out de IslandLayout.cs, la zona más cercana es Residencial B a
     ~19 m—, frente a 0,974 en el centro elegido. El encuadre FUNCIONA igualmente
     (87 % ≥ 0,70 verificado; el argumento de robustez ante cambio de semilla sobrevive
     por la densidad local), pero un /// que dice «el pico más alto» y no lo es manda al
     siguiente por el mismo camino equivocado. Acción: reescribir la justificación sin
     el superlativo («un macizo denso: ≥0,70 en el 87 % de su radio») o reapuntar al
     pico verdadero si se quiere la foto canónica del macizo máximo.
AFIRMACIONES SIN RESPALDO:
  - Las cifras del gradiente de flores_a_ras (−35,8 → −24,8) no las reproducí tal
    cual: con tercios de imagen mido −31,6 cerca / +25,0 en franja media. El sentido
    (prado cálido abajo, cielo azul arriba) se confirma y el método queda validado por
    los dos decimales exactos del panorama, así que lo anoto como banda de muestreo no
    especificada, no como mentira.
  - «~500-700 flores... ~1 m² de fragmentos»: lo declara ella misma como estimación no
    medida. Correcto declararlo; sigue pendiente como ella dice.
LO QUE ESTÁ BIEN (no deshacer en la próxima vuelta):
  - El hallazgo de la §3: la niebla vivía solo en la escena y en el editor. Sin ese
    hallazgo, el encargo entero habría sido un sistema escrito y apagado — la enfermedad
    clásica de este repo, detectada antes de entregar.
  - Cambio quirúrgico de verdad: un rango, dos números, una entrada de captura. No tocó
    densidad ni tamaño pese a la advertencia de presupuesto, y lo dice.
  - Comentarios /// reescritos contando la historia nueva con la matemática delante
    (sen/cos, el compromiso del cierre en 260 y qué se pierde), no la vieja maquillada.
  - Honestidad de medición: el 87 %/0,849 y los B−R del panorama se reproducen al
    decimal; distinguió nube de flor en su auditoría de píxeles; separó lo medido de lo
    sentido y dejó lo sentido fuera del informe.
```

**Resumen para el orquestador:** el contenido técnico está bien y NO hay que revertir
nada del código. El rechazo es por (1) la edición de la escena fuera de carpeta —que tú
debes ratificar, no deshacer— y (2) un superlativo falso en un comentario duradero, que
se arregla reescribiendo dos frases. Otra vuelta barata.
