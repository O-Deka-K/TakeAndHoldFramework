using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework
{
    public class DebugPatches
    {

		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPrefix]
		public static bool AddPointDebugText(TNH_Manager __instance)
        {
			foreach(TNH_HoldPoint hold in __instance.HoldPoints)
            {

				TNHFrameworkLogger.Log("Adding text!", TNHFrameworkLogger.LogType.TNH);

				GameObject canvas = new GameObject("Canvas");
				canvas.transform.rotation = Quaternion.LookRotation(Vector3.right);
				canvas.transform.position = hold.SpawnPoint_SystemNode.position + Vector3.up * 0.2f;

				Canvas canvasComp = canvas.AddComponent<Canvas>();
				RectTransform rect = canvasComp.GetComponent<RectTransform>();
				canvasComp.renderMode = RenderMode.WorldSpace;
				rect.sizeDelta = new Vector2(1, 1);

				GameObject text = new GameObject("Text");
				text.transform.SetParent(canvas.transform);
				text.transform.rotation = canvas.transform.rotation;
				text.transform.localPosition = Vector3.zero;

				text.AddComponent<CanvasRenderer>();
				Text textComp = text.AddComponent<Text>();
				Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

				textComp.text = "Hold " + __instance.HoldPoints.IndexOf(hold);
				textComp.alignment = TextAnchor.MiddleCenter;
				textComp.fontSize = 32;
				text.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
				textComp.font = ArialFont;
				textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

			return true;
        }


		/*
		[HarmonyPatch(typeof(ObjectTable))]
		[HarmonyPatch("Initialize")]
		[HarmonyPatch(new Type[] { typeof(ObjectTableDef), typeof(FVRObject.ObjectCategory), typeof(List<FVRObject.OTagEra>), typeof(List<FVRObject.OTagSet>), typeof(List<FVRObject.OTagFirearmSize>), typeof(List<FVRObject.OTagFirearmAction>), typeof(List<FVRObject.OTagFirearmFiringMode>), typeof(List<FVRObject.OTagFirearmFiringMode>), typeof(List<FVRObject.OTagFirearmFeedOption>), typeof(List<FVRObject.OTagFirearmMount>), typeof(List<FVRObject.OTagFirearmRoundPower>), typeof(List<FVRObject.OTagAttachmentFeature>), typeof(List<FVRObject.OTagMeleeStyle>), typeof(List<FVRObject.OTagMeleeHandedness>), typeof(List<FVRObject.OTagFirearmMount>), typeof(List<FVRObject.OTagPowerupType>), typeof(List<FVRObject.OTagThrownType>), typeof(List<FVRObject.OTagThrownDamageType>), typeof(int), typeof(int), typeof(int), typeof(bool)})]
		[HarmonyPrefix]
		public static bool Initialize(ObjectTable __instance, ObjectTableDef Def, FVRObject.ObjectCategory category, List<FVRObject.OTagEra> eras, List<FVRObject.OTagSet> sets, List<FVRObject.OTagFirearmSize> sizes, List<FVRObject.OTagFirearmAction> actions, List<FVRObject.OTagFirearmFiringMode> modes, List<FVRObject.OTagFirearmFiringMode> excludeModes, List<FVRObject.OTagFirearmFeedOption> feedoptions, List<FVRObject.OTagFirearmMount> mountsavailable, List<FVRObject.OTagFirearmRoundPower> roundPowers, List<FVRObject.OTagAttachmentFeature> features, List<FVRObject.OTagMeleeStyle> meleeStyles, List<FVRObject.OTagMeleeHandedness> meleeHandedness, List<FVRObject.OTagFirearmMount> mounttype, List<FVRObject.OTagPowerupType> powerupTypes, List<FVRObject.OTagThrownType> thrownTypes, List<FVRObject.OTagThrownDamageType> thrownDamageTypes, int minCapacity, int maxCapacity, int requiredExactCapacity, bool isBlanked)
		{
			__instance.MinCapacity = minCapacity;
			__instance.MaxCapacity = maxCapacity;
			if (isBlanked)
			{
				TNHFrameworkLogger.Log("Table is blanked, not populating!", TNHFrameworkLogger.LogType.TNH);
				return false;
			}
			if (Def.UseIDListOverride)
			{
				TNHFrameworkLogger.Log("Using IDOverride! Will only add IDs manually", TNHFrameworkLogger.LogType.TNH);

				for (int i = 0; i < Def.IDOverride.Count; i++)
				{
					__instance.Objs.Add(IM.OD[Def.IDOverride[i]]);
				}
				return false;
			}

			TNHFrameworkLogger.Log("Not using IDOverride, table will populate automatically", TNHFrameworkLogger.LogType.TNH);

			__instance.Objs = new List<FVRObject>(ManagerSingleton<IM>.Instance.odicTagCategory[category]);

			TNHFrameworkLogger.Log("Aquired all objects from Category (" + category + "), Listing them below", TNHFrameworkLogger.LogType.TNH);
			TNHFrameworkLogger.Log(string.Join("\n", __instance.Objs.Select(o => o.ItemID).ToArray()), TNHFrameworkLogger.LogType.TNH);

			TNHFrameworkLogger.Log("Going through and removing items that do not match desired tags", TNHFrameworkLogger.LogType.TNH);

			for (int j = __instance.Objs.Count - 1; j >= 0; j--)
			{
				FVRObject fvrobject = __instance.Objs[j];
				TNHFrameworkLogger.Log("Looking at item (" + fvrobject.ItemID + ")", TNHFrameworkLogger.LogType.TNH);

				if (!fvrobject.OSple)
				{
					TNHFrameworkLogger.Log("OSple is false, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (minCapacity > -1 && fvrobject.MaxCapacityRelated < minCapacity)
				{
					TNHFrameworkLogger.Log("Magazines not big enough, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (maxCapacity > -1 && fvrobject.MinCapacityRelated > maxCapacity)
				{
					TNHFrameworkLogger.Log("Magazines not small enough, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (requiredExactCapacity > -1 && !__instance.DoesGunMatchExactCapacity(fvrobject))
				{
					TNHFrameworkLogger.Log("Not exact capacity, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (eras != null && eras.Count > 0 && !eras.Contains(fvrobject.TagEra))
				{
					TNHFrameworkLogger.Log("Wrong era, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (sets != null && sets.Count > 0 && !sets.Contains(fvrobject.TagSet))
				{
					TNHFrameworkLogger.Log("Wrong set, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (sizes != null && sizes.Count > 0 && !sizes.Contains(fvrobject.TagFirearmSize))
				{
					TNHFrameworkLogger.Log("Wrong size, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (actions != null && actions.Count > 0 && !actions.Contains(fvrobject.TagFirearmAction))
				{
					TNHFrameworkLogger.Log("Wrong actions, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else if (roundPowers != null && roundPowers.Count > 0 && !roundPowers.Contains(fvrobject.TagFirearmRoundPower))
				{
					TNHFrameworkLogger.Log("Wrong round power, removing", TNHFrameworkLogger.LogType.TNH);
					__instance.Objs.RemoveAt(j);
				}
				else
				{
					if (modes != null && modes.Count > 0)
					{
						bool flag = false;
						for (int k = 0; k < modes.Count; k++)
						{
							if (!fvrobject.TagFirearmFiringModes.Contains(modes[k]))
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							TNHFrameworkLogger.Log("Wrong firing modes, removing", TNHFrameworkLogger.LogType.TNH);
							__instance.Objs.RemoveAt(j);
							break;
						}
					}
					if (excludeModes != null)
					{
						bool flag2 = false;
						for (int l = 0; l < excludeModes.Count; l++)
						{
							if (fvrobject.TagFirearmFiringModes.Contains(excludeModes[l]))
							{
								flag2 = true;
								break;
							}
						}
						if (flag2)
						{
							TNHFrameworkLogger.Log("Excluded firing modes, removing", TNHFrameworkLogger.LogType.TNH);
							__instance.Objs.RemoveAt(j);
							break;
						}
					}
					if (feedoptions != null)
					{
						bool flag3 = false;
						for (int m = 0; m < feedoptions.Count; m++)
						{
							if (!fvrobject.TagFirearmFeedOption.Contains(feedoptions[m]))
							{
								flag3 = true;
								break;
							}
						}
						if (flag3)
						{
							TNHFrameworkLogger.Log("Wrong feed options, removing", TNHFrameworkLogger.LogType.TNH);
							__instance.Objs.RemoveAt(j);
							break;
						}
					}
					if (mountsavailable != null)
					{
						bool flag4 = false;
						for (int n = 0; n < mountsavailable.Count; n++)
						{
							if (!fvrobject.TagFirearmMounts.Contains(mountsavailable[n]))
							{
								flag4 = true;
								break;
							}
						}
						if (flag4)
						{
							TNHFrameworkLogger.Log("Wrong mounts, removing", TNHFrameworkLogger.LogType.TNH);
							__instance.Objs.RemoveAt(j);
							break;
						}
					}
					if (powerupTypes != null && powerupTypes.Count > 0 && !powerupTypes.Contains(fvrobject.TagPowerupType))
					{
						TNHFrameworkLogger.Log("Wrong powerup type, removing", TNHFrameworkLogger.LogType.TNH);
						__instance.Objs.RemoveAt(j);
					}
					else if (thrownTypes != null && thrownTypes.Count > 0 && !thrownTypes.Contains(fvrobject.TagThrownType))
					{
						TNHFrameworkLogger.Log("Wrong thrown type, removing", TNHFrameworkLogger.LogType.TNH);
						__instance.Objs.RemoveAt(j);
					}
					else if (thrownTypes != null && thrownTypes.Count > 0 && !thrownTypes.Contains(fvrobject.TagThrownType))
					{
						__instance.Objs.RemoveAt(j);
					}
					else if (meleeStyles != null && meleeStyles.Count > 0 && !meleeStyles.Contains(fvrobject.TagMeleeStyle))
					{
						TNHFrameworkLogger.Log("Wrong melee style, removing", TNHFrameworkLogger.LogType.TNH);
						__instance.Objs.RemoveAt(j);
					}
					else if (meleeHandedness != null && meleeHandedness.Count > 0 && !meleeHandedness.Contains(fvrobject.TagMeleeHandedness))
					{
						TNHFrameworkLogger.Log("Wrong melee handedness, removing", TNHFrameworkLogger.LogType.TNH);
						__instance.Objs.RemoveAt(j);
					}
					else if (mounttype != null && mounttype.Count > 0 && !mounttype.Contains(fvrobject.TagAttachmentMount))
					{
						TNHFrameworkLogger.Log("Wrong mount type, removing", TNHFrameworkLogger.LogType.TNH);
						__instance.Objs.RemoveAt(j);
					}
					else if (features != null && features.Count > 0 && !features.Contains(fvrobject.TagAttachmentFeature))
					{
						TNHFrameworkLogger.Log("Wrong features, removing", TNHFrameworkLogger.LogType.TNH);
						__instance.Objs.RemoveAt(j);
					}
                    else
                    {
						TNHFrameworkLogger.Log("Keeping item!", TNHFrameworkLogger.LogType.TNH);
					}
				}
			}

			return false;
		}
		*/

	}
}
