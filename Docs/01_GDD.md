# Isla Nimbo — Documento de Diseño de Juego (GDD)

> **Versión:** 1.0 — 2026-08-08
> **Motor:** Unity 6000.5.5f1, URP
> **Arquitectura:** `Docs/00_ARQUITECTURA.md` manda sobre este documento.

---

## 1. Visión

**Frase:** *Isla Nimbo* es un simulador social de islas flotantes donde creas
habitantes, los observas vivir sus vidas y moldeas su mundo sin controlarlos
directamente.

**Pilar de fantasía:** Tus habitantes tienen voluntad propia. Tú construyes el
escenario —la isla, sus casas, las reglas— pero ellos deciden a quién quieren, de
qué se ríen y qué sueñan. Eres el arquitecto de un mundo que respira sin ti.

**Público objetivo:** Jugadores de 16 a 40 años que disfrutan los simuladores
sociales, la gestión relajada y las narrativas emergentes. No requiere reflejos,
inglés ni experiencia previa con el género.

**Plataforma:** PC (Steam), mando y teclado/ratón. Una sola pantalla, sin online.

---

## 2. Bucle de juego

### 2.1 Bucle de sesión (2–5 minutos)

Cada sesión corta es una "visita a la isla":

1. **Llegada (15 s):** la cámara vuela desde las nubes hasta la isla. Ves a tus
   habitantes en sus cosas. Si algo urgente pasó mientras no estabas, un globo de
   aviso lo indica.
2. **Observar e intervenir (1–3 min):** recorres la isla, pinchas en habitantes
   para ver sus pensamientos, sus necesidades (hambre 23%, energía 78%) y sus
   relaciones. Puedes:
   - Dar un objeto del inventario (comida, regalo, herramienta).
   - Resolver un "suceso" — un habitante te pide consejo, dos discuten, alguien
     encontró algo raro.
   - Editar una vivienda (modo construcción).
   - Visitar una tienda y comprar.
   - Jugar un minijuego (cocina, pesca, música).
3. **Salida (15 s):** resumen de lo que cambió desde la última visita. Ni
   obligatorio ni largo: dos frases y un botón de "volver".

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
- **No elige** de quién se enamoran, aunque puede influir con regalos y eventos.
- **No fuerza** amistades ni reconcilia peleas directamente.
- **No decide** la profesión ni la rutina. Sugiere, no ordena.

El jugador es el arquitecto de la isla y un observador privilegiado que puede
**soltar piedras en el estanque** — el dónde y el tamaño de las ondas lo decide
el sistema.

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
- No hay relaciones jugador-isleño románticas. El jugador es una entidad
  externa, no un isleño.

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
| Minijuego | Cuando el jugador quiere | 1 isleño | Cocina (puzle de ingredientes), Pesca (timing), Música (ritmo) |
| Festival | 1 cada 15–20 días de juego | Todos | "Noche de estrellas fugaces", "Festival de la cosecha Nimbo", "Torneo de cocina" |
| Visita misteriosa | 1 cada 7–10 días | Todos | Un viajero de otra isla llega con objetos raros e historias |
| Expedición | Cuando se desbloquea | 2–4 isleños | Explorar una nube densa y encontrar objetos únicos |

### 9.2 Minijuegos concretos

1. **Cocina:** el jugador elige ingredientes (máx. 4) y los coloca en un
   tablero 3×3. Cada receta pide una disposición concreta. Acierto = plato
   cocinado (mejor efecto que ingredientes sueltos). Fallo = "engrudo" (efecto
   mínimo).
2. **Pesca en las nubes:** el isleño lanza una caña al borde de la isla. El
   jugador pulsa en el momento justo cuando el flotador se hunde (timing).
   Peces raros valen más monedas.
3. **Ritmo:** los isleños bailan o tocan en el escenario. El jugador pulsa
   botones al ritmo de una melodía (4 carriles, estilo Taiko simplificado).

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
- **Nada de fallar:** los minijuegos dan recompensa incluso si lo haces mal
  (fallar cocina = "engrudo comestible", no "pierdes los ingredientes"). La
  puntuación alta da recompensa extra, no es requisito.
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

## 12. Alcance v1

La v1 es el juego completo que se publica. No hay "early access": se lanza
cuando todo lo de `[NÚCLEO]` está pulido y lo de `[IMPORTANTE]` está
funcional. Lo de `[SI DA TIEMPO]` puede llegar en un parche post-lanzamiento.

### 12.1 `[NÚCLEO]` — Sin esto no hay juego

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
- [x] 3 minijuegos (cocina, pesca, ritmo).
- [x] Eventos: sucesos diarios, sueños, conciertos, noticias, festivales.
- [x] El Árbol Nimbo funcional.
- [x] Semana de juego estructurada (lunes a domingo con bonos).
- [x] Guardado y carga de partida (JSON versionado, copia atómica).
- [x] UI: HUD, ficha, tienda, creador, construcción y menú principal (con pausa y ajustes).
- [~] Sonido: voces sintetizadas, efectos y ambiente generativo están. La música
      todavía no cambia con lo que pasa: suena el mismo ambiente en calma y en
      fiesta, y eso es lo que falta para poder marcarlo.

### 12.2 `[IMPORTANTE]` — El juego cojea sin esto

- [x] 8 prendas base + pool de 150 en tienda de ropa rotatoria.
- [x] 40 peinados: 32 de salida y 8 que se ganan subiendo el nivel de isla.
- [ ] Conjuntos de muebles temáticos (rústico, moderno, japonés).
- [ ] Eventos de "visita misteriosa" y "expedición".
- [x] Sistema de logros: 44 logros con recompensa en nimbos, pantalla y aviso.
- [x] Ampliación de apartamento: 2 niveles (8×8 → 11×11 → 14×14).
- [ ] El Espejo de introspección (reajuste de personalidad).
- [ ] Sueños con escenas visuales (no solo texto).
- [ ] Sala de la fama (isleños nivel 50).
- [x] Personalización de la isla: 32 adornos con plano cenital por zona.

### 12.3 `[SI DA TIEMPO]` — El juego no lo necesita para ser bueno

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

> **Fin del GDD v1.0.** Este documento lo escribe el agente de diseño y lo
> aprueba el orquestador. Los números son puntos de partida; se ajustan con
> datos de playtest.
