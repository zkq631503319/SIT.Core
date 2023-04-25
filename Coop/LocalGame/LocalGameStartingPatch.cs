﻿//using CoopTarkovGameServer;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using SIT.Coop.Core.Matchmaker;
using SIT.Core.Coop;
using SIT.Core.Misc;
using SIT.Tarkov.Core;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.LocalGame
{
    /// <summary>
    /// Target that smethod_3 like
    /// </summary>
    public class LocalGameStartingPatch : ModulePatch
    {
        //public static EchoGameServer gameServer;
        private static ConfigFile _config;

        //private static LocalGameSpawnAICoroutinePatch gameSpawnAICoroutinePatch;

        public LocalGameStartingPatch(ConfigFile config)
        {
            _config = config;
            //gameSpawnAICoroutinePatch = new SIT.Coop.Core.LocalGame.LocalGameSpawnAICoroutinePatch(_config);
        }

        protected override MethodBase GetTargetMethod()
        {
            //foreach(var ty in SIT.Tarkov.Core.PatchConstants.EftTypes.Where(x => x.Name.StartsWith("BaseLocalGame")))
            //{
            //    Logger.LogInfo($"LocalGameStartingPatch:{ty}");
            //}
            _ = typeof(EFT.BaseLocalGame<GamePlayerOwner>);

            //var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName.StartsWith("EFT.LocalGame"));
            var t = typeof(EFT.LocalGame);
            //var t = typeof(EFT.BaseLocalGame<GamePlayerOwner>);
            if (t == null)
                Logger.LogInfo($"LocalGameStartingPatch:Type is NULL");

            var method = ReflectionHelpers.GetAllMethodsForType(t, false)
                .FirstOrDefault(x => x.GetParameters().Length >= 3
                && x.GetParameters().Any(x => x.Name.Contains("botsSettings"))
                && x.GetParameters().Any(x => x.Name.Contains("backendUrl"))
                && x.GetParameters().Any(x => x.Name.Contains("runCallback"))
                );

            Logger.LogInfo($"LocalGameStartingPatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPostfix]
        public static async void PatchPostfix(
            BaseLocalGame<GamePlayerOwner> __instance
            , Task __result
            )
        {
            await __result;

            //LocalGamePatches.LocalGameInstance = __instance;
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                Logger.LogError("GameWorld is NULL");
                return;
            }
            if (gameWorld.TryGetComponent<CoopGameComponent>(out CoopGameComponent coopGameComponent))
            {
                GameObject.Destroy(coopGameComponent);
            }

            // Hideout is SinglePlayer only. Do not create CoopGameComponent
            if (__instance.GetType().Name.Contains("HideoutGame"))
                return;

            var coopGC = gameWorld.GetOrAddComponent<CoopGameComponent>();
            if (!string.IsNullOrEmpty(MatchmakerAcceptPatches.GetGroupId()))
                coopGC.ServerId = MatchmakerAcceptPatches.GetGroupId();
            else
            {
                GameObject.Destroy(coopGameComponent);
                coopGC = null;
                Logger.LogInfo("No Server Id found, Deleting Coop Game Component");
            }

        }

        private static void EchoGameServer_OnLog(string text)
        {
            Logger.LogInfo(text);
        }

    }
}
