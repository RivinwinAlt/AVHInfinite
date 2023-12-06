using HarmonyLib;
using MarchingBytes;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static MelonLoader.MelonLogger;
using static WaveSpawner;

namespace AVHInfiniteMod.Patches
{
	public class SpawnerSettings
	{
		public int round = 0, skipRound = -1, bloonMultiplier = 1;
		public bool infDifficultySelected, bloonMultiplierLocked = true;
		public System.Random rand;
		public Spawner mySpawner;

		private static SpawnerSettings instance = null;
		public static SpawnerSettings Instance
		{
			get
			{
				if (instance == null)
				{
					ModMain.mlog.Msg("Creating a new SpawnerSettings instance");
					instance = new SpawnerSettings();
				}
				return instance;
			}
		}

		private SpawnerSettings()
		{
			rand = new System.Random();
		}

		public void Reset()
		{
			infDifficultySelected = false;
			round = 0;
			skipRound = -1;
			bloonMultiplier = 1;
			bloonMultiplierLocked = true;
		}

		public void SetRound(int value)
		{
			skipRound = Mathf.Clamp(value, 0, 500);
		}

		public static Wave CombineWaves(Wave a, Wave b)
		{
			// Object we'll be passing out at the end
			Wave tempWave = new Wave();
			tempWave.name = "GeneratedWave";

			// This is kinda fun code, lol -JH
			// The intent is to quickly identify if one or both of the passed Waves are null and still return as much Wave info as possible, with the added benefit that if both are null the function immediately returns null on the first check, intended and efficient
			// ---------------------
			if (a == null) return b;
			if (b == null) return a;
			// ---------------------

			// Replace the empty array with a properly sized one
			System.Array.Resize(ref tempWave.enemies, a.enemies.Length + b.enemies.Length);

			// Copy the contents of both passed waves to the new wave
			a.enemies.CopyTo(tempWave.enemies, 0);
			b.enemies.CopyTo(tempWave.enemies, a.enemies.Length);

			// Change the Wave's spawn delay
			if (SpawnerSettings.Instance.round > 99)
			{
				tempWave.delay = 0.1f - 0.005f * (float)(SpawnerSettings.Instance.round - 100); // Slowly starts spawning faster over time
				if (tempWave.delay < 0.05f) tempWave.delay = 0.05f;
			}

			/* RANDOMIZE
			// Fisher-Yates array randomization Algorithm
			int count = tempWave.enemies.Length;
			while (count > 1)
			{
				int i = SpawnerSettings.Instance.rand.Next(count--);
				(tempWave.enemies[i], tempWave.enemies[count]) = (tempWave.enemies[count], tempWave.enemies[i]);
			}
			// End Algorithm
			*/

			return tempWave;
		}
	}

	public class Spawner : MonoBehaviour
	{
		// VARIABLES
		public WaveSpawner wSpawner; // Set by base Spawner during instantiation

		// Arrays
		public List<UnityEngine.GameObject> bloonList = new List<UnityEngine.GameObject>();

		// HARMONY PATCHES

		[HarmonyPatch(typeof(WaveSpawner), "Start")]
		public class WaveSpawnerStart_Patch
		{
			[HarmonyPostfix]
			public static void Postfix(ref WaveSpawner __instance)
			{
				GameObject container = new GameObject();
				if (!SpawnerSettings.Instance.mySpawner) SpawnerSettings.Instance.mySpawner = container.AddComponent<Spawner>();
				SpawnerSettings.Instance.mySpawner.wSpawner = __instance;
				if (SpawnerSettings.Instance.infDifficultySelected)
				{
					__instance.lastRound = -1; // Not really necessary but acts as an indicator
					__instance.SetWaveNumberText(SpawnerSettings.Instance.round, -1);
				}
			}
		}

		[HarmonyPatch(typeof(WaveSpawner), "Update")]
		public class WaveSpawnerUpdate_Patch
		{
			static private float searchCountdown = 1f;

			[HarmonyPrefix]
			public static bool Prefix(ref WaveSpawner __instance, ref WaveSpawner.SpawnState ___state, ref float ___waveCountDown)
			{
				if (SpawnerSettings.Instance.infDifficultySelected)
				{
					if (PauseScript.instance.paused)
					{
						return false; // dont run original function
					}
					if (___state == SpawnState.WAITING)
					{
						if (EnemyIsAlive())
						{
							return false; // dont run original function
						}
						__instance.GetType().GetMethod("WaveCompleted",  BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[0]);
					}
					if (___waveCountDown <= 0f)
					{
						if (___state != 0 && !PauseScript.instance.gameFinished) // ___state != 0 and an above branch means that this only triggers when state == WAITING
						{
							// Check if we're skipping to a different round
							if (SpawnerSettings.Instance.skipRound != -1)
							{
								SpawnerSettings.Instance.round = SpawnerSettings.Instance.skipRound;
								SpawnerSettings.Instance.skipRound = -1;
							}
							// Check if the autostart next round setting is enabled
							if (!PauseScript.instance.autoStart)
							{
								__instance.spawnNextWaveText.enabled = true;
							}
							// Check if the R key was pressed
							if (Input.GetKeyDown(KeyCode.R) || PauseScript.instance.autoStart)
							{
								SpawnerSettings.Instance.mySpawner.StartCustomSpawnWave();
							}
						}
					}
					else
					{
						// Count down a timer after last enemy dies before the round ends
						___waveCountDown -= Time.deltaTime;
					}

					return false; // dont run original function
				}

				return true; // run original function
			}

