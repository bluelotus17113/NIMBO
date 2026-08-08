# Isla Nimbo — Arquitectura y contrato de trabajo

> **Este documento manda.** Si un diseño, un GDD o una idea choca con lo que aquí se
> define, gana esto. Cualquier cambio en el contrato lo hace el orquestador (Claude),
> nunca un agente por su cuenta.

Motor: **Unity 6000.5.5f1**, render **URP**, C# con **assembly definitions**.
Objetivo: un simulador social tipo *Tomodachi Life* en 3D chibi, ambientado en islas
flotantes, con edición de vivienda al estilo *Los Sims* y creador de personajes.

---

## 1. Por qué esta arquitectura

Trabajan varios agentes a la vez. La única forma de que no se pisen es que **cada
fichero tenga un dueño y uno solo**, y que la comunicación entre módulos pase por
tipos que nadie edita mientras se programa.

De ahí las tres reglas duras:

1. **Un fichero, un dueño.** Si tu encargo no lista un fichero, no lo tocas. Ni para
   "arreglar" algo que veas mal: lo reportas.
2. **Los módulos no se llaman entre sí.** Hablan por `EventBus` (avisos) y por
   interfaces registradas en `ServiceRegistry` (consultas). Un módulo nunca hace
   `using Nimbo.Housing;` desde `Nimbo.Social`.
3. **Los datos son tontos.** `Nimbo.Data` son structs y clases serializables sin
   lógica. Toda la lógica vive en su módulo. Así el guardado no depende de nadie.

---

## 2. Mapa de ensamblados

Cada carpeta con `.asmdef` compila por separado. Las flechas son las **únicas**
dependencias permitidas.

```
                    ┌──────────────┐
                    │  Nimbo.Data  │  structs serializables. Sin lógica. Sin MonoBehaviour.
                    └──────┬───────┘     No depende de nadie: es el suelo.
                           │
                    ┌──────▼───────┐
                    │  Nimbo.Core  │  EventBus, ServiceRegistry, reloj, guardado, RNG
                    └──────┬───────┘     y las interfaces de servicio, que nombran tipos de Data.
                           │
        ┌──────────┬───────┼────────┬───────────┬──────────┐
        │          │       │        │           │          │
┌───────▼──┐ ┌─────▼────┐ ┌▼──────┐ ┌▼────────┐ ┌▼───────┐ ┌▼─────────┐
│Personality│ │Simulation│ │Social │ │ Housing │ │ Island │ │  Economy │
└───────────┘ └──────────┘ └───────┘ └─────────┘ └────────┘ └──────────┘
        │          │       │        │           │          │
        └──────────┴───────┼────────┴───────────┴──────────┘
                           │
                    ┌──────▼───────┐      ┌──────────────┐
                    │   Nimbo.UI   │      │  Nimbo.Art   │  malla procedural, materiales
                    └──────────────┘      └──────────────┘
                           │
                    ┌──────▼───────┐
                    │  Nimbo.Game  │  arranque, escenas, cableado. Solo el orquestador.
                    └──────────────┘
```

`Nimbo.Art` no depende de ningún módulo de juego: genera geometría y materiales a
partir de `Nimbo.Data`. Eso permite dejar el arte para el final sin bloquear nada.

---

## 3. Convenciones de código

- Namespace = ruta de carpeta: `Assets/_Project/Scripts/Social/Romance/` →
  `namespace Nimbo.Social.Romance`.
- Un tipo público por fichero, y el fichero se llama como el tipo.
- Nada de `static` mutable fuera de `Nimbo.Core`. Nada de singletons propios: se usa
  `ServiceRegistry`.
- Nada de `GameObject.Find` ni `SendMessage`.
- `Resources.Load` **solo al arrancar**, y solo para los JSON de configuración de
  `Resources/Config`. Nunca en el bucle de juego ni a mitad de partida. Se permite
  porque los catálogos son datos de diseño que los agentes escriben a mano, y un
  `ScriptableObject` obligaría a abrir el editor para cambiar una coma.
