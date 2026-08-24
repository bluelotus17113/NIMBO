# Veredicto — revisión de nimbo-rasdesuelo (segunda vuelta)

*Verificador de Isla Nimbo, 23/08/2026. Segunda pasada sobre la corrección del
defecto 2; el defecto 1 era acción del orquestador. Todas las medidas son propias,
corridas hoy sobre HEAD (835cba2).*

```
VEREDICTO: APROBADO
DIFF REAL: El delta declarado de esta vuelta son tres ficheros: comentario en
           CapturaEstilo.cs:39-51, Informes/informe-rasdesuelo.md e
           Informes/rasdesuelo-replica-ruido.py. No hay commits intermedios que lo
           aíslen — todo entró al punto de control 835cba2 del orquestador junto al
           enganche del Árbol — así que la verificación es por contenido en HEAD:
           el comentario corregido está, Meadow.cs:326 (55–85°) e
           IslandLighting.cs:70-71 (30/260) siguen exactamente lo aprobado, y
           Isla.unity solo trae lo ratificado (niebla :21-22, cámara :222-224).
¿CUADRA CON SU INFORME?: Sí. Cada cifra nueva se reproduce exacta:
           - Pico global 0,9907 en (−65.25, 0.00): mi réplica float32 da 0.9907 en
             (−65.25, 0.00) ✓ (el comentario lo redondea a «(−65.3, 0.0) con 0,991»,
             correcto).
           - Centro elegido 0,9738 ✓ · círculo del elegido 86,9 % / 0,849 ✓.
           - Círculo del pico 76,3 % / 0,790 ✓ — la cifra NUEVA que no había medido
             la primera vez; verificada hoy con la misma réplica.
           - Gradiente de flores_a_ras con método declarado (tercios de 1280×720,
             media B−R por banda): −35,5 / +8,3 / +51,7 — reproduzco los tres
             EXACTOS sobre el PNG actual. El defecto menor de la primera vuelta
             («banda de muestreo no especificada») queda cerrado declarando el
             método.
           - ~19,3 m del pico a Residencial B: IslandLayout.cs:48-50 dice centro
             (−52,−14) radio 14; √(13.25²+14²) = 19,28 ✓.
           Única ambigüedad de redacción: llama «la nueva de esta entrega» a
           flores_a_ras_estilo.png y dos líneas después aclara que se renderizó a
           las 11:32, ANTES del arreglo de comentario. Se contradice en la frase y
           se corrige sola en el párrafo; el razonamiento de fondo (comentario no
           cambia geometría → el PNG sigue siendo evidencia válida) es correcto.
PRUEBAS: arbolhook2-EditMode.xml 563/561/0 rojas/2 saltadas (13:06, posterior a su
           corrección de 12:44 — cubre EditMode aunque él no lo corriera; su
           argumento «un comentario no puede cambiar resultados» además es válido).
           retomo-PlayMode.xml 133/123/0 rojas/10 saltadas (20:52). Su corrida
           rasdesuelo-PlayMode.xml 132/121/1/10: la única roja era
           DelanteDelArbolElCartelOfreceHablarle, ajena (enganche del Árbol), y en
           retomo pasa. Todo por encima de la línea base 514/92. Nota de costura:
           el editor neto baja 2 respecto a ola34 (565→563) porque el commit mixto
           borra 9 pruebas de diálogo y añade 7 de árbol/ropa — NINGUNA del área
           rasdesuelo; es el punto de control del orquestador, no este agente.
FUERA DE SU CARPETA: Ninguno en esta vuelta. El defecto 1 (Isla.unity) quedó donde
           lo dejé: él no volvió a tocarla — en HEAD la escena contiene solo las
           cinco líneas ratificadas (niebla 30/260 y cámara 5,5/18/1,2) — y la
           RATIFICACIÓN FORMAL sigue pendiente en el orquestador
           (RETOMAR-AQUI.md, punto 5).
¿ENCHUFADO?: Sí, sin cambios desde la primera vuelta: nadie llama
           IslandLighting.Apply en runtime, así que los RenderSettings serializados
           SON el camino de entrega, y están en Isla.unity:21-22. CapturaEstilo.cs:78
           carga la escena Isla de verdad y los PNG existen (flores_a_ras y
           panorama_estilo, 11:32). El gradiente medido sobre el PNG real se
           reproduce exacto — la evidencia y el camino están vivos los dos.
DEFECTOS: Ninguno bloqueante. Dos notas menores, para constancia:
           1. El docstring de Informes/rasdesuelo-replica-ruido.py cita
              «WorldView.cs:170-172» para el offset cero de la siembra; hoy esa
              llamada está en WorldView.cs:189 (las líneas se movieron con el
              enganche del árbol). Es un artefacto de auditoría, no código
              duradero; no vuelve a rechazar nada.
           2. El overdraw del prado a 5 m sigue sin medirse. Él lo declara pendiente
               en cada entrega y no subió densidad ni tamaño, así que es deuda
               conocida, no defecto nuevo — pero sigue abierta para quien tenga el
               presupuesto entre manos.
AFIRMACIONES SIN RESPALDO: Ninguna nueva. Las estimaciones (~500–700 flores en
           encuadre) siguen etiquetadas como estimación, que es lo correcto.
LO QUE ESTÁ BIEN (no deshacer):
           - Midió ANTES de escribir: replicó mi medida del pico antes de tocar el
             comentario, y el texto nuevo sale de su propia tabla, no de memoria.
           - El comentario corregido cuenta la verdad completa —no es el pico, pero
             su vecindad es más espesa (87 % vs 76 %)—: quien lo lea mañana no
             repite el error ni descarta el encuadre.
           - No reapuntó la cámara al pico verdadero: invalidaría los PNG ya
             renderizados y aprobaría una foto nueva sin haberla visto nadie.
             Rechazar la opción más brillante por procedimiento es exactamente lo
             que pide este repo.
           - No tocó nada de lo aprobado: rango 55–85°, niebla 30/260, los siete
             encuadres escritos y el dinámico, todos intactos.
```

**Resumen para el orquestador:** la corrección es exactamente la pedida y nada más.
Pendientes tuyos, no suyos: ratificar formalmente `Isla.unity:21-22` (punto 5 de
RETOMAR-AQUI.md) y el overdraw del prado, que sigue sin dueño.
