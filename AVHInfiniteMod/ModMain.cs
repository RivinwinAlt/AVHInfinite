using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.UI;

namespace AVHInfiniteMod
{
	public class ModMain : MelonMod
	{
		// ===============
		// == VARIABLES ==
		// ===============
		internal static string modFolder = $"{Environment.CurrentDirectory}\\Mods\\{Assembly.GetExecutingAssembly().GetName().Name}";
		private static KeyCode toggleKey = KeyCode.Tab;
		public static MelonLogger.Instance mlog;

		// Objects
		public static GameObject difficultiesGroup, levelGroup; // Menu objects we are looking for in the scene
		public static GameObject newDifficulty; // New menu object we create for our dificulty button

		// ==================
		// == MELON EVENTS ==
		// ==================
		public override void OnEarlyInitializeMelon() // Constructor only, no logging or game objects, before MelonLoader is fully initialized
		{
		}

		public override void OnInitializeMelon() // Before game start, after MelonLoader is fully initialized
		{
			mlog = LoggerInstance;

			// Minimum visual check by user that the mod is installed. When working properly this is all the user should see.
			LoggerInstance.Msg("Infinite Mod is installed");

			if (!ModConfig.Instance.CheckValid()) // Checks if config is accessable
			{
				LoggerInstance.Msg("Mod config cannot be accessed");
			}
			else if (!ModConfig.Instance.CheckValid("initialized")) // Check if key has been written to
			{
				LoggerInstance.Msg("No saved settings, initializing");
				FirstTimeSetup();
			}

			// Check if the config can be written to, mark config as having been initialized
			if (!ModConfig.Instance.Write("initialized", true.ToString())) LoggerInstance.Msg("Mod config couldn't be written to");
		}

		// Run when there is no existing config file
		private void FirstTimeSetup()
		{

		}

		public override void OnSceneWasInitialized(int buildIndex, string sceneName)
		{
			if (sceneName == "MainMenu")
			{
				mlog.Msg("Difficulty boolean is being set to false");
				Patches.SpawnerSettings.Instance.Reset(); // when the player loses or quits to menu in an unforseen way we want to disable the infinite difficulty mode

				// Start adding a chain of function triggers for menu buttons for the end goal of editing the difficulty select screen
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

			levelGroup.transform.GetChild(0).gameObject.GetComponent<Button>().onClick.RemoveListener(SetCustomIcon);
			levelGroup.transform.GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(SetCustomIcon);
			levelGroup.transform.GetChild(1).gameObject.GetComponent<Button>().onClick.RemoveListener(SetCustomIcon);
			levelGroup.transform.GetChild(1).gameObject.GetComponent<Button>().onClick.AddListener(SetCustomIcon);
			
			// Find root UI Element for difficulties
			if (difficultiesGroup == null) difficultiesGroup = GameObject.Find("DifficultiesGroup");
			if (difficultiesGroup == null) return;

			// Copy impopable menu item
			newDifficulty = GameObject.Instantiate(difficultiesGroup.transform.GetChild(3).gameObject, difficultiesGroup.transform);
			newDifficulty.name = "Infinite";

			// Load custom image from embedded asset bundle, could just load image directly, but eh. already implemented this
			AssetBundle myBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("AVHInfiniteMod.Properties.infiniterounds.assets"));

			Texture2D srcTex = myBundle.LoadAsset<Texture2D>("Monkey Fan Club");

			// Adjust image properties
			Image newImage = newDifficulty.GetComponent<Image>();
			newImage.color = Color.white;
			Sprite newSprite = Sprite.Create(srcTex, new Rect(0.0f, 0.0f, srcTex.width, srcTex.height), new Vector2(0.5f, 0.5f), 100.0f);
			newImage.sprite = newSprite;

			// Move icon on screen
			GridLayoutGroup gridGroup = difficultiesGroup.GetComponent<GridLayoutGroup>();
			gridGroup.spacing = new Vector2(120, 250);
			gridGroup.childAlignment = UnityEngine.TextAnchor.UpperCenter;

			// Update menu item title
			TMPro.TextMeshProUGUI newText = newDifficulty.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshProUGUI>();
			newText.text = "Infinite";
			newText.color = Color.white;

			// Add listener to menu button to engage harmony patches
			Button newButton = newDifficulty.GetComponent<Button>();
			newButton.GetComponent<Button>().onClick.AddListener(SetCustomDifficulty);
		}

		public void SetCustomDifficulty() // assigned to button delegate
		{
			mlog.Msg("Difficulty boolean is being set to true");
			Patches.SpawnerSettings.Instance.infDifficultySelected = true;
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
				Currency.instance.UpdateCurrency(100000000, true);
				Patches.SpawnerSettings.Instance.SetRound(99);
			}
		}

		public override void OnDeinitializeMelon() // Destructor
		{
			// write to config if needed
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
				mlog.Msg("Player Start: Difficulty boolean is " + Patches.SpawnerSettings.Instance.infDifficultySelected.ToString());
				if (Patches.SpawnerSettings.Instance.infDifficultySelected)
				{
					__instance.health = 1;
					__instance.UpdateHealth(0);
				}
			}
		}

		[HarmonyPatch(typeof(Enemy), "Start")]
		public class EnemyStart_Patch
		{
			[HarmonyPostfix]
			public static void Postfix(Enemy __instance)
			{
				if (Patches.SpawnerSettings.Instance.infDifficultySelected)
				{
					if (Patches.SpawnerSettings.Instance.round > 99)
					{
						__instance.health *= (int)(1.0 + Math.Floor(Patches.SpawnerSettings.Instance.round / 100.0));
						// Other effects
					}
				}
			}
		}
	}

	// Helper class to access private game variables
	public static class Private
	{
		public static object GetPrivateValue<T>(this T type, string field)
		{
			return type.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(type);
		}

		public static void SetPrivateValue<T>(this T type, string field, object value)
		{
			type.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).SetValue(type, value);
		}
	}
}