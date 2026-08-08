#!/usr/bin/env python3
"""Genera los 4 catálogos de Isla Nimbo y los valida."""

import json, os, sys
from collections import Counter

OUT = "/home/vaknadesu/Proyectos/isla-nimbo/Docs/Contratos"

# ── utilidades ──────────────────────────────────────────────────

def make_id(prefix, name):
    """Convierte un nombre descriptivo en catalogId válido."""
    import unicodedata
    name = unicodedata.normalize('NFKD', name).encode('ascii', 'ignore').decode()
    return prefix + name.lower().replace(" ", "_").replace("'", "").replace("-", "_")

def tier_price(tier, base, spread):
    """Reparte precios alrededor de base con variación ±spread."""
    import random
    return max(3, base + random.randint(-spread, spread))

# ── COMIDA (45) ─────────────────────────────────────────────────

foods = []
def f(name, desc, price, unlock, tags, hunger, kind):
    foods.append({
        "catalogId": make_id("food_", name),
        "displayName": name,
        "description": desc,
        "price": price,
        "unlockLevel": unlock,
        "tags": tags,
        "hungerRestore": hunger,
        "foodKind": kind
    })

# snacks (8): +15 hunger, baratos
f("Nube de algodón", "Algodón de azúcar que crece en las nubes bajas. Se deshace en la boca.", 5, 1, ["dulce", "nube"], 15, "snack")
f("Crujiente de viento", "Galleta ligera que cruje como una ráfaga. Sabe a canela y aire.", 4, 1, ["salado", "crujiente"], 15, "snack")
f("Brocheta de frutas nimbo", "Frutas que solo crecen en islas flotantes. Ácidas y refrescantes.", 6, 2, ["fruta", "fresco"], 15, "snack")
f("Palomitas de cúmulo", "Palomitas con sal de nube. El snack oficial del cine flotante.", 5, 1, ["salado", "cine"], 15, "snack")
f("Rollito de miel celeste", "Miel de abejas que polinizan en las alturas, envuelta en hojaldre.", 7, 3, ["dulce", "hojaldre"], 15, "snack")
f("Galletas de la suerte nimba", "Cada galleta trae un mensaje. Hoy: 'Alguien te está pensando'.", 6, 2, ["dulce", "galleta"], 15, "snack")
f("Churros de corriente", "Churros espolvoreados con azúcar de rayo. Energía en cada bocado.", 5, 1, ["dulce", "frito"], 15, "snack")
f("Bolitas de arroz flotante", "Arroz inflado que levita unos centímetros sobre el plato.", 4, 3, ["salado", "arroz"], 15, "snack")

# homemade (12): +35 hunger, baratos (ingredientes)
f("Sopa de nube", "Caldo humeante con trocitos de verduras de huerto aéreo. Reconforta el alma.", 8, 1, ["caliente", "casero", "nube"], 35, "homemade")
f("Tortilla de la abuela nimba", "Con patatas de las islas bajas y un toque de cebolla dorada.", 6, 2, ["casero", "huevo"], 35, "homemade")
f("Ensalada de zafiro", "Lechuga fresca, tomates diminutos y aderezo de hierbas celestiales.", 5, 1, ["fresco", "casero", "verdura"], 35, "homemade")
f("Guiso de tres vientos", "Legumbres cocinadas a fuego lento con el calor de tres corrientes de aire.", 7, 3, ["caliente", "casero", "legumbre"], 35, "homemade")
f("Pasta al pesto de albahaca", "Albahaca cultivada en macetas colgantes. La receta secreta de la isla.", 8, 2, ["casero", "pasta", "italiano"], 35, "homemade")
f("Arroz frito con huevo de altura", "Las gallinas de la isla ponen huevos más ligeros. O eso dicen.", 6, 1, ["casero", "arroz", "huevo"], 35, "homemade")
f("Croquetas de jamón del cielo", "Crujientes por fuera, cremosas por dentro. Vuelan del plato.", 7, 4, ["casero", "frito", "carne"], 35, "homemade")
f("Tostada de aguacate estelar", "Aguacate maduro sobre pan de centeno. El desayuno de los soñadores.", 5, 2, ["casero", "tostada", "desayuno"], 35, "homemade")
f("Lentejas al vapor de géiser", "Cocinadas con el vapor natural de los géiseres de la isla. Pura tradición Nimba.", 7, 3, ["caliente", "casero", "legumbre"], 35, "homemade")
f("Huevos revueltos con queso de cabra", "Queso de cabras que pastan en prados flotantes. Cremoso y suave.", 6, 1, ["casero", "huevo", "desayuno"], 35, "homemade")
f("Tacos de pescado volador", "Pescado que salta entre islas, con pico de gallo y lima.", 9, 5, ["casero", "pescado", "mexicano"], 35, "homemade")
f("Pizza margarita casera", "Masa fina, tomate, mozzarella y albahaca. Hecha con amor en horno de piedra.", 7, 2, ["casero", "pizza", "italiano"], 35, "homemade")

