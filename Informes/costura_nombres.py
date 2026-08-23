#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Costura de Isla Nimbo — comprobación repetible de referencias por nombre.

Uso:  python3 /tmp/opencode/costura_nombres.py [raiz_del_proyecto]

Comprueba, sobre el árbol actual:
  1. ids de zona (IslandLayout) contra todo literal "zona_*" del código
  2. ids de tienda (ShopDefinition.All) contra IslandLayout.ShopId y UiRoot.Show(...)
  3. seedIds del catálogo de cultivos contra el switch de FarmView.LookFor
  4. catalogId de todos los catálogos JSON contra literales escritos a mano en C#
     (ids que aparecen en el código y no existen en ningún catálogo)
  5. ids de receta, logro, evento de aldea, nodo de recolección
  6. nombres buscados con GameObject.Find / transform.Find contra los nombres
     que alguien crea realmente
  7. propiedades de shader usadas desde C# contra los bloques Properties de .shader
  8. eventos del EventBus: struct en GameEvents.cs vs Publish/Subscribe
  9. servicios: TryGet<T>/Get<T> vs Register<T>

Salida: lista de costuras rotas con fichero:línea. Código de salida 0 si todo
encaja, 1 si hay alguna rota.
"""
import json
import os
import re
import sys

RAIZ = sys.argv[1] if len(sys.argv) > 1 else "/home/vaknadesu/Proyectos/isla-nimbo"
SCRIPTS = os.path.join(RAIZ, "Assets/_Project/Scripts")
TESTS = os.path.join(RAIZ, "Assets/_Project/Tests")
CONFIG = os.path.join(RAIZ, "Assets/_Project/Resources/Config")
SHADERS = os.path.join(RAIZ, "Assets/_Project/Shaders")

rotas = []          # (categoria, detalle)
avisos = []         # cosas preexistentes fuera del alcance, solo informativo


def cs_files():
    for base in (SCRIPTS, TESTS):
        for dirpath, _, files in os.walk(base):
            for f in files:
                if f.endswith(".cs"):
                    yield os.path.join(dirpath, f)


def shader_files():
    for dirpath, _, files in os.walk(SHADERS):
        for f in files:
            if f.endswith(".shader") or f.endswith(".hlsl") or f.endswith(".cginc"):
                yield os.path.join(dirpath, f)


def read(path):
    with open(path, encoding="utf-8") as fh:
        return fh.read()


def line_of(text, pos):
    return text.count("\n", 0, pos) + 1


def rel(path):
    return os.path.relpath(path, RAIZ)


# ── 0. carga todos los .cs una vez ───────────────────────────────────────────
CS = {}   # ruta -> contenido
for path in cs_files():
    CS[path] = read(path)

# ── 1. zonas ────────────────────────────────────────────────────────────────
layout_path = None
for p in CS:
    if p.endswith("Island/Zones/IslandLayout.cs"):
        layout_path = p
zonas_declaradas = set(re.findall(r'ZoneId\s*=\s*"([^"]+)"', CS[layout_path]))

refs_zona = []
patron_zona = re.compile(r'"(zona_[a-z_]+)"')
for p, text in CS.items():
    if p.endswith("IslandLayout.cs"):
        continue
    for m in patron_zona.finditer(text):
        refs_zona.append((p, line_of(text, m.start()), m.group(1)))

for p, linea, zid in sorted(refs_zona):
    if zid not in zonas_declaradas:
        rotas.append(("ZONA", f"{rel(p)}:{linea} usa '{zid}' — no existe en IslandLayout "
                              f"(declaradas: {sorted(zonas_declaradas)})"))

# ── 2. tiendas ──────────────────────────────────────────────────────────────
shop_def_path = next(p for p in CS if p.endswith("Economy/Shops/ShopDefinition.cs"))
tiendas_declaradas = set(re.findall(r'new ShopDefinition\(\s*"([^"]+)"', CS[shop_def_path]))
# los ids también pueden venir como primer argumento tras salto de línea; reintentamos
if not tiendas_declaradas:
    tiendas_declaradas = set(re.findall(r'ShopDefinition\(\s*\n?\s*"([^"]+)"', CS[shop_def_path]))

refs_tienda = []
patron_shopid = re.compile(r'ShopId\s*=\s*"([^"]+)"')
patron_show = re.compile(r'\.Show\(\s*"([^"]+)"')
for p, text in CS.items():
    if p.endswith("ShopDefinition.cs"):
        continue
    for m in patron_shopid.finditer(text):
        refs_tienda.append((p, line_of(text, m.start()), m.group(1)))
    for m in patron_show.finditer(text):
        refs_tienda.append((p, line_of(text, m.start()), m.group(1)))

for p, linea, sid in sorted(refs_tienda):
    if sid not in tiendas_declaradas:
        rotas.append(("TIENDA", f"{rel(p)}:{linea} usa '{sid}' — no existe en ShopDefinition "
                                f"(declaradas: {sorted(tiendas_declaradas)})"))

# ── 3. cultivos ─────────────────────────────────────────────────────────────
cat_cultivos = json.load(open(os.path.join(CONFIG, "catalogo_cultivos.json"), encoding="utf-8"))
seeds_json = {c["seedId"] for c in cat_cultivos["crops"]} if "crops" in cat_cultivos else \
             {c["seedId"] for c in cat_cultivos.get("items", cat_cultivos.get("crops", []))}
farmview_path = next(p for p in CS if p.endswith("Art/World/FarmView.cs"))
cases_farmview = set(re.findall(r'"(seed_[a-z]+)"\s*=>', CS[farmview_path]))
for s in sorted(seeds_json - cases_farmview):
    rotas.append(("CULTIVO", f"{rel(farmview_path)}: sin forma para '{s}' del catálogo "
                             f"(caería en la genérica sin aviso)"))
for s in sorted(cases_farmview - seeds_json):
    avisos.append(f"CULTIVO: FarmView dibuja '{s}' que ya no está en el catálogo (muerto)")

# otros literales seed_* escritos a mano fuera del catálogo y del switch
for p, text in CS.items():
    if p is farmview_path or "/Tests/" in p.replace(RAIZ, ""):
        continue
    for m in re.finditer(r'"(seed_[a-z]+)"', text):
        sid = m.group(1)
        if sid not in seeds_json:
            rotas.append(("CULTIVO", f"{rel(p)}:{line_of(text, m.start())} usa '{sid}' — "
                                     f"no está en catalogo_cultivos.json"))

# ── 4. catálogos de objetos: ids declarados vs ids usados a mano ────────────
# Solo código de producción: los tests construyen catálogos propios (fixtures).
ids_catalogo = {}
for fname in os.listdir(CONFIG):
    if not fname.endswith(".json") or fname == "personalidades.json":
        continue
    data = json.load(open(os.path.join(CONFIG, fname), encoding="utf-8"))
    items = data.get("items", [])
    for it in items:
        for clave in ("catalogId", "achievementId", "recipeId", "nodeId", "eventId",
                      "dropId", "outputId"):
            if clave in it:
                ids_catalogo.setdefault(it[clave], []).append(fname)

# literales tipo prefijo_id en C#: candidatas a id de objeto
PATRON_ID = re.compile(r'"((?:food|furniture|decor|clothing|tool|material|resource|'
                       r'gift|recipe|logro|node|drop|acabado|wallpaper|flooring)[a-z0-9_]*)"')
EXCLUIR_FICHEROS = ("FarmView.cs", "ShopDefinition.cs", "IslandLayout.cs")
for p, text in CS.items():
    if "/Tests/" in p or any(p.endswith(x) for x in EXCLUIR_FICHEROS):
        continue
    lineas = text.split("\n")
    for m in PATRON_ID.finditer(text):
        iid = m.group(1)
        if iid in ids_catalogo:
            continue
        linea = line_of(text, m.start())
        ctx = lineas[linea - 1]
        # comentarios de documentación no son código
        if ctx.lstrip().startswith("///") or ctx.lstrip().startswith("//"):
            continue
        # prefijos y etiquetas de función no son ids: StartsWith/Replace/== con tag
        if re.search(r'(StartsWith|EndsWith|Replace|Function\s*==|function\s*==|kind\s*==)\s*\(?\s*"', ctx):
            continue
        # nombres de elemento de interfaz (UiTheme.Card("x"), Add("x")) no son ids de catálogo
        if re.search(r'UiTheme\.\w+\(\s*$', ctx[:m.start()]) or \
                re.search(r'(Card|Add|Q|Name)\(\s*"' + re.escape(iid) + '"', ctx):
            continue
        rotas.append(("OBJETO?", f"{rel(p)}:{linea} usa '{iid}' — "
                                 f"no está en ningún catálogo JSON"))

# ── 5. eventos de aldea (VillageEventStarted.EventId) ───────────────────────
ve_paths = [p for p in CS if "/Events/" in p]
ids_evento_aldea = set()
for p in ve_paths:
    ids_evento_aldea |= set(re.findall(r'(?:EventId|eventId)\s*=\s*"([^"]+)"', CS[p]))
    ids_evento_aldea |= set(re.findall(r'new VillageEventDef[^"]*"([^"]+)"', CS[p]))
for p, text in CS.items():
    for m in re.finditer(r'"(evento_[a-z_]+)"', text):
        eid = m.group(1)
        if eid == "evento_puente_aparece":
            avisos.append(f"FLAG: {rel(p)}:{line_of(text, m.start())} exige flag "
                          f"'{eid}' y nadie llama a SaveGame.SetFlag con él (preexistente)")
            continue
        if ids_evento_aldea and eid not in ids_evento_aldea:
            rotas.append(("EVENTO-ALDEA", f"{rel(p)}:{line_of(text, m.start())} usa '{eid}'"))

# ── 6. GameObject.Find / transform.Find ─────────────────────────────────────
# Los nombres pueden nacer en código (new GameObject("X")) o en la escena (m_Name: X).
nombres_existentes = set()
for p, text in CS.items():
    nombres_existentes |= set(re.findall(r'new GameObject\(\s*"([^"]+)"', text))
    nombres_existentes |= set(re.findall(r'name\s*=\s*"([^"]+)"', text))
for dirpath, _, files in os.walk(os.path.join(RAIZ, "Assets/_Project/Scenes")):
    for f in files:
        if f.endswith(".unity"):
            nombres_existentes |= set(re.findall(r'm_Name: (.+)', read(os.path.join(dirpath, f))))

nombres_buscados = []
for p, text in CS.items():
    if "/Tests/" in p:
        continue
    for m in re.finditer(r'(?:GameObject\.Find|transform\.Find)\(\s*"([^"]+)"\)', text):
        nombres_buscados.append((p, line_of(text, m.start()), m.group(1)))
for p, linea, nombre in sorted(nombres_buscados):
    if nombre not in nombres_existentes:
        rotas.append(("FIND", f"{rel(p)}:{linea} busca '{nombre}' y nadie lo crea con ese nombre"))

# ── 7. propiedades de shader ────────────────────────────────────────────────
# _Surface/_Blend/_SrcBlend/... son propiedades estándar de URP: no son costura nuestra.
URP_STD = {"_Surface", "_Blend", "_SrcBlend", "_DstBlend", "_ZWrite", "_ZWriteControl",
           "_AlphaClip", "_Metallic", "_Smoothness", "_Cull", "_QueueOffset",
           "_BaseMap", "_BaseColor", "_BumpMap", "_EmissionColor"}
props_shader = set()
for p in shader_files():
    props_shader |= set(re.findall(r'^\s*_([A-Za-z0-9_]+)\s*\("', read(p), re.M))
usos_shader = []
for p, text in CS.items():
    for m in re.finditer(r'Shader\.PropertyToID\(\s*"(_?[A-Za-z0-9_]+)"\)', text):
        usos_shader.append((p, line_of(text, m.start()), m.group(1)))
    for m in re.finditer(r'material\.(?:Set[A-Za-z]+)\(\s*"(_?[A-Za-z0-9_]+)"', text):
        usos_shader.append((p, line_of(text, m.start()), m.group(1)))
norm = {x.lstrip("_") for x in props_shader}
for p, linea, prop in sorted(usos_shader):
    if prop.lstrip("_") not in norm and prop.lstrip("_") not in {x.lstrip("_") for x in URP_STD} \
            and props_shader:
        rotas.append(("SHADER", f"{rel(p)}:{linea} usa la propiedad '{prop}' — "
                                f"no declarada en ningún .shader"))

# ── 8. eventos del bus ──────────────────────────────────────────────────────
ge_path = next(p for p in CS if p.endswith("Core/Events/GameEvents.cs"))
eventos = set(re.findall(r'public readonly struct (\w+)', CS[ge_path]))
publica = {}
suscribe = {}
for p, text in CS.items():
    for m in re.finditer(r'\.Publish(?:<([\w.]+)>)?\(', text):
        publica.setdefault(m.group(1), []).append((rel(p), line_of(text, m.start())))
    # Publish(new X ...) sin genérico explícito
    for m in re.finditer(r'Publish\(new (\w+)', text):
        publica.setdefault(m.group(1), []).append((rel(p), line_of(text, m.start())))
    # suscripción explícita: Subscribe<X>(...)
    for m in re.finditer(r'(?:Bus|EventBus)?\.?Subscribe<([\w.]+)>', text):
        suscribe.setdefault(m.group(1).split(".")[-1], []).append((rel(p), line_of(text, m.start())))
    # suscripción por grupo de métodos: Subscribe(_campo) con campo Action<X>
    for m in re.finditer(r'Subscribe\(\s*(_?[A-Za-z_]\w*)\s*\)', text):
        campo = m.group(1)
        decl = re.search(rf'Action<([\w.]+)>\s+{re.escape(campo)}\b', text)
        if decl:
            suscribe.setdefault(decl.group(1).split(".")[-1],
                                []).append((rel(p), line_of(text, m.start())))

sin_oyente = []
sin_emisor = []
for e in sorted(eventos):
    pub = publica.get(e, [])
    sub = suscribe.get(e, [])
    if sub and not pub:
        sin_emisor.append((e, sub))
    if pub and not sub:
        sin_oyente.append((e, pub))

# ── 9. servicios ────────────────────────────────────────────────────────────
registrados = {}
pedidos = {}
for p, text in CS.items():
    for m in re.finditer(r'ServiceRegistry\.Register<([\w.]+)>', text):
        registrados.setdefault(m.group(1).split(".")[-1], []).append((rel(p), line_of(text, m.start())))
    for m in re.finditer(r'ServiceRegistry\.(?:TryGet|Get)<([\w.]+)>', text):
        pedidos.setdefault(m.group(1).split(".")[-1], []).append((rel(p), line_of(text, m.start())))
    # TryGet(out _campo) con el tipo en la declaración del campo o variable local:
    # el genérico se infiere y un grep de <T> no lo ve.
    tipos_declarados = {campo: tipo for tipo, campo in re.findall(
        r'(?:private|protected|public|internal)?\s*([A-Z]\w*)\s+(_\w+)\s*[;=]', text)}
    for m in re.finditer(r'ServiceRegistry\.TryGet\(out\s+(_\w+)', text):
        campo = m.group(1)
        tipo = tipos_declarados.get(campo)
        if tipo:
            pedidos.setdefault(tipo, []).append((rel(p), line_of(text, m.start())))

servicios_huerfanos = sorted(set(pedidos) - set(registrados))
registros_sin_pedido = sorted(set(registrados) - set(pedidos))

# ── informe ─────────────────────────────────────────────────────────────────
print("=" * 72)
print("COSTURA DE ISLA NIMBO — referencias por nombre")
print("=" * 72)

print(f"\nzonas declaradas: {len(zonas_declaradas)} · referencias a zona: {len(refs_zona)}")
print(f"tiendas declaradas: {len(tiendas_declaradas)} · referencias a tienda: {len(refs_tienda)}")
print(f"seedIds en catálogo: {len(seeds_json)} · casos en FarmView: {len(cases_farmview)}")
print(f"ids de objeto en catálogos: {len(ids_catalogo)}")

print("\n--- COSTURAS ROTAS ---")
if not rotas:
    print("(ninguna)")
for cat, det in rotas:
    print(f"[{cat}] {det}")

print("\n--- EVENTOS SIN OYENTE (se publican, nadie escucha) ---")
if not sin_oyente:
    print("(ninguno)")
for e, pubs in sin_oyente:
    print(f"  {e}: publicado en {pubs[:3]}{'…' if len(pubs) > 3 else ''}")

print("\n--- OYENTES SIN EMISOR (suscritos, nadie publica) ---")
if not sin_emisor:
    print("(ninguno)")
for e, subs in sin_emisor:
    print(f"  {e}: suscrito en {subs[:3]}{'…' if len(subs) > 3 else ''}")

print("\n--- SERVICIOS PEDIDOS Y NO REGISTRADOS ---")
if not servicios_huerfanos:
    print("(ninguno)")
for s in servicios_huerfanos:
    print(f"  {s}: pedido en {pedidos[s][:4]}")

print("\n--- REGISTRADOS SIN NADIE QUE LOS PIDA ---")
if not registros_sin_pedido:
    print("(ninguno)")
for s in registros_sin_pedido:
    print(f"  {s}: registrado en {registrados[s]}")

print("\n--- AVISOS (preexistentes o informativos) ---")
for a in avisos:
    print(f"  {a}")

print(f"\nRESUMEN: {len(rotas)} costuras rotas · {len(sin_oyente)} eventos sin oyente · "
      f"{len(sin_emisor)} oyentes sin emisor · {len(servicios_huerfanos)} servicios huérfanos")

sys.exit(1 if rotas else 0)
