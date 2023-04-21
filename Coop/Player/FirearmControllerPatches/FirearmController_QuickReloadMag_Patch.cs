﻿using Newtonsoft.Json.Linq;
using SIT.Coop.Core.Web;
using SIT.Core.Misc;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SIT.Core.Coop.Player.FirearmControllerPatches
{
    public class FirearmController_QuickReloadMag_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player.FirearmController);
        public override string MethodName => "QuickReloadMag";

        protected override MethodBase GetTargetMethod()
        {
            var method = ReflectionHelpers.GetMethodForType(InstanceType, MethodName);
            return method;
        }

        public static Dictionary<string, bool> CallLocally
            = new Dictionary<string, bool>();


        [PatchPrefix]
        public static bool PrePatch(EFT.Player.FirearmController __instance, EFT.Player ____player)
        {
            var player = ____player;
            if (player == null)
                return false;

            var result = false;
            if (CallLocally.TryGetValue(player.Profile.AccountId, out var expecting) && expecting)
                result = true;

            return result;
        }

        [PatchPostfix]
        public static void PostPatch(
            EFT.Player.FirearmController __instance
            , MagazineClass magazine
            , EFT.Player ____player)
        {
            var player = ____player;
            if (player == null)
                return;

            if (CallLocally.TryGetValue(player.Profile.AccountId, out var expecting) && expecting)
            {
                CallLocally.Remove(player.Profile.AccountId);
                return;
            }


            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("t", DateTime.Now.Ticks);
            dictionary.Add("mg.id", magazine.Id);
            dictionary.Add("mg.tpl", magazine.Template);
            dictionary.Add("m", "QuickReloadMag");
            ServerCommunication.PostLocalPlayerData(player, dictionary);

        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {
            //Logger.LogInfo("FirearmController_ReloadMag_Patch:Replicated");

            var timestamp = long.Parse(dict["t"].ToString());
            if (HasProcessed(GetType(), player, dict))
                return;

            if (player.HandsController is EFT.Player.FirearmController firearmCont)
            {
                try
                {
                    var magazine = new MagazineClass(dict["mg.id"].ToString(), JObject.Parse(dict["mg.tpl"].ToString()).ToObject<MagazineTemplate>());
                    CallLocally.Add(player.Profile.AccountId, true);
                    Logger.LogInfo("Replicated: Calling Quick Reload Mag");
                    firearmCont.QuickReloadMag(magazine, (IResult) =>
                    {

                        Logger.LogInfo($"Replicated: Callback Quick Reload Mag: {IResult}");

                    });
                }
                catch (Exception e)
                {
                    Logger.LogInfo(e);
                }
            }
        }
    }
}
