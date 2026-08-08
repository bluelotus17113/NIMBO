# Isla Nimbo — Las 16 personalidades

> Generado desde `Docs/Contratos/personalidades.json` por
> `Docs/Agentes/generar-doc-personalidades.py`. **No editar a mano**: se edita el
> JSON y se vuelve a generar, que es lo que mantiene a los dos diciendo lo mismo.

## Cómo funciona

Cuatro ejes continuos en `[-1, 1]`. El signo de los cuatro da uno de dieciséis
tipos, y el tipo no se guarda nunca: se deriva. Así el creador de personajes solo
mueve deslizadores y el tipo cae solo.

| Eje | −1 | +1 | Bit |
|---|---|---|---|
| `Energy` | Calmado | Enérgico | 1 |
| `Expression` | Reservado | Expresivo | 2 |
| `Attitude` | Independiente | Sociable | 4 |
| `Outlook` | Práctico | Soñador | 8 |

```
index = (Energy>0 ? 1:0) | (Expression>0 ? 2:0) | (Attitude>0 ? 4:0) | (Outlook>0 ? 8:0)
```

Un habitante con los cuatro ejes cerca de cero es de su tipo «a medias» y se
comporta de forma más neutra: la intensidad modula, el tipo clasifica.

## Tabla resumen

| # | Tipo | Ejes | Frase |
|---|---|---|---|
| 0 | **Ermitaño** | −E −E −A −O | *La soledad no me pesa; me aligera.* |
| 1 | **Atleta** | +E −E −A −O | *Un objetivo, un cuerpo, ningún atajo.* |
| 2 | **Artesano** | −E +E −A −O | *Cada cosa rota merece una segunda vida.* |
| 3 | **Audaz** | +E +E −A −O | *El riesgo no se calcula: se prueba.* |
| 4 | **Afable** | −E −E +A −O | *Si alguien tiene que ceder, que empiece por mí.* |
| 5 | **Líder** | +E −E +A −O | *Apunta alto, habla claro, no dejes a nadie atrás.* |
| 6 | **Anfitrión** | −E +E +A −O | *Mi casa es tuya, y tu historia también.* |
| 7 | **Fiestero** | +E +E +A −O | *Si no hay música, la pongo; si no hay baile, lo invento.* |
| 8 | **Poeta** | −E −E −A +O | *Las palabras que no digo son las que más pesan.* |
| 9 | **Visionario** | +E −E −A +O | *Lo que otros ven imposible yo lo estoy construyendo.* |
| 10 | **Artista** | −E +E −A +O | *Lo que siento no cabe en palabras; por eso pinto.* |
| 11 | **Genio** | +E +E −A +O | *Las reglas son para los que no pueden reescribirlas.* |
| 12 | **Romántico** | −E −E +A +O | *El mundo está bien, pero podría estar mejor con dos.* |
| 13 | **Explorador** | +E −E +A +O | *El mapa se acaba donde empieza mi curiosidad.* |
| 14 | **Cuentista** | −E +E +A +O | *Cada persona es una historia; yo solo la leo en voz alta.* |
| 15 | **Entusiasta** | +E +E +A +O | *Cada día es un regalo, ¡y pienso desenvolverlo!* |

## Ficha de cada tipo

### 0. Ermitaño  

*«La soledad no me pesa; me aligera.»*

| | |
|---|---|
| Identificador | `PT_ERMITANIO` |
| Ejes | calmado, reservado, independiente, práctico |
| Al andar | ×0.7, se para 4.5 s entre destinos |
| Voz | tono grave, ritmo lento |
| Emociones | serenidad, introspección, desconfianza |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 0.9 | 1.2 | 0.7 | 0.7 | 0.9 |

**Qué pide** — `item` ×1.5, `food` ×1.0, `complaint` ×1.0.  
**Qué casi nunca pide** — `advice` ×0.3, `clothing` ×0.5.

**Congenia con** — Afable (+3), Artesano (+5), Poeta (+7).  
**Choca con** — Entusiasta (-4), Fiestero (-6).