# restaurant (10): +50 hunger, precio medio-alto
f("Filete de atún celeste", "Atún sellado a la plancha con costra de sésamo. Se deshace en la boca.", 18, 5, ["pescado", "restaurante", "elegante"], 50, "restaurant")
f("Risotto de setas nimbo", "Setas bioluminiscentes que crecen en las cuevas de la isla. Brilla en el plato.", 22, 8, ["setas", "restaurante", "italiano"], 50, "restaurant")
f("Hamburguesa cumulonimbo", "Doble carne, queso fundido, cebolla caramelizada. Para días de tormenta.", 16, 4, ["carne", "restaurante", "contundente"], 50, "restaurant")
f("Paella de marisco isleño", "Arroz con gambas, calamares y mejillones de las aguas bajo las islas.", 20, 7, ["arroz", "restaurante", "marisco"], 50, "restaurant")
f("Curry de boniatos al coco", "Suave, cremoso, con un toque de especias. El plato favorito de los viajeros.", 17, 6, ["curry", "restaurante", "vegetariano"], 50, "restaurant")
f("Salmón a la parrilla con verduras", "Salmón crujiente por fuera, jugoso por dentro. Acompañado de espárragos.", 19, 7, ["pescado", "restaurante", "saludable"], 50, "restaurant")
f("Lasaña de la casa", "Capas de pasta, carne, bechamel y queso gratinado. La receta del chef.", 18, 5, ["pasta", "restaurante", "italiano"], 50, "restaurant")
f("Bao de cerdo glaseado", "Panecillo al vapor con panceta caramelizada y pepinillos. Pequeño pero bestial.", 15, 8, ["carne", "restaurante", "asiatico"], 50, "restaurant")
f("Ramen de la cumbre", "Caldo de cerdo con fideos, huevo marinado y alga nori de las cascadas.", 17, 6, ["caliente", "restaurante", "asiatico"], 50, "restaurant")
f("Wrap de pollo y cilantro", "Pollo a la parrilla envuelto en tortilla con salsa de yogur y cilantro fresco.", 14, 4, ["pollo", "restaurante", "fresco"], 50, "restaurant")

# dessert (8): +20 hunger, precio medio
f("Flan de caramelo nimbo", "Flan casero con caramelo hecho al momento. Tiembla al tocarlo.", 10, 2, ["dulce", "postre", "casero"], 20, "dessert")
f("Tarta de tres leches", "Pastel empapado en tres leches. El postre oficial de las celebraciones.", 12, 4, ["dulce", "postre", "tarta"], 20, "dessert")
f("Helado de nube de tormenta", "Helado grisáceo con chispas de chocolate que parecen relámpagos.", 9, 3, ["dulce", "helado", "nube"], 20, "dessert")
f("Brownie con nueces del cielo", "Chocolate denso con nueces que caen de los árboles celestiales.", 11, 5, ["dulce", "chocolate", "horno"], 20, "dessert")
f("Mousse de maracuyá flotante", "Espuma ligera de maracuyá. Tan aireada que casi levita.", 10, 6, ["dulce", "postre", "fruta"], 20, "dessert")
f("Crepes de dulce de leche", "Crepes finos rellenos de dulce de leche y plátano. Adictivos.", 9, 2, ["dulce", "postre", "crepe"], 20, "dessert")
f("Natilla con canela y galleta", "Natilla cremosa con galleta triturada. Sabe a infancia en las nubes.", 8, 1, ["dulce", "postre", "casero"], 20, "dessert")
f("Fruta de dragón en almíbar", "Pitahaya rosada bañada en almíbar ligero. Tan bonita como rica.", 11, 7, ["dulce", "postre", "fruta"], 20, "dessert")

# drinks (7): +10 hunger (es bebida, no comida), precio bajo
f("Té de menta celeste", "Infusión de menta cultivada en los jardines de la cumbre. Relaja y refresca.", 5, 1, ["bebida", "caliente", "menta"], 8, "drink")
f("Batido de frutas del alba", "Mezcla de frutas tropicales con un toque de yogur. Energía líquida.", 7, 2, ["bebida", "frio", "fruta"], 8, "drink")
f("Café de altura", "Café cultivado en la ladera del Nimbo. Fuerte, negro y necesario.", 4, 1, ["bebida", "caliente", "cafe"], 8, "drink")
f("Limonada de nube", "Limón, azúcar y agua de lluvia recién recogida. La bebida del verano.", 5, 1, ["bebida", "frio", "citrico"], 8, "drink")
f("Chocolate caliente espeso", "Cacao puro con leche y una pizca de canela. Espeso como la niebla.", 6, 3, ["bebida", "caliente", "chocolate"], 8, "drink")
f("Zumo de estrella fugaz", "Mezcla de naranja, zanahoria y jengibre. Dicen que trae suerte.", 7, 4, ["bebida", "frio", "zumo"], 8, "drink")
f("Refresco de brisa marina", "Bebida gaseosa con sabor a algas dulces. El sabor de la isla en botella.", 5, 2, ["bebida", "frio", "gaseosa"], 8, "drink")

assert len(foods) == 45, f"Comida: {len(foods)} items, deberían ser 45"

# ── ROPA (60) ───────────────────────────────────────────────────

clothes = []
def c(name, desc, price, unlock, slot, style, palette, tags=None):
    clothes.append({
        "catalogId": make_id("cloth_", name),
        "displayName": name,
        "description": desc,
        "price": price,
        "unlockLevel": unlock,
        "tags": tags or [],
        "slot": slot,
        "style": style,
        "palette": palette
    })

