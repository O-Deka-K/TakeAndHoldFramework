using BepInEx;
using FistVR;
using System.Collections.Generic;
using System.Linq;
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
                        if (mag == null)
                            TNHFrameworkLogger.Log("Spawning nothing since group was compatible magazines, and could not find a compatible magazine for player", TNHFrameworkLogger.LogType.TNH);
                        else
                            selectedItem = mag.ItemID;
                    }
                    else
                    {
                        var list = selectedGroup.GetObjects();

                        if (!list.Any())
                            TNHFrameworkLogger.Log("Spawning nothing since group was empty", TNHFrameworkLogger.LogType.TNH);
                        else
                            selectedItem = list.GetRandom();
                    }

                    // If list is empty, then there's nothing to spawn
                    if (selectedItem.IsNullOrWhiteSpace())
                        continue;

                    if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(selectedItem))
                    {
                        TNHFrameworkLogger.Log($"Spawning vault file {selectedItem}", TNHFrameworkLogger.LogType.TNH);

                        GameObject newObject = new("SosigDropMarker");
                        newObject.transform.position = transform.position + (Vector3.up * 0.1f * (spawnedItems + 1));
                        newObject.transform.rotation = Quaternion.identity;
                        newObject.transform.localScale = transform.localScale;
                        VaultSystem.SpawnVaultFile(LoadedTemplateManager.LoadedVaultFiles[selectedItem], newObject.transform, true, false, false, out _, Vector3.zero);
                        Destroy(newObject, 10f);
                    }
                    else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(selectedItem) && LoadedTemplateManager.LoadedLegacyVaultFiles[selectedItem] != null)
                    {
                        TNHFrameworkLogger.Log($"Spawning legacy vault file {selectedItem}", TNHFrameworkLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnLegacyVaultFile(LoadedTemplateManager.LoadedLegacyVaultFiles[selectedItem],
                            transform.position + (Vector3.up * 0.1f * (spawnedItems + 1)), Quaternion.identity, M, true));
                    }
                    else if (IM.OD[selectedItem] != null)
                    {
                        TNHFrameworkLogger.Log($"Spawning item {selectedItem}", TNHFrameworkLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnItemRoutine(M, transform.position + (Vector3.up * 0.1f * (spawnedItems + 1)), Quaternion.identity, IM.OD[selectedItem], true));
                    }

                    spawnedItems++;

                    // Haptic buzz for dropped item
                    if (TNHFramework.SosigItemDropVibrate.Value)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            FVRViveHand hand = GM.CurrentMovementManager.Hands[i];
                            hand.Buzz(hand.Buzzer.Buzz_GunShot);
                        }
                    }
                }
            }
        }

        void OnApplicationQuit()
        {
            dontDrop = true;
        }
    }
}