**Frases**

- *Contento:* «Así, en silencio… está bien.»
- *Contento:* «Hoy el ruido no me encontró. Buen día.»
- *Aburrido:* «Ya me miraron demasiado. Hora de irme.»
- *Aburrido:* «¿Falta mucho? Tengo un rincón esperándome.»
- *Enfadado:* «Te dije que no me tocaras las cosas.»
- *Enfadado:* «No necesito explicaciones. Necesito distancia.»
- *Al conocer a alguien:* «…podemos conversar. Despacio.»
- *Al conocer a alguien:* «No esperes que te cuente mi vida en un minuto.»

### 1. Atleta  

*«Un objetivo, un cuerpo, ningún atajo.»*

| | |
|---|---|
| Identificador | `PT_ATLETA` |
| Ejes | enérgico, reservado, independiente, práctico |
| Al andar | ×1.3, se para 1.2 s entre destinos |
| Voz | tono medio, ritmo rapido |
| Emociones | determinación, impaciencia, orgullo |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.2 | 1.0 | 1.4 | 0.7 | 1.1 |

**Qué pide** — `activity` ×1.5, `food` ×1.4, `item` ×1.2.  
**Qué casi nunca pide** — `advice` ×0.4, `clothing` ×0.5.

**Congenia con** — Audaz (+6), Líder (+7), Visionario (+4).  
**Choca con** — Cuentista (-4), Romántico (-3).

**Frases**

- *Contento:* «¿Viste mi marca? La rompí yo solo.»
- *Contento:* «Hoy el cuerpo respondió. Eso vale.»
- *Aburrido:* «Esto no entrena nada. Me voy a correr.»
- *Aburrido:* «¿Cuánto más hay que esperar? Tengo series.»
- *Enfadado:* «No me digas que no puedo. Eso me toca a mí decidirlo.»
- *Enfadado:* «Baja el tono o lo arreglamos en la pista.»
- *Al conocer a alguien:* «¿Qué entrenas? Así sé con quién hablo.»
- *Al conocer a alguien:* «No doy muchas vueltas. Si quieres algo, dilo.»

### 2. Artesano  

*«Cada cosa rota merece una segunda vida.»*

| | |
|---|---|
| Identificador | `PT_ARTESANO` |
| Ejes | calmado, expresivo, independiente, práctico |
| Al andar | ×0.8, se para 3.0 s entre destinos |
| Voz | tono medio, ritmo normal |
| Emociones | concentración, satisfacción, melancolía |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 0.9 | 1.0 | 0.8 | 0.8 | 1.0 |

**Qué pide** — `item` ×1.8, `favor` ×1.0, `confession` ×1.0.  
**Qué casi nunca pide** — `complaint` ×0.4, `socialIntro` ×0.5.

**Congenia con** — Anfitrión (+5), Artista (+7), Ermitaño (+5).  
**Choca con** — Explorador (-3), Líder (-4).

**Frases**

- *Contento:* «Quedó justo como lo imaginé. Mira.»
- *Contento:* «Hoy las manos no me temblaron. Día redondo.»
- *Aburrido:* «Si no hay nada que arreglar, me lo invento.»
- *Aburrido:* «Mis herramientas llevan dos horas quietas. Eso es raro.»
- *Enfadado:* «Eso no se rompió solo. Y yo no miento.»
- *Enfadado:* «No me toques el taller. Último aviso.»
- *Al conocer a alguien:* «¿Tienes algo que necesite arreglo? Así empezamos.»
- *Al conocer a alguien:* «Cuéntame mientras trabajo. No me molesta.»

### 3. Audaz  

*«El riesgo no se calcula: se prueba.»*

| | |
|---|---|
| Identificador | `PT_AUDAZ` |
| Ejes | enérgico, expresivo, independiente, práctico |
| Al andar | ×1.2, se para 1.0 s entre destinos |
| Voz | tono medio, ritmo rapido |
| Emociones | euforia, desafío, impaciencia |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.0 | 1.1 | 1.3 | 0.7 | 1.0 |