# outfits (30)
c("Camiseta basica nimbo", "La camiseta oficial de la isla. Cómoda, suave, con el logo del Nimbo.", 30, 1, "outfit", "casual", ["#D4E4F7","#FFFFFF","#87A8D0"])
c("Vestido de algodon celeste", "Vestido ligero color cielo. Ideal para los dias de brisa.", 50, 2, "outfit", "casual", ["#B8D4E8","#FFFFFF","#7BA3C9"])
c("Camisa de lino blanca", "Camisa de lino fresco. El uniforme no oficial del buen vivir isleño.", 40, 1, "outfit", "casual", ["#FFFFFF","#F0EDE8","#E0D8CC"])
c("Pantalones cargo marrones", "Pantalones con bolsillos para guardar semillas, piedras y sueños.", 35, 2, "outfit", "casual", ["#8B7355","#A0896E","#6B5640"])
c("Mono de trabajo azul", "Mono resistente para construir y reparar. El overol del manitas.", 45, 3, "outfit", "deportivo", ["#4A6FA5","#3D5C8A","#2C4A70"])
c("Chaqueta de punto crema", "Chaqueta tejida a mano por las abuelas de la isla. Abriga y abraza.", 60, 4, "outfit", "casual", ["#F5E6D3","#E8D5BC","#D4C0A5"])
c("Pantalon de vestir gris", "Para las ocasiones que lo merecen. Elegante sin ser estirado.", 55, 5, "outfit", "formal", ["#A0A0A0","#808080","#C0C0C0"])
c("Traje de dos piezas azul marino", "Americana y pantalon a juego. Para entrevistas, bodas y negocios nimios.", 120, 10, "outfit", "formal", ["#1B2A4A","#2C4270","#152040"])
c("Vestido de gala plateado", "Lentejuelas que brillan como estrellas. La envidia de cualquier fiesta.", 150, 12, "outfit", "formal", ["#C8C8D0","#E8E8F0","#A0A0B0"])
c("Chandal deportivo naranja", "Chaqueta y pantalon transpirables. Para correr entre islas.", 50, 3, "outfit", "deportivo", ["#FF8C42","#FF6B1A","#CC5500"])
c("Leggings y top morados", "Conjunto elastico para yoga al amanecer en la cumbre.", 45, 4, "outfit", "deportivo", ["#7B4FBF","#9B6FDF","#5B2F9F"])
c("Uniforme escolar clasico", "Falda plisada y blazer con el escudo de la isla.", 70, 6, "outfit", "formal", ["#2C3E50","#FFFFFF","#34495E"])
c("Traje de aventurero caqui", "Chaleco multibolsillo y pantalon reforzado. Para explorar islas nuevas.", 65, 5, "outfit", "deportivo", ["#C3A87C","#A08860","#8B7355"])
c("Kimono de seda cerezo", "Kimono ligero con estampado de flores. Pura elegancia flotante.", 100, 11, "outfit", "fantasia", ["#FFB7C5","#FF8FA3","#E57388"])
c("Tunica de druida del viento", "Tunica verde musgo con bordados de runas. Para canalizar corrientes.", 90, 9, "outfit", "fantasia", ["#6B8E6B","#4A7A4A","#8BAA8B"])
c("Traje de capitan nimbo", "Uniforme naval con hombreras doradas. Al mando del navio flotante.", 130, 14, "outfit", "fantasia", ["#1A3A5C","#D4AF37","#0D2640"])
c("Pijama de franela a cuadros", "Pantalon y camisa de franela suave. Para soñar con ovejas de las nubes.", 35, 1, "outfit", "pijama", ["#CC4444","#226644","#DDDDDD"])
c("Bata de estar por casa", "Bata mullida color crema. El uniforme de los domingos perezosos.", 40, 2, "outfit", "pijama", ["#F5ECD7","#E8DCC8","#D8CCB4"])
c("Traje de gala festivo rojo", "Para la gran fiesta anual del Nimbo. Rojo como el atardecer.", 140, 15, "outfit", "festivo", ["#CC2233","#FF3344","#881122"])
c("Vestido de flores primaveral", "Estampado de margaritas silvestres. La primavera hecha vestido.", 80, 7, "outfit", "festivo", ["#FFE4B5","#FFB6C1","#98D8C8"])
c("Traje de buceo nimbo", "Neopreno azul para explorar las cascadas que caen de la isla.", 75, 8, "outfit", "deportivo", ["#1E90FF","#00BFFF","#00598B"])
c("Abrigo de lana de llama", "Lana de llama de las islas altas. Cálido como un abrazo andino.", 95, 9, "outfit", "casual", ["#C87533","#E8985B","#A05520"])
c("Poncho de los vientos", "Poncho tradicional de la isla. Cada franja cuenta una historia.", 55, 4, "outfit", "casual", ["#B87333","#D4956B","#8B4513"])
c("Vestido sirena turquesa", "Vestido largo con cola de sirena. Brilla como el mar bajo las islas.", 110, 13, "outfit", "fantasia", ["#40E0D0","#00CED1","#008B8B"])
c("Traje de astronauta nimbo", "Mono blanco con casco de burbuja. Para explorar islas aún más altas.", 160, 20, "outfit", "fantasia", ["#FFFFFF","#E0E0E0","#C0C0C0"])
c("Camiseta de rayas marineras", "Rayas azules y blancas. Clásico marinero de las islas flotantes.", 30, 1, "outfit", "casual", ["#3366AA","#FFFFFF","#225588"])
c("Jersey de ochos verde bosque", "Jersey de lana gruesa con trenzado de ochos. Abriga tres inviernos.", 65, 5, "outfit", "casual", ["#3D6B3D","#2D5B2D","#5D8B5D"])
c("Sudadera con capucha gris", "Capucha forrada de pelito. Para los dias de niebla espesa.", 40, 2, "outfit", "deportivo", ["#A0A0A0","#C0C0C0","#808080"])
c("Capa de explorador nimbo", "Capa impermeable verde musgo. Para adentrarse en la niebla.", 60, 6, "outfit", "deportivo", ["#5D7B4D","#4D6B3D","#7D9B5D"])
c("Traje tipico de la isla", "Traje tradicional con bordados de nubes y vientos. Patrimonio nimbo.", 85, 10, "outfit", "festivo", ["#E8A840","#C83030","#3068C8"])

