using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using MarchingBytes;
using System.Linq;
using System.Xml.Linq;
using DG.Tweening.Plugins.Core.PathCore;
using CodeMonkey;
using TMPro;
using Mono.Unix.Native;
using System.Security.Cryptography;
using EPOOutline;
using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Runtime.Remoting.Messaging;
using static UnityEngine.Random;
using static WaveSpawner;
using static MelonLoader.bHaptics;
using static UnityStandardAssets.Utility.TimedObjectActivator;

namespace AVHInfiniteMod
{
    public class ModMain : MelonMod
    {
        // ===============
        // == VARIABLES ==
        // ===============
        private static KeyCode toggleKey = KeyCode.Tab;
        public static int round = 0;
        public static bool difficultySelected;

        // Objects
        public ModConfig Config;
        public static GameObject difficultiesGroup, levelGroup; // Menu objects
        public static GameObject newDifficulty; // Unity object that the difficulty icons are assigned to like an array

        // Arrays
        public static List<UnityEngine.GameObject> bloonList = new List<UnityEngine.GameObject>();



        // ==================
        // == MELON EVENTS ==
        // ==================
        public override void OnEarlyInitializeMelon() // Constructor only, no logging or game objects, before MelonLoader is fully initialized
        {
        }

        public override void OnInitializeMelon() // Before game start, after MelonLoader is fully initialized
        {
            // Create / load config instance
            Config = new ModConfig(Assembly.GetExecutingAssembly().GetName().Name);
            if (Config == null) LoggerInstance.Msg("Infinite Rounds Mod failed to load settings");

            // Success message
            LoggerInstance.Msg("Infinite Rounds Mod is installed");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                difficultySelected = false; // when the player loses or quits to menu we want to disable

                // start adding a chain of triggers for menu loads
                GameManagerScript.instance.mainMenu.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(SetCustomIcon);
                GameManagerScript.instance.mainMenu.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(SetCustomIcon);
            }
        }

        public void SetCustomIcon()
        {
            // Check if new menu item has already been patched in
            if (newDifficulty != null) return;

            // Check for level select menu and patch deeper
            if (levelGroup == null) levelGroup = GameObject.Find("LevelGroup");
            if(levelGroup == null) return;

            levelGroup.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(SetCustomIcon);
            levelGroup.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(SetCustomIcon);
            levelGroup.transform.GetChild(1).gameObject.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(SetCustomIcon);
            levelGroup.transform.GetChild(1).gameObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(SetCustomIcon);
            
            // Find root UI Element for difficulties
            if (difficultiesGroup == null) difficultiesGroup = GameObject.Find("DifficultiesGroup");
            if (difficultiesGroup == null) return;

            // Copy easy menu item
            newDifficulty = GameObject.Instantiate(difficultiesGroup.transform.GetChild(0).gameObject, difficultiesGroup.transform);
            newDifficulty.name = "Infinite";

            // load custom asett bundle
            AssetBundle myBundle = AssetBundle.LoadFromFile(Config.modFolder + "\\infiniterounds.assets");

            Texture2D srcTex = myBundle.LoadAsset<Texture2D>("Monkey Fan Club");
            UnityEngine.UI.Image newImage = newDifficulty.GetComponent<UnityEngine.UI.Image>();
            Sprite newSprite = Sprite.Create(srcTex, new Rect(0.0f, 0.0f, srcTex.width, srcTex.height), new Vector2(0.5f, 0.5f), 100.0f);
            newImage.sprite = newSprite;

            // Move icon on screen
            UnityEngine.UI.GridLayoutGroup gridGroup = difficultiesGroup.GetComponent<GridLayoutGroup>();
            gridGroup.spacing = new Vector2(120, 250);
            gridGroup.childAlignment = UnityEngine.TextAnchor.UpperCenter;

            // Update menu item title
            TMPro.TextMeshProUGUI newText = newDifficulty.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            newText.text = "Infinite";

            // Add listener to menu button to engage harmony patches
            UnityEngine.UI.Button newButton = newDifficulty.GetComponent<UnityEngine.UI.Button>();
            newButton.GetComponent<Button>().onClick.AddListener(SetCustomDifficulty);
        }

