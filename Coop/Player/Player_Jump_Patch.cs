﻿using SIT.Coop.Core.Web;
using SIT.Core.Misc;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SIT.Core.Coop.Player
{
    internal class Player_Jump_Patch : ModuleReplicationPatch
    {
        public static List<string> CallLocally = new();
        public override Type InstanceType => typeof(EFT.Player);
        public override string MethodName => "Jump";

        protected override MethodBase GetTargetMethod()
        {
            var method = ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch(EFT.Player __instance)
        {
            var result = false;
            return result;
        }

        [PatchPostfix]
        public static void PostPatch(
           EFT.Player __instance
            )
        {
            var player = __instance;

            if (CallLocally.Contains(player.Profile.AccountId))
            {
                CallLocally.Remove(player.Profile.AccountId);
                return;
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { "t", DateTime.Now.Ticks },
                { "m", "Jump" }
            };
            ServerCommunication.PostLocalPlayerData(player, dictionary, true);
        }


        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {
            if (HasProcessed(GetType(), player, dict))
                return;

            try
            {
                player.CurrentState.Jump();
            }
            catch (Exception e)
            {
                Logger.LogInfo(e);
            }

        }
    }
}