# hats (15)
c("Gorra nimbo clasica", "Gorra azul con el logo de la isla. Hacia atras o hacia delante.", 20, 1, "hat", "casual", ["#4488CC","#2266AA","#FFFFFF"])
c("Sombrero de paja", "Sombrero de ala ancha tejido con paja de los campos flotantes.", 25, 2, "hat", "casual", ["#D4B896","#C4A886","#E4C8A6"])
c("Boina francesa roja", "Boina de lana roja. Un toque de estilo parisino en las nubes.", 30, 4, "hat", "formal", ["#CC3333","#AA2222","#EE4444"])
c("Gorro de lana con pompom", "Gorro de lana gruesa con pompom blanco. Para los inviernos de altura.", 25, 3, "hat", "casual", ["#885544","#FFDDAA","#664433"])
c("Diadema de flores", "Corona de flores frescas. Cambia con cada estacion.", 20, 2, "hat", "festivo", ["#FF69B4","#FFB6C1","#FF1493"])
c("Sombrero de copa magico", "Chistera negra con cinta morada. Dicen que saca conejos.", 40, 8, "hat", "fantasia", ["#1A1A2E","#6B3FA0","#0D0D1A"])
c("Casco de constructor", "Casco amarillo de seguridad. La isla esta en obras.", 22, 1, "hat", "deportivo", ["#FFD700","#FFC107","#CC9900"])
c("Orejas de gato celestes", "Diadema con orejas de gato. Miau.", 35, 5, "hat", "fantasia", ["#FFB6C1","#FFC0CB","#DB7093"])
c("Tiara de princesa nimba", "Tiara plateada con cristales que reflejan la luz de las estrellas.", 50, 10, "hat", "fantasia", ["#C0C0C0","#E8E8E8","#A0D8F0"])
c("Bandana pirata roja", "Bandana de lunares. Para navegar los mares de nubes.", 20, 3, "hat", "fantasia", ["#CC1111","#FFFFFF","#AA0000"])
c("Gorro de chef blanco", "Gorro alto de cocinero. Para los maestros de los fogones.", 25, 5, "hat", "formal", ["#FFFFFF","#F0F0F0","#E0E0E0"])
c("Sombrero vaquero marrón", "Sombrero de ala ancha. Para pastorear cabras en los prados altos.", 30, 7, "hat", "casual", ["#8B6B4A","#6B4B2A","#AB8B6A"])
c("Corona de rey nimbo", "Corona dorada con gemas. Para el rey o reina de la fiesta.", 55, 15, "hat", "festivo", ["#FFD700","#FFA500","#FF8C00"])
c("Visera deportiva verde", "Visera transpirable. Para correr sin que el sol te gane.", 18, 2, "hat", "deportivo", ["#33AA55","#228844","#44CC66"])
c("Capucha de seta magica", "Capucha roja con puntos blancos. Parece sacada de un cuento.", 30, 6, "hat", "fantasia", ["#FF3333","#FFFFFF","#CC0000"])

# accessories (15)
c("Bufanda de seda celeste", "Bufanda ligera que flota con la brisa. Hecha con seda de gusano nimbo.", 30, 2, "accessory", "casual", ["#88CCEE","#AAE0F8","#66AADD"])
c("Gafas de sol redondas", "Cristales ahumados y montura dorada. Para mirar al sol sin fruncir el ceño.", 25, 1, "accessory", "casual", ["#D4AF37","#332211","#FFFFFF"])
c("Collar de conchas marinas", "Conchas recogidas en las playas bajo las islas. Cada una es unica.", 20, 1, "accessory", "casual", ["#F5DEB3","#E8D8A0","#D2B48C"])
c("Reloj de bolsillo antiguo", "Reloj de cuerda heredado de los primeros pobladores. Aun funciona.", 40, 5, "accessory", "formal", ["#D4AF37","#B8960A","#8B7332"])
c("Mochila de explorador", "Mochila de cuero reforzado. Para llevar tesoros de isla en isla.", 35, 3, "accessory", "deportivo", ["#8B6B4A","#6B4B2A","#AB8B6A"])
c("Alas de hada tornasol", "Alas de tul iridiscente. No vuelan, pero la ilusion cuenta.", 45, 9, "accessory", "fantasia", ["#E8F0FF","#C8E0FF","#FFD8E8"])
c("Pajarita de seda negra", "Pajarita clasica. El toque final de cualquier traje.", 25, 4, "accessory", "formal", ["#111111","#333333","#000000"])
c("Pulsera de la amistad", "Hilos trenzados con los colores de la isla. La llevan los mejores amigos.", 15, 2, "accessory", "casual", ["#FF6699","#66AADD","#FFCC44"])
c("Mascara de carnaval veneciano", "Mascara dorada con plumas. Misteriosa y elegante.", 35, 8, "accessory", "festivo", ["#D4AF37","#FFD700","#8B6914"])
c("Cinturon de herramientas", "Cinturon de cuero con martillo y llave inglesa. Para el manitas de la isla.", 30, 3, "accessory", "deportivo", ["#6B4226","#8B5A2E","#4A2A14"])
c("Bolso de mano elegante", "Bolso de charol con cierre dorado. Cabe lo justo y necesario.", 50, 10, "accessory", "formal", ["#222222","#FFD700","#111111"])
c("Paraguas de burbujas", "Paraguas transparente con pompas de jabon dentro. Magico y practico.", 40, 7, "accessory", "fantasia", ["#E0F0FF","#FFFFFF","#B0D0F0"])
c("Guantes de encaje blanco", "Guantes finos que llegan hasta la muñeca. Pura distincion.", 30, 6, "accessory", "formal", ["#FFFFFF","#F8F0E8","#F0E8E0"])
c("Silbato de viento", "Silbato que imita el sonido de la brisa. Los pajaros responden.", 15, 1, "accessory", "casual", ["#C0C0C0","#A0A0A0","#808080"])
c("Baston de paseo tallado", "Baston de madera de arbol nimbo con empuñadura de pajaro tallado.", 45, 12, "accessory", "formal", ["#6B4226","#8B6B4A","#3A1A0A"])

assert len(clothes) == 60, f"Ropa: {len(clothes)} items, deberian ser 60"

# ── MUEBLES (80) ────────────────────────────────────────────────

