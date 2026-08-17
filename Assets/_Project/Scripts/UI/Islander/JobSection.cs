using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// El bloque de trabajo de la ficha: en qué trabaja, cuánto gana y cómo cambiarlo.
    /// </summary>
    /// <remarks>
    /// Enseña la afinidad de cada oficio con ese habitante porque es la decisión
    /// interesante: el jugador ve que su Ermitaño rendiría mejor de bibliotecario que
    /// de guía turístico, y eso le enseña algo del personaje sin leer una ficha.
    /// </remarks>
    public sealed class JobSection
    {
        private readonly Label _current;
        private readonly Label _wage;
        private readonly VisualElement _options;

        private string _islanderId;

        public VisualElement Root { get; }

        public JobSection()
        {
            Root = UiTheme.Card();
            Root.Add(UiTheme.Title("Trabajo"));

            _current = UiTheme.Body("—");
            Root.Add(_current);

            _wage = UiTheme.Body("", soft: true);
            _wage.style.marginBottom = 8;
            Root.Add(_wage);

            _options = new VisualElement();
            Root.Add(_options);
        }

        public void Refresh(string islanderId)
        {
            _islanderId = islanderId;
            _options.Clear();

            if (!ServiceRegistry.TryGet<IJobService>(out var jobs)) return;
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;
            if (!registry.TryGet(islanderId, out var islander)) return;

            var job = islander.Job;

            if (job.HasJob)
            {
                _current.text = $"{NameOf(job.Kind)} · rango {job.Rank} de {JobState.MaxRank}";
                _wage.text = $"{jobs.DailyWage(islanderId)} nimbos por turno · " +
                             $"{job.ShiftsWorked} turnos hechos · {job.TotalEarned} en total";
            }
            else
            {
                _current.text = "Sin trabajo.";
                _wage.text = "No entra nada por aquí.";
            }

            BuildOptions(jobs, job.Kind);
        }

        private void BuildOptions(IJobService jobs, JobKind current)
        {
            // Repartir los trabajos de la aldea se gana (Aldea 2, §12.3). Hasta
            // entonces la ficha dice en qué trabaja cada uno pero no deja tocarlo: ser
            // el que manda tiene que costar algo, o no significa nada.
            if (!Gates.Allows(Unlock.AssignJobs, out string falta))
            {
                _options.Add(UiTheme.Body(falta, soft: true));
                return;
            }

            var available = jobs.AvailableJobs;
            if (available.Count == 0)
            {
                _options.Add(UiTheme.Body("Todavía no hay dónde trabajar en la isla.", soft: true));
                return;
            }

            for (int i = 0; i < available.Count; i++)
            {
                var kind = available[i];
                if (kind == current) continue;

                float affinity = jobs.AffinityFor(_islanderId, kind);

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 5;

                var name = UiTheme.Body(NameOf(kind));
                name.style.width = 118;
                row.Add(name);

                row.Add(UiTheme.Chip(FitLabel(affinity), FitColor(affinity)));

                var spacer = new VisualElement();
                spacer.style.flexGrow = 1;
                row.Add(spacer);

                string islanderId = _islanderId;
                var button = UiTheme.Action("Ponerle aquí", () =>
                {
                    jobs.Assign(islanderId, kind);
                    Refresh(islanderId);
                });
                button.style.backgroundColor = UiTheme.CreamDeep;
                row.Add(button);

                _options.Add(row);
            }
        }

        /// <summary>Lo bien que le iría, en palabras: un número del 0 al 1 no dice nada.</summary>
        private static string FitLabel(float affinity) =>
            affinity >= 0.78f ? "le encantaría" :
            affinity >= 0.62f ? "le iría bien" :
            affinity >= 0.45f ? "aceptable" :
                                "no es lo suyo";

        private static Color FitColor(float affinity) =>
            affinity >= 0.78f ? UiTheme.Mint :
            affinity >= 0.62f ? UiTheme.Sage :
            affinity >= 0.45f ? UiTheme.Butter :
                                UiTheme.Rose;

        private static string NameOf(JobKind kind) => kind switch
        {
            JobKind.Cook => "Cocinero",
            JobKind.Shopkeeper => "Tendero",
            JobKind.Gardener => "Jardinero",
            JobKind.Artist => "Artista",
            JobKind.Builder => "Constructor",
            JobKind.Musician => "Músico",
            JobKind.Guide => "Guía",
            JobKind.Librarian => "Bibliotecario",
            _ => "Sin trabajo",
        };
    }
}