        public void SetCustomDifficulty()
        {
            difficultySelected = true;
        }

        public override void OnUpdate() // Each frame during gameplay
        {
        }

        public override void OnGUI() // When the gui updates
        {
        }

        public override void OnLateUpdate() // At end of each frame during gameplay
        {
            if (Input.GetKeyDown(toggleKey))
            {
            }
        }

        public override void OnDeinitializeMelon() // Destructor
        {
        }



        // ======================
        // == Helper functions ==
        // ======================

        public static void CombineWaves(ref Wave a, Wave b)
        {
            EnemySpawned[] newEnemies = new EnemySpawned[a.enemies.Length + b.enemies.Length];
            a.enemies.CopyTo(newEnemies, 0);
            b.enemies.CopyTo(newEnemies, a.enemies.Length);

            if (round > 99)
            {
                a.delay = 1.0f - 0.005f * (float)(round - 100); // Slowly starts spawning faster over time
                if (a.delay < 0.1f) a.delay = 0.1f;
            }

            a.enemies = newEnemies;
        }



        // =============
        // == PATCHES ==
        // =============

        [HarmonyPatch(typeof(PlayerHealth), "Start")]
        public class PlayerHealthStart_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ref PlayerHealth __instance)
            {
                if (difficultySelected)
                {
                    __instance.health = 1;
                    __instance.UpdateHealth(0);
                }
            }
        }

        [HarmonyPatch(typeof(WaveSpawner), "Start")]
        public class WaveSpawnerStart_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ref WaveSpawner __instance)
            {
                if (difficultySelected)
                {
                    __instance.lastRound = -1;
                    __instance.SetWaveNumberText(round, -1);
                }
            }
        }

        [HarmonyPatch(typeof(WaveSpawner), "WaveCompleted")]
        public class WaveSpawnerWaveCompleted_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref WaveSpawner __instance, ref WaveSpawner.SpawnState ___state, ref float ___waveCountDown)
            {
                if (difficultySelected)
                {
                    // Original code with call to CompletedAllWaves() removed
                    ___state = WaveSpawner.SpawnState.COUNTING;
                    ___waveCountDown = __instance.timeBetweenWaves;
                    Currency.instance.UpdateCurrency(101 + __instance.nextwave, true);
                    __instance.WaveFinishedEvent.Invoke();
                    //__instance.nextwave++;

                    // New code
                    round++;
                    __instance.nextwave = round % 100;
                    if (round > 99) CombineWaves(ref __instance.waves[__instance.nextwave], __instance.waves[(round + 30) % 100]); // Call a function to edit the upcoiming wave contents now
                    PlayerHealth.instance.UpdateHealth(1); // Adds one health per round

                    if (__instance.waves.Length <= __instance.nextwave)
                    {
                        // Rerun waves with aditional modifiers, patterns, and health
                    }

                    return false; // Dont run original code
                }

                return true; // run original function
            }
        }

        // Expands the helper function that sets the HUD wave numbers to handle infinite waves
        [HarmonyPatch(typeof(WaveSpawner), "SetWaveNumberText")]
        public class WaveSpawnerSetWaveNumberText_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(int lastRoundInt, int nextWaveInt, ref WaveSpawner __instance)
            {
                if ((bool)__instance.waveNumberText && difficultySelected)
                {
                    __instance.waveNumberText.text = (round + 1).ToString();
                }
            }
        }

        [HarmonyPatch(typeof(Enemy), "Start")]
        public class EnemyStart_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Enemy __instance)
            {
                if (difficultySelected)
                {
                    if (round > 99)
                    {
                        __instance.health *= (int)(1.0 + Math.Floor(round / 100.0));
                        // Other effects
                    }
                }
            }
        }
    }
}