furniture = []
def m(name, desc, price, unlock, layer, fx, fy, func, bonus, tags=None):
    furniture.append({
        "catalogId": make_id("furn_", name),
        "displayName": name,
        "description": desc,
        "price": price,
        "unlockLevel": unlock,
        "tags": tags or [],
        "layer": layer,
        "footprintX": fx,
        "footprintY": fy,
        "function": func,
        "needBonus": bonus
    })

# beds (8)
m("Cama individual nimbo", "Cama de una plaza con colchon de plumas de ave celeste. Sueños garantizados.", 120, 1, "Furniture", 1, 2, "bed", {"energy": 12})
m("Cama doble acogedora", "Cama de matrimonio con dosel de gasa. Para dormir abrazado a una nube.", 250, 5, "Furniture", 2, 2, "bed", {"energy": 14})
m("Hamaca de isla flotante", "Hamaca de lona que se mece con la brisa. La siesta perfecta.", 80, 2, "Furniture", 2, 1, "bed", {"energy": 8})
m("Litera de madera rustica", "Dos camas en una. Para hermanos, amigos o visitantes inesperados.", 200, 6, "Furniture", 2, 1, "bed", {"energy": 11})
m("Cama king size celestial", "Tres plazas de puro lujo. Con almohadas que parecen nubes de verdad.", 450, 15, "Furniture", 3, 2, "bed", {"energy": 16})
m("Cuna de bebe nimbo", "Cuna de madera con movil de estrellas. Para los habitantes mas pequeños.", 100, 12, "Furniture", 1, 1, "bed", {"energy": 10})
m("Futon japones", "Colchoneta de algodon que se guarda de dia. Minimalismo isleño.", 90, 4, "Furniture", 2, 1, "bed", {"energy": 9})
m("Nido colgante de nubes", "Cama redonda que cuelga del techo. Mecerse hasta dormirse.", 300, 18, "Furniture", 2, 2, "bed", {"energy": 13})

# seats (12)
m("Silla de madera sencilla", "Silla de pino nimbo. Hace su trabajo sin quejarse.", 25, 1, "Furniture", 1, 1, "seat", {"energy": 2})
m("Sillon orejero verde", "Butacon mullido con orejas laterales. Leer aqui es perderse del mundo.", 100, 4, "Furniture", 1, 1, "seat", {"energy": 4})
m("Taburete de barra alto", "Taburete de metal y madera. Ideal para islas de cocina.", 35, 2, "Furniture", 1, 1, "seat", {"energy": 2})
m("Sofa de dos plazas gris", "Sofa comodo para dos. Caben tres si se quieren.", 150, 5, "Furniture", 2, 1, "seat", {"energy": 5, "social": 3})
m("Sofa rinconero modular", "Sofa en forma de L. Para familias grandes o muy buenos amigos.", 280, 10, "Furniture", 3, 2, "seat", {"energy": 6, "social": 5})
m("Puf gigante naranja", "Puf informe y blandisimo. Te tragas y no quieres salir.", 60, 3, "Furniture", 1, 1, "seat", {"energy": 3})
m("Banco de jardin interior", "Banco de madera tratada. Un trocito de parque dentro de casa.", 40, 2, "Furniture", 2, 1, "seat", {"energy": 2, "social": 2})
m("Mecedora de mimbre", "Mecedora que cruje al ritmo del viento. Ideal para atardeceres.", 70, 4, "Furniture", 1, 1, "seat", {"energy": 3})
m("Silla de escritorio giratoria", "Silla con ruedas y respaldo ajustable. Gira, sube, baja. Productividad total.", 80, 5, "Furniture", 1, 1, "seat", {"energy": 2})
m("Banqueta acolchada", "Banqueta baja con tela estampada. Para los pies o para visitas extra.", 30, 2, "Furniture", 1, 1, "seat", {"energy": 1})
m("Sofa cama convertible", "De dia sofa, de noche cama. Magia sin varita.", 180, 8, "Furniture", 2, 1, "seat", {"energy": 5})
m("Columpio de interior", "Columpio colgado del techo. La casa es un parque si tu quieres.", 90, 11, "Furniture", 1, 1, "seat", {"energy": 2, "social": 2})

# tables (10)
m("Mesa de centro redonda", "Mesa baja de madera. Para el te, las galletas y las revistas.", 50, 1, "Furniture", 1, 1, "table", {})
m("Mesa de comedor cuadrada", "Mesa para cuatro. Donde ocurren las cenas importantes.", 90, 3, "Furniture", 2, 2, "table", {"social": 4})
m("Escritorio de estudio", "Mesa con cajones y espacio para libros. Aqui se escriben las ideas.", 70, 2, "Furniture", 2, 1, "table", {})
m("Mesa alta de cocina", "Mesa estrecha y alta. Para desayunos rapidos y charlas de pie.", 60, 3, "Furniture", 2, 1, "table", {})
m("Mesa de billar", "Mesa de billar con tapete verde. El juego favorito del club de la isla.", 300, 14, "Furniture", 3, 2, "table", {"social": 8})
m("Mesita de noche", "Mesa pequena para el despertador y el libro que nunca terminas.", 35, 1, "Furniture", 1, 1, "table", {})
m("Mesa plegable multiusos", "Se pliega y se guarda. Util para manualidades, banquetes y emergencias.", 45, 2, "Furniture", 2, 1, "table", {})
m("Mesa de dibujo inclinable", "Tablero inclinable para artistas. Con porta lapices incorporado.", 65, 7, "Furniture", 2, 1, "table", {})
m("Mesa redonda de terraza", "Mesa de hierro forjado con dibujos de nubes. Para cafes al aire libre.", 100, 6, "Furniture", 2, 2, "table", {"social": 3})
m("Tocador con espejo", "Mesa con espejo ovalado y luces. Prepararse nunca fue tan glamuroso.", 130, 8, "Furniture", 2, 1, "table", {"hygiene": 2})

