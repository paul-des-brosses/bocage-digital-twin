using System;

namespace Bocage.SimulationCore.Logging
{
    /// <summary>
    /// Three-channel logger living in Couche 1 (pure C#) so every layer can
    /// log without referencing UnityEngine. Couche 5 wires Unity's Debug.Log
    /// to these events at bootstrap.
    /// </summary>
    public static class SimLogger
    {
        public static event Action<string> OnDebug;
        public static event Action<string> OnSimulation;
        public static event Action<string> OnUserAction;

        public static void DebugLog(string message)
        {
            OnDebug?.Invoke(message);
        }

        public static void SimulationLog(string message)
        {
            OnSimulation?.Invoke(message);
        }

        public static void UserActionLog(string message)
        {
            OnUserAction?.Invoke(message);
        }
    }
}