**Qué pide** — `activity` ×1.5, `favor` ×1.4, `food` ×1.2.  
**Qué casi nunca pide** — `advice` ×0.3, `clothing` ×0.4.

**Congenia con** — Atleta (+6), Fiestero (+5), Genio (+7).  
**Choca con** — Poeta (-3), Romántico (-4).

**Frases**

- *Contento:* «¡Lo hice! Y parecía imposible. Mejor así.»
- *Contento:* «Ese subidón no se compra. Se salta y ya.»
- *Aburrido:* «Esto es demasiado seguro. ¿Dónde está el filo?»
- *Aburrido:* «Si nadie va a moverse, me muevo yo solo.»
- *Enfadado:* «¿Miedo? Guárdatelo. Yo no freno por eso.»
- *Enfadado:* «No me digas que espere. La ventana se cierra.»
- *Al conocer a alguien:* «¿De qué eres capaz? Cuéntame algo que asuste.»
- *Al conocer a alguien:* «A ver, dime lo más loco que has hecho.»

### 4. Afable  

*«Si alguien tiene que ceder, que empiece por mí.»*

| | |
|---|---|
| Identificador | `PT_AFABLE` |
| Ejes | calmado, reservado, sociable, práctico |
| Al andar | ×0.8, se para 3.5 s entre destinos |
| Voz | tono medio, ritmo lento |
| Emociones | serenidad, empatía, preocupación |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 0.9 | 0.8 | 0.8 | 1.2 | 1.0 |

**Qué pide** — `favor` ×1.6, `socialIntro` ×1.5, `reconcile` ×1.5.  
**Qué casi nunca pide** — `complaint` ×0.2, `activity` ×0.5.

**Congenia con** — Anfitrión (+5), Ermitaño (+3), Romántico (+6).  
**Choca con** — Genio (-4), Visionario (-3).

**Frases**

- *Contento:* «Todos tan contentos… hoy dormiré tranquilo.»
- *Contento:* «Pude ayudar. Eso ya hace que valga el día.»
- *Aburrido:* «Nadie necesita nada. Qué raro se siente.»
- *Aburrido:* «Mientras no haya prisa, me tomo otro té.»
- *Enfadado:* «No confundas mi calma con que no me importa.»
- *Enfadado:* «Me empujaste tres veces. A la cuarta respondo.»
- *Al conocer a alguien:* «¿Estás bien? Tienes cara de que necesitas hablar.»
- *Al conocer a alguien:* «Ven, siéntate. No muerdo y sobra silla.»

### 5. Líder  

*«Apunta alto, habla claro, no dejes a nadie atrás.»*

| | |
|---|---|
| Identificador | `PT_LIDER` |
| Ejes | enérgico, reservado, sociable, práctico |
| Al andar | ×1.2, se para 1.5 s entre destinos |
| Voz | tono grave, ritmo normal |
| Emociones | determinación, frustración, orgullo |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.2 | 0.9 | 1.2 | 1.3 | 1.2 |

**Qué pide** — `advice` ×1.6, `socialIntro` ×1.5, `activity` ×1.5.  
**Qué casi nunca pide** — `confession` ×0.5, `islandBuilding` ×0.6.

**Congenia con** — Atleta (+7), Explorador (+5), Fiestero (+6).  
**Choca con** — Artesano (-4), Artista (-5).

**Frases**

- *Contento:* «El equipo funcionó. Eso es lo que importa.»
- *Contento:* «Hoy todo salió según el plan. Mañana, mejor.»
- *Aburrido:* «Sin objetivos claros, esto se desmorona.»
- *Aburrido:* «Alguien tiene que poner orden. Como siempre.»
- *Enfadado:* «Te di una responsabilidad y la tiraste.»
- *Enfadado:* «No grites. Di lo que hay que arreglar y arréglalo.»
- *Al conocer a alguien:* «Cuéntame en qué eres bueno. Quizá te necesite.»
- *Al conocer a alguien:* «Bienvenido. Aquí cada uno tiene un puesto.»

