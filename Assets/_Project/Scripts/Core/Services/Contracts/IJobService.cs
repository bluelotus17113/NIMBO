using System.Collections.Generic;
using Nimbo.Data.Islanders;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// El trabajo de los habitantes: quién trabaja en qué, cuándo hace su turno y
    /// cuánto entra por ello.
    /// </summary>
    /// <remarks>
    /// Es la fuente principal de ingresos del jugador. Las peticiones dan propinas y
    /// los minijuegos dan premios, pero lo que sostiene la economía día a día es que
    /// los habitantes vayan a trabajar.
    /// </remarks>
    public interface IJobService
    {
        /// <summary>Los oficios que están abiertos hoy, según las zonas desbloqueadas.</summary>
        IReadOnlyList<JobKind> AvailableJobs { get; }

        /// <summary>Cuánto le pega ese oficio a ese habitante, de 0 a 1.</summary>
        float AffinityFor(string islanderId, JobKind job);

        /// <summary>El oficio que mejor le va, entre los disponibles.</summary>
        JobKind BestJobFor(string islanderId);

        /// <summary>
        /// ¿Aceptaría ese puesto si se lo ofreces?
        /// </summary>
        /// <remarks>
        /// Puede decir que no, y ahí está la mitad de la gestión: si el oficio no le
        /// pega **y además no te tiene aprecio**, se niega. Con eso sigues sugiriendo y
        /// no ordenando (§3.3), y el trabajo social pasa a tener una recompensa
        /// concreta: para colocar a la gente donde rinde, primero hay que caerle bien.
        ///
        /// Las dos condiciones a la vez y no cualquiera de ellas: a un amigo le pides
        /// un favor aunque el puesto no le guste, y un puesto que le encanta lo coge
        /// aunque apenas te conozca.
        /// </remarks>
        bool WouldAccept(string islanderId, JobKind job);

        /// <summary>Le da el puesto. Falla si el oficio no está abierto o si dice que no.</summary>
        bool Assign(string islanderId, JobKind job);

        void Quit(string islanderId);

        /// <summary>Sueldo de un turno suyo hoy, con su rango y su afinidad ya dentro.</summary>
        int DailyWage(string islanderId);

        /// <summary>
        /// Hace su turno: cobra, gasta energía y acumula para el ascenso. Devuelve lo
        /// cobrado, o 0 si hoy ya trabajó o no tiene empleo.
        /// </summary>
        int WorkShift(string islanderId);

        /// <summary>La zona donde se trabaja ese oficio, para que la IA sepa adónde ir.</summary>
        string ZoneOf(JobKind job);
    }
}
