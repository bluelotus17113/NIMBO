# Veredicto — nimbo-ficha

```
VEREDICTO: RECHAZADO
DIFF REAL: Assets/_Project/Scripts/UI/Islander/{IslanderPanel,SocialSection,JobSection,HomeSection}.cs
           modificados (+178/−97 entre los cuatro); nuevos con .meta: AssemblyInfo.cs,
           IslandSocialCard.cs (171 lín), SocialLabels.cs (97 lín), TastesSection.cs (110 lín);
           Assets/_Project/Tests/PlayMode/FichaEnLaIslaTests.cs (308 lín) + .meta;
           y UNA EDICIÓN DECLARADA fuera de carpeta: Assets/_Project/Scripts/Art/World/IslandMeshBuilder.cs
¿CUADRA CON SU INFORME?: sí en todo lo verificable — cada cita fichero:línea del informe
           la comprobé contra el árbol y clava al dígito (UiRoot.cs:29 y :681;
           IslanderPanel.cs:63-65,92,98,104-107,113,117,191-224,231-246; SocialSection.cs:92,
           94-98,115,121-127; JobSection.cs:80-108; HomeSection.cs:93-105,132; JobSection.cs:172;
           TastesSection.cs:51,62-84; IslandSocialCard.cs:67-72,108-158,134-138,160-161;
           SocialLabels.cs:21-60,27-29,71-86). Los recuentos de prueba cuadran con los XML.
           NO cuadra una cosa: la justificación del arreglo fuera de carpeta (defecto 2).
PRUEBAS: revision-EditMode.xml → 529 totales / 527 ✓ / 0 ✗ / 2 ⊘
         revision-PlayMode.xml → 117 totales / 107 ✓ / 0 ✗ / 10 ⊘
         Contra línea base 514/92/0: superada, cero en rojo. Su filtro propio
         (ficha-PlayMode.xml): 7/7, nombres exactos a los del informe.
FUERA DE SU CARPETA: Assets/_Project/Scripts/Art/World/IslandMeshBuilder.cs — declarado por él
         mismo («Para el orquestador», punto 1). El resto del diff de ese fichero es de
         nimbo-falda; no veo nada más atribuible a ficha. UiRoot.cs intacto, como prometió.
¿ENCHUFADO?: sí. UiRoot.cs:113 instancia IslanderPanel y UiRoot.cs:681 lo refresca cada 0,4 s;
         todo lo nuevo cuelga del constructor del panel. FichaEnLaIslaTests.cs carga la escena
         «Isla» real (SceneManager.LoadSceneAsync, línea 44) con servicios registrados de verdad
         y comprueba los tres contratos: scroll (prueba 1), aviso que sobrevive a tres Refresh
         y muere al cambiar de persona (prueba 4), botones con la misma instancia tras dos
         pasadas (prueba 5), mapa social que nombra pareja/riña/flechazo sembrados en los datos
         (prueba 3) y gustos (prueba 6).
DEFECTOS:
1. Editó fuera de su carpeta. IslandMeshBuilder.cs no es suyo y la regla de la casa manda
   DESCRIBIR el enganche —fichero, línea, qué falta— para que lo aplique el orquestador, no
   aplicarlo uno mismo. Que esta vez no chocara con nadie no lo legitima: nimbo-falda estaba
   iterando sobre ese mismo fichero en paralelo (todo el diff actual de IslandMeshBuilder es
   suyo) y la carrera pudo salir al revés. Corrección: no hay código que rehacer — la línea
   que borró ya no existe como tal (falda reescribió FillSurface encima)—, pero el orquestador
   tiene que decidir y dejar registrado quién sostiene ese cambio, y el informe debe quedar
   sin la acción consumada.
2. La justificación del defecto 1 no es verificable: «error CS0127 en IslandMeshBuilder.cs:84
   que bloqueaba la compilación» no aparece en ningún log conservado. postreinicio-EditMode.log
   (02:17, previo a su pasada) solo contiene errores de DiagnosticoTooltip.cs y
   TemaEnLaIslaTests.cs —de nimbo-tema—, y grep 'CS0127' e 'IslandMeshBuilder' sobre todos los
   logs de Informes/pruebas/ da vacío. Puede que el log del intento con el error se perdiera
   con el reinicio, pero tal como queda, la única edición fuera de carpeta se sostiene en una
   afirmación sin respaldo. Corrección: citar el log donde constaba, o reformularlo como lo
   que hoy se puede sostener.
3. Menor: HomeSection.Refresh no invalida _actionSignature cuando faltan servicios (return
   temprano en la rama !TryGet), mientras JobSection.Refresh sí lo hace con comentario expreso
   («se invalida la firma para que la próxima pasada con servicio reconstruya de verdad»,
   JobSection.cs:60-63). Inofensivo hoy porque ServiceRegistry no se vacía en caliente, pero
   es la misma enfermedad en germen dos veces escrita de forma distinta en ficheros vecinos.
4. Menor: ElMapaSocialNombraParejasYRinas (FichaEnLaIslaTests.cs:99-101) afirma Count >= 2 e
   inmediatamente después Count >= 3 — el primer assert es muerto. Dejar solo el de 3.
AFIRMACIONES SIN RESPALDO:
- «CS0127 bloqueaba la compilación de todo el proyecto» (informe, §Cómo empezó y punto 1 de
  «Para el orquestador») — sin constancia en logs; ver defecto 2.
- La tabla «El árbol ahora mismo» reporta fallos ajenos (falda 04:16: 1 ✗ editor; PlayMode 04:22:
  6 ✗). Estaba fechado y etiquetado como instantánea de otras pasadas, y la revisión final del
  orquestador (07:25) da 0 ✗ en ambas suites, así que no es mentira — pero quien lea solo la
  tabla se lleva la foto vieja. Nada que corregir en código; constancia aquí para el orquestador.
LO QUE ESTÁ BIEN (que no lo deshaga la siguiente vuelta):
- El arreglo del refresco va donde el encargo sugería: Refresh ya no destruye lo que no cambió,
  y UiRoot sigue intocado. Las firmas por cadena (SocialSection.Signature :121-127,
  IslanderPanel.RefreshRequests :191-224, JobSection.OptionsSignature :87-105,
  HomeSection :93-105) son el patrón mínimo; nada de banderas ni abstracciones de más.
- IslandSocialCard no recalcula compatibilidad: lee las agendas del servicio social y nombra con
  SocialLabels. Exactamente lo que el encargo prohibía reinventar, respetado.
- SocialLabels elimina duplicación real (StatusOf/StatusColor vivían dentro de IslanderPanel) y
  de paso arregla el caso Confessed, que caía al bloque de amistad y salía «se conocen» — con
  su prueba (CadaEstadoTieneUnaPalabra, :253-254).
- Comentarios /// que explican el porqué en todos los puntos raros: por qué el aviso sobrevive
  a un cambio de etapa pero no de día (SocialSection.cs:89-93), por qué la firma incluye lo que
  te falta (HomeSection.cs:93-94), por qué Say es internal (SocialSection.cs:106-114). Español,
  identificadores en inglés, convención del repo entera.
- El seam con nimbo-tema está bien derivado: HomeSection.cs:132 y JobSection.cs:172 ponen
  backgroundColor inline sobre botones de UiTheme.Action, y lo entrega a su dueño en vez de
  tocar UiTheme. Así se describe un enganche — este párrafo del informe es el modelo de lo que
  habría debido pasar con IslandMeshBuilder.
- InternalsVisibleTo abre solo Say, con el motivo escrito en AssemblyInfo.cs. Lo mínimo.
