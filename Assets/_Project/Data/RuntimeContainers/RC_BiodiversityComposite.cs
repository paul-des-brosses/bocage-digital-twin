using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the composite biodiversity Hero KPI
    /// (sub-étape 8b). Aggregates fauna abundance, hedge density and
    /// inverse water-table depth into a single <c>[0, 1]</c> score
    /// (cf <c>BiodiversityCompositeIndicator</c>).
    /// <para>
    /// Single writer (<c>RefonteSimulationRunner</c>), many readers (UI label
    /// binding, gauges). The score is unit-range by construction so
    /// the raw and normalized channels carry the same value — both are
    /// written together to keep the API symmetric with sibling RCs.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_BiodiversityComposite",
        fileName = "RC_BiodiversityComposite")]
    public sealed class RC_BiodiversityComposite : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float score;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float Score => score;
        public float Normalized01 => normalized01;

        public void Set(float newScore, float newNormalized01)
        {
            if (score == newScore && normalized01 == newNormalized01) return;
            score = newScore;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(score);
        }

        public void ResetToZero()
        {
            score = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
