# Isla Nimbo — Documento de Diseño de Juego (GDD)

> **Versión:** 2.0 — 2026-08-17
> **Motor:** Unity 6000.5.5f1, URP
> **Arquitectura:** `Docs/00_ARQUITECTURA.md` manda sobre este documento.
> **Giro:** `Docs/04_ALDEA.md` manda sobre este en lo que se contradigan. La v2.0
> incorpora ese giro en vez de dejarlo en un documento aparte.

**Qué cambió de la v1.0 a la v2.0.** La v1.0 describía un juego de observación: el
jugador era una entidad externa que no tenía cuerpo, no se enamoraba de nadie y no
subía de nivel. Eso se decidió cambiar el 9 de agosto (`04_ALDEA.md`) y el código
lleva desde entonces yendo por otro lado, pero el GDD seguía midiendo el juego
viejo: su lista de alcance no mencionaba ni una vez huerto, recolección, crafteo,
inventario ni protagonista. La v2.0 arregla eso. Secciones nuevas: §12 a §16.
Secciones reescritas: §1, §8.4 y el alcance (ahora §17).

---

## 1. Visión

**Frase:** *Isla Nimbo* es un **Rune Factory en el cielo**: una granja, un oficio y
una vida en una aldea de islas flotantes cuyos habitantes viven por su cuenta —se
hacen amigos, se enamoran y se casan entre ellos sin que tú intervengas— y a la que
tú acabas gobernando.

**Los tres pilares, y en este orden de peso:**

1. **Simulación de vida (Tomodachi).** Los habitantes tienen voluntad propia. Sus
   romances, sus riñas y sus bodas ocurren **sin el jugador**, por personalidad y
   convivencia. Es lo que hace que la isla merezca la pena mirarla.
2. **Gestión de la aldea.** Tú asignas los trabajos, amplías las casas, organizas
   los eventos y atiendes lo que piden. No mandas sobre las personas: mandas sobre
   el escenario en el que viven, y ellas responden.
3. **Granja y oficio (RPG).** Huerto, recolección, herramientas, crafteo y venta.
   El protagonista tiene cuerpo, sube de nivel y desbloquea cosas al subir.

**Pilar de fantasía:** Eres uno más de la aldea, no un dios. Puedes cortejar a
alguien y que te diga que no porque quiere a otro. Puedes organizar la fiesta donde
dos vecinos se conocen y no enterarte hasta que te lo cuentan. Sueltas piedras en el
estanque; el tamaño de las ondas lo decide el sistema.

**La tensión y cómo se resuelve.** Tomodachi va de mirar y que te sorprenda; Rune
Factory va de hacer y optimizar. Tiran en direcciones opuestas, y la regla que lo
resuelve es de `04_ALDEA.md` §2 y sigue siendo contrato: **lo que hace el
protagonista es riqueza opcional, no trabajo obligatorio.** La aldea progresa aunque
hoy no juegues a nada; no hay castigo por el reloj; el vigor no mata; no hay
estaciones. Si una mecánica nueva obliga a entrar a diario, está mal aunque sea
divertida.

**Público objetivo:** Jugadores de 16 a 40 años que disfrutan los simuladores
sociales, la granja relajada y las narrativas emergentes. No requiere reflejos,
inglés ni experiencia previa con el género.

**Plataforma:** PC (Steam), mando y teclado/ratón. Una sola pantalla, sin online.

---

## 2. Bucle de juego

### 2.1 Bucle de sesión (10–25 minutos)

Cada sesión es un día en la aldea, y **el jugador lo camina**. La sesión corta de
2–5 minutos de la v1.0 era de un juego sin cuerpo; ahora hay una casa de la que se
sale y un puente que se cruza, y eso lleva su tiempo.

1. **Amanecer en tu casa (1 min):** despiertas donde te acostaste. La Crónica (§13.3)
   te espera con lo que pasó anoche: quién empezó a salir con quién, quién dejó de
   hablarse, si hay boda esta semana.
2. **Tu parcela (2–4 min):** riegas, recoges lo que ha crecido, siembras lo que
   quieras. Es pequeño a propósito.
3. **Salir a por material (5–10 min):** la isla y la aldea del otro lado del puente
   tienen nodos que se reponen solos. Madera, piedra, hierbas, flores.
4. **La aldea (5–10 min):** hablas con quien te cruces, regalas, atiendes lo que
   piden. Y **gobiernas**: repartes los trabajos, amplías una casa con lo que has
   traído, pones una fiesta en el calendario. Nada de esto tiene fecha límite.
5. **A dormir cuando quieras.** No hay hora de cierre y no te desmayas. Dormir pasa
   al día siguiente; no dormir tampoco te castiga.

**Ninguno de los cinco pasos es obligatorio.** Se puede jugar una sesión entera
sentado en un banco viendo pasar a la gente, y la aldea progresa igual.

### 2.2 Bucle diario (24 minutos de juego = 1 día)

El día dura **24 minutos reales** (1 minuto = 1 hora de juego). El jugador puede
acelerarlo hasta ×4 con un botón. No se pausa solo al abrir menús.

Un día típico:

| Hora de juego | Qué pasa |
|---|---|
| 06:00–08:00 | Los habitantes se despiertan, desayunan, se asean. |
| 08:00–12:00 | Trabajo, exploración, recados. Según personalidad, unos madrugan más. |
| 12:00–14:00 | Almuerzo. Los sociables comen juntos; los independientes, solos. |
| 14:00–18:00 | Tiempo libre: aficiones, tiendas, relaciones, minijuegos. |
| 18:00–20:00 | Cena, vida social. Mayor probabilidad de eventos. |
| 20:00–23:00 | Ocio nocturno, visitas entre apartamentos. |
| 23:00–06:00 | Sueño. La isla sigue viva: sueños, algún insomne. |

El jugador **no controla** lo que hace cada habitante. Ve la agenda del día y
puede "sugerir" una actividad (prioridad +1), pero el habitante decide según su
personalidad y necesidades.

### 2.3 Bucle de largo plazo (semanas)

- **1 semana:** aprendes los ritmos, desbloqueas la tienda de muebles.
- **2–4 semanas:** nuevos habitantes (máximo 12 en la isla inicial), primeras
  amistades y roces, mejoras de apartamento.
- **1–2 meses:** primeras parejas, desbloqueo de la segunda isla flotante,
  eventos especiales (festival de la isla, noche de estrellas).
- **3–6 meses:** varias islas conectadas por puentes de nubes, familias,
  colección completa de muebles y ropa, progresión al máximo.

---

## 3. El habitante

### 3.1 Qué es

Un *isleño* es una entidad autónoma con identidad visual, personalidad,
necesidades fisiológicas, estado de ánimo, relaciones sociales y un bolsillo.
Vive en un apartamento, tiene una rutina diaria y toma decisiones sin el jugador.

Cada isleño se compone de los bloques definidos en `00_ARQUITECTURA.md §5`:

| Bloque | Ejemplos de datos |
|---|---|
| Identidad | nombre, apodo, fecha de creación, voz (timbre, tono) |
| Aspecto | cuerpo, cara, pelo, color de piel, ropa equipada |
| Personalidad | 4 ejes `[-1, 1]` → 1 de 16 tipos (otro agente los diseña) |
| Necesidades | hambre, energía, higiene, social, ocio, vejiga — todas 0–100 |
| Ánimo | feliz, neutro, triste, enfadado, eufórico, melancólico |
| Relaciones | libro de relaciones con otros isleños y con el jugador |
| Vivienda | apartamento asignado, layout de la habitación |
| Economía | monedas (0–99999), inventario (máx. 20 objetos), nivel (1–50) |
| Progresión | experiencia, nivel de isla, hitos desbloqueados |

### 3.2 Qué lo hace sentir vivo

- **Agenda propia:** cada isleño calcula su día al despertar, eligiendo
  actividades según personalidad + necesidades + relaciones + eventos.
- **Pensamientos visibles:** al pinchar un isleño, un bocadillo muestra qué está
  pensando ahora mismo ("Tengo hambre", "Alba me cae bien", "Qué sueño...").
- **Expresiones faciales y postura:** cambian con el ánimo. Un isleño triste
  camina más lento, mira al suelo, suspira.
- **Interacciones emergentes:** dos isleños que se cruzan pueden saludarse,
  ignorarse, discutir o compartir objeto según su relación y personalidad.
- **Memoria a corto plazo:** recuerdan lo relevante del día anterior (una pelea,
  un regalo, un sueño) y lo mencionan.

### 3.3 Qué NO controla el jugador

- El jugador **no mueve** a los isleños. No hay "ve a la cocina".
- **No elige** de quién se enamoran. Puede influir con regalos y montando los
  eventos donde se conocen (§15.3), y puede cortejar él mismo y que le digan que no
  (§14), pero el romance entre habitantes lo deciden ellos (§13).
- **No fuerza** amistades ni reconcilia peleas directamente. Con Convivencia 9 puede
  mediar, que baja un escalón de conflicto — no lo borra.
- **No decide** la profesión ni la rutina. Ofrece el puesto; el habitante lo acepta
  si le pega o si le tienes aprecio ganado, y si no, dice que no (§15.1).

El jugador es el arquitecto de la isla y un vecino más, no un dios: puede **soltar
piedras en el estanque** — el dónde y el tamaño de las ondas lo decide el sistema.

---

## 4. Isla flotante

### 4.1 La isla inicial: Nimbo

Una isla flotante de **200 × 200 metros** en el cielo, rodeada de nubes que
ocultan el horizonte. El suelo es una mezcla de hierba, tierra y roca flotante
con bordes irregulares. Siempre es de día o atardeciendo; no hay ciclo
nocturno gráfico (la noche es un filtro azul sobre la escena).

**Zonas y desbloqueo:**

| # | Zona | Descripción | Se desbloquea |
|---|---|---|---|
| 1 | Plaza central | Árbol Nimbo gigante, banco para sentarse, tablón de noticias. Es el punto de llegada del jugador. | Disponible al inicio |
| 2 | Residencial A | 4 apartamentos vacíos (estudio, 6×8 baldosas cada uno). | Al crear el primer isleño |
| 3 | Tienda de comida | NPC tendero estático. Vende ingredientes y platos preparados. | Al crear el primer isleño |
| 4 | Residencial B | 4 apartamentos adicionales. | 3 isleños en la isla |
| 5 | Tienda de muebles | Vende muebles, suelos, paredes y decoración. Catálogo rota cada día. | 3 isleños en la isla |
| 6 | Tienda de ropa | Vende ropa y accesorios. Catálogo rota cada 2 días. | 5 isleños en la isla |
| 7 | Parque | Zona verde con columpio, estanque y banco. Favorece interacciones sociales. | 5 isleños en la isla |
| 8 | Residencial C | 4 apartamentos adicionales (máximo 12 isleños en la isla). | 8 isleños en la isla |
| 9 | Escenario | Tarima al aire libre para conciertos, obras de teatro y festivales. | 8 isleños + primer nivel 10 |
| 10 | Embarcadero | Puente de nubes hacia la segunda isla. | 12 isleños + evento "El puente aparece" |

### 4.2 Escalado a varias islas

Cuando el jugador desbloquea una isla nueva:

1. La cámara vuela de una isla a otra (transición de 3 segundos).
2. La isla nueva tiene su propio límite de isleños (12 por isla), sus propias
   tiendas y su propio tema visual (isla tropical, isla nevada, isla mecánica).
3. Los isleños pueden visitar otras islas si tienen relación con alguien allí y
   hay puente.
4. Cada isla añade una mecánica nueva: la segunda, mascotas; la tercera,
   clima y estaciones; la cuarta, profesiones con minijuegos.

Para la v1, solo la isla inicial (Nimbo) está implementada. Las demás son
contenido post-lanzamiento.

