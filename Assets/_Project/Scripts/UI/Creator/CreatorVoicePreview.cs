// ═══════════════════════════════════════════════════════════════════════
//  COSTURA PENDIENTE — no es un fichero más del creador: es un aviso.
//
//  Este evento debería vivir en Nimbo.Core/Events/GameEvents.cs (sección
//  «partida»), y AudioDirector (Nimbo.Art/Audio) debería suscribirse a él
//  para sintetizar la muestra con VoiceSynth. Ninguno de los dos ficheros
//  es de este encargo, así que el struct se define aquí —en el namespace
//  que le corresponde, para que el que lo aplique no tenga que tocar ni
//  una línea del panel— y viaja con estas instrucciones:
//
//  1. Mover el struct de abajo tal cual a GameEvents.cs.
//  2. BORRAR ESTE FICHERO. Si no, Nimbo.UI tendrá dos tipos con el mismo
//     nombre completo y dejará de compilar: el error señalará aquí.
//  3. En AudioDirector.OnEnable (~línea 110), junto a los demás:
//         EventBus.Subscribe<VoicePreviewRequested>(OnVoicePreview);
//     y en OnDisable (~línea 132) su Unsubscribe. El manejador, junto a
//     SpeakLine (línea 349), del que es gemelo sin censo:
//
//         private const string LineaDeMuestra = "¡Hola, hola, isla!";
//
//         private void OnVoicePreview(VoicePreviewRequested evt)
//         {
//             if (_currentVoice != null)
//             {
//                 _voice.Stop();
//                 Destroy(_currentVoice);
//             }
//             _currentVoice = VoiceSynth.Speak(evt.Voice, evt.Line, "creador");
//             _voice.clip = _currentVoice;
//             _voice.Play();
//         }
//
//  Mientras nadie aplique esto, publicar el evento es un no-op inofensivo:
//  el canal existe y no tiene oyentes. El creador ya deja pedido el sonido;
//  falta quien lo conteste.
// ═══════════════════════════════════════════════════════════════════════

namespace Nimbo.Core.Events
{
    /// <summary>
    /// Alguien quiere oír cómo suena una voz antes de decidirse: el creador
    /// de personajes, cada vez que el jugador toca un timbre.
    /// </summary>
    /// <remarks>
    /// Va por aviso y no por llamada porque quien lo pide (<c>Nimbo.UI</c>) y
    /// quien sabe sintetizar (<c>Nimbo.Art</c>) no se ven en el grafo de
    /// ensamblados. La frase viaja dentro porque la duración de la muestra es
    /// parte de lo que se oye: dos sílabas no dejan juzgar un timbre.
    /// </remarks>
    public readonly struct VoicePreviewRequested
    {
        public readonly Data.Islanders.VoiceConfig Voice;
        public readonly string Line;

        public VoicePreviewRequested(Data.Islanders.VoiceConfig voice, string line)
        {
            Voice = voice; Line = line;
        }
    }
}
