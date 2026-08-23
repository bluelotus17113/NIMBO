# Veredicto: nimbo-cultivos

Revisado por nimbo-verificador · 2026-08-23 · Contra `Informes/informe-cultivos.md` y
`Informes/encargos/nimbo-cultivos.md`.

```
VEREDICTO: APROBADO
DIFF REAL: Assets/_Project/Scripts/Art/World/FarmView.cs (+540/−60 aprox., git diff --stat);
           nuevos: Assets/_Project/Tests/SiluetaDeCultivosTests.cs(+.meta),
           Assets/_Project/Tests/PlayMode/CapturaHuerto.cs(+.meta),
           Capturas/huerto_doce_ahora.png, Capturas/huerto_cerca_ahora.png,
           Informes/informe-cultivos.md
¿CUADRA CON SU INFORME?: sí. Única discrepancia: errata de prosa en el informe —la tabla
           (línea 71) dice «raisnube» y la prosa (línea 55) «raiznube»; el catálogo trae
           seed_raisnube, así que la tabla es la correcta. No afecta al código.
PRUEBAS: revisión fresca del orquestador (Informes/pruebas/revision-*.xml, 07:25–07:27):
           EditMode 529 / 527 pasadas / 0 rojas / 2 saltadas (contra base 514/0/2).
           PlayMode 117 / 107 / 0 / 10 (contra base 92/0/9). Ninguna prueba borrada:
           git diff de los cuatro ficheros de prueba modificados no elimina ningún
           [Test]/[UnityTest].
FUERA DE SU CARPETA: ninguno sin declarar. FarmView.cs era su carpeta; MeshShapes.cs
           quedó intacto como dice el informe. Los dos ficheros de prueba nuevos están
           fuera de la carpeta estricta, pero el encargo los pide («añade una herramienta
           [Explicit] igual que CapturaEstilo», que vive en Tests/PlayMode/) y el informe
           los declara uno a uno. Cambios en EconomiaHuertoTests.cs, Nimbo.Tests.asmdef,
           BootTests.cs y .gitignore son de otros agentes (tiendas, tema, cámara,
           orquestador): nada de ellos toca huerto ni mallas.
¿ENCHUFADO?: sí, por dos vías. (1) El sistema ya estaba enchufado antes de este encargo y
           lo sigue estando: BootTests.cs:693 (ElHuertoSeDibujaDondeSeTrabaja) carga Isla
           y exige el GameObject «Huerto»; :646 trabaja una casilla de principio a fin.
           (2) Lo nuevo de este agente: SiluetaDeCultivosTests cierra la costura
           catálogo→switch (los doce ids de Resources/Config/catalogo_cultivos.json
           coinciden uno a uno con FarmView.cs:146-158, comprobado con grep contra el
           JSON), y CapturaHuerto.RetrataLosDoce ([Explicit]) carga Isla, siembra las doce
           por IFarmingService dentro de FarmPlot.UsableSize y espera a que FarmView
           levante el huerto (CapturaHuerto.cs:120-128) — si FarmView se desenchufa, la
           herramienta falla con Assert. Las doce siluetas son distintas dos a dos por
           nombres de pieza y vértices (prueba de editor, verde en revisión).
DEFECTOS: ninguno bloqueante.
AFIRMACIONES SIN RESPALDO: ninguna relevante. Verifiqué por mi cuenta: líneas citadas
           exactas (LookFor :142, Looks :43, Nimbocalabaza :283, Generica :503); mtimes
           código 00:29 < capturas 00:50, como dice; los 5 fallos de su pasada de juego
           son exactamente los atribuidos (3 de TemaEnLaIslaTests, 2 de TiendasEnLaIslaTests,
           cultivos-PlayMode.xml) y en la revisión fresca están en cero. Miré yo mismo
           Capturas/huerto_doce_ahora.png: hay doce plantas, cada una con silueta distinta
           de vecina, y las sombras en cruz que declara bajo raiznube/vidrioestelar están
           donde dice. La calidad de lo que se ve a tres metros la decide quien mira.
LO QUE ESTÁ BIEN: (1) La prueba de costura ataca justo la enfermedad del encargo —un id
           escrito a mano en un switch— y usa el catálogo real de producción, no una lista
           copiada. (2) El fallback genérico conserva el comportamiento antiguo para ids
           desconocidos, con prueba propia. (3) Mallas compartidas por casilla en un
           diccionario estático con limpieza en OnDestroy que no destruye dos veces un mesh
           compartido entre Growing y Ready (FarmView.cs:634-648). (4) Compone con lo que ya
           había (MeshShapes, FoliageMeshBuilder.Weave, Meadow.Weave) en vez de inventar
           geometría nueva; MeshShapes.cs intacto. (5) Informe honesto: separa hechos medidos
           de juicios visuales, declara las incidencias del entorno y atribuye los fallos
           ajeno con nombre y mensaje. No deshacer nada de esto en la siguiente vuelta.
