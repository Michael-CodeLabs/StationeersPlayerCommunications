using Assets.Scripts.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BrainClock.PlayerComms
{

    /*
    [HarmonyPatch(typeof(NetworkBase), "DeserializeReceivedData")]
    public static class DeserializeReceivedDataTranspiler
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            // Find the instruction where message type checks happen
            for (int i = 0; i < code.Count; i++)
            {
                // Looking for `obj2 is NetworkMessages.Handshake || obj2 is NetworkMessages.VerifyPlayerRequest ...`
                if (code[i].opcode == OpCodes.Isinst) // Checks for type casting
                {
                    Type originalType = code[i].operand as Type;

                    // Add our custom message types to the allowed list
                    if (originalType == typeof(NetworkMessages.Handshake))  // Found the start of the message checks
                    {
                        // Inject additional type checks
                        code.Insert(i + 1, new CodeInstruction(OpCodes.Dup)); // Duplicate obj2
                        code.Insert(i + 2, new CodeInstruction(OpCodes.Isinst, typeof(VoiceMessage))); // Check for MyCustomMessage
                        code.Insert(i + 3, new CodeInstruction(OpCodes.Brtrue_S, code[i + 4].operand)); // If true, jump to existing success condition

                        //code.Insert(i + 4, new CodeInstruction(OpCodes.Dup)); // Duplicate obj2
                        //code.Insert(i + 5, new CodeInstruction(OpCodes.Isinst, typeof(MyOtherMessage))); // Check for MyOtherMessage
                        //code.Insert(i + 6, new CodeInstruction(OpCodes.Brtrue_S, code[i + 4].operand)); // If true, jump to existing success condition

                        break; // Exit after modification
                    }
                }
            }

            return code.AsEnumerable();
        }
    }
    */
}
