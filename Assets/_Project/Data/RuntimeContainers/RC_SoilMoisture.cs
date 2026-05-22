using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the soil moisture proxy (sub-étape 9α).
    /// Consumed by the meadow shader binding to drive the dry↔moist
    /// gradient of the prairie sprites. Single writer
    /// (<c>SimulationRunner</c>), many readers.
    /// <para>
    /// The proxy is unit-range by construction so the raw and normalized
    /// channels carry the same value — both are written together to keep
    /// the API symmetric with sibling RCs (Hero KPIs that DO carry a
    /// unit on the raw channel — m, t/ha, €/ha/yr).
    /// </para>
    /// <para>
    /// Per CLAUDE.md §9 (sensor primacy), the value is derived from
    /// <see cref="Bocage.SimulationCore.Model.EcosystemModel.WaterTableDepth"/>,
    /// which itself maps to the piezometer sensor — no calendar input,
    /// no scenic ambient cue.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_SoilMoisture",
        fileName = "RC_SoilMoisture")]
    public sealed class RC_SoilMoisture : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float moisture01;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float Moisture01 => moisture01;
        public float Normalized01 => normalized01;

        public void Set(float newMoisture01, float newNormalized01)
        {
            if (moisture01 == newMoisture01 && normalized01 == newNormalized01) return;
            moisture01 = newMoisture01;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(moisture01);
        }

        public void ResetToZero()
        {
            moisture01 = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
