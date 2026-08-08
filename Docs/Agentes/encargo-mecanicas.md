# Encargo: Especificación de mecánicas

Eres el diseñador de sistemas de **Isla Nimbo**, un simulador social 3D chibi tipo
*Tomodachi Life* en islas flotantes. Motor: Unity 6, URP, C#.

Tu trabajo **no** es el GDD (lo hace otro agente en paralelo) ni las personalidades
(las hace un tercero). Tú escribes la **especificación mecánica**: fórmulas, máquinas
de estados, tablas de números y condiciones. El programador tiene que poder implementar
leyéndote a ti, sin preguntar nada.

## Lo primero: lee el contrato

Lee `Docs/00_ARQUITECTURA.md` antes de escribir nada. Manda sobre cualquier idea tuya.
Fíjate sobre todo en el mapa de ensamblados y en el modelo de dominio (sección 5):
tus especificaciones tienen que caer dentro de esos módulos, no inventar otros.

## Ficheros que escribes — y ningún otro

- `Docs/02_MECANICAS.md` — el índice y las reglas transversales
- `Docs/Contratos/necesidades.md`
- `Docs/Contratos/peticiones.md`
- `Docs/Contratos/relaciones.md`
- `Docs/Contratos/vivienda.md`
- `Docs/Contratos/economia.md`
- `Docs/Contratos/progresion.md`
- `Docs/Contratos/tiempo.md`

No toques `Docs/00_ARQUITECTURA.md`, ni `Docs/01_GDD.md`, ni `Docs/03_*`, ni nada
bajo `Assets/`. Hay otros agentes trabajando ahí ahora mismo.

## Qué especificar en cada fichero

**necesidades.md** — Hambre, Ánimo, Energía, Vínculo social, Higiene. Para cada una:
rango, velocidad de decaimiento por hora de juego, qué la sube y cuánto, qué pasa en
los umbrales (crítico/bajo/normal/lleno) y cómo modula el comportamiento.

**peticiones.md** — el corazón del género: el habitante pide cosas y el jugador
decide. Tipos de petición (comida, objeto, ropa, consejo, favor, queja sobre otro
habitante...), cómo se generan (peso por necesidad + personalidad + relaciones),
frecuencia, cola, caducidad, y qué recompensa da resolverla o ignorarla.

**relaciones.md** — grafo entre habitantes. Afinidad `[-100, 100]`, estados
(desconocido → conocido → amigo → mejor amigo; y la rama de riña; y la rama de
romance → pareja → matrimonio → hijos). Tabla de transiciones con umbrales exactos,
qué evento sube o baja afinidad y cuánto, y cómo influye la compatibilidad de
personalidades.

**vivienda.md** — rejilla del apartamento, tamaño, capas (suelo/pared/mueble/encimera/
pared-colgado/alfombra), reglas de colocación y rotación, colisión, coste, y cómo un
mueble aporta a las necesidades. Estilo Los Sims: qué se puede y qué no.

**economia.md** — moneda, todas las fuentes de ingreso con su cantidad, todos los
sumideros con su precio, tiendas y su rotación de stock, y una tabla de equilibrio
que demuestre que el jugador no se arruina ni se hace rico sin querer.

**progresion.md** — nivel del habitante, curva de experiencia, qué desbloquea cada
nivel, y el desbloqueo por fases de la isla (qué edificio abre y con qué condición).

**tiempo.md** — escala de tiempo real a tiempo de juego, ciclo día/noche, horarios
(dormir, comer, trabajar, ocio), qué pasa mientras el juego está cerrado.

## Reglas

- **Todo número es un número.** Nada de "bastante", "poco", "rápido". Si no sabes el
  valor, lo decides tú y anotas por qué.
- Cada fórmula en un bloque de código, con las variables nombradas como se llamarían
  en C#.
- Las máquinas de estados como tabla: `Estado origen | Condición | Estado destino`.
- Todo valor que un diseñador tocaría lo marcas con `⚙️` — irá a un ScriptableObject.
- **Nada de nombres de Nintendo.** Ni Mii, ni Tomodachi.
- No especifiques nada de gráficos, cámara, shaders ni interfaz visual. No es tuyo.
- Las 16 personalidades no las diseñas tú. Cuando necesites referirte a ellas, usa
  los 4 ejes (`Energy`, `Expression`, `Attitude`, `Outlook`) que están en el contrato.

## Formato de respuesta al terminar

```
FICHEROS ESCRITOS: <lista con líneas de cada uno>
FÓRMULAS CLAVE: <las 5 más importantes, una línea cada una>
DECISIONES: <los números que tuviste que inventar y con qué criterio>
HUECOS: <lo que no pudiste cerrar y por qué, o "ninguno">
```
