using System.Collections.Generic;
using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// Root data definition aggregating the fauna species visible in the
    /// scene. One asset per scene; read by <see cref="FaunaPool"/> at
    /// Awake to pre-instantiate the per-trajectory sprite pool, and by
    /// <see cref="FaunaPoolBinding"/> to drive probabilistic spawns from
    /// biodiv observables.
    /// <para>
    /// Why a separate root SO from <c>SceneCompositionDefinition</c>:
    /// the fauna pool has a distinct lifecycle (driven by biodiv signal,
    /// not by static landscape composition) and a distinct authoring
    /// surface (trajectories, spawn rates), so keeping it apart matches
    /// the pattern set by <see cref="Sensors.SensorPlacementDefinition"/>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Scene/Fauna Placement Definition",
        fileName = "FaunaPlacement_Default")]
    public sealed class FaunaPlacementDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Species visible in the scene. Each species pre-instantiates one sprite per trajectory it declares.")]
        private FaunaSpeciesDefinition[] species = new FaunaSpeciesDefinition[0];

        public IReadOnlyList<FaunaSpeciesDefinition> Species => species;
    }
}