---

## 5. Vivienda

### 5.1 Apartamentos

Cada isleño tiene un apartamento. Al crear un isleño, se le asigna uno vacío.
Si no hay vacíos, no se puede crear más habitantes.

El apartamento es un **interior 3D visto en corte** (la pared frontal no se
dibuja, como en Los Sims). La cámara orbita alrededor del apartamento en modo
construcción.

**Dimensiones:** 6 × 8 baldosas (estudio inicial), ampliable a 8 × 10 y luego
10 × 12 gastando monedas. Cada baldosa mide 1 × 1 metro en el mundo.

### 5.2 Modo construcción

El jugador entra al modo construcción desde el menú del apartamento. El tiempo
se pausa para todos los isleños menos el dueño (si está dentro, se sienta en
una esquina y observa).

**Lo que SÍ puede hacer el jugador:**
- Colocar, rotar (90°, 4 direcciones) y retirar muebles sobre la rejilla.
- Cambiar el suelo por baldosa (12 tipos base) y la pared por panel (8 tipos
  base).
- Pintar paredes y suelos con 24 colores cada uno.
- Mover muebles arrastrándolos por la rejilla.
- Colocar objetos funcionales: cama (dormir), cocina (cocinar), nevera
  (guardar comida), baño (higiene), espejo (arreglarse), estantería (guardar
  objetos), equipo de música (bailar, tocar), televisión (entretenerse).

**Lo que NO puede:**
- Cambiar la forma del apartamento (siempre rectangular).
- Poner puertas ni ventanas (el exterior no se renderiza en vivienda).
- Construir una segunda planta.
- Colocar muebles fuera de la rejilla (no hay "medio baldosa").
- Tocar el apartamento de otro isleño si no hay relación de amistad (nivel ≥ 3).

### 5.3 Muebles y catálogo

- **Catálogo base:** 40 muebles disponibles desde el inicio (cama básica, mesa,
  silla, nevera, cocina, baño, lámpara, alfombra, estantería, espejo, equipo
  de música, TV pequeña, sofá, mesa de centro, maceta, cuadro, reloj de pared,
  perchero, cesto, escritorio).
- **Catálogo de tienda:** 8–12 muebles nuevos cada día, rotando entre un pool
  de 200. Precios entre 50 y 5000 monedas.
- **Muebles temáticos:** conjuntos de 5–8 piezas que comparten estilo (rústico,
  moderno, japonés, submarino, celestial). Se desbloquean por eventos y logros.
- **Objetos funcionales vs. decorativos:** una cama es funcional (el isleño la
  usa); un cuadro es decorativo (da +1 a la valoración del apartamento, que
  afecta al ánimo del isleño al llegar a casa).

---

## 6. Creador de personajes

### 6.1 Flujo

1. El jugador pulsa "Nuevo habitante" desde la plaza central.
2. Se abre el creador en pantalla completa. El isleño de prueba aparece en el
   centro, sobre fondo neutro, en pose A.
3. El jugador modela al isleño por pestañas: Cuerpo → Cara → Voz → Ropa →
   Personalidad → Nombre.
4. Al confirmar, el isleño aparece en la plaza, saluda con una frase generada
   según su personalidad y busca su apartamento.

### 6.2 Cuerpo y cara

| Atributo | Rango | Descripción |
|---|---|---|
| Altura | 0.8–1.2 (escala) | Slider continuo. 1.0 = altura base. |
| Complexión | –1 a +1 | Slider: delgado (−1) a corpulento (+1). Escala horizontal del torso. |
| Tono de piel | 24 colores | Paleta fija, de claro a oscuro, con matices cálidos y fríos. |
| Peinado | 32 opciones base | Con color de pelo (24 colores). Desbloqueables por tienda de ropa (8 más). |
| Ojos | 16 formas, 12 colores | Forma del iris, posición de cejas. |
| Boca | 8 formas | Define la expresión neutra. |
| Nariz | 6 formas | Sutil; el estilo chibi simplifica. |
| Accesorios faciales | gafas (6), pendientes (4), bigote/barba (4) | Opcionales. |

### 6.3 Voz

Tres parámetros independientes:

- **Timbre:** 5 opciones (grave, medio-grave, medio, medio-agudo, agudo).
- **Tono:** slider –1 (monótono) a +1 (cantarín). Afecta la entonación al
  hablar (los isleños no hablan con palabras, usan sonidos sintetizados tipo
  "blablablá" con curvas de pitch).
- **Velocidad:** slider 0.8× a 1.5×. Un isleño enérgico habla más rápido.

### 6.4 Ropa

- **Prendas base (gratuitas):** 8 camisetas, 6 pantalones/faldas, 4 zapatos,
  3 sombreros. Combinación libre.
- **Prendas de tienda:** 4–8 nuevas cada 2 días, precios entre 30 y 2000
  monedas. Pool de 150 prendas.
- **Paleta de color:** 24 colores por prenda. Algunas prendas tienen patrón
  fijo; otras admiten recoloración.
- **Conjuntos temáticos:** se desbloquean por logros (ej. "conjunto pirata" al
  encontrar el cofre perdido en la isla).

### 6.5 Personalidad → 4 ejes

El jugador **no elige el tipo** directamente. Mueve 4 sliders continuos de –1 a
+1 (se muestran como "más/menos" sin números):

| Slider | Extremo izquierdo (–1) | Extremo derecho (+1) |
|---|---|---|
| Energía | Calmado | Enérgico |
| Expresión | Reservado | Expresivo |
| Actitud | Independiente | Sociable |
| Perspectiva | Práctico | Soñador |

El tipo de personalidad se deriva automáticamente al cruzar los 4 ejes (16
combinaciones). Los nombres y comportamientos de los 16 tipos los define otro
agente (ver `Docs/00_ARQUITECTURA.md §6`).

El jugador puede reajustar los sliders más tarde gastando 500 monedas en el
"Espejo de introspección" (objeto funcional de vivienda).

### 6.6 Nombre

El jugador escribe un nombre (3–16 caracteres, alfanumérico + espacios). El
juego sugiere uno aleatorio de una tabla de 200 nombres si el jugador no sabe
cuál poner. El apodo es opcional y lo generan otros isleños al alcanzar nivel
de amistad ≥ 5.

---

## 7. Progresión y economía

### 7.1 Monedas (Nimbos)

La moneda del juego es el **Nimbo** (símbolo: **N◉**). Sin microtransacciones,
sin dinero real.

### 7.2 Cómo entran las monedas

| Fuente | Cantidad | Frecuencia | Condición |
|---|---|---|---|
| Regalo diario | 50–150 N◉ | 1 vez/día real | Iniciar sesión |
| Trabajo del isleño | 30–200 N◉ | 1 vez/día de juego | Según nivel y tipo de personalidad (los enérgicos ganan +15%) |
| Venta de objetos | 50% del precio de compra | Cuando el jugador quiere | Objeto en inventario |
| Minijuegos | 10–100 N◉ | Por partida | Según puntuación |
| Eventos especiales | 200–1000 N◉ | 1–2 veces/mes | Completar el evento |
| Logros | 100–500 N◉ | Una vez | Desbloquear hito |

### 7.3 Cómo salen las monedas

| Destino | Rango de precio | Notas |
|---|---|---|
| Comida (ingredientes) | 5–30 N◉ | Una comida casera gasta 2–4 ingredientes |
| Comida (platos preparados) | 15–80 N◉ | Efecto inmediato, sin cocinar |
| Muebles | 50–5000 N◉ | Según rareza y funcionalidad |
| Ropa | 30–2000 N◉ | Según rareza |
| Ampliación de apartamento | 3000 / 8000 N◉ | Primer aumento / segundo aumento |
| Reajuste de personalidad | 500 N◉ | Por uso del Espejo de introspección |
| Objetos de evento | 100–500 N◉ | Edición limitada |

### 7.4 Balance diario estimado

Un jugador en sesión de 30 minutos (≈1 día de juego ×2 acelerado) debería ganar
entre **200 y 500 N◉** netos tras gastos básicos de comida. Un mueble caro
(5000 N◉) representa entre 10 y 25 sesiones de ahorro — unas 2–3 semanas de
juego real si juega a diario.

### 7.5 Niveles del isleño

> Esto es el nivel de un **habitante**, que sube solo mientras vive su vida. El
> protagonista sube por otro sitio y con otra curva: **§12**.

Cada isleño tiene nivel independiente (1 a 50). La experiencia se gana con
acciones cotidianas:

| Acción | EXP |
|---|---|
| Comer hasta saciarse | +10 |
| Dormir una noche completa | +20 |
| Interactuar con otro isleño | +5–15 |
| Usar un objeto funcional nuevo | +25 (una vez por objeto) |
| Completar un minijuego | +30–50 |
| Participar en evento | +50–100 |
| Recibir regalo del jugador | +20 |
| Subir nivel de amistad | +40 |

**Tabla de niveles (puntos acumulados):**

| Nivel | EXP total | Desbloquea |
|---|---|---|
| 1 | 0 | — |
| 5 | 500 | Puede cocinar platos intermedios |
| 10 | 1500 | Desbloquea el escenario (colectivo) |
| 15 | 3500 | Puede visitar otras islas |
| 20 | 7000 | 2º apartamento (máx. 2 por isleño — para parejas) |
| 25 | 12000 | Puede tener mascota |
| 30 | 20000 | Profesión avanzada (mejor paga) |
| 40 | 40000 | Ropa legendaria |
| 50 | 80000 | Máximo. El isleño aparece en el salón de la fama de la isla. |

### 7.6 Desbloqueos de isla (colectivos)

La isla tiene un "nivel de isla" del 1 al 10, que sube con la suma de niveles
de todos los isleños. Cada nivel desbloquea algo:

| Nivel isla | Suma de niveles | Desbloquea |
|---|---|---|
| 1 | 0 | Plaza, Residencial A, Tienda de comida |
| 2 | 10 | Residencial B, Tienda de muebles |
| 3 | 25 | Tienda de ropa, Parque |
| 4 | 50 | Residencial C |
| 5 | 80 | Escenario, primer evento de isla |
| 6 | 120 | Ampliación de apartamento nivel 1 disponible |
| 7 | 170 | 2ª isla flotante |
| 8 | 230 | Mascotas |
| 9 | 300 | Profesiones avanzadas |
| 10 | 400 | Festival del Nimbo (evento culminante) |

---

## 8. Relaciones

### 8.1 El libro de relaciones

Cada isleño guarda un `RelationshipBook` con una entrada por cada otro isleño
que conoce. Una entrada contiene:

| Campo | Tipo | Rango |
|---|---|---|
| Puntuación de afinidad | int | –100 a +100 |
| Nivel de relación | enum | 0–10 |
| Estado actual | enum | ver §8.3 |
| Historial de interacciones | lista | últimas 20, con fecha |
| Recuerdos compartidos | lista | eventos relevantes juntos |

### 8.2 Niveles de relación

| Nivel | Nombre | Umbral de afinidad | Qué desbloquea |
|---|---|---|---|
| 0 | Desconocido | 0 | No se conocen aún |
| 1 | Conocido | +5 | Se saludan al cruzarse |
| 2 | Vecino | +15 | Pueden visitar sus apartamentos |
| 3 | Amigo | +30 | El jugador puede editar ambos apartamentos |
| 4 | Buen amigo | +50 | Se prestan objetos, se defienden |
| 5 | Íntimo | +65 | Apodos, secretos, abrazos |
| 6 | Alma afín | +80 | Pueden ser pareja |
| 7 | Pareja | +85 + evento | Viven juntos si el jugador amplía apartamento |
| 8 | Familia | +90 + evento | Adoptan un Nimbín (mascota compartida) |
| 9 | Inseparable | +95 | Bono a todas las actividades juntos |
| 10 | Legendario | +100 | Evento especial de "para siempre" |

