using Valve.Newtonsoft.Json;
using FistVR;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine.Networking;
using Steamworks;
using UnityEngine;

namespace TNHFramework.Patches
{
    public static class HighScorePatches
    {

        private static string[] equipment = { "Spawnlock", "Limited" };
        private static string[] health = { "Standard", "One-Hit", "Custom"};
        private static string[] length = { "5-Hold", "Endless", "3-Hold" };

        private static bool waitForScore = false;


        [HarmonyPatch(typeof(TNH_Manager), "DelayedInit")]
        [HarmonyPrefix]
        public static bool StartOfGamePatch(TNH_Manager __instance)
        {
            if (!__instance.m_hasInit && __instance.AIManager.HasInit)
            {
                //Clear all entries from the tracked stats
                TNHFramework.HoldActions.Clear();
                TNHFramework.HoldStats.Clear();

                TNHFramework.GunsRecycled = 0;
                TNHFramework.ShotsFired = 0;

                TNHFrameworkLogger.Log("Delayed init", TNHFrameworkLogger.LogType.TNH);
            }
            
            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "InitPlayerPosition")]
        [HarmonyPrefix]
        public static bool TrackPlayerSpawnPatch(TNH_Manager __instance)
        {
            TNHFramework.HoldActions[0].Add($"Spawned At Supply {__instance.m_curPointSequence.StartSupplyPointIndex}");

            TNHFrameworkLogger.Log("Spawned Player", TNHFrameworkLogger.LogType.TNH);

            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "HoldPointCompleted")]
        [HarmonyPrefix]
        public static bool TrackHoldCompletion(TNH_Manager __instance)
        {
            TNHFrameworkLogger.Log("Hold Completion", TNHFrameworkLogger.LogType.TNH);

            TNHFramework.HoldStats.Add(new HoldStats()
            {
                SosigsKilled = __instance.Stats[3],
                MeleeKills = __instance.Stats[5],
                Headshots = __instance.Stats[4],
                TokensSpent = __instance.Stats[8],
                GunsRecycled = TNHFramework.GunsRecycled,
                AmmoSpent = TNHFramework.ShotsFired
            });

            __instance.Stats[3] = 0;
            __instance.Stats[5] = 0;
            __instance.Stats[4] = 0;
            __instance.Stats[8] = 0;
            TNHFramework.GunsRecycled = 0;
            TNHFramework.ShotsFired = 0;

            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetLevel")]
        [HarmonyPrefix]
        public static bool TrackNextLevel(TNH_Manager __instance)
        {
            TNHFrameworkLogger.Log("Set Level", TNHFrameworkLogger.LogType.TNH);
            TNHFramework.HoldActions.Add([]);

            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
        [HarmonyPrefix]
        public static bool TrackDeath(TNH_Manager __instance)
        {
            TNHFrameworkLogger.Log("Died", TNHFrameworkLogger.LogType.TNH);
            TNHFramework.HoldActions.Last().Add("Died");

            TNHFramework.HoldStats.Add(new HoldStats()
            {
                SosigsKilled = __instance.Stats[3],
                MeleeKills = __instance.Stats[5],
                Headshots = __instance.Stats[4],
                TokensSpent = __instance.Stats[8],
                GunsRecycled = TNHFramework.GunsRecycled,
                AmmoSpent = TNHFramework.ShotsFired
            });

            __instance.Stats[3] = 0;
            __instance.Stats[5] = 0;
            __instance.Stats[4] = 0;
            __instance.Stats[8] = 0;
            TNHFramework.GunsRecycled = 0;
            TNHFramework.ShotsFired = 0;

            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
        [HarmonyPrefix]
        public static bool TrackVictory(TNH_Manager __instance)
        {
            TNHFrameworkLogger.Log("Victory", TNHFrameworkLogger.LogType.TNH);
            TNHFramework.HoldActions.Last().Add("Victory");

            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "DispatchScore")]
        [HarmonyPrefix]
        public static bool ResetStats(TNH_Manager __instance)
        {
            foreach(HoldStats stat in TNHFramework.HoldStats)
            {
                __instance.Stats[3] += stat.SosigsKilled;
                __instance.Stats[5] += stat.MeleeKills;
                __instance.Stats[4] += stat.Headshots;
                __instance.Stats[8] += stat.TokensSpent;
            }

            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "OnShotFired")]
        [HarmonyPrefix]
        public static bool TrackShotFired(TNH_Manager __instance)
        {
            TNHFramework.ShotsFired += 1;

            return true;
        }


        [HarmonyPatch(typeof(TNH_HoldPoint), "BeginHoldChallenge")]
        [HarmonyPrefix]
        public static bool TrackHoldStart(TNH_HoldPoint __instance)
        {
            TNHFrameworkLogger.Log("Hold Start", TNHFrameworkLogger.LogType.TNH);
            TNHFramework.HoldActions[__instance.M.m_level].Add($"Entered Hold {__instance.M.HoldPoints.IndexOf(__instance)}");

            return true;
        }


        [HarmonyPatch(typeof(TNH_GunRecycler), "Button_Recycler")]
        [HarmonyPrefix]
        public static bool TrackRecyclePatch(TNH_GunRecycler __instance)
        {
            TNHFrameworkLogger.Log("Recycle button", TNHFrameworkLogger.LogType.TNH);
            if (__instance.m_selectedObject != null)
            {
                TNHFramework.HoldActions[__instance.M.m_level].Add($"Recycled {__instance.m_selectedObject.ObjectWrapper.DisplayName}");
                TNHFramework.GunsRecycled += 1;
            }

            return true;
        }


        [HarmonyPatch(typeof(TNH_SupplyPoint), "TestVisited")]
        [HarmonyPrefix]
        public static bool TrackSupplyVisitsPatch(TNH_SupplyPoint __instance, ref bool __result)
        {
            if (!__instance.m_isconfigured)
            {
                __result = false;
                return false;
            }

            bool flag = __instance.TestVolumeBool(__instance.Bounds, GM.CurrentPlayerBody.transform.position);
            if (flag)
            {
                if (!__instance.m_hasBeenVisited && __instance.m_contact != null)
                {
                    __instance.m_contact.SetVisited(true);
                    TNHFrameworkLogger.Log("Visiting supply", TNHFrameworkLogger.LogType.TNH);
                    TNHFramework.HoldActions[__instance.M.m_level].Add($"Entered Supply {__instance.M.SupplyPoints.IndexOf(__instance)}");
                }
                __instance.m_hasBeenVisited = true;
            }
            __result = flag;

            return false;
        }
        

        [HarmonyPatch(typeof(TNH_ScoreDisplay), "UpdateHighScoreCallbacks")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool RequestScores(TNH_ScoreDisplay __instance)
        {
            if (waitForScore) return false;

            //The first thing we do is get the top scores
            if (__instance.m_doRequestScoresTop)
            {
                TNHFrameworkLogger.Log("Requesting Top Scores", TNHFrameworkLogger.LogType.TNH);
                __instance.m_doRequestScoresTop = false;
                __instance.m_scoresTop = [];

                AnvilManager.Instance.StartCoroutine(GetHighScores(6, __instance));
            }

            //After the top scores are retrieved, request the players score
            if (__instance.m_doRequestScoresPlayer)
            {
                TNHFrameworkLogger.Log("Requesting Player Scores", TNHFrameworkLogger.LogType.TNH);
                __instance.m_doRequestScoresPlayer = false;
                __instance.m_scoresPlayer = [];

                //If the players score is also in the selection of top scores, we just display the top
                if (__instance.m_scoresTop.Any(o => o.name == SteamFriends.GetPersonaName()))
                {
                    __instance.m_scoresPlayer = [];
                    __instance.m_hasScoresPlayer = true;

                    __instance.SetGlobalHighScoreDisplay(__instance.m_scoresTop);
                }

                //If the players scores are not at the top, we must also find their scores
                else
                {
                    AnvilManager.Instance.StartCoroutine(GetPlayerScores(1, 1, __instance));
                }
            }

            return false;
        }


        public static ScoreEntry GetScoreEntry(TNH_Manager instance, int score)
        {
            ScoreEntry entry = new ScoreEntry();

            entry.Name = SteamFriends.GetPersonaName();
            entry.Score = score;
            entry.Character = instance.C.DisplayName;
            entry.Map = instance.LevelName;
            entry.EquipmentMode = equipment[(int)GM.TNHOptions.EquipmentModeSetting];
            entry.HealthMode = health[(int)GM.TNHOptions.HealthModeSetting];
            entry.GameLength = length[(int)GM.TNHOptions.ProgressionTypeSetting];
            entry.HoldActions = JsonConvert.SerializeObject(TNHFramework.HoldActions);
            entry.HoldStats = JsonConvert.SerializeObject(TNHFramework.HoldStats);

            return entry;
        }

        private static List<Vector3IntSerializable> GetHoldList()
        {
            return GM.TNH_Manager.HoldPoints.Select(o => new Vector3IntSerializable(o.SpawnPoint_SystemNode.position)).ToList();
        }

        private static List<Vector3IntSerializable> GetSupplyList()
        {
            return GM.TNH_Manager.SupplyPoints.Select(o => new Vector3IntSerializable(o.SpawnPoint_PlayerSpawn.position)).ToList();
        }
        

        public static IEnumerator GetHighScores(int count, TNH_ScoreDisplay instance)
        {
            TNHFrameworkLogger.Log("Getting high scores from TNH Dashboard", TNHFrameworkLogger.LogType.TNH);

            string url = "https://tnh-dashboard.azure-api.net/v1/api/scores";

            if(GM.TNH_Manager != null)
            {
                url += "?character=" + GM.TNH_Manager.C.DisplayName;
                url += "&map=" + GM.TNH_Manager.LevelName;
                url += "&health=" + health[(int)GM.TNHOptions.HealthModeSetting];
                url += "&equipment=" + equipment[(int)GM.TNHOptions.EquipmentModeSetting];
                url += "&length=" + length[(int)GM.TNHOptions.ProgressionTypeSetting];
                url += "&startingIndex=" + 0 + "&count=" + count;
            }
            else
            {
                TNH_UIManager manager = GameObject.FindObjectOfType<TNH_UIManager>();
                if(manager == null)
                {
                    TNHFrameworkLogger.LogError("Neither the TNH Manager or the UI Manager were found! Scores will not display");
                    yield break;
                }

                url += "?character=" + manager.CharDatabase.GetDef((TNH_Char)GM.TNHOptions.LastPlayedChar).DisplayName;
                url += "&map=" + manager.CurLevelID;
                url += "&health=" + health[(int)GM.TNHOptions.HealthModeSetting];
                url += "&equipment=" + equipment[(int)GM.TNHOptions.EquipmentModeSetting];
                url += "&length=" + length[(int)GM.TNHOptions.ProgressionTypeSetting];
                url += "&startingIndex=" + 0 + "&count=" + count;
            }

            TNHFrameworkLogger.Log("Request URL: " + url, TNHFrameworkLogger.LogType.TNH);

            using (UnityWebRequest wwwGetScores = UnityWebRequest.Get(url))
            {
                yield return wwwGetScores.Send();

                if (wwwGetScores.isError)
                {
                    TNHFrameworkLogger.LogError("Something bad happened getting scores \n" + wwwGetScores.error);
                }
                else if(wwwGetScores.responseCode == 404)
                {
                    TNHFrameworkLogger.LogWarning("High scores not found for table!");
                }
                else
                {
                    TNHFrameworkLogger.Log("Got Scores!", TNHFrameworkLogger.LogType.TNH);

                    string scores = wwwGetScores.downloadHandler.text;
                    TNHFrameworkLogger.Log(scores, TNHFrameworkLogger.LogType.TNH);

                    List<ScoreEntry> highScores = JsonConvert.DeserializeObject<List<ScoreEntry>>(scores);

                    for (int i = 0; i < highScores.Count; i++)
                    {
                        instance.m_scoresTop.Add(new RUST.Steamworks.HighScoreManager.HighScore()
                        {
                            name = highScores[i].Name,
                            rank = highScores[i].Rank,
                            score = highScores[i].Score
                        });
                    }
                }
            }

            instance.m_hasScoresTop = true;
            instance.m_doRequestScoresPlayer = true;
        }



        public static IEnumerator GetPlayerScores(int num_before, int num_after, TNH_ScoreDisplay instance)
        {
            TNHFrameworkLogger.Log("Getting player scores from TNH Dashboard", TNHFrameworkLogger.LogType.TNH);

            string url = "https://tnh-dashboard.azure-api.net/v1/api/scores/search";
            List<RUST.Steamworks.HighScoreManager.HighScore> combinedScores = [];

            if (GM.TNH_Manager != null)
            {
                url += "?character=" + GM.TNH_Manager.C.DisplayName;
                url += "&map=" + GM.TNH_Manager.LevelName;
                url += "&health=" + health[(int)GM.TNHOptions.HealthModeSetting];
                url += "&equipment=" + equipment[(int)GM.TNHOptions.EquipmentModeSetting];
                url += "&length=" + length[(int)GM.TNHOptions.ProgressionTypeSetting];
                url += "&name=" + SteamFriends.GetPersonaName();
                url += "&num_before=1";
                url += "&num_after=1";
            }
            else
            {
                TNH_UIManager manager = GameObject.FindObjectOfType<TNH_UIManager>();
                if (manager == null)
                {
                    TNHFrameworkLogger.LogError("Neither the TNH Manager or the UI Manager were found! Scores will not display");
                    yield break;
                }

                url += "?character=" + manager.CharDatabase.GetDef((TNH_Char)GM.TNHOptions.LastPlayedChar).DisplayName;
                url += "&map=" + manager.CurLevelID;
                url += "&health=" + health[(int)GM.TNHOptions.HealthModeSetting];
                url += "&equipment=" + equipment[(int)GM.TNHOptions.EquipmentModeSetting];
                url += "&length=" + length[(int)GM.TNHOptions.ProgressionTypeSetting];
                url += "&name=" + SteamFriends.GetPersonaName();
                url += "&num_before=1";
                url += "&num_after=1";
            }

            TNHFrameworkLogger.Log("Request URL: " + url, TNHFrameworkLogger.LogType.TNH);

            using (UnityWebRequest wwwGetScores = UnityWebRequest.Get(url))
            {
                yield return wwwGetScores.Send();

                if (wwwGetScores.isError)
                {
                    TNHFrameworkLogger.LogError("Something bad happened getting scores \n" + wwwGetScores.error);
                }
                else if (wwwGetScores.responseCode == 404)
                {
                    TNHFrameworkLogger.LogWarning("High scores not found for player in table!");

                    combinedScores.AddRange(instance.m_scoresTop.Take(6));
                }
                else
                {
                    TNHFrameworkLogger.Log("Got Scores!", TNHFrameworkLogger.LogType.TNH);

                    string scores = wwwGetScores.downloadHandler.text;
                    TNHFrameworkLogger.Log(scores, TNHFrameworkLogger.LogType.TNH);

                    List<ScoreEntry> playerScores = JsonConvert.DeserializeObject<List<ScoreEntry>>(scores);

                    for (int i = 0; i < playerScores.Count; i++)
                    {
                        instance.m_scoresPlayer.Add(new RUST.Steamworks.HighScoreManager.HighScore()
                        {
                            name = playerScores[i].Name,
                            rank = playerScores[i].Rank,
                            score = playerScores[i].Score
                        });
                    }

                    if (instance.m_scoresTop != null)
                    {
                        combinedScores.AddRange(instance.m_scoresTop.Take(3));
                    }
                    if (instance.m_scoresPlayer != null)
                    {
                        combinedScores.AddRange(instance.m_scoresPlayer.Take(3));
                    }
                }
            }

            instance.m_hasScoresPlayer = true;
            instance.SetGlobalHighScoreDisplay(combinedScores);
        }



        public static IEnumerator SendScore(int score)
        {
            TNHFrameworkLogger.Log("Sending modded score to the TNH Dashboard", TNHFrameworkLogger.LogType.TNH);
            waitForScore = true;

            //First, send the map data for this map
            using (UnityWebRequest wwwSendMap = new UnityWebRequest("https://tnh-dashboard.azure-api.net/v1/api/maps", "Put"))
            {
                wwwSendMap.SetRequestHeader(Globals.Accept, "*/*");
                wwwSendMap.SetRequestHeader(Globals.Content_Type, Globals.ApplicationJson);

                GetHoldList().ForEach(o => TNHFrameworkLogger.Log($"Hold: x={o.x}, z={o.z}", TNHFrameworkLogger.LogType.TNH));

                MapData mapData = new MapData()
                {
                    MapName = GM.TNH_Manager.LevelName,
                    HoldPointLocations = JsonConvert.SerializeObject(GetHoldList()),
                    SupplyPointLocations = JsonConvert.SerializeObject(GetSupplyList())
                };

                string data = JsonConvert.SerializeObject(mapData);
                wwwSendMap.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                wwwSendMap.uploadHandler.contentType = "application/json";

                yield return wwwSendMap.Send();

                if (wwwSendMap.isError)
                {
                    TNHFrameworkLogger.LogError("Something bad happened sending map data! \n" + wwwSendMap.error);
                }
                else
                {
                    TNHFrameworkLogger.Log("Sent map data", TNHFrameworkLogger.LogType.TNH);
                }
            }


            //Now send the score
            using (UnityWebRequest wwwScores = new UnityWebRequest("https://tnh-dashboard.azure-api.net/v1/api/scores", "Put"))
            {
                wwwScores.SetRequestHeader(Globals.Accept, "*/*");
                wwwScores.SetRequestHeader(Globals.Content_Type, Globals.ApplicationJson);

                ScoreEntry entry = GetScoreEntry(GM.TNH_Manager, score);
                string data = JsonConvert.SerializeObject(entry);
                wwwScores.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                wwwScores.uploadHandler.contentType = "application/json";

                yield return wwwScores.Send();

                if (wwwScores.isError)
                {
                    TNHFrameworkLogger.LogError("Something bad happened sending score! \n" + wwwScores.error);
                }
                else
                {
                    TNHFrameworkLogger.Log("Sent score data", TNHFrameworkLogger.LogType.TNH);
                }
            }

            waitForScore = false;
        }
    }
}