- Todo lo que se guarde va en `Nimbo.Data` y lleva `[Serializable]`.
- Los números que un diseñador querría tocar van en un `ScriptableObject` de
  configuración, nunca escritos a pelo en el código.
- Comentarios: solo donde el *porqué* no se lea en el código. Nada de comentar lo obvio.
- Español en textos de juego y documentación; **inglés en identificadores de código**.

### Ficheros pequeños, a propósito

Un sistema grande se parte en varios ficheros aunque en solitario cabría en uno. El
criterio no es la elegancia, es que dos agentes puedan trabajar el mismo sistema sin
tocar el mismo fichero. Ejemplo: las 16 personalidades son **16 ficheros**, uno por tipo.

---

## 4. Comunicación entre módulos

### 4.1 `EventBus` — avisar de que algo pasó

```csharp
EventBus.Publish(new IslanderMoodChanged(id, oldMood, newMood));
EventBus.Subscribe<IslanderMoodChanged>(OnMoodChanged);   // recuerda desuscribirte
```

Los tipos de evento viven **solo** en `Core/Events/GameEvents.cs`, propiedad del
orquestador. Si necesitas un evento nuevo, lo pides; no lo añades.

### 4.2 `ServiceRegistry` — preguntar algo

```csharp
var housing = ServiceRegistry.Get<IHousingService>();
```

Las interfaces de servicio viven en `Core/Services/Contracts/`, un fichero por
interfaz, y las **implementa** el módulo dueño. Un módulo consume la interfaz, jamás
la implementación.

---

## 5. Modelo de dominio

El habitante (`Islander`) es el centro de todo. Su estado se reparte así:

| Bloque | Tipo | Módulo dueño |
|---|---|---|
| Identidad y aspecto | `IslanderIdentity`, `AppearanceData` | CharacterCreator |
| Personalidad | `PersonalityProfile` (4 ejes → 1 de 16 tipos) | Personality |
| Necesidades y ánimo | `NeedState`, `MoodState` | Simulation |
| Relaciones | `RelationshipBook` | Social |
| Vivienda | `HomeAssignment`, `RoomLayout` | Housing |
| Bolsillo e inventario | `Wallet`, `Inventory` | Economy |
| Progresión | `ProgressionState` (nivel, exp, recompensas) | Simulation |

Nadie escribe en un bloque que no sea suyo. Se pide por servicio o se publica un evento.

---

## 6. Las 16 personalidades

Cuatro ejes continuos `[-1, 1]`, cada uno partido en dos mitades → 4 cuadrantes ×
4 combinaciones = **16 tipos**:

| Eje | −1 | +1 |
|---|---|---|
| `Energy` | Calmado | Enérgico |
| `Expression` | Reservado | Expresivo |
| `Attitude` | Independiente | Sociable |
| `Outlook` | Práctico | Soñador |

El tipo es **derivado**, nunca guardado: `PersonalityType.From(profile)`. Así el
creador de personajes solo mueve sliders y el tipo cae solo.

Los nombres de los 16 tipos son **propios**, no los de Nintendo. Misma estructura,
identidad nuestra.

Cada tipo implementa `IPersonalityBehaviour` en **su propio fichero** y define:
qué peticiones hace, cómo reacciona a los eventos, su ritmo de movimiento, sus
expresiones favoritas, sus afinidades con otros tipos y sus frases.

---

## 7. Estado del guardado

Un único `SaveGame` serializado a JSON (Newtonsoft), versionado con `SaveVersion`.
Regla dura, y viene de un incidente real en otro proyecto: **el guardado se escribe
a fichero temporal y se renombra encima**, nunca se abre el bueno en modo escritura.
Ningún agente que no sea el orquestador toca `Core/Save/`.

---

## 8. Cómo se valida el trabajo

Un encargo está terminado cuando:

1. Compila. `Docs/Agentes/compilar.sh` saca los errores de consola de Unity.
2. Los tests de su módulo pasan (`Assets/_Project/Tests/`).
3. No tocó ningún fichero fuera de su lista. Se comprueba con `git status`.

"A mí me funciona" no es una validación. Si no hay salida de comando, no está hecho.