### 6. Anfitrión  

*«Mi casa es tuya, y tu historia también.»*

| | |
|---|---|
| Identificador | `PT_ANFITRION` |
| Ejes | calmado, expresivo, sociable, práctico |
| Al andar | ×0.9, se para 2.0 s entre destinos |
| Voz | tono medio, ritmo normal |
| Emociones | calidez, curiosidad, decepción |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.0 | 0.8 | 0.9 | 1.3 | 1.1 |

**Qué pide** — `socialIntro` ×1.5, `reconcile` ×1.5, `food` ×1.4.  
**Qué casi nunca pide** — `complaint` ×0.3, `activity` ×0.5.

**Congenia con** — Afable (+5), Artesano (+5), Cuentista (+6).  
**Choca con** — Artista (-3), Genio (-3).

**Frases**

- *Contento:* «La mesa llena, las risas… así se vive.»
- *Contento:* «Vinieron todos. No sabes lo que eso significa.»
- *Aburrido:* «La casa está muy callada. Hay que invitar a alguien.»
- *Aburrido:* «Sin gente alrededor, la comida no sabe igual.»
- *Enfadado:* «En mi mesa no se falta al respeto. Punto.»
- *Enfadado:* «Te abrí la puerta y me la cerraste en la cara.»
- *Al conocer a alguien:* «¡Pasa, pasa! Justo iba a preparar algo.»
- *Al conocer a alguien:* «Cuéntame de dónde vienes. Me encantan las historias nuevas.»

### 7. Fiestero  

*«Si no hay música, la pongo; si no hay baile, lo invento.»*

| | |
|---|---|
| Identificador | `PT_FIESTERO` |
| Ejes | enérgico, expresivo, sociable, práctico |
| Al andar | ×1.3, se para 1.0 s entre destinos |
| Voz | tono agudo, ritmo rapido |
| Emociones | euforia, sorpresa, frustración |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.2 | 0.7 | 1.4 | 1.4 | 1.0 |

**Qué pide** — `socialIntro` ×1.5, `activity` ×1.5, `food` ×1.3.  
**Qué casi nunca pide** — `advice` ×0.5, `complaint` ×0.5.

**Congenia con** — Audaz (+5), Entusiasta (+7), Líder (+6).  
**Choca con** — Ermitaño (-6), Visionario (-4).

**Frases**

- *Contento:* «¡Esto está que arde! ¡Y lo que falta!»
- *Contento:* «No sé qué celebramos, ¡pero celébralo conmigo!»
- *Aburrido:* «¿Nadie se mueve? Esto es un funeral.»
- *Aburrido:* «Tres minutos sin ruido. Mi récord personal.»
- *Enfadado:* «¡No me cortes el rollo! Estábamos en lo mejor.»
- *Enfadado:* «Si vienes a aguar la fiesta, la puerta está allí.»
- *Al conocer a alguien:* «¡Otro más! Cuéntame rápido: ¿sabes bailar?»
- *Al conocer a alguien:* «Tú tienes cara de que te gusta la marcha. ¿Acierto?»

### 8. Poeta  

*«Las palabras que no digo son las que más pesan.»*

| | |
|---|---|
| Identificador | `PT_POETA` |
| Ejes | calmado, reservado, independiente, soñador |
| Al andar | ×0.7, se para 4.0 s entre destinos |
| Voz | tono grave, ritmo lento |
| Emociones | melancolía, introspección, asombro |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 0.7 | 1.3 | 0.7 | 0.7 | 0.9 |

**Qué pide** — `item` ×1.6, `islandBuilding` ×1.4, `complaint` ×1.3.  
**Qué casi nunca pide** — `clothing` ×0.4, `food` ×0.5.

**Congenia con** — Artista (+6), Ermitaño (+7), Romántico (+4).  
**Choca con** — Audaz (-3), Cuentista (-4).