			static public bool EnemyIsAlive() // This function reimplements the private function found in AVH
			{
				searchCountdown -= Time.deltaTime;
				if (searchCountdown <= 0f)
				{
					searchCountdown = 1f;
					if (GameObject.FindGameObjectWithTag("Bloon") == null && GameObject.FindGameObjectWithTag("Ceramic") == null && GameObject.FindGameObjectWithTag("MOAB") == null)
					{
						return false;
					}
				}
				return true;
			}
		}

		private void StartCustomSpawnWave()
		{
			SpawnerSettings.Instance.mySpawner.wSpawner.nextwave = SpawnerSettings.Instance.round > 99 ? (SpawnerSettings.Instance.round % 50) + 50 : SpawnerSettings.Instance.round;
			wSpawner.spawnNextWaveText.enabled = false;
			Private.SetPrivateValue(wSpawner, "state", SpawnState.SPAWNING);
			wSpawner.SetWaveNumberText(wSpawner.nextwave, wSpawner.lastRound);

			if (SpawnerSettings.Instance.round > 99)
			{
				WaveSpawner tempSpawner = SpawnerSettings.Instance.mySpawner.wSpawner;
				StartCoroutine(CustomSpawnWave(SpawnerSettings.CombineWaves(tempSpawner.waves[tempSpawner.nextwave], tempSpawner.waves[(SpawnerSettings.Instance.round + 30) % 100]), true));
			} else
			{
				StartCoroutine(CustomSpawnWave(wSpawner.waves[wSpawner.nextwave], false));
			}
		}

		private IEnumerator CustomSpawnWave(Wave _wave, bool randomizeBloon)
		{
			if (!randomizeBloon)
			{
				foreach (EnemySpawned enemy in _wave.enemies)
				{
					int newCount = enemy.count * SpawnerSettings.Instance.bloonMultiplier;
					for (int i = 0; i < newCount; i++)
					{
						SpawnEnemy(enemy.enemySpawned);
						yield return new WaitForSeconds(_wave.delay);
					}
				}
			} else
			{
				// Eat through the wave spawning one bloon at a time in random order and shifting empty entries to the end of the array
				int workingLength = _wave.enemies.Length;
				while(workingLength > 0)
				{
					int r = SpawnerSettings.Instance.rand.Next(workingLength);
					if (_wave.enemies[r].count > 0)
					{
						_wave.enemies[r].count--;
						SpawnEnemy(_wave.enemies[r].enemySpawned);
						yield return new WaitForSeconds(_wave.delay);
					}
					else
					{
						workingLength--;
						(_wave.enemies[r], _wave.enemies[workingLength]) = (_wave.enemies[workingLength], _wave.enemies[r]);
					}
				}

			}

			Private.SetPrivateValue(wSpawner, "state", SpawnState.WAITING);
		}

		// Gives us a nice editable version of the base bloon spawning function
		private void SpawnEnemy(Transform _enemy)
		{
			Vector3 position = wSpawner.transform.position;
			if (wSpawner.spawnPoints.Length != 0)
			{
				int num = UnityEngine.Random.Range(0, wSpawner.spawnPoints.Length);
				position = wSpawner.spawnPoints[num].transform.position;
			}
			GameObject objectFromPool = EasyObjectPool.instance.GetObjectFromPool(_enemy.name, position, Quaternion.identity);
			if (wSpawner.pathPoints.Length != 0)
			{
				objectFromPool.GetComponent<Enemy>().followPath = true;
				objectFromPool.GetComponent<Enemy>().points = wSpawner.pathPoints;
			}
			objectFromPool.GetComponent<OnCreateScript>().OnCreate(spawning: true);
			objectFromPool.transform.position = position;
		}


		[HarmonyPatch(typeof(WaveSpawner), "RemoteSpawnWave")]
		public class WaveSpawnerRemoteSpawnWave_Patch : MonoBehaviour
		{
			[HarmonyPrefix]
			public static bool Prefix(ref WaveSpawner __instance)
			{
				if (SpawnerSettings.Instance.infDifficultySelected)
				{
					SpawnerSettings.Instance.mySpawner.StartCustomSpawnWave();

					return false; // Dont run original code
				}

				return true; // run original function
			}
		}

		[HarmonyPatch(typeof(WaveSpawner), "WaveCompleted")]
		public class WaveSpawnerWaveCompleted_Patch
		{
			[HarmonyPrefix]
			public static bool Prefix(ref WaveSpawner __instance, ref WaveSpawner.SpawnState ___state, ref float ___waveCountDown)
			{
				if (SpawnerSettings.Instance.infDifficultySelected)
				{
					// Original code with call to CompletedAllWaves() removed
					___state = WaveSpawner.SpawnState.COUNTING;
					___waveCountDown = __instance.timeBetweenWaves;
					Currency.instance.UpdateCurrency(101 + __instance.nextwave, true);

					// New code
					SpawnerSettings.Instance.round++;

					__instance.nextwave = SpawnerSettings.Instance.round > 99 ? (SpawnerSettings.Instance.round % 50) + 50 : SpawnerSettings.Instance.round;
					PlayerHealth.instance.UpdateHealth(1); // Adds one health per round

					// Original code
					__instance.WaveFinishedEvent.Invoke();

					return false; // Dont run original code
				}

				return true; // run original function
			}
		}

		// Expands the helper function that sets the HUD wave numbers to handle infinite waves by chopping of the " / #" part
		[HarmonyPatch(typeof(WaveSpawner), "SetWaveNumberText")]
		public class WaveSpawnerSetWaveNumberText_Patch
		{
			[HarmonyPostfix]
			public static void Postfix(int lastRoundInt, int nextWaveInt, ref WaveSpawner __instance)
			{
				if ((bool)__instance.waveNumberText && SpawnerSettings.Instance.infDifficultySelected)
				{
					__instance.waveNumberText.text = (SpawnerSettings.Instance.round + 1).ToString();
				}
			}
		}
	}
}