# storage (10)
m("Armario ropero sencillo", "Armario de madera con puertas batientes. Guarda ropa, secretos y polillas.", 70, 1, "Furniture", 1, 2, "storage", {})
m("Comoda de tres cajones", "Comoda baja con tiradores dorados. Calcetines arriba, sueños abajo.", 60, 2, "Furniture", 2, 1, "storage", {})
m("Estanteria alta de madera", "Cinco baldas para libros, fotos y objetos de otras islas.", 55, 1, "Furniture", 1, 2, "storage", {})
m("Biblioteca de pared completa", "Estanteria que cubre toda la pared. Para los que leen hasta volar.", 180, 9, "Furniture", 3, 1, "storage", {})
m("Armario empotrado blanco", "Armario de suelo a techo con puertas correderas. Minimalista y amplio.", 200, 10, "Furniture", 2, 2, "storage", {})
m("Baul de los recuerdos", "Baul de madera reforzada con herrajes. Guarda tesoros de aventuras pasadas.", 90, 5, "Furniture", 1, 1, "storage", {})
m("Cajonera de plastico colorida", "Cajones apilables de colores. Organizar nunca fue tan alegre.", 35, 2, "Furniture", 1, 1, "storage", {})
m("Perchero de pie elegante", "Perchero de madera torneada. Para colgar abrigos, bufandas y sombreros.", 50, 3, "Furniture", 1, 1, "storage", {})
m("Zapatero inclinado", "Mueble estrecho para zapatos. Cada par tiene su sitio por fin.", 45, 4, "Furniture", 2, 1, "storage", {})
m("Caja fuerte nimba", "Caja de seguridad con combinacion. Para guardar ahorros y objetos valiosos.", 120, 15, "Furniture", 1, 1, "storage", {})

# kitchen (8)
m("Fogon de dos fuegos", "Cocina de gas con dos fuegos. Donde nacen las sopas de nube.", 150, 3, "Furniture", 2, 1, "kitchen", {"hunger": 15})
m("Nevera compacta azul", "Frigorifico de una puerta. Frio como la cima del Nimbo.", 200, 4, "Furniture", 1, 1, "kitchen", {"hunger": 10})
m("Horno de piedra tradicional", "Horno de piedra volcanica. El pan sale con corteza de otro mundo.", 250, 7, "Furniture", 2, 1, "kitchen", {"hunger": 20})
m("Encimera de marmol nimbo", "Encimera de marmol blanco veteado. Donde la magia culinaria sucede.", 180, 5, "Furniture", 2, 1, "kitchen", {"hunger": 5})
m("Lavavajillas magico", "Lavavajillas que susurra mientras limpia. Nadie sabe como funciona.", 300, 12, "Furniture", 1, 1, "kitchen", {"hygiene": 3})
m("Despensa de especias giratoria", "Estante giratorio con botes de especias exoticas. Aroma a aventura.", 80, 4, "Furniture", 1, 1, "kitchen", {"hunger": 5})
m("Microondas compacto", "Calienta en segundos. No preguntes como.", 70, 2, "Counter", 1, 1, "kitchen", {"hunger": 8})
m("Tostadora con disenos de nubes", "Tostadora que graba nubecitas en el pan. El desayuno mas mono.", 40, 2, "Counter", 1, 1, "kitchen", {"hunger": 3})

# bath (6)
m("Ducha de cascada", "Ducha con efecto cascada. El agua cae como en las cascadas de la isla.", 180, 3, "Furniture", 1, 1, "bath", {"hygiene": 60})
m("Banera de hidromasaje", "Banera con burbujas y cromoterapia. El lujo de los dioses de la nube.", 450, 12, "Furniture", 2, 2, "bath", {"hygiene": 100})
m("Lavabo sobre encimera", "Lavabo de ceramica blanca con grifo dorado. Elegancia basica.", 100, 2, "Furniture", 1, 1, "bath", {"hygiene": 15})
m("Inodoro compacto", "Inodoro blanco con cisterna silenciosa. Funcional y discreto.", 80, 1, "Furniture", 1, 1, "bath", {"hygiene": 10})
m("Tocador de baño con espejo", "Espejo con luces calidas y estante para cremas. Rutina facial completa.", 130, 6, "WallMounted", 1, 1, "bath", {"hygiene": 5})
m("Toallero calefactor", "Toallero electrico. Salir de la ducha y envolverse en calor no tiene precio.", 60, 5, "WallMounted", 1, 1, "bath", {"hygiene": 3})

# entertainment (6)
m("Televisor de pantalla plana", "Tele de 42 pulgadas. Para ver el canal del clima de la isla en HD.", 250, 7, "Furniture", 2, 1, "entertainment", {"social": 3})
m("Equipo de musica vintage", "Tocadiscos con altavoces de madera. El vinilo suena mejor en las alturas.", 180, 8, "Furniture", 2, 1, "entertainment", {"social": 4})
m("Consola de videojuegos nimbo", "La ultima consola con mandos flotantes. Juegos en la nube, literalmente.", 300, 12, "Furniture", 1, 1, "entertainment", {"social": 5})
m("Maquina recreativa retro", "Arcade con 100 juegos clasicos. La nostalgia tambien flota.", 400, 16, "Furniture", 1, 2, "entertainment", {"social": 6})
m("Tablero de ajedrez de mesa", "Tablero de madera noble con piezas talladas a mano. Jaque mate entre nubes.", 60, 3, "Counter", 1, 1, "entertainment", {"social": 2})
m("Caballete de pintura", "Caballete de madera con lienzo. Para pintar atardeceres nimbo.", 70, 4, "Furniture", 1, 1, "entertainment", {})