**Frases**

- *Contento:* «Hoy el cielo pesa menos. Algo hice bien.»
- *Contento:* «Encontré la palabra justa. Eso basta.»
- *Aburrido:* «El silencio es bueno… pero hoy sobra.»
- *Aburrido:* «Ni una nube, ni un verso. Páramo total.»
- *Enfadado:* «Tus palabras talan más que un hacha. Cállate.»
- *Enfadado:* «No necesito que me entiendas. Necesito que te vayas.»
- *Al conocer a alguien:* «Dime algo breve. Lo breve salva.»
- *Al conocer a alguien:* «No esperes que te cuente todo. Lo importante cabe en tres líneas.»

### 9. Visionario  

*«Lo que otros ven imposible yo lo estoy construyendo.»*

| | |
|---|---|
| Identificador | `PT_VISIONARIO` |
| Ejes | enérgico, reservado, independiente, soñador |
| Al andar | ×1.1, se para 1.8 s entre destinos |
| Voz | tono medio, ritmo rapido |
| Emociones | intensidad, frustración, asombro |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.1 | 1.1 | 1.2 | 0.7 | 0.9 |

**Qué pide** — `item` ×1.7, `activity` ×1.5, `islandBuilding` ×1.4.  
**Qué casi nunca pide** — `clothing` ×0.5, `socialIntro` ×0.5.

**Congenia con** — Atleta (+4), Explorador (+5), Genio (+7).  
**Choca con** — Afable (-3), Fiestero (-4).

**Frases**

- *Contento:* «El prototipo funciona. Mañana lo cuento; hoy lo vivo.»
- *Contento:* «Por fin alguien lo entendió. Uno basta.»
- *Aburrido:* «Esto ya lo resolví mentalmente hace una hora.»
- *Aburrido:* «Conversación de mantenimiento. No aporta.»
- *Enfadado:* «No me digas que es imposible. Dime que no lo ves.»
- *Enfadado:* «Cada vez que me frenan, doblo la apuesta.»
- *Al conocer a alguien:* «¿Qué construirías si nadie te dijera que no?»
- *Al conocer a alguien:* «Dime algo que no sepa. Lo demás sobra.»

### 10. Artista  

*«Lo que siento no cabe en palabras; por eso pinto.»*

| | |
|---|---|
| Identificador | `PT_ARTISTA` |
| Ejes | calmado, expresivo, independiente, soñador |
| Al andar | ×0.8, se para 2.5 s entre destinos |
| Voz | tono medio, ritmo normal |
| Emociones | inspiración, melancolía, euforia |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 0.9 | 1.2 | 0.8 | 0.8 | 0.9 |

**Qué pide** — `item` ×1.9, `confession` ×1.5, `islandBuilding` ×1.4.  
**Qué casi nunca pide** — `advice` ×0.5, `socialIntro` ×0.5.

**Congenia con** — Artesano (+7), Cuentista (+5), Poeta (+6).  
**Choca con** — Anfitrión (-3), Líder (-5).

**Frases**

- *Contento:* «El trazo salió solo. Esos son los buenos.»
- *Contento:* «Hoy los colores no mienten. Todo encaja.»
- *Aburrido:* «El lienzo en blanco y yo también.»
- *Aburrido:* «Necesito que algo me rompa el cascarón.»
- *Enfadado:* «No me digas cómo se siente. Lo sé antes que tú.»
- *Enfadado:* «Criticar no es crear. Enséñame lo tuyo y hablamos.»
- *Al conocer a alguien:* «¿Qué color te gusta? Así empiezo a conocerte.»
- *Al conocer a alguien:* «Muéstrame algo que hayas hecho. Lo que sea.»

### 11. Genio  

*«Las reglas son para los que no pueden reescribirlas.»*

