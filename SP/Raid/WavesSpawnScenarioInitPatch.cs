﻿using BepInEx.Configuration;
using EFT;
using SIT.Core.Misc;
using SIT.Tarkov.Core;
using System;
using System.Linq;
using System.Reflection;
using static GClass1649;

namespace SIT.Core.SP.Raid
{
    internal class WavesSpawnScenarioInitPatch : ModulePatch
    {
        private static Random Random = new Random();

        private static EFT.WavesSpawnScenario CurrentInstance;

        private static ConfigFile _config;

        public WavesSpawnScenarioInitPatch(ConfigFile config)
        {
            _config = config;
        }

        protected override MethodBase GetTargetMethod()
        {
            return ReflectionHelpers.GetMethodForType(typeof(EFT.WavesSpawnScenario), "Init");
        }

        [PatchPrefix]
        public static bool PrePatch(EFT.WavesSpawnScenario __instance, WildSpawnWave[] waves)
        {
            CurrentInstance = __instance;
            var EnableAISpawnWaveSystem = _config.Bind("Coop", "EnableAISpawnWaveSystem", true
                  , new ConfigDescription("Whether to run the Wave Spawner System. Useful for testing.")).Value;

            var result = !SIT.Coop.Core.Matchmaker.MatchmakerAcceptPatches.IsClient && EnableAISpawnWaveSystem;

            
            ReflectionHelpers.SetFieldOrPropertyFromInstance(__instance, "Enabled", result);
            Wave[] spawnWaves;
            if (waves != null && result)
            {
                spawnWaves = waves
                    .Select(new Func<WildSpawnWave, Wave>(method_1)).ToArray();
            }
            else
            {
                spawnWaves = new Wave[0];
            }
            ReflectionHelpers.SetFieldOrPropertyFromInstance(__instance, "SpawnWaves", spawnWaves);
            return false;
        }


        private static Wave method_1(WildSpawnWave wave)
        {
            if (Random == null)
                Random = new Random();


            int botsCount = Random.Next(wave.slots_min, wave.slots_max);
            Wave spawnWaves = new Wave
            {
                Time = (float)UnityEngine.Random.Range(wave.time_min, wave.time_max),
                BotsCount = botsCount,
                Difficulty = wave.GetDifficulty(),
                //WildSpawnType = wave.WildSpawnType == WildSpawnType.sptUsec ? WildSpawnType.pmcBot : wave.WildSpawnType,
                WildSpawnType = wave.WildSpawnType,
                SpawnAreaName = wave.SpawnPoints,
                Side = wave.BotSide,
                IsPlayers = wave.isPlayers,
                ChanceGroup = wave.ChanceGroup
            };
            WaveInfo item = new WaveInfo(spawnWaves.BotsCount, spawnWaves.WildSpawnType, spawnWaves.Difficulty);
            CurrentInstance.BotsCountProfiles.Add(item);
            return spawnWaves;
        }



    }
}