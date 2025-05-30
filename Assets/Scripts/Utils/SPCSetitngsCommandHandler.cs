using Assets.Scripts;
using Util.Commands;
using System.Globalization;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class SPCSettingsCommandHandler : CommandBase
    {
        public override string HelpText
        {
            get
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("SPC commands:");
                sb.AppendLine("SPC SET <Value> <Target>");
                sb.AppendLine("\tSets an audio configuration value.");
                sb.AppendLine("\tTargets: Human, Radio");
                return sb.ToString();
            }
        }

        public override string[] Arguments => new[] { "SET" };

        public override bool IsLaunchCmd => false;

        public override string Execute(string[] args)
        {
            // ConsoleWindow.PrintAction($"[SPC] Execute called with args: {string.Join(", ", args)}");

            if (args.Length == 0)
            {
                // ConsoleWindow.PrintError("No subcommand provided. Usage:\n" + HelpText);
                return null;
            }

            string subcommand = args[0].ToLowerInvariant();
            switch (subcommand)
            {
                case "set":
                    return SetAudioConfig(args);

                default:
                    // ConsoleWindow.PrintError($"Unknown subcommand '{args[0]}'. Usage:\n" + HelpText);
                    return null;
            }
        }

        private static string SetAudioConfig(string[] args)
        {
            // ConsoleWindow.PrintAction($"[SPC] SetAudioConfig called with: {string.Join(", ", args)}");

            if (ConsoleWindow.IsInvalidSyntax(args, 3))
            {
                ConsoleWindow.PrintError("Invalid syntax. Usage: SPC SET <Value> <Target>");
                return null;
            }

            if (!CommandBase.Get(args, 1, "Value", out float value))
            {
                ConsoleWindow.PrintError($"Argument given is not a valid float '{args[1]}'");
                return null;
            }

            var playerCommunicationsManager = PlayerCommunicationsManager.Instance;
            var audioClipInterfaceHuman = PlayerCommunicationsManager.Instance.GetComponent<AudioClipInterfaceHuman>();
            var audioClipInterfaceRadio = PlayerCommunicationsManager.Instance.GetComponent<AudioClipInterfaceRadio>();
            string target = args[2].ToLowerInvariant();
            switch (target)
            {
                case "human":
                    audioClipInterfaceHuman.VolumeMultiplier = value;
                    // ConsoleWindow.PrintAction($"Changed Human Voice Volume Multiplier to {value:0.00}");
                    NotifyAudioConfigChanged("human", value);
                    break;

                case "radio":
                    Radio.VolumeMultiplier = value;
                    // ConsoleWindow.PrintAction($"Changed Radio Voice Volume Multiplier to {value:0.00}");
                    NotifyAudioConfigChanged("radio", value);
                    break;

                default:
                    // ConsoleWindow.PrintError($"Unknown target '{args[2]}'. Valid options are: Human, Radio.");
                    break;
            }

            return null;
        }

        public static void NotifyAudioConfigChanged(string target, float value)
        {
            // ConsoleWindow.PrintAction($"[SPC] Notifying system: {target} = {value:0.00}");
            StationeersPlayerCommunications.AudioConfigChanged(target, value);
        }
    }
}
