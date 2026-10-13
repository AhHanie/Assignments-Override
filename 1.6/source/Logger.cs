using System;
using System.Diagnostics;
using Verse;

namespace Assignment_Overrides
{
    public static class Logger
    {
        [Conditional("DEBUG")]
        public static void Message(string message)
        {
            Log.Message("[AssignmentOverrides] " + message);
        }

        [Conditional("DEBUG")]
        public static void Warning(string message)
        {
            Log.Warning("[AssignmentOverrides] " + message);
        }

        [Conditional("DEBUG")]
        public static void Error(string message)
        {
            Log.Error("[AssignmentOverrides] " + message);
        }

        [Conditional("DEBUG")]
        public static void Exception(Exception exception, string context = null)
        {
            if (exception == null)
            {
                return;
            }

            var prefix = string.IsNullOrWhiteSpace(context)
                ? "[AssignmentOverrides] "
                : "[AssignmentOverrides] " + context + ": ";
            Log.Error(prefix + exception);
        }
    }
}