La afinidad baja con interacciones negativas: discusión (–5 a –15), ignorar
saludo (–2), rechazar regalo (–10), no asistir a evento importante (–20).

### 8.3 Estados de relación

| Estado | Disparador | Se resuelve |
|---|---|---|
| Neutral | Por defecto | — |
| Interesado | Afinidad sube 2 días seguidos | Se hace amigo o se enfría |
| Enfadado | Discusión o traición | Se reconcilian (evento o regalo) |
| Rencoroso | 3+ discusiones sin resolver | El jugador media con un objeto "ofrenda de paz" |
| Enamorado | Afinidad ≥ 80 + evento "Flechazo" | Se vuelven pareja o el sentimiento se desvanece (3 días) |
| Celoso | Triángulo amoroso detectado | La pareja oficial se reafirma o rompen |
| Roto | Ruptura de pareja | 7 días de enfriamiento; no pueden volver a ser pareja |
| Admirativo | Un isleño presencia un logro del otro | Dura 2 días, +10 a interacciones |
| Agradecido | Recibe ayuda en momento crítico (hambre 0%, otro le da comida) | Dura 3 días |

### 8.4 Qué NO hacemos con las relaciones

- No hay poliamor (un isleño solo puede tener una pareja a la vez).
- No hay muerte de isleños. Si dos se odian a –100, simplemente se evitan.
- No hay rupturas forzadas por el jugador. Puedes influir, no decidir.
- **No hay romance por acumulación.** Nadie se enamora de ti por hablarle todos los
  días. El cortejo es un acto declarado con un coste y una respuesta que puede ser
  no (§14).

> **Cambio en la v2.0.** La v1.0 decía aquí «no hay relaciones jugador-isleño
> románticas; el jugador es una entidad externa». Ya no: el protagonista tiene
> cuerpo y puede cortejar. Lo que se conserva de aquella regla es lo que la hacía
> valiosa —que no puedes comprar a nadie con constancia— y eso está ahora en §14.

---

## 9. Eventos y espectáculo

Los eventos son el "espectáculo" del juego: momentos donde la isla entera
reacciona a algo. No son obligatorios, pero generan las situaciones más
memorables.

### 9.1 Tipos de eventos

| Tipo | Frecuencia |Participantes | Ejemplos |
|---|---|---|---|
| Suceso diario | 3–5 por día de juego | 1–3 isleños | "Alba encontró una receta antigua", "Leo y Mia discutieron por el último pastel" |
| Sueño | 1 por noche | 1 isleño | Sueños absurdos: "Soñé que el Árbol Nimbo me hablaba con voz de pato" |
| Concierto | 1 cada 3–5 días | 1–4 isleños | Un isleño canta en el escenario; los demás miran y aplauden (o abuchean) |
| Noticia de la isla | 1 por día | Todos | "El mercado de muebles se inunda de ofertas", "Avistado un Nimbo dorado" |
| Minijuego | Cuando el jugador quiere | el protagonista | Cocina (los pasos de la receta), Pesca (tirar o aguantar), Música (ritmo) — §9.2 |
| Festival | 1 cada 15–20 días de juego | Todos | "Noche de estrellas fugaces", "Festival de la cosecha Nimbo", "Torneo de cocina" |
| Visita misteriosa | 1 cada 7–10 días | Todos | Un viajero de otra isla llega con objetos raros e historias |
| Expedición | Cuando se desbloquea | 2–4 isleños | Explorar una nube densa y encontrar objetos únicos |

### 9.2 Minijuegos concretos

**Hecho.** Los tres estaban escritos y probados desde hacía meses y **no los
construía nadie**: cero usos fuera de su propia carpeta. Lo que faltaba no era
lógica, era quién los empieza, dónde se juegan y qué pasa con sus puntos.

Lo que hay ahora no es exactamente lo que decía este apartado, y conviene dejar
escrito en qué se separó, porque el código llevaba razón:

| | Lo que decía el diseño | Lo que hay | Por qué |
|---|---|---|---|
| Cocina | tablero 3×3 de ingredientes | seguir los pasos de la receta: cortar, remover, sazonar | el tablero pide una interfaz de arrastrar y una tabla de disposiciones por receta; los pasos usan las seis recetas de cocina que ya existen |
| Pesca | pulsar cuando se hunde el flotador | tirar o aguantar mientras el pez tensa el sedal | el timing puro castiga la mano, no la cabeza, y este juego no va de reflejos |
| Ritmo | cuatro carriles tipo Taiko | un carril y una aguja que llega a la marca | cuatro carriles necesitan un mando; uno se toca con la barra espaciadora y se entiende sin explicarlo |

**Dónde se juega cada uno.** Los tres tienen un sitio, y ninguno es un botón de
la barra de arriba:

- **La cocina, en el fogón de tu casa.** `CraftStation.Kitchen` existía, tenía
  seis recetas y no había cocina en ninguna parte: se cocinaba desde una pestaña
  del menú. Ahora hay fogón junto al huerto y darle a una receta de cocina abre
  el minijuego en vez de fabricarla de un clic. La dificultad es **cuántos
  ingredientes lleva**: lo que cuesta reunir cuesta también hacerlo.
- **La pesca, en el borde de la isla**, con la caña en la mano. En el borde y no
  en el embarcadero aunque el embarcadero exista, porque esa zona pide doce
  vecinos y una bandera de suceso: pescar habría nacido bloqueado hasta el final
  de la partida. El borde está desde el primer día y lo tienen las dos islas.
- **El ritmo, mientras hay concierto**, en el escenario si la aldea ya tiene, y
  si no en el Árbol Nimbo. El concierto lo pone el calendario y ocurre haya
  escenario o no.

**Cada uno cobra en la moneda que le pega**, y eso es lo que evita que sean tres
máquinas de nimbos con temática distinta:

- La cocina **no da monedas**: da el plato. Y los ingredientes se gastan gane o
  pierda — si no, el minijuego sería un trámite que se repite hasta que sale.
- La pesca da lo pescado y unas monedas. Si el pez escapa no da nada, y eso es
  lo que hace que elegir entre recoger y aguantar sea elegir. Cuanto más limpia
  la pelea, mejor el pez; y una de cada ocho veces sale una bota vieja.
- El ritmo da monedas pocas y, si suena durante un concierto, **ánimo a toda la
  aldea**. Tocar bien en la fiesta del pueblo tiene que notarse en el pueblo, no
  en tu monedero.

La caña (`tool_cana`) se fabrica a mano con madera, fibra y una concha, así que
pescar es algo a lo que se llega recogiendo, no comprando.

### 9.3 El Árbol Nimbo

El Árbol Nimbo es el corazón de la isla. Crece con el nivel de isla: empieza
como un brote (nivel 1) y llega a árbol gigante con hojas doradas (nivel 10).
Una vez al día, el jugador puede "hablar con el Árbol" y recibir una pista,
un chiste o un objeto aleatorio.

---

## 10. Estructura de una semana de juego

### Lunes — Día de mercado

- La tienda de muebles renueva su catálogo entero (8–12 objetos, en vez de 4–6).
- Los isleños prácticos van de compras; los soñadores pasean mirando escaparates.
- Evento posible: "Ganga del lunes" (un mueble aleatorio al 50%).

### Martes — Día tranquilo

- Sin bonos ni eventos fijos. Los isleños trabajan y descansan.
- Mayor probabilidad de sucesos de amistad.

### Miércoles — Día de inspiración

- Los isleños soñadores tienen +20% de probabilidad de crear algo (receta,
  canción, poema para otro isleño).
- Evento posible: "Exposición de arte" en el parque (los isleños muestran lo
  que crearon).

### Jueves — Día de trabajo

- Los isleños enérgicos ganan +25% de monedas hoy.
- La tienda de ropa renueva catálogo.
- Evento posible: "Doble turno" (un isleño trabaja el doble y gana ×2, pero
  llega a casa con energía 10%).

### Viernes — Día social

- +30% de probabilidad de interacciones sociales positivas.
- El parque tiene el doble de afluencia.
- Evento posible: "Cita a ciegas" (dos isleños con afinidad 0 se presentan).

### Sábado — Día de espectáculo

- Siempre hay un evento grande: concierto, torneo o festival.
- Las tiendas cierran a las 14:00 (los tenderos también van al evento).
- El jugador recibe una invitación al evento al iniciar sesión.

### Domingo — Día de descanso

- Los isleños duermen 1 hora más (se despiertan a las 07:00).
- Las tiendas abren a las 10:00.
- El Árbol Nimbo da el doble de recompensa.
- Evento posible: "Siesta colectiva" en el parque (todos los isleños se tumban
  bajo el árbol a las 15:00).

---

## 11. Tono y estilo

### 11.1 Humor

El humor de *Isla Nimbo* es **absurdo y tierno**, nunca cínico ni cruel. Los
isleños hacen cosas ridículas con total seriedad. Un isleño puede declararse
enamorado de una tostada, organizar un funeral por un mueble roto o pasarse
tres horas mirando una nube porque "tiene forma de patata".

### 11.2 Ritmo

El juego **no mete prisa.** No hay penalizaciones por ausencia, no hay deadlines,
no hay "game over". Un isleño con hambre al 0% no muere: se sienta en el suelo,
suspira y espera. Si el jugador no entra en 3 días, los isleños sobreviven con
lo que haya en la nevera y se quejan al volver ("¡Te echamos de menos!").

### 11.3 Qué NO hacemos

- **Nada de estrés:** sin temporizadores que caducan, sin sanciones por jugar
  mal o lento. No hay forma de "perder".
- **Nada de fallar en seco:** de los minijuegos no se sale con las manos vacías
  y sin nada que enseñar. Quemar la cocina da **engrudo**, que se come; lo que
  se pierde es el plato bueno, no la cena. Los ingredientes sí se gastan, y esa
  es la única forma de que el minijuego signifique algo: si al fallar se
  devolvieran, sería un trámite que se repite hasta que sale. La puntuación alta
  da de más, no es requisito.
- **Nada de conflicto grave:** las discusiones son por tonterías (quién se comió
  el último pastel, no quién traicionó a quién). Las reconciliaciones son
  cálidas y un poco ridículas.
- **Nada de estereotipos dañinos:** los 16 tipos de personalidad no se mapean a
  géneros, razas ni culturas reales.
- **Nada de dinero real:** sin microtransacciones, sin suscripciones, sin cajas
  de botín. El juego se compra una vez.
- **Nada de muerte, enfermedad ni decadencia:** la isla siempre mejora, nunca
  empeora. Los isleños no envejecen.

### 11.4 Estética

