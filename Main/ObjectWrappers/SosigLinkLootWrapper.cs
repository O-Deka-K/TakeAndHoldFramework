using BepInEx;
using FistVR;
using System.Collections.Generic;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework
{
    public class SosigLinkLootWrapper : MonoBehaviour
    {
        public CustomCharacter character;
        public EquipmentGroup group;
        public bool dontDrop = false;
        public bool shouldDropOnCleanup;

        void OnDestroy()
        {
            if (dontDrop)
                return;
            
            TNHTweakerLogger.Log("TNHTWEAKER -- Lootable link was destroyed!", TNHTweakerLogger.LogType.TNH);

            List<EquipmentGroup> selectedGroups = group.GetSpawnedEquipmentGroups();
            string selectedItem = null;
            int spawnedItems = 0;

            foreach (EquipmentGroup selectedGroup in selectedGroups)
            {
                for (int itemIndex = 0; itemIndex < selectedGroup.ItemsToSpawn; itemIndex++)
                {
                    if (selectedGroup.IsCompatibleMagazine)
                    {
                        FVRObject mag = FirearmUtils.GetAmmoContainerForEquipped(selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity, character.GlobalObjectBlacklist, character.GetMagazineBlacklist());
                        if (mag != null)
                        {
                            selectedItem = mag.ItemID;
                        }
                        else
                        {
                            TNHTweakerLogger.Log("TNHTWEAKER -- Spawning nothing since group was compatible magazines, and could not find a compatible magazine for player", TNHTweakerLogger.LogType.TNH);
                            //return;
                        }
                    }
                    else
                    {
                        var list = selectedGroup.GetObjects();

                        if (list.Count == 0)
                        {
                            TNHTweakerLogger.Log("TNHTWEAKER -- Spawning nothing since group was empty", TNHTweakerLogger.LogType.TNH);
                        }
                        else
                        {
                            selectedItem = list.GetRandom();
                        }
                    }

                    // If list is empty, then there's nothing to spawn
                    if (selectedItem.IsNullOrWhiteSpace())
                        continue;

                    if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(selectedItem))
                    {
                        TNHTweakerLogger.Log($"TNHTWEAKER -- Spawning vault file {selectedItem}", TNHTweakerLogger.LogType.TNH);

                        Transform newTransform = transform;
                        newTransform.position = transform.position + (Vector3.up * 0.1f * spawnedItems);
                        VaultSystem.SpawnVaultFile(LoadedTemplateManager.LoadedVaultFiles[selectedItem], newTransform, true, false, false, out _, Vector3.zero);
                    }
                    else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(selectedItem))
                    {
                        TNHTweakerLogger.Log($"TNHTWEAKER -- Spawning legacy vault file {selectedItem}", TNHTweakerLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnFirearm(LoadedTemplateManager.LoadedLegacyVaultFiles[selectedItem],
                            transform.position + (Vector3.up * 0.1f * spawnedItems), transform.rotation));
                    }
                    else
                    {
                        TNHTweakerLogger.Log($"TNHTWEAKER -- Spawning item {selectedItem}", TNHTweakerLogger.LogType.TNH);
                        Instantiate(IM.OD[selectedItem].GetGameObject(), transform.position + (Vector3.up * 0.1f * spawnedItems), transform.rotation);
                    }

                    spawnedItems += 1;
                }
            }
        }
    }
}