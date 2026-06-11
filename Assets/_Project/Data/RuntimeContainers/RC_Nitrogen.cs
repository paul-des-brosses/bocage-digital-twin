using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Conteneur observable de l'azote minéral du sol (kgN/ha) — Hero KPI.
    /// Même pattern que <see cref="RC_IntegratedProfitability"/> : un
    /// seul écrivain (le SimulationRunner), plusieurs lecteurs (labels UI,
    /// panneau Climat &amp; ressources).
    /// </summary>
    [CreateAssetMenu(menuName = "Bocage/Data/RC_Nitrogen", fileName = "RC_Nitrogen")]
    public sealed class RC_Nitrogen : ScriptableObject
    {
        [SerializeField] private float kgNPerHectare;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float KgNPerHectare => kgNPerHectare;
        public float Normalized01 => normalized01;

        public void Set(float newKgNPerHectare, float newNormalized01)
        {
            if (kgNPerHectare == newKgNPerHectare && normalized01 == newNormalized01) return;
            kgNPerHectare = newKgNPerHectare;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(kgNPerHectare);
        }

        public void ResetToZero()
        {
            kgNPerHectare = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
