using Assets.Scripts;
using Assets.Scripts.Networking;
using HarmonyLib;
using Util.Commands;

namespace BrainClock.PlayerComms
{
    [HarmonyPatch(typeof(CommandLine), "ExecutePostLaunchCommands")]
    class CommandPatch
    {
        public static void Postfix()
        {
            if (NetworkManager.IsServer) 
            return;

            ConsoleWindow.PrintAction("[SPC] Registering SPC command...");
            SPCSettingsCommandHandler spcCommand = new SPCSettingsCommandHandler();
            CommandLine.AddCommand("spc", spcCommand); // lowercase for compatibility
        }
    }
}