# decor (12) — no need bonus
m("Planta colgante de helecho", "Helecho frondoso que cae en cascada verde. Purifica el aire y el alma.", 25, 1, "Furniture", 1, 1, "decor", {})
m("Cuadro de paisaje nimbo", "Pintura al oleo de la isla al atardecer. La firma un artista local.", 40, 2, "WallMounted", 1, 1, "decor", {})
m("Jarron de ceramica artesanal", "Jarron hecho a mano con esmalte azul y blanco. Para flores o para mirarlo.", 35, 2, "Furniture", 1, 1, "decor", {})
m("Reloj de pared cuco", "Reloj de madera con un pajarito que sale cada hora. Canta la hora nimba.", 55, 3, "WallMounted", 1, 1, "decor", {})
m("Espejo de cuerpo entero", "Espejo con marco dorado. Refleja la mejor version de ti.", 80, 4, "WallMounted", 1, 1, "decor", {})
m("Globo terraqueo flotante", "Globo que levita y gira solo. Muestra islas que aun no existen.", 100, 10, "Furniture", 1, 1, "decor", {})
m("Atrapasueños artesanal", "Atrapasueños tejido con plumas y cuentas. Filtra las pesadillas.", 30, 3, "WallMounted", 1, 1, "decor", {})
m("Acuario de bolsillo nimbo", "Pecera diminuta con peces que brillan en la oscuridad. Hipnotiza.", 70, 8, "Furniture", 1, 1, "decor", {})
m("Movil de estrellas colgante", "Estrellas de papel que giran con el viento. Un cielo en miniatura.", 25, 2, "Ceiling", 1, 1, "decor", {})
m("Mapa antiguo de las islas", "Mapa en pergamino con rutas de navegacion aerea. Historia enmarcada.", 50, 6, "WallMounted", 2, 1, "decor", {})
m("Maceta de girasol radiante", "Girasol que gira siguiendo la luz. Un trocito de sol dentro de casa.", 20, 1, "Furniture", 1, 1, "decor", {})
m("Coleccion de caracolas gigantes", "Tres caracolas de las playas bajas. Si acercas el oido, oyes el viento.", 45, 5, "Furniture", 1, 1, "decor", {})

# light (8)
m("Lampara de mesa nimbo", "Lampara con pantalla de nube. Luz calida y envolvente.", 30, 1, "Counter", 1, 1, "light", {})
m("Arbol de luz de pie", "Lampara de pie con ramas luminosas. Un arbol que da luz en vez de fruta.", 80, 5, "Furniture", 1, 1, "light", {})
m("Guirnalda de luces calidas", "Cable con bombillas diminutas. Ambiente magico instantaneo.", 25, 2, "Ceiling", 2, 1, "light", {})
m("Lampara de techo araña", "Arraña de cristal con gotas que brillan. Para los que quieren brillar.", 150, 12, "Ceiling", 2, 2, "light", {})
m("Farolillo de papel flotante", "Farolillo que flota sin cuerda. Luz que desafia la gravedad.", 45, 6, "Ceiling", 1, 1, "light", {})
m("Flexo de escritorio articulado", "Lampara de brazo flexible. Apuntas donde necesitas leer.", 35, 3, "Counter", 1, 1, "light", {})
m("Vela infinita nimba", "Vela que nunca se consume. La cera se recicla sola. Magia nimba.", 20, 2, "Counter", 1, 1, "light", {})
m("Neon de nube para pared", "Letrero de neon con forma de nube. Rosa, azul o blanco segun el humor.", 90, 10, "WallMounted", 2, 1, "light", {})

assert len(furniture) == 80, f"Muebles: {len(furniture)} items, deberian ser 80"

# ── ACABADOS (40) ───────────────────────────────────────────────

finishes = []
def a(name, desc, price, unlock, surface, color, pattern):
    finishes.append({
        "catalogId": make_id("deco_" if surface == "wall" else "floor_", name),
        "displayName": name,
        "description": desc,
        "price": price,
        "unlockLevel": unlock,
        "tags": [],
        "surface": surface,
        "baseColor": color,
        "pattern": pattern
    })

# Walls (20)
walls = [
    ("Pared nube blanca", "Blanco roto con textura suave. La pared de toda la vida.", 5, 1, "wall", "#F5F0E8", "liso"),
    ("Pared cielo diurno", "Azul celeste con degradado sutil. Como vivir dentro del cielo.", 10, 2, "wall", "#B8D8F0", "liso"),
    ("Pared atardecer nimbo", "Naranja y rosa mezclados. La hora dorada todo el dia.", 15, 3, "wall", "#F0A898", "liso"),
    ("Pared noche estrellada", "Azul oscuro salpicado de puntitos brillantes. Para soñadores.", 20, 5, "wall", "#1A2040", "liso"),
    ("Pared de rayas verticales crema", "Rayas alternas en dos tonos crema. Amplia visualmente.", 12, 2, "wall", "#F5ECD7", "rayas"),
    ("Pared de rayas marineras", "Rayas horizontales azul marino y blanco. Brisa marina en casa.", 15, 4, "wall", "#224488", "rayas"),
    ("Pared de cuadros rojos", "Cuadros tartan rojos y verdes. Calida y acogedora.", 18, 5, "wall", "#CC4444", "cuadros"),
    ("Pared de cuadros vichy azul", "Cuadros pequeños azul claro y blanco. Estilo cocina campestre.", 15, 3, "wall", "#88AACC", "cuadros"),
    ("Pared floral primaveral", "Flores pequeñas sobre fondo crema. Un jardin en cada habitacion.", 20, 6, "wall", "#F5ECD7", "flores"),
    ("Pared floral silvestre", "Flores silvestres sobre fondo verde suave. Pradera interior.", 22, 7, "wall", "#C8D8C0", "flores"),
    ("Pared de piedra caliza", "Piedra clara con vetas grises. Solida como la isla misma.", 25, 8, "wall", "#E0D8C8", "piedra"),
    ("Pared de ladrillo visto", "Ladrillo rojizo con juntas blancas. Estilo loft neoyorquino en las nubes.", 30, 9, "wall", "#B86040", "piedra"),
    ("Pared de madera clara", "Listones horizontales de pino. Calidez nordica.", 20, 4, "wall", "#D4B896", "madera"),
    ("Pared de madera oscura", "Paneles de nogal. Elegancia clasica de biblioteca.", 30, 8, "wall", "#5C3A28", "madera"),
    ("Pared de azulejo blanco", "Azulejos blancos brillantes. Para cocinas y baños impecables.", 25, 6, "wall", "#FFFFFF", "azulejo"),
    ("Pared de azulejo hidraulico", "Azulejos con dibujos geometricos de colores. Obra de arte en la pared.", 35, 12, "wall", "#4488AA", "azulejo"),
    ("Pared de estuco mediterraneo", "Estuco rugoso color terracota. Calor del sur en las alturas.", 28, 10, "wall", "#D08060", "liso"),
    ("Pared verde botanico", "Verde oscuro mate. Para los que aman la jungla.", 18, 5, "wall", "#3B5E3B", "liso"),
    ("Pared de papel arroz dorado", "Papel de arroz con motivos dorados. Inspiracion oriental.", 40, 15, "wall", "#F0E8C8", "flores"),
    ("Pared galaxia profunda", "Violeta oscuro con nebulosas. El universo en tu salon.", 45, 20, "wall", "#2A1040", "liso"),
]

