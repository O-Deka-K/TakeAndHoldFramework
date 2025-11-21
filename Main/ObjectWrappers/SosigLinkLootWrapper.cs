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
        public TNH_Manager M;
        public CustomCharacter character;
        public EquipmentGroup group;
        public bool dontDrop = false;
        public bool shouldDropOnCleanup;

        void OnDestroy()
        {
            if (dontDrop)
                return;
            
            TNHFrameworkLogger.Log("Lootable link was destroyed!", TNHFrameworkLogger.LogType.TNH);

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
                            TNHFrameworkLogger.Log("Spawning nothing since group was compatible magazines, and could not find a compatible magazine for player", TNHFrameworkLogger.LogType.TNH);
                            //return;
                        }
                    }
                    else
                    {
                        var list = selectedGroup.GetObjects();

                        if (list.Count == 0)
                        {
                            TNHFrameworkLogger.Log("Spawning nothing since group was empty", TNHFrameworkLogger.LogType.TNH);
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
                        TNHFrameworkLogger.Log($"Spawning vault file {selectedItem}", TNHFrameworkLogger.LogType.TNH);

                        GameObject newObject = new("SosigDropMarker");
                        newObject.transform.position = transform.position + (Vector3.up * 0.1f * spawnedItems);
                        newObject.transform.rotation = transform.rotation;
                        newObject.transform.localScale = transform.localScale;
                        VaultSystem.SpawnVaultFile(LoadedTemplateManager.LoadedVaultFiles[selectedItem], newObject.transform, true, false, false, out _, Vector3.zero);
                        Destroy(newObject, 10f);
                    }
                    else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(selectedItem))
                    {
                        TNHFrameworkLogger.Log($"Spawning legacy vault file {selectedItem}", TNHFrameworkLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnLegacyVaultFile(LoadedTemplateManager.LoadedLegacyVaultFiles[selectedItem],
                            transform.position + (Vector3.up * 0.1f * spawnedItems), transform.rotation, M));
                    }
                    else
                    {
                        TNHFrameworkLogger.Log($"Spawning item {selectedItem}", TNHFrameworkLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnItemRoutine(M, transform.position + (Vector3.up * 0.1f * spawnedItems), transform.rotation, IM.OD[selectedItem]));
                    }

                    spawnedItems++;
                }
            }
        }
    }
}