Chibi 3D con materiales suaves (sin PBR realista), sombreado cel, contornos
finos (#2b1b35, heredado de la guía de estilo de Mystic Emporium). Paleta
pastel con acentos vibrantes. Las nubes son personajes visuales: cambian de
forma, color y densidad según el evento del día.

---

## 12. Progresión del protagonista

### 12.1 Cinco vías, no una barra

El protagonista sube por **cinco vías independientes**, cada una de nivel 1 a 10.
No hay un único número de nivel que lo resuma todo.

**Por qué cinco y no una.** Con una sola barra de experiencia, una tarde de hachazos
paga los desbloqueos sociales, y al revés. Eso convierte el juego en «haz lo que más
XP dé», que es justo la optimización que `04_ALDEA.md` §2 prohíbe. Con vías
separadas, quien solo quiere convivir sube Convivencia y desbloquea lo social sin
tocar una azada, y quien solo quiere granja no se queda sin progresar por no hablar
con nadie. Nadie se queda fuera y nadie puede saltarse una vía comprándola con otra.

| Vía | Sube haciendo | Evento que ya se publica |
|---|---|---|
| **Cultivo** | labrar, sembrar, regar, cosechar | `TileChanged`, `CropHarvested` |
| **Recolección** | golpear y recoger nodos | `NodeHit`, `NodeGathered` |
| **Oficio** | craftear en cualquier mesa | `ItemCrafted` |
| **Convivencia** | hablar, regalar, atender peticiones | `AffinityChanged`, `ItemGifted`, `RequestResolved` |
| **Aldea** | asignar trabajos, ampliar casas, organizar eventos | `HomeUpgraded`, `RequestResolved` |

**Nivel de aldeano** (el número que se ve en el HUD) es la media de las cinco vías,
redondeada hacia abajo. No se gana directamente: es un resumen. Alimenta el «nivel
de isla» de §7.6 junto con los niveles de los habitantes.

### 12.2 Curva

La curva del diseño da la experiencia **total** de cada nivel como `40·nivel²`. Lo
que se implementa es el escalón, que es su diferencia:

```csharp
int XpForLevel(int level) => 40 * (2 * level + 1);      // de este nivel al siguiente
int TotalXpForLevel(int level) => 40 * level * level - 40;
```

| Nivel | Cuesta el escalón | XP total desde cero | Referencia |
|---|---|---|---|
| 2 | 120 | 120 | una tarde |
| 3 | 200 | 320 | dos o tres sesiones |
| 5 | 360 | 960 | primera semana de juego |
| 7 | 520 | 1920 | segunda semana |
| 10 | 760 | 3960 | tope; unas 4–5 semanas si te dedicas a esa vía |

Una acción da entre 2 y 12 XP según lo que cueste. Lo que paga cada una vive en
`PlayerProgressionConfig` ⚙️, y la diferencia entre lo más barato —una casilla
labrada, 2— y lo más caro —pagar la obra de una casa, 12— es de seis veces y no de
cien: un juego donde una acción rinde cien veces más que otra es un juego donde solo
se hace esa.

### 12.3 Qué desbloquea cada nivel

Esta tabla es el contenido de la progresión. Todo lo que aparece aquí es una mejora
que el jugador **nota al usarla**, no un porcentaje invisible.

**Estado.** El motor está hecho y funcionando: las cinco vías suben con lo que ya
publicaba la isla, se guardan con la partida, avisan al subir y tienen pantalla
propia. De la tabla de abajo está hecho lo marcado `[x]`; lo demás sigue en pie.
Lo que falta se agrupa en tres bolsas y conviene saber por qué:

- **Lo que necesita contenido nuevo** (bancal de nube, invernadero, muebles
  legendarios, nodos raros): no es progresión, son cosas que todavía no existen en
  ningún catálogo. Cultivo 6 y 10, Recolección 3, Oficio 10.
- **Lo que necesita un menú de interacciones sociales** que hoy no existe: pulsar E
  junto a un vecino siempre es «charlar», así que casi toda la vía de Convivencia no
  tiene dónde ponerle puerta. Eso llega con §14 (el cortejo).
- **Lo que necesita sistemas de aldea que no están escritos**: ascensos de rango,
  organizar eventos y festivales, invitar por encima del cupo. Aldea 4 en adelante.

**Cultivo** — la parcela arranca con 4×3 casillas útiles de las 8×6 que hay.

| Nivel | Desbloquea |
|---|---|
| 2 | `[x]` +1 fila de parcela (4×4) |
| 3 | `[x]` La regadera moja 3 casillas en línea |
| 4 | `[x]` +1 fila y +1 columna (6×5) |
| 5 | `[x]` Las cosechas rinden +1 unidad con 25% de probabilidad |
| 6 | Bancal de nube: 4 casillas que no necesitan riego diario |
| 8 | `[x]` Parcela completa (8×6) |
| 10 | Invernadero: 6 casillas que crecen al doble de velocidad |

**Recolección**

| Nivel | Desbloquea |
|---|---|
| 2 | `[x]` Ves el nombre y el material del nodo antes de golpearlo |
| 3 | Los nodos raros (geoda de nube, orquídea etérea) aparecen el doble |
| 5 | `[x]` Un golpe menos en árboles y rocas |
| 6 | `[x]` Los nodos se reponen un día antes |
| 8 | `[x]` Recoges el doble de flores y hierbas |
| 10 | `[x]` Ves los nodos maduros en el mapa (`MapPanel`) |

**Oficio** — sustituye la puerta por nivel de isla que hoy tiene `AvailableAt`.

| Nivel | Desbloquea |
|---|---|
| 2 | `[x]` Recetas de nivel medio (las que pedían isla 4 o más) |
| 4 | `[x]` Crafteo en lote (×5 de una vez), menos en la cocina |
| 5 | `[x]` Las recetas más finas (las que pedían isla 8 o más) |
| 6 | `[x]` **Herramientas de nivel 2** (§12.4) |
| 8 | **El anillo de compromiso** (§14.4) |
| 10 | Muebles legendarios y adornos de isla |

**Convivencia**

| Nivel | Desbloquea |
|---|---|
| 1 | `[x]` Hablar, contar un chiste (`Chat`, `Joke`) |
| 2 | `[x]` Halagar (`Compliment`) |
| 3 | Un regalo más al día |
| 4 | `[x]` Abrazar y jugar (`Hug`, `PlayTogether`) |
| 5 | `[x]` **Cortejar** (`Confess` — §14) |
| 7 | Pedir un favor: un vecino te trae material que necesitas |
| 9 | Mediar en una riña: baja un escalón de `ConflictStage` |

**Aldea** — hoy todo esto está abierto desde el minuto uno. Ponerlo detrás de la vía
es lo que convierte «ser el alcalde» en algo que se gana.

| Nivel | Desbloquea |
|---|---|
| 1 | Ver la ficha y las necesidades de un habitante |
| 2 | `[x]` **Asignar trabajos** (§15.1) |
| 3 | `[x]` **Ampliar casas** (§15.2) |
| 4 | Aprobar ascensos de rango |
| 5 | Organizar eventos pequeños: merienda en el parque, concierto |
| 7 | Organizar festivales |
| 8 | Invitar a un habitante nuevo por encima del cupo de la zona |
| 10 | Alcalde: la aldea entera gana +10% de ánimo base |

### 12.4 Herramientas de nivel 2

**Hecho.** Las cinco herramientas de siempre son de nivel 1 y cuestan monedas. El
nivel 2 se **craftea** en la mesa de trabajo (Oficio 6) y es el sumidero que le da
sentido a acumular madera y piedra.

| Herramienta | Nivel 1 | Nivel 2 (crafteada) |
|---|---|---|
| Azada recia | 1 casilla | 3 casillas en línea |
| Regadera grande | 1 casilla | 3 casillas |
| Hacha buena | los golpes del nodo | un golpe menos |
| Pico bueno | los golpes del nodo | un golpe menos |
| Guadaña larga | 1 mata | un arco de hasta 3 matas |

**Cada receta se come la herramienta vieja.** Si no, acabarías con las dos en la
mochila y usando la mala por descuido, y el segundo escalón sería un objeto más en
vez de una mejora.

Tres detalles que salieron al escribirlo:

- **La barrida es de lado, no hacia delante.** Barrer hacia delante alcanza casillas
  que no se ven y de las que no salía ningún cartel, así que se labra sin haber
  mirado. De lado se ve lo que se está segando.
- **Ningún nodo cae de un solo golpe**, sumen lo que sumen las mejoras. Con
  Recolección 5 y la herramienta buena se descuentan tres golpes de una vez, y un
  árbol que cae al primer toque deja de ser un sitio al que ir y pasa a ser un botón.
- **Cae "aguanta el doble de agua"**, que decía la tabla vieja: no hay depósito en la
  regadera, así que no era una mejora de nada. Si algún día lo hay, vuelve.

Y la regadera de tres casillas se consigue **por dos caminos**: esta herramienta
(Oficio 6) o Cultivo 3. Es a propósito — quien se dedica al huerto lo consigue
regando y quien se dedica al taller lo consigue fabricando. Dos vías separadas tienen
que poder llegar a lo mismo por su cuenta, o dejan de ser cinco caminos y vuelven a
ser una lista.

No hay nivel 3. Dos escalones bastan para que se note la mejora y no obligan a
rehacer el equilibrio de todos los nodos.

### 12.5 Cómo se implementa

Un servicio nuevo, `PlayerProgressionService`, que **se suscribe a eventos que ya
existen** y no cambia ni una línea de Farming, Gathering, Crafting o Social. Es el
patrón del `EventBus` funcionando como se diseñó: la progresión escucha lo que la
isla ya cuenta.

- `PlayerState` gana un `SkillSet`: cinco pares (nivel, XP). Se guarda con el resto,
  y se rellena solo al leerlo — una partida de antes no trae la lista, y cargarla no
  puede dejar al protagonista sin saber hacer nada.
- Contrato `IPlayerProgression` en `Nimbo.Core.Services.Contracts`:
  `LevelOf`, `XpOf`, `XpNeededFor`, `VillagerLevel`, `IsUnlocked`, `RequirementFor`
  y `Grant`. `RequirementFor` está para poder **explicar** la puerta: nunca se
  esconde un botón sin decir qué falta, porque un hueco vacío parece un fallo y «te
  hace falta Aldea 2» es una razón para seguir jugando.
- Eventos nuevos en `GameEvents`: `SkillLeveledUp(skill, level)` y `UnlockGained(id)`.
  Dos y no uno, y salen dos carteles cuando coinciden: subir de nivel pasa a menudo y
  se lee de un vistazo, desbloquear algo pasa poco y hay que pararse a leerlo. El
  aviso reutiliza `AchievementToast`, que ahora guarda el texto ya escrito en vez de
  identificadores de logro.
- Las puertas se preguntan desde la UI (`Gates.Allows(Unlock.AssignJobs)`), no desde
  la lógica: así una partida vieja sigue cargando y lo único que cambia es qué
  botones se ven. **La contrapartida hay que tenerla presente:** el servicio de
  debajo sigue aceptando la llamada, así que el día que algo que no sea la pantalla
  reparta trabajos, la puerta habrá que ponerla también allí.

**Las tres excepciones a lo de no tocar los servicios.** Tres desbloqueos no se
pueden preguntar desde arriba porque cambian lo que pasa dentro, y ahí sí hay una
línea nueva: el huerto comprueba el nivel de Cultivo **solo al labrar** (lo ya
labrado sigue funcionando pase lo que pase con los niveles, así que ninguna partida
guardada pierde una planta), la recolección resta el golpe de más y dobla lo que se
coge agachándose, y la cosecha sortea la unidad extra con la casilla como semilla —
si no, bastaría con recoger con la mochila llena y volver a intentarlo hasta que
saliera la buena.

---

## 13. Romance autónomo entre habitantes

**Esto es el corazón del pilar Tomodachi y ya funciona casi entero.** `RomanceEvaluator`
lleva la rama del flechazo a la boda sin que nadie la empuje: un flechazo nace si hay
amistad hecha, afinidad y compatibilidad; si es correspondido pasan a salir; si el
otro ni le aprecia, se le pasa y duele (`HeartbreakAffinity`). Salir sube a
prometidos, y la afinidad a la baja rompe en cualquier punto. Todo por su cuenta.

Faltan tres cosas, y las tres son la diferencia entre un grafo que se mueve y una
aldea que te cuenta historias.

### 13.1 Las bodas no llegan a ocurrir

`RomanceStage.Engaged` es hoy un callejón sin salida: `TryMarry` y `TryHaveBaby`
están escritos y **nadie los llama nunca**. Una pareja se promete y se queda ahí para
siempre.

**Solución — la boda es un evento del calendario, no un botón.** Un `WeddingPlanner`
mira las parejas prometidas una vez al día; a los 5 días de prometidos con los dos
por encima del umbral, mete una boda en el `EventCalendar` (que ya existe) para 3
días después. El evento la celebra y llama a `TryMarry`.

Esto resuelve la tensión de golpe: la boda ocurre **sin ti** —es su historia, no
tuya— pero te avisan con antelación, puedes ir, y si vas ganas afinidad con los dos.
Si no vas, te lo cuentan en la crónica. Nadie te espera.

**Bebés:** a los 10 días de casados, si la casa está ampliada al menos un nivel y hay
sitio, `TryHaveBaby` por su cuenta. Que haga falta casa grande es lo que engancha la
gestión con la vida de la aldea: **tú no decides que nazca nadie, pero si nunca
amplías casas, la aldea no crece.**

### 13.2 Rivales y triángulos

**Hecho.** Antes, dos habitantes podían tener un flechazo por la misma persona y no
pasaba nada: no se enteraban el uno del otro. Era la historia más jugosa que el
sistema podía dar y se estaba tirando.

Cuando nace un flechazo de A hacia B se mira si B ya recibía otro de C. Si lo hay:

- Los dos pretendientes se ponen en `ConflictStage.Rivalry`.
- Pierden afinidad **entre ellos** cada día que dure. Con B no se enfadan: no es
  culpa suya.
- A los 6 días gana quien tenga más `afinidad + compatibilidad × 20`. El que gana
  pasa a salir con B; el que pierde vuelve a `None` con el golpe de desamor y 5 días
  en los que no le puede nacer otro flechazo.
- La rivalidad **no se cura sola**: queda hasta que uno se disculpa (`Apologize`).

Lo importante de la resolución es que el jugador no decide nada y aun así la
entiende: gana quien mejor se lleva con ella. La compatibilidad entra en la cuenta
porque si solo contara la afinidad ganaría siempre quien más veces se haya cruzado
con ella, y eso premia el azar de por dónde pasean.

Cinco cosas que salieron al escribirlo y conviene tener anotadas:

- **`Rivalry` va al final del enum, con su número escrito**, aunque en gravedad esté
  entre la tirantez y la riña. Estos valores acaban en las partidas guardadas y
  colarlo en medio convertiría las riñas viejas en enemistades. Para ordenar por
  gravedad está `ConflictStages.Severity()`, y las comparaciones que decían
  `>= Quarrel` ahora dicen `IsSerious()`.
- **La reevaluación diaria borraba la rivalidad.** `StageEvaluator` recalcula el
  conflicto desde la afinidad cada mañana, así que el triángulo se deshacía antes de
  llegar a verse. Ahora solo la tapa algo peor.
- **Escribir en una sola agenda no basta.** El que ganaba empezaba a salir con ella y
  ella no salía con él, porque el flechazo era de una dirección y ella nunca había
  apuntado nada sobre él. Una pareja escrita en un solo lado la deshace el evaluador
  de romance a la mañana siguiente.
- **Dos rivales no se pelean a gritos**: se ignoran, y de vez en cuando uno da el
  paso. Es la única forma que tienen de salir de ahí.
- **Se disculpa uno y se levanta en los dos.** Dejarla puesta en el otro daría un
  vecino que sigue viendo un rival en quien acaba de venir a pedirle perdón, y eso no
  hay forma de deshacerlo desde el juego.

**Lo que queda:** que los dos suban su ritmo de interacción con B —«van a buscarle
más»— vive en `IslanderBrain`, que hoy elige a quién visitar por cercanía y
necesidad; y la mediación del jugador (Convivencia 9) necesita el menú de
interacciones sociales que llega con §14.

### 13.3 Nadie se entera de nada

`RomanceStageChanged`, `BabyBorn` y `ConflictStageChanged` se publican y no los lee
nadie que se lo cuente al jugador.

**Solución — la Crónica, en el menú.** Una página que lista lo que ha pasado en la
aldea por días: «Alba y Leo empezaron a salir», «Mia le retiró la palabra a Alba»,
«Boda el jueves: Leo y Alba». Va **en el menú y no en el mundo**: leer la vida social
es un gesto de sentarse a mirar, no un cartel que te interrumpe mientras riegas. El
mundo lo refuerza con burbujas y caras, pero el canal principal es la Crónica.
Reutiliza `NewsBoard`, que ya está escrito.

---

## 14. El romance del protagonista

### 14.1 La regla

**Hecho** §14.1, §14.2 y §14.3. Lo de §14.4 (que tu declaración entre en el triángulo
de la aldea) y §14.5 (la boda contigo) siguen en pie: ver el final de §14.5.


El protagonista puede cortejar a un habitante, **y puede fallar**. No es una barra
que se llena: es una declaración que se hace una vez y tiene respuesta.

Hoy el protagonista está deliberadamente fuera del grafo romántico —`DevelopCrushes`
pide el otro al censo y el protagonista no está— y el motivo era bueno: en un juego
donde puedes hablar con la misma persona todos los días, el romance por acumulación
es inevitable y aburrido. **Eso se conserva.** Lo que se añade es una puerta distinta.

### 14.2 El cortejo

Hacen falten tres cosas a la vez:

1. Convivencia ≥ 5.
2. Que el habitante te tenga afinidad de **Amigo** (nivel 3) o más.
3. **Un ramo**, crafteado con flores que hayas recogido. Se consume.

Con eso, `Confess` pone `RomanceStage.Confessed` en la ficha que ese habitante tiene
de ti. La respuesta no es inmediata: se resuelve **al día siguiente**, y ese día de
espera es a propósito.

### 14.3 La conducta del protagonista

Con la misma `Compatibility` que se usa entre habitantes, cruzando los cuatro ejes.
Pero el protagonista no tiene `PersonalityProfile` —y no debe tenerlo, porque su
personalidad «la pone quien juega con lo que hace»—.

**Solución: los cuatro ejes se deducen de su conducta.** Y hay cuatro decisiones de
diseño en cómo se deducen; cada una arregla un modo de fallar.

#### 14.3.1 Proporciones, nunca cantidades

Ninguna medida puede ser un total. Si «metros corridos al día» fuera el eje de
Energía, con 3000 m = +1, entonces **quien juega tres horas es enérgico y quien juega
veinte minutos es calmado**, y lo que estaríamos midiendo es la duración de la sesión,
no el carácter. Es el fallo que invalida la idea entera si no se ve a tiempo.

Cada eje es la **proporción entre dos conductas que compiten por el mismo momento** —
cosas que se hacen *en vez de* la otra. Así la sesión larga y la corta dan el mismo
perfil, que es lo único correcto.

| Eje | −1 | +1 | La proporción |
|---|---|---|---|
| **Energía** | calmado | enérgico | segundos corriendo / segundos en movimiento. Es Shift o no Shift, y `PlayerBody` ya lo distingue |
| **Expresión** | reservado | expresivo | interacciones expresivas (`Joke`, `Hug`, `Compliment`, `PlayTogether`) / total de interacciones |
| **Actitud** | independiente | sociable | tiempo con un vecino a menos de 8 m / tiempo a la intemperie |
| **Perspectiva** | práctico | soñador | gestos gratuitos / gestos de renta |

Los «gestos» de Perspectiva, que es el eje que menos se deduce solo:

- **Soñador:** colocar un adorno, craftear algo decorativo, sembrar una flor, hablar
  con el Árbol Nimbo, sentarse en un banco.
- **Práctico:** vender material en crudo por el cajón, ampliar una casa, craftear
  herramientas y muebles funcionales.

Los dos lados tienen que estar al alcance en la misma sesión, y lo están.

#### 14.3.2 El arranque: confianza, no valor

El primer día no hay muestras. Y cero **no es neutro**: `PersonalityProfile.TypeIndex`
resuelve con `> 0f`, así que un perfil de cuatro ceros sale del tipo 0 —calmado,
reservado, independiente y práctico—, que es un arquetipo concreto y no «promedio».

Dos reglas:

1. **El perfil del protagonista no pasa nunca por `Compatibility.Full` ni por nada que
   lea su `TypeIndex`.** Solo por `Compatibility.Between`, que trabaja con los ejes. El
   protagonista tiene ejes; no tiene tipo.
2. **Encogimiento por número de muestras**, para que un eje con poca información tire a
   cero él solo, sin casos especiales:

```csharp
float axis = raw * n / (n + K);      // K = 40 muestras ⚙️
```

Mientras `n < K/2` la pantalla dice «todavía no está claro» en vez de un número, que
además es verdad.

#### 14.3.3 La ventana: decaimiento, no historial

Una media de los últimos 7 días obliga a guardar siete días de contadores y hace que
el perfil **salte** cuando un día se cae de la ventana. Peor: convierte la identidad en
«lo que hiciste esta semana», y un fin de semana picando piedra te reescribe.

Mejor una media exponencial con vida media larga: dos floats por eje, decaídos una vez
al día en `DayPassed`.

```csharp
const float Halflife = 14f;                     // días de juego ⚙️
float decay = Mathf.Pow(0.5f, 1f / Halflife);   // ≈ 0.952
positive *= decay;
total    *= decay;
```

- **Ocho floats en el guardado**, no un historial.
- La personalidad **deriva** en vez de dar saltos, que es lo que se quiere sentir.
- Y es la defensa contra el farmeo, sin una sola regla anti-farmeo: mover un eje a
  propósito cuesta unas dos semanas de juego, más que los 10 días de espera tras un
  rechazo. **Farmear el eje sale estrictamente peor que cortejar a alguien
  compatible.** No hay que prohibirlo, basta con que sea el camino lento.

#### 14.3.4 La compatibilidad pone el precio, no el veredicto

Un umbral de «compatibilidad ≥ 0.55 o no» es un muro, y un muro en un juego cuyo
contrato es no castigar. Además el jugador no ve el número, así que un «no» seco se lee
como arbitrario.

**La compatibilidad mueve la afinidad que hace falta, no la respuesta.**

```csharp
float required = Mathf.Lerp(90f, 62f, Mathf.InverseLerp(-0.2f, 0.8f, compat));  // ⚙️
```

Con `Compatibility.Between` y nunca con `Full`: `Full` suma el sesgo entre tipos de
personalidad, y el protagonista no tiene tipo — tiene ejes.

| Compatibilidad | Afinidad necesaria | Qué se siente |
|---|---|---|
| 0.8 — os parecéis | 62 | sale casi solo |
| 0.5 | 74 | hay que trabajarlo |
| 0.2 | 85 | cuesta, pero se puede |
| −0.2 — opuestos | 90 | una historia larga |

**Nadie es imposible.** Los opuestos pueden quererse; solo cuesta el doble de
convivencia. Y los números caen dentro del tramo que ya existe en §8.2: Íntimo en +65,
Alma afín en +80.

#### 14.3.5 El «no» tiene que enseñar

Si te rechaza, la frase **nombra el eje que más lejos quedó**, tomando el de mayor
distancia ponderada:

- Energía → «Eres de los que no paran quietos, y yo necesito calma.»
- Actitud → «Siempre estás rodeado de gente. Yo no sé estar así.»
- Expresión → «No sé nunca lo que estás pensando.»
- Perspectiva → «Tú tienes los pies en el suelo. Yo estoy en las nubes.»

Con eso el jugador aprende el sistema sin tutorial y sin ver un número. Se queda en
Amigo, con −5 de afinidad y 10 días antes de poder volver a intentarlo. Y aquí está la
respuesta buena al farmeo: **puedes cambiar, y cambiar de verdad lleva semanas**, que
es justo lo que hace que signifique algo.

#### 14.3.6 Cómo se implementa

El nombre lleva la intención: es **conducta observada**, no personalidad elegida.

```csharp
[Serializable]
public class ConductRecord            // Nimbo.Data.Player
{
    public float[] Positive = new float[4];
    public float[] Total    = new float[4];
    public PersonalityProfile AsProfile(float k = 40f);   // proporción + encogimiento
}
```

`PersonalityProfile` ya vive en `Nimbo.Data.Islanders`, así que esto no toca ningún
ensamblado: `PlayerState` gana un campo y ya está.

Contrato `IConductService` en Core: `Note(axis, towardPositive, weight)`,
`Profile { get; }`, `ConfidenceOf(axis)`. Se suscribe a `DayPassed` para el
decaimiento, y a `ItemCrafted`, `DecorPlaced` y `AffinityChanged` para lo que ya llega
por evento. Lo continuo lo empujan dos sitios que **ya están haciendo ese trabajo**:

- `PlayerBody` ya distingue Shift para elegir la velocidad: acumula dos segundos y los
  suelta en `HourPassed`.
- `PlayerInteractor.FindTarget()` ya recorre a todos los vecinos con sus posiciones en
  su cadencia lenta: contar si había alguien a menos de 8 m no añade ni una iteración.

A diferencia de la progresión de §12.5, esto **sí toca unos cinco sitios**, de una
línea cada uno. No es gratis, pero es poco y no cambia ninguna firma existente.

#### 14.3.7 Si hay que hacerlo en una tarde

**Dos ejes en vez de cuatro: Actitud y Energía.** Actitud pesa 0.35 en
`Compatibility`, casi el doble que los demás, y Energía sale gratis del Shift. Los
otros dos se quedan a cero con confianza cero, y el encogimiento de §14.3.2 los
neutraliza solo: la fórmula funciona igual. Añadir Expresión y Perspectiva después no
cambia ni una firma.

#### 14.3.8 Cómo se comprueba que está bien

1. **Dos partidas, una de 20 min/día y otra de 3 h/día haciendo lo mismo, dan la misma
   proporción.** Es el test que caza la vuelta a los contadores absolutos.

   *Corregido al implementarlo:* decía «el mismo perfil», y el perfil **sí** difiere,
   porque el encogimiento tira a cero lo que tiene pocas muestras y cinco minutos de
   juego son cinco minutos de pruebas. Lo que no puede depender de la duración es la
   proporción en crudo, y eso es lo que se comprueba (`RawAxisOf`). Que la confianza
   suba con el rato es correcto y se comprueba aparte.

   *Y una unidad que no cuadraba:* los ejes continuos llegaban en **segundos** y los
   de golpe de uno en uno, con la misma `K = 40`. Con eso la Energía quedaba decidida
   antes de cruzar el prado mientras la Expresión seguía pidiendo cuarenta
   conversaciones, y la confianza de un eje no significaba lo mismo que la del otro.
   Ahora lo continuo llega en **minutos**: cuarenta minutos andando valen lo mismo que
   cuarenta conversaciones.
2. Un eje sin muestras da exactamente 0 y confianza 0.
3. Cambiar de conducta a propósito tarda ≥ 10 días de juego en mover un eje 0.5.
4. `Compatibility.Between` con el perfil del jugador nunca sale de [−1, +1].
5. Ningún camino del código pasa el perfil del protagonista por `Compatibility.Full`
   ni lee su `TypeIndex`.

#### 14.3.9 Y de regalo, una pantalla

En el menú, junto a la Crónica: **cómo te ve la aldea**. Cuatro rasgos en palabras, no
en números, con su confianza. Nadie la ha escrito a mano y describe al jugador de
verdad. Va en el menú y no en el mundo, por lo mismo que la Crónica (§13.3).

### 14.4 Los rivales, y por qué puede no funcionar

Si al declararte ese habitante ya tiene un flechazo o está saliendo con otro
habitante, tu confesión **entra en el triángulo de §13.2** como un vértice más:

- Compites con el rival por las mismas reglas: `afinidad + compatibilidad × 20`.
- El rival reacciona: pierde afinidad contigo y acelera su propio cortejo.
- **Si la pareja de habitantes llega a prometerse antes de que tú ganes, se acabó.**
  Quedan fuera de tu alcance mientras sigan juntos.
- Si ganas, el rival te queda en `Rivalry`. Se le pasa con disculpas o mediación,
  pero mientras dure te cuesta afinidad con quien sea amigo suyo.

Esto es lo que el jugador pidió y es lo que hace que el romance importe: **hay alguien
más queriendo lo mismo, y no está esperando su turno.**

### 14.5 La boda con el protagonista

**Hecho.** Era el sitio donde el juego se paraba: podías salir con alguien y ahí se
acababa.

Tres requisitos, uno de cada mitad del juego:

| Requisito | De qué mitad viene |
|---|---|
| Salir contigo 10 días con afinidad ≥ 85 | social |
| **Anillo** crafteado (Oficio 8, material raro) | granja y oficio |
| Tu cabaña ampliada al nivel 1 | gestión |

Después, la pareja se muda a tu cabaña, hace **una acción del huerto al día** por su
cuenta (riega lo que esté seco), y sus necesidades pasan a estar parcialmente a tu
cargo. Nada de esto es obligatorio y nada caduca si no entras.

---

**Cómo quedó, y las tres cosas que hicieron falta antes.**

- **El anillo pide un material que no existía.** La geoda de nube —el nodo que más
  tarda en reponerse, diez días— pasa a soltar `mat_cristal_nimbo` en vez de piedra.
  Eso es lo que ata el romance a la isla: el anillo no se compra, se va a por él. Y de
  paso arranca la variedad de materiales, que era otro pendiente.
- **Tu cabaña no tenía niveles.** `HomeUpgradeService` solo sabía de vecinos, así que
  el tercer requisito pedía algo que no existía. Ahora `PlayerState.HomeLevel`, con los
  mismos precios y la misma obra que las de ellos, y botón dentro de casa junto a
  «Amueblar».
- **La boda no ocurre en el acto.** La aldea pone fecha a tres días, y la reserva va en
  la misma lista que las de los vecinos para que la crónica la anuncie con las mismas
  plantillas. Al planificador de la aldea hay que apartarle la tuya: su primera limpieza
  te borraba por no encontrarte en el censo, y el censo nunca te va a encontrar.

**Después**, tu pareja riega **una** casilla al día. Una sola a propósito: con el huerto
entero regado cada mañana, la regadera dejaría de tener sentido y con ella media capa de
granja. Lo que hace es que se note que ya no vives solo, no ahorrarte el trabajo. Lo de
«sus necesidades parcialmente a tu cargo» se queda para más adelante.

**Un fallo de verdad que salió al probarlo:** `SocialService` leía `_clock.Day` dentro
de la evaluación diaria en vez del día que trae el aviso `DayPassed`. Son dos fuentes
para el mismo dato y solo una es la buena. En el juego coincidían, así que no se había
notado nunca; con la boda no llegaba nunca la fecha. Afectaba igual a los triángulos y
a los flechazos.

**Lo que queda de §14:**

**§14.4 — hecho.** Al declararte a alguien que ya tenía pretendiente, el rival se
entera: te pone en `Rivalry` y pierde afinidad contigo. Y al contestarte se compara tu
puntuación con la suya —cariño más lo que os parecéis por veinte, **la misma tabla con
la que compiten dos vecinos entre ellos**— así que llegar al listón no basta y no hay
premio por haber llegado primero. Cuando el «no» es por eso, la frase lo dice y nombra
a la otra persona: soltarle el discurso de los ejes le haría cambiar de conducta
durante semanas para arreglar algo que no era el problema.

Una asimetría anotada: la rivalidad se escribe **solo en la agenda del vecino**. El
protagonista no está en el censo y no tiene agenda; lo que importa es lo que él siente
por ti, que es lo que se lee en su ficha y lo que le hace evitarte.

---

## 15. Gestión de la aldea

### 15.1 Trabajos

Ya está escrito y funciona: ocho oficios con afinidad por personalidad, rangos,
ascensos por turnos trabajados, sueldos, y oficios que solo existen si su zona está
abierta. El jugador asigna desde la ficha (`JobSection`).

Lo que falta para que sea gestión y no una lista desplegable:

- **El habitante puede negarse. Hecho.** Si la afinidad del oficio es menor de 0.3 y
  no te tiene aprecio (por debajo de Amigo), dice que no. Las dos condiciones a la vez
  y no cualquiera: a un amigo le pides un favor aunque el puesto no le guste, y un
  puesto que le encanta lo coge aunque apenas te conozca.

  *Un cuidado que hizo falta:* el reparto de puestos de salida (`EmployEveryone`) va
  por un camino aparte que no pregunta. Pasando por `Assign`, a quien no le cuadrara
  ningún oficio se le quedaría en paro para siempre —nunca te va a coger aprecio si no
  sale de casa— y la economía no cierra sin sueldos.
- **Palabras en vez de números.** La ficha ya lo dice así —«le encantaría», «no es lo
  suyo»— desde antes, y se queda: una frase se lee de un vistazo y cinco estrellas hay
  que contarlas.
- **Los ascensos los apruebas tú** (Aldea 4) y cuestan monedas de la isla. Un ascenso
  sube el sueldo del habitante y también lo que rinde.

### 15.2 Casas

`HomeUpgradeService` **está entero y no está enchufado**: dos niveles (8×8 → 11×11 →
14×14), remapeo del suelo sin tocar los muebles, cobro atómico. No lo registra
`GameBootstrap` ni lo llama ninguna UI.

- **Enchufarlo** es media hora: registrarlo y un botón en la ficha (Aldea 3).
- **Cambiar el precio de monedas a monedas + material.** Nivel 1: 1200 N◉ + 30 madera
  + 20 piedra. Nivel 2: 3500 N◉ + 80 madera + 60 piedra + 10 savia. Esto es **el
  puente más importante del juego**: es la razón por la que talar un árbol le importa
  a la aldea.
- **La comodidad ya se mide** (`ComfortScore`) y el ánimo ya afecta al trabajo: quien
  llega al turno sin energía pierde el día. Así que ampliar una casa tiene un efecto
  medible en lo que la aldea produce, sin inventar ninguna fórmula nueva.

### 15.3 Eventos

`EventScheduler` y `EventCalendar` existen y disparan solos. Lo que falta es que el
jugador pueda **poner uno en el calendario**: `Propose(eventId, day)`, pagando
monedas y material, con Aldea 5 para los pequeños y Aldea 7 para los festivales.

**Y lo que hace que organizar un evento valga la pena:** en un evento, cada pareja de
asistentes tira una vez su compatibilidad. Es donde nacen los flechazos. El jugador
no puede emparejar a nadie, pero **puede montar la fiesta donde se conozcan** — que
es exactamente la piedra en el estanque de §3.3, y la mejor herramienta social que se
le puede dar sin romper la autonomía de nadie.

La asistencia depende de la agenda y de la afinidad: quien te aprecia va aunque le
pille mal.

### 15.4 Atender necesidades

**Hecho.** Las peticiones eran un botón gratis: `Resolve(requestId)` repartía ánimo,
experiencia y monedas a cambio de un clic, sin mirar si el jugador tenía lo que le
estaban pidiendo. Con doce vecinos pidiendo cosas todo el día eso no es un bucle: es
una máquina de nimbos con forma de conversación.

Hay **tres formas de pedir**, y `RequestDemand` las distingue:

| Forma | Quién la usa | Qué cuesta |
|---|---|---|
| No cuesta objetos | consejo, favor, queja, presentación, plan, obra, confesión, paces | tu rato — y el rato ya lo pagas yendo hasta allí |
| Pide esto exacto, y tantas | `RequestKind.Material` | «tráeme 5 maderas», y solo se paga con cinco maderas |
| Vale cualquiera de su familia | comida, ropa, un mueble | uno cualquiera, y **elige el jugador** |

Lo exacto se reserva al material a propósito: la ropa y los muebles se compran en
tiendas cuyo surtido rota cada día, así que pedir una prenda concreta sería pedir algo
que la mayoría de los días no se puede conseguir. El material lo sueltan los nodos de
la isla y siempre hay dónde ir a por él — y por eso el generador elige del **catálogo
de nodos** y no del de objetos: de los treinta materiales del catálogo, la isla solo
suelta unos pocos.

Las cuatro decisiones que sostienen esto:

- **El pago sale de la mochila y de la despensa.** El jugador guarda en dos sitios sin
  haberlo decidido —lo que recoge cae en la mochila, lo que compra en la despensa— y
  mirando solo uno el vecino rechazaría la comida que acabas de comprarle. La pregunta
  «¿tengo con qué?» se contesta en un solo sitio (`OptionsFor`), o la pantalla acaba
  enseñando un botón que el servicio rechaza.
- **Nunca se elige por el jugador.** Coger «lo primero que valga» de la mochila gasta
  el plato que guardaba para otro, y eso no se deshace. Sin elección, `Resolve`
  devuelve falso.
- **Dárselo a un vecino renta más que venderlo en el cajón** (×1,35 sobre el precio,
  más la paga del favor). Si rentase menos, el encargo sería un impuesto al que se
  molesta en atenderlo y nadie volvería a leer el tablón después de hacer la cuenta
  una vez.
- **Traer algo alegra más que solo escuchar**, y acertar con lo que le encanta alegra
  más todavía. Es lo que hace que saber los gustos de cada uno sirva para algo fuera
  de los regalos.

El **tablón de la plaza** es un poste con una tabla en el borde sur, que es por donde
se entra viniendo del puente; dice de lejos cuántas cosas hay pendientes antes de
abrirlo. Y hay un botón «Encargos» en la barra **además** del tablón, igual que
«Hacer» convive con la mesa de trabajo: el tablón es donde uno mira al pasar, el botón
es para no cruzar el puente solo para comprobar que no hay nada.

No es una lista de tareas: no hay contador de completadas ni recompensa por vaciarlo,
y se enseña también lo que no puedes atender —con el aviso de qué te falta— en vez de
esconderlo. Negarse sigue costando ánimo (`Refuse`), que es lo que hace que decir sí
signifique algo.

**Lo que queda:** `RequestKind.IslandBuilding` sigue sin costar nada. Pide una mejora
para la isla, así que su precio natural es material y obra, y eso vive en
`IBuildService`.

---

## 16. Cómo se enganchan las tres mitades

El problema de hoy no es que falten sistemas: es que **las mitades no se tocan**. Ni
`Simulation`, ni `Social`, ni `Events` mencionan recolección, huerto ni crafteo. Son
dos juegos corriendo en la misma isla.

Estos son los enganches, y cada uno es la salida de una mitad usada como entrada de
otra:

| De | A | Por dónde |
|---|---|---|
| Granja y recolección | Gestión | material para ampliar casas (§15.2) |
| Granja y recolección | Social | regalos, ramos y encargos de material (§15.4) |
| Granja y oficio | Romance | el ramo y el anillo (§14) |
| Social | Gestión | te aceptan el trabajo si te aprecian (§15.1) |
| Gestión | Simulación de vida | casa mejor → más ánimo → mejor turno; casa grande → bebés (§13.1) |
| Gestión | Romance ajeno | los eventos son donde nacen los flechazos (§15.3) |
| Vida de la aldea | Gestión | cada boda y cada bebé son un vecino más al que dar trabajo |
| Todo | Protagonista | las cinco vías suben haciendo cualquiera de estas cosas (§12) |

**La regla que evita que esto se vuelva una lista de tareas:** ninguno de estos
enganches es obligatorio ni caduca. Si no amplías ninguna casa, la aldea sigue
viviendo, solo crece más despacio. Si no organizas ninguna fiesta, los flechazos
nacen igual, solo más lento. `04_ALDEA.md` §2 sigue mandando: **riqueza opcional, no
trabajo obligatorio.**

---

## 17. Alcance v1

La v1 es el juego completo que se publica. No hay "early access": se lanza
cuando todo lo de `[NÚCLEO]` está pulido y lo de `[IMPORTANTE]` está
funcional. Lo de `[SI DA TIEMPO]` puede llegar en un parche post-lanzamiento.

**Tres estados, no dos.** La v1.0 solo tenía `[x]` y `[ ]`, y eso escondía la
categoría más peligrosa del proyecto: sistemas escritos y probados que **no están
enchufados a nada**. Un servicio que nadie registra y una clase que nadie instancia
pasan los tests y no existen en el juego.

| Marca | Significa |
|---|---|
| `[x]` | Hecho y jugable |
| `[~]` | Escrito, a medias o sin enchufar. La casilla dice qué falta |
| `[ ]` | Sin empezar |

### 17.1 `[NÚCLEO]` — Sin esto no hay juego

- [x] Arquitectura base (EventBus, ServiceRegistry, guardado, reloj, RNG).
- [x] 1 isla flotante (Nimbo) con 10 zonas.
- [x] Creador de personajes completo (cuerpo, cara, voz, ropa, personalidad).
- [x] 12 isleños máximo en la isla.
- [x] Sistema de necesidades: hambre, energía, higiene y social. Ocio y vejiga se cayeron — el ánimo es derivado y no una necesidad más, y la vejiga no aporta decisiones al jugador, solo ruido.
- [x] Los 16 tipos de personalidad con comportamientos diferenciados.
- [x] Agenda diaria autónoma por isleño.
- [x] Modo construcción de vivienda (rejilla, muebles, paredes, suelos).
- [x] 40 muebles del catálogo base + pool de 200 en tienda rotatoria.
- [x] Sistema de relaciones con los 10 niveles y 9 estados.
- [x] Economía con monedas, tiendas, trabajo y balance diario.
- [x] 3 minijuegos (cocina, pesca, ritmo), con su servicio, su pantalla y un sitio
      en el mundo cada uno (§9.2). La caña se fabrica a mano.
- [x] Eventos: sucesos diarios, sueños, conciertos, noticias, festivales. Encendidos
      desde el arranque; el tablón alimenta además la Crónica (§13.3).
- [x] El Árbol Nimbo funcional.
- [x] Semana de juego estructurada (lunes a domingo con bonos).
- [x] Guardado y carga de partida (JSON versionado, copia atómica).
- [x] UI: HUD, ficha, tienda, creador, construcción y menú principal (con pausa y ajustes).
- [x] Sonido: voces sintetizadas, efectos y ambiente generativo, y **la música cambia
      con lo que pasa**. Tres humores y no más —calma, noche y fiesta—, porque cada uno
      tiene que reconocerse *sin mirar la pantalla*, y con seis matices ninguno lo es.
      La noche entra a las 21 y sale a las 6; la fiesta le gana a la hora, que es el
      caso normal: los conciertos empiezan a las cinco y acaban de noche, y un fondo
      que se apagara a mitad del concierto parecería roto.

      Tres decisiones que no son obvias y conviene no deshacer:

      - **Los tres salen de la misma escala y de la misma semilla.** Lo que cambia es el
        paso, la octava y el brillo. Durante los dos segundos y medio del cruce se oyen
        los dos a la vez: con tonalidades distintas, ese cruce sonaría a error. Y con la
        misma semilla, el fondo de noche es *tu* fondo de noche —la misma sucesión de
        acordes que reconoces de día, tocada de otra manera.
      - **Dos fuentes de audio, no una.** Cambiar el clip de una sola fuente es un
        silencio de un frame, y un silencio en el fondo se oye como un fallo aunque dure
        nada. El intercambio se hace al **empezar** el cruce, no al acabarlo, para que
        una fiesta que arranque justo en el amanecer no devuelva la saliente de golpe al
        volumen entero.
      - **Los tres fondos se sintetizan al cargar la partida**, no cuando toca cada uno.
        Son setecientos mil senos por clip: un frame perdido. Ahí no se ve; al empezar la
        fiesta se vería justo cuando el jugador está mirando.

      Para que el sonido pudiera enterarse hubo que sacar el aviso del módulo: el
      calendario solo avisaba con eventos de C#, y a esos únicamente se les escucha desde
      dentro de `Nimbo.Events`. Ahora `EventScheduler` publica además `VillageEventStarted`
      y `VillageEventEnded` en el bus, que es lo que ven `Nimbo.Art` y `Nimbo.UI`.

      Los tests dicen que la noche cruza menos veces por cero y que la fiesta cruza más,
      y eso es todo lo que un test puede decir de una música. Para lo otro está
      `CapturaMusica`, que saca los tres bucles a `Capturas/musica_*.wav`:

      ```
      unity -runTests -testPlatform PlayMode -testFilter CapturaMusica
      ```

**La capa de granja y oficio** (§1, pilar 3; `04_ALDEA.md` §5). No estaba en la lista
de la v1.0 y es la mitad del juego de hoy:

- [x] Protagonista con cuerpo, movimiento, vigor y cámara que le sigue.
- [x] Mochila con huecos, herramientas y objeto en mano.
- [x] Recolección: 16 tipos de nodo, 120 en el mundo, herramienta requerida, reposición.
- [x] Huerto: labrar, sembrar, regar, crecer, recoger. 12 cultivos.
- [x] Crafteo: 36 recetas y mesas de trabajo.
- [x] Venta por el cajón de envíos.
- [x] Casa propia con interior amueblable, y la aldea al otro lado del puente.
- [x] **Progresión del protagonista: las cinco vías, la curva y los desbloqueos (§12).**
      El motor entero y diecisiete puertas. Lo que queda de la tabla de §12.3 está
      marcado allí: pide contenido que no existe, o sistemas de aldea sin escribir.
- [x] Herramientas de nivel 2 crafteables (§12.4). Cada una se come la de siempre.
- [x] Variedad de materiales: 16 nodos dan **12** materiales, todos con fuente y con
      uso. Los cinco nuevos arreglan cinco sinsentidos: un helecho que soltaba conchas,
      un arbusto de bayas que soltaba fibra, dos flores idénticas, cuatro árboles
      idénticos y seis recetas de cocina hechas con fibra vegetal. `MaterialesTests`
      protege las dos reglas simétricas — ninguno sin fuente, ninguno sin uso.
- [x] Economía del huerto equilibrada, y las semillas se compran.

      *La nota que había aquí estaba mal.* Decía que vender paga el 100% del catálogo
      y que comprar y vender es neutro. No lo es: el cajón solo compra materiales y
      cosechas, y ninguna tienda los vende, así que ese ciclo no existe. Al medirlo
      salieron dos cosas peores:

      - **Las semillas no se podían comprar en ninguna parte.** Ninguna tienda declara
        `ItemCategory.Seed`. Se arrancaba con ocho de un solo cultivo y, al acabarse,
        el huerto se quedaba en tierra labrada para siempre. Media capa de granja
        colgaba de un regalo de bienvenida.
      - **Un cultivo rentaba cinco veces más que otro** (74 nimbos al día contra 14).
        El huerto no era una decisión: había una respuesta correcta y once
        equivocadas, que es justo la optimización que este proyecto evita.

      Ahora el reparto va de 21,5 a 29 al día —los que rebrotan medidos por su ritmo
      **sostenido**, que es donde se escondía el dominante— y la parcela de salida da
      unos 300 al día, dentro de la banda de §7.4. Las semillas se venden en NimboMart
      y en el mercado flotante, y el surtido garantiza **una de cada familia** que la
      tienda declara: con sorteo plano, doce semillas contra cuarenta y cinco comidas
      salían dos días de cada tres sin ninguna.

      Los números viven en `catalogo_materiales.json` y los fija `EconomiaHuertoTests`
      contra la banda del diseño, no contra cifras concretas: lo que se protege es la
      forma de la economía.

**El pilar de vida y gestión** (§13, §14, §15):

- [x] Romance autónomo entre habitantes: flechazo, correspondido, salir, prometerse,
      desamor y ruptura, todo sin el jugador.
- [x] **Bodas y bebés autónomos** (§13.1). `WeddingPlanner` revisa las parejas cada
      día: a los 5 días de prometidos pone fecha y lo anuncia, 3 días después los casa
      llamando a `TryMarry`, y a los 10 días de casados llega un bebé si la casa está
      ampliada. Revisa la lista en vez de escuchar el aviso, y eso es a propósito: así
      recoge también a las parejas que ya llevaban semanas congeladas en partidas
      guardadas. 13 tests en `BodasTests`.
- [x] Rivales y triángulos amorosos entre habitantes (§13.2).
- [x] **La Crónica** (§13.3). Botón «Crónica» en la barra, lo más reciente arriba,
      agrupado por días con «Hoy» y «Ayer» en palabras. Se guarda en la partida —lo que
      se quiere leer al volver es lo que pasó mientras no estabas— con tope de 150
      líneas, y guarda el texto ya escrito y no los identificadores, para que la línea de
      un vecino que se fue no salga en blanco. Lo escribe el `NewsBoard`, que ya tenía
      las plantillas.
- [x] Cortejo y boda del protagonista (§14). El rechazo —ramo, respuesta al día
      siguiente, frase que nombra el eje, espera de diez días—, la boda —diez días
      saliendo, anillo de cristal de nimbo y tu cabaña ampliada, con fecha puesta por
      la aldea y tu pareja regando una casilla al día— y los rivales: declararte a
      alguien que ya tiene pretendiente te mete en el triángulo de §13.2 como un
      vértice más, y al contestarte se compara tu puntuación con la suya con la misma
      tabla (§14.4).
- [x] Los cuatro ejes del protagonista deducidos de su conducta (§14.3), con su
      pantalla de «cómo te ve la aldea».
- [x] Trabajos: 8 oficios, afinidad por personalidad, rangos, sueldos, asignación.
- [~] Que un habitante pueda negarse a un trabajo (§15.1). La regla está escrita,
      enchufada y probada —si el oficio no le pega **y además** no te tiene aprecio,
      dice que no— pero **el listón está donde no llega nadie**. `MinJobAffinity` vale
      0,30 y la afinidad es `1 − distancia`: con los dieciséis arquetipos canónicos a
      ±0,75, el oficio que peor le puede caer a alguien saca **0,350** (Músico y Guía).
      Ni un solo tipo de personalidad baja del listón con ningún oficio. Midiendo con
      los isleños de verdad, que salen con cada eje en ±[0,35 · 1], **el 0,22 % tendría
      algún oficio por debajo**: con doce vecinos, dos de cada cien partidas ven un «no»
      alguna vez.

      Lo delató un test que se saltaba a sí mismo. `GestionAldeaTests` buscaba el peor
      oficio de una Bea sorteada al azar y, si no lo encontraba, hacía `Assert.Ignore`;
      al hacerlo exhaustivo —los ocho oficios contra los dieciséis arquetipos— resultó
      que la peor pareja posible tampoco baja de 0,30. Un test que unas veces comprueba
      y otras no, no comprueba.

      **Es un número, no código.** El barrido dice que a 0,50 el 61 % de los vecinos
      rechazaría algún oficio, el 13,7 % de los pares se cae, y **nadie se queda sin
      ningún sí** — que es la condición que no se puede romper, porque un vecino sin
      oficio posible no cobra nunca. Queda decidir el valor.
- [x] **Ampliación de casas, cobrada en obra** (§15.2). `HomeUpgradeService` registrado
      en `GameBootstrap` y con su bloque en la ficha del habitante, que dice qué falta
      cuando falta. Nivel 1: 1200 N◉ + 30 madera + 20 piedra. Nivel 2: 3500 N◉ + 80
      madera + 60 piedra + 10 savia. Todo o nada: si falta un solo material no se gasta
      ninguno ni se cobran las monedas.
- [x] **Que el jugador pueda organizar eventos** (§15.3). Botón «Fiestas» en la barra,
      con todas las del calendario a la vista —incluidas las que no puedes pagar o
      desbloquear todavía, y con el motivo escrito—, 300 nimbos las pequeñas y 1200 los
      festivales, una cada vez. De cada evento sale como mucho **un** flechazo, el de la
      pareja libre que mejor pega, y sale de un solo lado: montar la fiesta es poner el
      sitio donde la gente coincide, no emparejar a nadie.

      **Al hacerlo salió una variante nueva de la enfermedad de siempre**, y esta es la
      peor de todas: el enchufe estaba puesto **en un agujero que no existe**. Tres
      eventos —concierto, festival de la isla y concurso de talentos— pedían las zonas
      `stage` y `plaza_central`, que no son los nombres del plano (`zona_escenario` y
      `zona_plaza`). `IsUnlocked` decía que no para siempre, así que esos tres eventos
      **no podían ocurrir nunca**, ni sorteados ni pagados — y con el concierto se caía
      el minijuego de ritmo, que solo se puede jugar si hay uno en marcha. Lo tapaba que
      el único test que abría la zona a mano la abría con el nombre equivocado también.
      Ahora hay un test que recorre el calendario contra el plano y exige que toda zona
      pedida exista.
- [x] Peticiones que cuestan lo que piden (§15.4): material exacto, o cualquiera de su
      familia eligiendo tú. Se cobra de la mochila y de la despensa.
- [x] **`EventsService` encendido.** Sucesos diarios, sueños, conciertos, noticias y
      festivales estaban escritos, probados y apagados —el juego no los construía— igual
      que las ampliaciones de casa. Ya arranca con la partida y el reloj.
- [x] Al enchufarlo salió un fallo que ningún test veía: **casi todos los cambios de
      relación se publican por los dos lados**, así que cada boda y cada riña salían
      contadas dos veces, con dos plantillas distintas. El tablón filtra ahora los
      repetidos por pareja y etapa — menos los flechazos, que sí son direccionales: que
      a Ana le guste Leo y que a Leo le guste Ana son dos noticias, y en eso está la
      gracia.

### 17.2 `[IMPORTANTE]` — El juego cojea sin esto

- [x] 8 prendas base + pool de 150 en tienda de ropa rotatoria.
- [x] 40 peinados: 32 de salida y 8 que se ganan subiendo el nivel de isla.
- [ ] Conjuntos de muebles temáticos (rústico, moderno, japonés).
- [ ] Eventos de "visita misteriosa" y "expedición".
- [x] Sistema de logros: 44 logros con recompensa en nimbos, pantalla y aviso.
- [x] Ampliación de apartamento: 2 niveles (8×8 → 11×11 → 14×14), pagada en monedas y
      en material recogido.
- [ ] El Espejo de introspección (reajuste de personalidad).
- [ ] Sueños con escenas visuales (no solo texto).
- [ ] Sala de la fama (isleños nivel 50).
- [x] Personalización de la isla: 32 adornos con plano cenital por zona.
- [x] Tablón de encargos en la plaza (§15.4), y botón «Encargos» en la barra.
- [x] `RequestKind.Material`: peticiones que se pagan con lo recogido (§15.4).
- [x] Mediar en una riña (Convivencia 9). Baja un escalón, no la borra.
- [x] Pedir un favor a un vecino (Convivencia 7). Uno al día, y cuesta un poco de
      aprecio.

### 17.3 `[SI DA TIEMPO]` — El juego no lo necesita para ser bueno

- [ ] Segunda isla flotante (tropical).
- [ ] Mascotas (Nimbín).
- [ ] Clima y estaciones visuales.
- [ ] Profesiones avanzadas con minijuegos únicos.
- [ ] Editor de patrones para ropa.
- [ ] Puentes de nubes entre islas (viaje de isleños).
- [ ] Álbum de fotos (capturas dentro del juego con poses).
- [ ] Modo "calma" (solo observar, sin intervenir, ritmo aún más lento).
- [ ] Integración con Steam Workshop para compartir isleños.

---

## 18. Por dónde empezar

El orden no es por tamaño: es por cuánto juego aparece por cada hora de trabajo. Las
cuatro primeras son código que ya existe y solo hay que conectar.

| # | Qué | Coste | Qué aparece |
|---|---|---|---|
| ~~1~~ | ~~Enchufar `HomeUpgradeService` y cobrarlo en material (§15.2)~~ | **hecho** | el puente entre la granja y la aldea |
| ~~2~~ | ~~Bodas y bebés por el calendario: `WeddingPlanner` (§13.1)~~ | **hecho** | prometidos deja de ser un callejón; la aldea crece sola |
| ~~3~~ | ~~La Crónica en el menú, y `EventsService` encendido (§13.3)~~ | **hecho** | la aldea deja de vivir a ciegas |
| ~~4~~ | ~~`Resolve` que consume el payload + `RequestKind.Material` y el tablón (§15.4)~~ | **hecho** | recolectar tiene un porqué social |
| ~~5~~ | ~~Enchufar los tres minijuegos (§9.2)~~ | **hecho** | tres verbos escritos y apagados |
| ~~6~~ | ~~Las cinco vías y sus desbloqueos (§12)~~ | **hecho** (el motor y catorce puertas; ver §12.3) | la progresión entera |
| ~~7~~ | ~~Rivales y triángulos (§13.2)~~ | **hecho** | las historias que el jugador va a contar |
| ~~8~~ | ~~Cortejo del protagonista con rechazo (§14)~~ | **hecho** (§14.1–§14.3; ver el final de §14.5) | el pilar romántico |

**La regla que salió de hacer los tres primeros.** Al cerrar el punto 2 quedó claro que
una boda que el jugador no puede percibir no está entregada, y por eso la Crónica subió
del puesto 7 al 3. Cuatro sistemas de este proyecto estaban escritos, probados y
apagados: las ampliaciones de casa, las bodas, el módulo de eventos y los minijuegos.
Ninguno lo delataba un test.

Así que de ahora en adelante, **un sistema no está hecho hasta que hay una prueba que
carga la isla de verdad y comprueba que el jugador tiene por dónde llegar a él**. Eso es
`CronicaEnLaIslaTests`: mira que el servicio esté registrado y que el botón exista en la
barra. Cuesta veinte líneas y es la única clase de prueba que habría cazado los cuatro.

El punto 4 añadió una variante de la misma enfermedad, y conviene tenerla escrita:
**las peticiones no estaban apagadas, estaban enterradas.** Funcionaban, se generaban,
caducaban y restaban ánimo — y solo se podían leer entrando en la ficha de cada vecino,
de uno en uno. Con doce vecinos eso son doce pantallas para averiguar si hay algo que
hacer, y ese recorrido no lo hace nadie dos veces. Un sistema encendido con una forma de
mirarlo que nadie usaría está tan apagado como el que no arranca.

Y §15.3 añadió las dos últimas, que son las peores porque **ningún test las ve como
fallo**:

- **El enchufe puesto en un agujero que no existe.** Tres eventos pedían zonas con
  nombres que no están en el plano, así que la condición para que ocurrieran era falsa
  para siempre. No hay excepción, no hay aviso: simplemente no pasan. Se caza
  comprobando que **toda referencia por nombre existe en el otro lado** —los ids de
  zona, los de receta, los de objeto—, y eso son cuatro líneas por catálogo.
- **El listón puesto donde no llega nadie.** La regla de negarse a un trabajo funciona,
  pero su umbral está por debajo del mínimo que el sistema puede producir, así que
  nunca se cumple. Se caza midiendo el rango real —el peor caso posible contra el
  umbral— en vez de probar un caso concreto y darlo por bueno.

---

> **Fin del GDD v2.0.** Este documento lo escribe el agente de diseño y lo
> aprueba el orquestador. Los números son puntos de partida; se ajustan con
> datos de playtest.
