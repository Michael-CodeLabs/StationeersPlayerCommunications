using HarmonyLib;
using Util.Commands;

namespace BrainClock.PlayerComms
{

    /// <summary>
    /// Adds our Drunk command to the game console after the game adds their own.
    /// </summary>
    [HarmonyPatch(typeof(CommandLine), "ExecutePostLaunchCommands")]
    class CommandPatch
    {
        public static void Postfix()
        {
            SendVoiceCommand voiceCommand = new SendVoiceCommand();
            CommandLine.AddCommand("SendVoice", voiceCommand as CommandBase);
        }
    }
}
