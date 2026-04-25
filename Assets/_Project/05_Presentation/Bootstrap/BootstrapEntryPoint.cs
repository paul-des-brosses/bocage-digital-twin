using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Bootstrap
{
    /// <summary>
    /// Entry point of the application. Wires the pure-C# SimLogger to Unity's
    /// console so every layer below Presentation can log without referencing
    /// UnityEngine. Attached to the _Bootstrap GameObject in Main.unity.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class BootstrapEntryPoint : MonoBehaviour
    {
        private void Awake()
        {
            SimLogger.OnDebug += HandleDebug;
            SimLogger.OnSimulation += HandleSimulation;
            SimLogger.OnUserAction += HandleUserAction;

            SimLogger.DebugLog("[Presentation] bootstrap OK");
        }

        private void OnDestroy()
        {
            SimLogger.OnDebug -= HandleDebug;
            SimLogger.OnSimulation -= HandleSimulation;
            SimLogger.OnUserAction -= HandleUserAction;
        }

        private static void HandleDebug(string message)
        {
            Debug.Log("[Debug] " + message);
        }

        private static void HandleSimulation(string message)
        {
            Debug.Log("[Sim] " + message);
        }

        private static void HandleUserAction(string message)
        {
            Debug.Log("[UserAction] " + message);
        }
    }
}
