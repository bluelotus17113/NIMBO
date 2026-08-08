# Encargo: catálogo de contenido

Segundo encargo. El primero (las mecánicas) ya está entregado y **no se toca**.

Ahora hace falta el contenido: los cientos de objetos que hacen que la isla se
sienta un juego terminado y no una demo. Es trabajo de tabla, y tiene que ser
consistente con la economía que tú mismo especificaste en `Docs/Contratos/economia.md`.

## Ficheros que escribes — y ningún otro

- `Docs/Contratos/catalogo_comida.json`
- `Docs/Contratos/catalogo_ropa.json`
- `Docs/Contratos/catalogo_muebles.json`
- `Docs/Contratos/catalogo_acabados.json`

No toques nada más. Ni tus ficheros del encargo anterior, ni `Docs/00_*`, ni
`Docs/01_*`, ni `Docs/03_*`, ni `Docs/Contratos/personalidades.json`, ni nada bajo
`Assets/`. Hay otros agentes trabajando en paralelo.

## Forma común

Todos los ficheros tienen la misma forma:

```json
{
  "version": 1,
  "category": "Food",
  "items": [
    {
      "catalogId": "food_sopa_nube",
      "displayName": "Sopa de nube",
      "description": "Sabe a poco y llena mucho. Cosas de las islas flotantes.",
      "price": 12,
      "unlockLevel": 1,
      "tags": ["caliente", "casero"],
      "hungerRestore": 35
    }
  ]
}
```

`catalogId` en minúsculas, sin tildes ni eñes, con prefijo por categoría
(`food_`, `cloth_`, `furn_`, `deco_`, `wall_`, `floor_`). Es un identificador de
código y una clave de guardado: si cambia, se rompen las partidas.

## Qué lleva cada fichero

**catalogo_comida.json** — 45 platos. Campos propios: `hungerRestore` (usa los
tramos de tu tabla: snack +15, casero +35, restaurante +50), `foodKind`
(`snack` | `homemade` | `restaurant` | `dessert` | `drink`). Reparte los precios
según lo que restauran, coherente con `economia.md`.

**catalogo_ropa.json** — 60 prendas. Campos propios: `slot` (`outfit` | `hat` |
`accessory`), `style` (`casual` | `formal` | `deportivo` | `fantasia` | `pijama` |
`festivo`), `palette` (3 colores hex).

**catalogo_muebles.json** — 80 muebles. Campos propios: `layer` (`Furniture` |
`Surface` | `WallMounted` | `Rug` | `Ceiling`), `footprintX`, `footprintY` (enteros
1-4), `function` (`bed` | `seat` | `table` | `storage` | `kitchen` | `bath` |
`entertainment` | `decor` | `light`), y `needBonus`: un objeto con lo que aporta
por hora al habitante que lo usa, con las claves `hunger`, `energy`, `social`,
`hygiene` (solo las que apliquen).

Cuidado aquí: los muebles con `function: "decor"` **no** aportan a ninguna
necesidad, aportan a la comodidad de la habitación. Ponles `needBonus: {}`.

**catalogo_acabados.json** — 20 papeles de pared y 20 suelos. Campos propios:
`surface` (`wall` | `floor`), `baseColor` (hex), `pattern` (`liso` | `rayas` |
`cuadros` | `flores` | `madera` | `piedra` | `azulejo`).

## Las trampas

1. **Las cuatro necesidades son `hunger`, `energy`, `social`, `hygiene`.** El ánimo
   NO es una necesidad, es derivado. Si escribes `mood` en un `needBonus`, está mal.
2. `unlockLevel` va de 1 a 50 y tiene que repartirse: aproximadamente la mitad del
   catálogo disponible en el nivel 10, tres cuartos en el 25. Nada de poner todo a 1.
3. Los precios tienen que cuadrar con los 200 nimbos iniciales y el bono diario de
   20 que fijaste: al empezar se puede comprar poco, y hay que querer más.
4. Ni una palabra de Nintendo. Los nombres son nuestros, y el tono es el del GDD:
   cozy, con humor suave, tema de islas flotantes y nubes.
5. **Nada de rellenar.** 80 muebles significa 80 muebles distintos con nombre y
   descripción propios, no "Silla 1".. "Silla 80".

## Validación obligatoria

Escribe y ejecuta un script de Python que compruebe, sobre los cuatro ficheros:

- que todos los `catalogId` son únicos entre los cuatro ficheros juntos
- que ningún `catalogId` lleva mayúsculas, tildes, eñes ni espacios
- el número exacto de elementos de cada fichero (45 / 60 / 80 / 40)
- que ningún `needBonus` usa la clave `mood`
- que `unlockLevel` está entre 1 y 50, y qué porcentaje del catálogo hay disponible
  en los niveles 10 y 25
- que no hay dos `displayName` iguales

**Enseña la salida del script.** Sin ella doy el encargo por no hecho.

## Formato de respuesta al terminar

```
FICHEROS: <lista con el número de elementos de cada uno>
VALIDACIÓN: <salida literal del script>
REPARTO POR NIVEL: <cuántos objetos se desbloquean en cada tramo de 10 niveles>
DECISIONES: <lo que tuviste que inventar>
```
