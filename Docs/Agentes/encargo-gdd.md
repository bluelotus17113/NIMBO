# Encargo: Documento de Diseño de Juego (GDD)

Eres el diseñador de **Isla Nimbo**, un simulador social 3D chibi tipo *Tomodachi Life*
ambientado en islas flotantes, con edición de vivienda al estilo *Los Sims* y creador
de personajes. Motor: Unity 6, URP.

## Lo primero: lee el contrato

Lee `Docs/00_ARQUITECTURA.md` antes de escribir nada. Manda sobre cualquier idea tuya.

## Ficheros que escribes — y ningún otro

- `Docs/01_GDD.md`

No toques `Docs/00_ARQUITECTURA.md`, ni `Docs/02_*`, ni `Docs/03_*`, ni nada bajo
`Assets/`. Otros agentes están trabajando ahí a la vez y te los cargarías.

## Qué tiene que contener `Docs/01_GDD.md`

Escribe en español, en Markdown, con encabezados numerados. Secciones obligatorias:

1. **Visión** — la frase de una línea, el pilar de fantasía y a quién va dirigido.
2. **Bucle de juego** — el bucle de sesión (2-5 min), el diario y el de largo plazo
   (semanas). Concreto: qué hace el jugador con las manos, minuto a minuto.
3. **El habitante** — qué es, qué lo hace sentir vivo, qué NO controla el jugador.
   Esto es clave del género: el jugador es un observador que interviene, no un titiritero.
4. **Isla flotante** — la primera isla: zonas, edificios, qué se desbloquea y en qué
   orden. Deja escrito cómo escalaría a varias islas después, pero el juego arranca
   con una.
5. **Vivienda** — apartamentos con interior editable tipo Los Sims: rejilla, paredes,
   suelos, muebles, objetos funcionales. Qué puede y qué no puede hacer el jugador.
6. **Creador de personajes** — qué se puede modelar (cara, cuerpo, voz, ropa) y cómo
   se traduce a los 4 ejes de personalidad.
7. **Progresión y economía** — monedas, cómo entran, cómo salen, niveles del habitante,
   desbloqueos de la isla. Da rangos numéricos, no adjetivos.
8. **Relaciones** — amistad, riña, romance, pareja, familia. Estados y transiciones.
9. **Eventos y espectáculo** — conciertos, sueños, viajes, noticias, minijuegos.
10. **Estructura de una semana de juego** — qué pasa un lunes, qué pasa un sábado.
11. **Tono y estilo** — humor, ritmo, qué NO hacemos (nada de estrés, nada de fallar).
12. **Alcance v1** — la lista de lo que entra en el juego completo, marcada por
    prioridad: `[NÚCLEO]`, `[IMPORTANTE]`, `[SI DA TIEMPO]`.

## Reglas

- **Números, no adjetivos.** "El hambre baja del 100% al 0% en 6 horas de juego"
  vale; "el hambre baja poco a poco" no vale.
- **Nada de nombres de Nintendo.** Ni Mii, ni Tomodachi, ni nombres de personalidades
  de ese juego. Diseñamos lo nuestro con la misma estructura.
- Los 16 tipos de personalidad **no los diseñas tú**, los diseña otro agente. Menciona
  que existen 4 ejes y 16 tipos y sigue adelante.
- No inventes sistemas que necesiten servidor, multijugador o dinero real.
- Extensión: entre 400 y 700 líneas. Si te sobra, corta lo decorativo, no lo concreto.

## Formato de respuesta al terminar

```
FICHERO: Docs/01_GDD.md
LÍNEAS: <n>
SECCIONES: <lista de las 12>
DUDAS: <lo que tuviste que decidir tú y por qué, o "ninguna">
```
