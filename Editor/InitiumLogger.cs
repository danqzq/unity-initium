using UnityEngine;

namespace Danqzq.Initium
{
    internal static class Logger
    {
        internal static bool IsEnabled { get; set; } = true;
        
        internal static void Log(string message)
        {
            if (!IsEnabled)
            {
                return;
            }
            Debug.Log($"<b>[Initium]</b> {message}");
        }

        internal static void LogWarning(string message)
        {
            if (!IsEnabled)
            {
                return;
            }
            Debug.LogWarning($"<b>[Initium]</b> {message}");
        }

        internal static void LogError(string message)
        {
            if (!IsEnabled)
            {
                return;
            }
            Debug.LogError($"<b>[Initium]</b> {message}");
        }
    }
}