| | |
|---|---|
| Identificador | `PT_GENIO` |
| Ejes | enérgico, expresivo, independiente, soñador |
| Al andar | ×1.2, se para 1.0 s entre destinos |
| Voz | tono medio, ritmo rapido |
| Emociones | intensidad, frustración, euforia |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.2 | 1.2 | 1.3 | 0.7 | 0.9 |

**Qué pide** — `item` ×1.8, `activity` ×1.5, `confession` ×1.5.  
**Qué casi nunca pide** — `clothing` ×0.4, `favor` ×0.5.

**Congenia con** — Audaz (+7), Entusiasta (+6), Visionario (+7).  
**Choca con** — Afable (-4), Anfitrión (-3).

**Frases**

- *Contento:* «¡Funciona! Y nadie lo vio venir. Como siempre.»
- *Contento:* «La idea era tan buena que se escribió sola.»
- *Aburrido:* «Otro problema trivial. ¿Alguien trae algo difícil?»
- *Aburrido:* «Mi cabeza va a ochenta y esto va a tres.»
- *Enfadado:* «No me subestimes. Es lo único que no perdono.»
- *Enfadado:* «Si no entiendes mi solución, el problema es tuyo.»
- *Al conocer a alguien:* «Sorpréndeme en diez segundos. Cronómetro andando.»
- *Al conocer a alguien:* «Dime algo incorrecto pero interesante. Así empiezo.»

### 12. Romántico  

*«El mundo está bien, pero podría estar mejor con dos.»*

| | |
|---|---|
| Identificador | `PT_ROMANTICO` |
| Ejes | calmado, reservado, sociable, soñador |
| Al andar | ×0.8, se para 3.0 s entre destinos |
| Voz | tono medio, ritmo lento |
| Emociones | ternura, melancolía, esperanza |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 0.9 | 1.1 | 0.8 | 1.2 | 1.1 |

**Qué pide** — `socialIntro` ×1.5, `reconcile` ×1.5, `favor` ×1.4.  
**Qué casi nunca pide** — `activity` ×0.5, `complaint` ×0.6.

**Congenia con** — Afable (+6), Cuentista (+5), Poeta (+4).  
**Choca con** — Atleta (-3), Audaz (-4).

**Frases**

- *Contento:* «Me miró distinto. Con eso ya duermo.»
- *Contento:* «Hoy el mundo fue amable. Hay que agradecer.»
- *Aburrido:* «Falta algo… y no sé qué. Eso es lo peor.»
- *Aburrido:* «Otro atardecer sin nadie al lado. Qué desperdicio.»
- *Enfadado:* «Me prometiste y no cumpliste. Eso no se olvida.»
- *Enfadado:* «No juegues con lo que yo pongo en serio.»
- *Al conocer a alguien:* «¿Crees en las señales? Yo sí. Y tú llegaste.»
- *Al conocer a alguien:* «Háblame de alguien a quien quieras. Así te conozco.»

### 13. Explorador  

*«El mapa se acaba donde empieza mi curiosidad.»*

| | |
|---|---|
| Identificador | `PT_EXPLORADOR` |
| Ejes | enérgico, reservado, sociable, soñador |
| Al andar | ×1.2, se para 1.3 s entre destinos |
| Voz | tono grave, ritmo normal |
| Emociones | curiosidad, asombro, inquietud |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.1 | 0.9 | 1.3 | 1.1 | 0.9 |

**Qué pide** — `item` ×1.5, `socialIntro` ×1.5, `activity` ×1.5.  
**Qué casi nunca pide** — `complaint` ×0.4, `clothing` ×0.7.

**Congenia con** — Entusiasta (+6), Líder (+5), Visionario (+5).  
**Choca con** — Artesano (-3), Cuentista (-3).

**Frases**

- *Contento:* «Encontré un sitio que no está en los mapas.»
- *Contento:* «Hoy toca ruta nueva. Las viejas ya me las sé.»
- *Aburrido:* «Otra vez el mismo paisaje. Necesito horizonte.»
- *Aburrido:* «Si no hay nada que descubrir, ¿para qué salir?»
- *Enfadado:* «No me ates a un sitio. Yo no funciono así.»
- *Enfadado:* «Cada frontera que pones es una que voy a cruzar.»
- *Al conocer a alguien:* «¿De dónde vienes? Lo importante es el camino.»
- *Al conocer a alguien:* «Cuéntame algo que solo se aprenda viajando.»

