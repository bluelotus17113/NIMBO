using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Crafting;
using Nimbo.Economy.Items;
using Nimbo.Gathering;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que la isla no se juegue igual en todas partes.
    /// </summary>
    /// <remarks>
    /// Dieciséis tipos de nodo daban seis materiales: roble, pino, abedul y tablón
    /// perdido eran todos madera, y la isla parecía variada mientras se jugaba
    /// idéntica. Peor, dos drops no tenían ni sentido — un **helecho soltaba conchas**
    /// y un arbusto de bayas soltaba fibra.
    ///
    /// Las dos reglas de abajo son las que hay que proteger, y son simétricas:
    ///
    /// - **Ningún material sin fuente.** Una receta que pide algo que no suelta nada es
    ///   una receta que no se puede hacer nunca, y no falla: simplemente el botón nunca
    ///   se enciende.
    /// - **Ningún material sin uso.** Un material que no pide ninguna receta es peso
    ///   muerto en la mochila y una promesa que la isla no cumple.
    ///
    /// Ninguna de las dos la caza el compilador ni la nota nadie hasta que un jugador
    /// se pasa media tarde juntando algo que no sirve para nada.
    /// </remarks>
    public class MaterialesTests
    {
        private ItemCatalog _catalogo;
        private NodeCatalog _nodos;
        private RecipeCatalog _recetas;

        [SetUp]
        public void SetUp()
        {
            _catalogo = new ItemCatalog();
            _nodos = new NodeCatalog();
            _recetas = new RecipeCatalog();
        }

        [Test]
        public void TodoMaterialLoSueltaAlgunNodo()
        {
            var fuentes = new HashSet<string>();
            foreach (var node in _nodos.All) fuentes.Add(node.DropId);

            foreach (var item in Materiales())
                Assert.That(fuentes, Does.Contain(item.CatalogId),
                    $"{item.CatalogId} no lo suelta ningún nodo de la isla");
        }

        [Test]
        public void TodoMaterialLoPideAlgunaReceta()
        {
            var usados = new HashSet<string>();
            foreach (var recipe in _recetas.All)
                foreach (var ing in recipe.Ingredients)
                    usados.Add(ing.CatalogId);

            foreach (var item in Materiales())
                Assert.That(usados, Does.Contain(item.CatalogId),
                    $"{item.CatalogId} no lo pide ninguna receta: es peso muerto en la " +
                    "mochila y una promesa que la isla no cumple");
        }

        [Test]
        public void TodaRecetaPideCosasQueExisten()
        {
            foreach (var recipe in _recetas.All)
                foreach (var ing in recipe.Ingredients)
                    Assert.That(_catalogo.GetItem(ing.CatalogId), Is.Not.Null,
                        $"{recipe.RecipeId} pide {ing.CatalogId}, que no está en el catálogo");
        }

        [Test]
        public void CadaFamiliaDeNodoDaAlgoDistinto()
        {
            // Los cuatro árboles daban lo mismo salvo uno, así que talar era talar y
            // daba igual cuál. Con esto, ir a por corteza es ir a por abedules.
            var porFamilia = new Dictionary<NodeKind, HashSet<string>>();
            var cuantos = new Dictionary<NodeKind, int>();

            foreach (var node in _nodos.All)
            {
                if (!porFamilia.TryGetValue(node.Kind, out var set))
                    porFamilia[node.Kind] = set = new HashSet<string>();

                set.Add(node.DropId);
                cuantos.TryGetValue(node.Kind, out int n);
                cuantos[node.Kind] = n + 1;
            }

            foreach (var pair in porFamilia)
            {
                if (cuantos[pair.Key] < 2) continue;

                Assert.That(pair.Value.Count, Is.GreaterThan(1),
                    $"los {cuantos[pair.Key]} nodos de tipo {pair.Key} sueltan todos lo " +
                    "mismo: se ven distintos y se juegan igual");
            }
        }

        [Test]
        public void UnHelechoNoSueltaConchas()
        {
            // El caso concreto que había, por si a alguien le da por volver a moverlo.
            foreach (var node in _nodos.All)
            {
                if (node.Kind != NodeKind.Herb && node.Kind != NodeKind.Flower) continue;

                Assert.That(node.DropId, Is.Not.EqualTo("mat_concha"),
                    $"{node.DisplayName} es una planta y suelta conchas");
            }
        }

        private IEnumerable<IItemDefinition> Materiales()
        {
            foreach (var item in _catalogo.ItemsOfCategory(ItemCategory.Material))
                yield return item;
        }
    }
}
