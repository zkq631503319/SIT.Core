﻿using SIT.Coop.Core.Web;
using SIT.Core.Coop;
using SIT.Core.Misc;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SIT.Coop.Core.Player
{
    internal class Player_Proceed_Meds_Patch : ModuleReplicationPatch
    {
        public override Type InstanceType => typeof(EFT.Player);

        public override string MethodName => "ProceedMeds";

        public static Dictionary<string, bool> CallLocally = new();

        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"Player_Proceed_Meds_Patch:Type is NULL");

            var method = ReflectionHelpers.GetAllMethodsForType(t).FirstOrDefault(x => x.Name == "Proceed" && x.GetParameters()[0].Name == "meds");

            //Logger.LogInfo($"PlayerOnTryProceedPatch:{t.Name}:{method.Name}");
            return method;
        }


        [PatchPrefix]
        public static bool PrePatch(
           EFT.Player __instance
            )
        {
            if (CallLocally.TryGetValue(__instance.Profile.AccountId, out var expecting) && expecting)
            {
                return true;
            }

            return false;
        }

        [PatchPostfix]
        public static void PostPatch(EFT.Player __instance
            , Meds meds, EBodyPart bodyPart, int animationVariant, bool scheduled)
        {
            if (CallLocally.TryGetValue(__instance.Profile.AccountId, out var expecting) && expecting)
            {
                CallLocally.Remove(__instance.Profile.AccountId);
                return;
            }

            // Stop Client Drone sending a Proceed back to the player
            if (__instance.TryGetComponent<PlayerReplicatedComponent>(out var prc))
            {
                if (prc.IsClientDrone)
                    return;
            }

            Dictionary<string, object> args = new();
            ItemAddressHelpers.ConvertItemAddressToDescriptor(meds.CurrentAddress, ref args);

            //Logger.LogInfo($"PlayerOnTryProceedPatch:Patch");
            args.Add("m", "ProceedMeds");
            args.Add("t", DateTime.Now.Ticks);
            args.Add("bodyPart", bodyPart.ToString());
            args.Add("item.id", meds.Id);
            args.Add("item.tpl", meds.TemplateId);
            args.Add("variant", animationVariant);
            args.Add("s", scheduled.ToString());
            ServerCommunication.PostLocalPlayerData(__instance, args);
        }

        public override void Replicated(EFT.Player player, Dictionary<string, object> dict)
        {
            if (HasProcessed(GetType(), player, dict))
                return;

            var coopGC = CoopGameComponent.GetCoopGameComponent();
            if (coopGC == null)
                return;

            var allItemsOfTemplate = player.Profile.Inventory.GetAllItemByTemplate(dict["item.tpl"].ToString());

            if (!allItemsOfTemplate.Any())
                return;

            var item = allItemsOfTemplate.FirstOrDefault(x => x.Id == dict["item.id"].ToString());

            if (item != null)
            {
                var meds = item as Meds;
                if (meds != null)
                {
                    CallLocally.Add(player.Profile.AccountId, true);
                    player.Proceed(meds, (EBodyPart)Enum.Parse(typeof(EBodyPart), dict["bodyPart"].ToString(), true), (IResult) => { }, 1, true);
                }
            }
        }
    }
}