### 14. Cuentista  

*«Cada persona es una historia; yo solo la leo en voz alta.»*

| | |
|---|---|
| Identificador | `PT_CUENTISTA` |
| Ejes | calmado, expresivo, sociable, soñador |
| Al andar | ×0.9, se para 2.0 s entre destinos |
| Voz | tono medio, ritmo normal |
| Emociones | curiosidad, calidez, nostalgia |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.0 | 0.9 | 0.9 | 1.3 | 1.0 |

**Qué pide** — `advice` ×1.5, `socialIntro` ×1.5, `confession` ×1.5.  
**Qué casi nunca pide** — `activity` ×0.5, `complaint` ×0.7.

**Congenia con** — Anfitrión (+6), Artista (+5), Romántico (+5).  
**Choca con** — Atleta (-4), Explorador (-3), Poeta (-4).

**Frases**

- *Contento:* «Me contaron una historia… y ahora es nuestra.»
- *Contento:* «Hoy las palabras fluyeron solas. Día bendito.»
- *Aburrido:* «Nadie tiene nada que contar. Eso sí es grave.»
- *Aburrido:* «El silencio está bien… pero no tres horas.»
- *Enfadado:* «No me cambies el final. La historia es mía.»
- *Enfadado:* «Mentiste. Y una mentira rompe todo el relato.»
- *Al conocer a alguien:* «Siéntate. Esto empieza con un «érase una vez…».»
- *Al conocer a alguien:* «Tú tienes una historia. Todos tienen una. Cuenta.»

### 15. Entusiasta  

*«Cada día es un regalo, ¡y pienso desenvolverlo!»*

| | |
|---|---|
| Identificador | `PT_ENTUSIASTA` |
| Ejes | enérgico, expresivo, sociable, soñador |
| Al andar | ×1.2, se para 1.0 s entre destinos |
| Voz | tono agudo, ritmo rapido |
| Emociones | euforia, asombro, decepción |

**Necesidades** — multiplicador de lo rápido que se le vacían:

| hunger | mood | energy | social | hygiene |
|---|---|---|---|---|
| 1.2 | 0.8 | 1.3 | 1.3 | 1.1 |

**Qué pide** — `socialIntro` ×1.5, `activity` ×1.5, `confession` ×1.5.  
**Qué casi nunca pide** — `complaint` ×0.3, `item` ×1.0.

**Congenia con** — Explorador (+6), Fiestero (+7), Genio (+6).  
**Choca con** — Ermitaño (-4).

**Frases**

- *Contento:* «¡Qué día! ¡Qué sol! ¡Qué gente! ¡Qué todo!»
- *Contento:* «Hoy pasó algo increíble. Siéntate que te cuento.»
- *Aburrido:* «¿Cómo puede alguien aburrirse con todo lo que hay?»
- *Aburrido:* «¡Hagamos algo ya! Lo que sea, pero ya.»
- *Enfadado:* «¡Con lo bonito que era y lo estropeaste!»
- *Enfadado:* «No me digas que me calme. ¡Estoy vivo!»
- *Al conocer a alguien:* «¡Hola! Ya te quiero conocer. Sí, así de rápido.»
- *Al conocer a alguien:* «Tienes que contarme todo. Pero todo todo.»

## Cómo se implementa

Cada tipo es un fichero en `Assets/_Project/Scripts/Personality/Types/`, con una
clase que hereda de `PersonalityBehaviourBase` y rellena una
`PersonalityDefinition`. Un fichero por tipo, para que dieciséis manos puedan
trabajar sin pisarse. El test `PersonalityTablesMatchJson` comprueba que los
números del código siguen siendo los de este documento.
