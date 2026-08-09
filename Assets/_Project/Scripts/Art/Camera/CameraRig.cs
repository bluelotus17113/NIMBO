using UnityEngine;

namespace Nimbo.Art.CameraWork
{
    /// <summary>
    /// Dónde está la cámara o dónde se quiere que esté. El pivote es el punto que se
    /// mira, y la posición se calcula esféricamente a partir de él.
    /// </summary>
    public struct CameraPose
    {
        public Vector3 Pivot;    // el punto al que mira la cámara
        public float Distance;   // a cuánto está del pivote
        public float Yaw;        // giro horizontal en grados, [0, 360)
        public float Pitch;      // inclinación en grados; positiva mira hacia abajo
    }

    /// <summary>
    /// Toda la matemática de la cámara, separada del MonoBehaviour para poder probarla
    /// sin montar una escena. No toca Transform ni GameObjects.
    /// </summary>
    public class CameraRig
    {
        private const float MinPitch = 12f;
        private const float MaxPitch = 78f;
        private const float MinDistance = 8f;
        private const float MaxDistance = 220f;

        /// <summary>
        /// Altura mínima de la cámara sobre el nivel del prado, en metros.
        /// </summary>
        /// <remarks>
        /// Tres metros porque es lo que mide de alto una casa de la isla: por debajo
        /// de eso, acercarse a alguien que esté junto a un edificio mete la cámara
        /// dentro del tejado y se ve el interior de la malla.
        /// </remarks>
        public const float MinHeight = 3f;

        private readonly float _pivotRadiusMax;

        private CameraPose _current;
        private CameraPose _target;

        /// <summary>Suavidad de la transición: más alto = más rápido.</summary>
        public float Sharpness { get; set; } = 8f;

        public CameraPose Current => _current;
        public CameraPose Target
        {
            get => _target;
            set => _target = ClampPose(value);
        }

        public CameraRig(float islandRadius)
        {
            // El pivote no puede salir de este círculo, o el jugador se pierde en la nada.
            _pivotRadiusMax = islandRadius * 1.1f;

            _current = new CameraPose
            {
                Pivot = Vector3.zero,
                Distance = 150f,
                Yaw = 0f,
                Pitch = 45f,
            };
            _target = _current;
        }

        // ── movimiento del objetivo ────────────────────────────────────────

        /// <summary>Apunta el objetivo a un punto concreto del mundo.</summary>
        public void Frame(Vector3 point, float distance)
        {
            _target.Pivot = point;
            _target.Distance = distance;
            _target = ClampPose(_target);
        }

        /// <summary>Rota alrededor del pivote. Los topes de pitch se aplican después.</summary>
        public void Orbit(float deltaYaw, float deltaPitch)
        {
            _target.Yaw += deltaYaw;
            _target.Pitch += deltaPitch;
            _target = ClampPose(_target);
        }

        /// <summary>Acerca o aleja. Positivo acerca, negativo aleja.</summary>
        public void Zoom(float delta)
        {
            _target.Distance -= delta;
            _target = ClampPose(_target);
        }

        /// <summary>Desplaza el pivote por el plano del suelo, en coordenadas del mundo.</summary>
        public void Pan(Vector3 worldDelta)
        {
            _target.Pivot += worldDelta;
            _target = ClampPose(_target);
        }

        // ── suavizado ──────────────────────────────────────────────────────

        /// <summary>
        /// Acerca Current a Target con suavizado exponencial, independiente de los
        /// fotogramas por segundo. Con lineal la cámara arranca de golpe; con
        /// Mathf.Lerp(a, b, 0.1f) la velocidad depende de los FPS, que es un error
        /// clásico y aquí no se repite.
        /// </summary>
        public void Advance(float deltaSeconds)
        {
            float t = 1f - Mathf.Exp(-Sharpness * deltaSeconds);

            _current.Pivot = Vector3.Lerp(_current.Pivot, _target.Pivot, t);
            _current.Distance = Mathf.Lerp(_current.Distance, _target.Distance, t);
            _current.Pitch = Mathf.Lerp(_current.Pitch, _target.Pitch, t);

            // El yaw gira por el lado corto o la cámara da la vuelta entera al enfocar.
            // Mathf.DeltaAngle da la diferencia firmada más corta; añadir solo esa
            // fracción evita que vaya de 350° a 10° pasando por 180°.
            float yawDelta = Mathf.DeltaAngle(_current.Yaw, _target.Yaw);
            _current.Yaw += yawDelta * t;
            _current.Yaw = NormalizeYaw(_current.Yaw);
        }

        /// <summary>
        /// Sin transición: Current se pega a Target. Imprescindible al cargar la
        /// partida para que la cámara no entre volando desde el infinito.
        /// </summary>
        public void SnapToTarget()
        {
            _current = _target;
        }

        // ── lo que se le pone a Unity ──────────────────────────────────────

        /// <summary>Posición en el mundo calculada desde Current.</summary>
        public Vector3 Position
        {
            get
            {
                // Coordenadas esféricas alrededor del pivote:
                // la cámara está a Distance del pivote, rotada por Yaw y Pitch.
                Quaternion rot = Quaternion.Euler(_current.Pitch, _current.Yaw, 0f);
                var position = _current.Pivot + rot * Vector3.back * _current.Distance;

                // Suelo duro. Los topes de arriba se cumplen y aun así la cámara se
                // metía dentro del césped y por debajo de los tejados: con el pitch
                // en su mínimo de 12° y la distancia en su mínimo de 8, la cámara
                // queda a metro y medio sobre el pivote, que es más bajo que una
                // casa. Se sube lo justo, sin tocar el pivote: la cámara sigue
                // mirando lo mismo, solo que desde un poco más arriba.
                if (position.y < MinHeight) position.y = MinHeight;

                return position;
            }
        }

        /// <summary>Rotación que mira al pivote, calculada desde Current.</summary>
        public Quaternion Rotation
        {
            get
            {
                Vector3 toPivot = (_current.Pivot - Position).normalized;
                if (toPivot.sqrMagnitude < 0.0001f) return Quaternion.identity;
                return Quaternion.LookRotation(toPivot, Vector3.up);
            }
        }

        // ── helpers ────────────────────────────────────────────────────────

        private CameraPose ClampPose(CameraPose pose)
        {
            pose.Pitch = Mathf.Clamp(pose.Pitch, MinPitch, MaxPitch);
            pose.Distance = Mathf.Clamp(pose.Distance, MinDistance, MaxDistance);
            pose.Yaw = NormalizeYaw(pose.Yaw);

            // El pivote no sale del círculo de la isla.
            Vector2 xz = new Vector2(pose.Pivot.x, pose.Pivot.z);
            if (xz.sqrMagnitude > _pivotRadiusMax * _pivotRadiusMax)
            {
                xz = xz.normalized * _pivotRadiusMax;
                pose.Pivot.x = xz.x;
                pose.Pivot.z = xz.y;
            }

            return pose;
        }

        /// <summary>Deja el yaw en [0, 360).</summary>
        private static float NormalizeYaw(float yaw)
        {
            yaw %= 360f;
            if (yaw < 0f) yaw += 360f;
            return yaw;
        }
    }
}