for w in walls:
    a(*w)

# Floors (20)
floors = [
    ("Suelo de madera de roble", "Tablones de roble claro. El suelo que cruje con historia.", 10, 1, "floor", "#D4B896", "madera"),
    ("Suelo de parquet espiga", "Parquet en patron de espiga. Artesania pura.", 20, 4, "floor", "#C4A070", "madera"),
    ("Suelo de madera oscura", "Tablones de nogal encerado. Refleja la luz de las lamparas.", 18, 3, "floor", "#4A3020", "madera"),
    ("Suelo de baldosa blanca", "Baldosas blancas cuadradas. Limpias y luminosas.", 8, 1, "floor", "#F8F4F0", "azulejo"),
    ("Suelo de baldosa damero", "Ajedrez blanco y negro. Un clasico que nunca falla.", 15, 3, "floor", "#FFFFFF", "cuadros"),
    ("Suelo de baldosa hidraulica", "Baldosas con mosaico de colores. Cada una es distinta.", 30, 8, "floor", "#4488AA", "flores"),
    ("Suelo de piedra natural", "Losa de piedra grisacea. Fresca en verano, firme siempre.", 12, 2, "floor", "#B0A898", "piedra"),
    ("Suelo de marmol blanco", "Marmol pulido con vetas grises. Lujo bajo los pies.", 35, 10, "floor", "#F0ECE4", "piedra"),
    ("Suelo de terrazo colorido", "Terrazo con motas de colores. Retro y alegre.", 25, 6, "floor", "#E8E0D8", "piedra"),
    ("Suelo de linoleo verde", "Linoleo verde manzana. Practico y con personalidad.", 8, 1, "floor", "#88BB88", "liso"),
    ("Suelo de vinilo imitacion madera", "Vinilo que parece madera pero no se ralla. Pragmatico.", 6, 1, "floor", "#C8A882", "madera"),
    ("Suelo de canto rodado", "Guijarros lisos encastrados. Como pasear por la orilla.", 22, 7, "floor", "#C0B8A8", "piedra"),
    ("Suelo de alfombra gris", "Moqueta gris suave. Silencio absoluto al caminar.", 14, 2, "floor", "#C0C0C0", "liso"),
    ("Suelo de baldosa roja rustica", "Baldosas de barro cocido. Sabor a casa de pueblo.", 16, 4, "floor", "#B85040", "azulejo"),
    ("Suelo de losas hexagonales", "Losetas hexagonales grises y blancas. Geometria moderna.", 28, 9, "floor", "#D0D0D0", "azulejo"),
    ("Suelo de bambu natural", "Lamas de bambu entrelazado. Sostenible y zen.", 18, 5, "floor", "#C8B878", "madera"),
    ("Suelo de moqueta azul cielo", "Moqueta azul mullida. Como caminar sobre una nube.", 20, 5, "floor", "#8899BB", "liso"),
    ("Suelo de ceramica artesanal", "Ceramica pintada a mano con motivos de la isla.", 32, 12, "floor", "#E8C070", "flores"),
    ("Suelo de resina blanca brillante", "Resina pulida blanca. Moderno, minimalista, impecable.", 40, 15, "floor", "#F8F8F8", "liso"),
    ("Suelo de corcho natural", "Corcho prensado. Calido, ecologico y amortigua las pisadas.", 15, 3, "floor", "#C89868", "liso"),
]

for f_ in floors:
    a(*f_)

assert len(finishes) == 40, f"Acabados: {len(finishes)} items, deberian ser 40"

# ── Escribir JSON ───────────────────────────────────────────────

catalogs = [
    ("catalogo_comida.json", "Food", foods),
    ("catalogo_ropa.json", "Clothing", clothes),
    ("catalogo_muebles.json", "Furniture", furniture),
    ("catalogo_acabados.json", "Finishes", finishes),
]

for filename, category, items in catalogs:
    data = {"version": 1, "category": category, "items": items}
    path = os.path.join(OUT, filename)
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, ensure_ascii=False, indent=2)
    print(f"Escrito {path} — {len(items)} items")

print("\n✅ Los 4 catálogos se han escrito correctamente.")
