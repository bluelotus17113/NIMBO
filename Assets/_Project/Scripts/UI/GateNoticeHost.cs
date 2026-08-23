using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI
{
    /// <summary>
    /// El anfitrión de la franja de avisos: la mantiene colgada y le da el latido.
    /// </summary>
    /// <remarks>
    /// Se crea solo al entrar en play (una vez, idempotente) y no depende de nadie:
    /// ni UiRoot ni los paneles saben que existe, que era el encargo — llegar a los
    /// siete sitios que escriben explicaciones sin tocar uno por uno.
    ///
    /// Va una franja **por documento**, y no una sola global: la escena Isla trae dos
    /// UIDocument (el juego y el menú), y la primera versión se colgaba al primero
    /// que encontraba — media veces al del menú — y se pasaba el pase entero mirando
    /// un árbol donde nunca iba a haber una explicación. Medido en la primera corrida
    /// de AvisosEnLaIslaTests: las dos pruebas en rojo con la franja viva pero
    /// sorda. Colgada una por documento, la que sobra se queda apagada sola porque
    /// su árbol no tiene tooltips.
    ///
    /// Se recuela solo. UiRoot vacía la raíz del documento cada vez que monta
    /// (UiRoot.cs:100) y con ella se lleva cualquier cosa colgada; al fotograma
    /// siguiente vuelve a colgarse. Y los documentos que mueren con la escena se
    /// podan del diccionario, porque Unity los deja «falsamente vivos» como claves.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GateNoticeHost : MonoBehaviour
    {
        private readonly Dictionary<UIDocument, GateNotice> _notices = new();

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Update()
        {
            Podar();

            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;

                if (!_notices.TryGetValue(document, out var notice))
                    _notices[document] = notice = new GateNotice();

                // La primera vez y cada vez que un Mount la descolgue.
                if (notice.Root.panel == null) notice.ColocarEn(document.rootVisualElement);

                notice.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            foreach (var pair in _notices)
                if (pair.Key != null && pair.Value != null)
                    pair.Value.Root.RemoveFromHierarchy();
            _notices.Clear();
        }

        /// <summary>Fuera los documentos que ya no existen: su franja también.</summary>
        private void Podar()
        {
            List<UIDocument> muertos = null;

            foreach (var pair in _notices)
            {
                if (pair.Key != null) continue;

                muertos ??= new List<UIDocument>();
                muertos.Add(pair.Key);
            }

            if (muertos == null) return;
            foreach (var muerto in muertos)
            {
                _notices[muerto].Root.RemoveFromHierarchy();
                _notices.Remove(muerto);
            }
        }

        /// <summary>
        /// Tras la primera escena y una sola vez por entrada en play. Las pruebas de
        /// editor no lo ejecutan nunca, y el guardia lo hace inofensivo por si el
        /// dominio no se recarga entre dos entradas en play.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            if (Object.FindFirstObjectByType<GateNoticeHost>() != null) return;
            new GameObject("aviso_puertas_host").AddComponent<GateNoticeHost>();
        }
    }
}
