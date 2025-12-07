using FistVR;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using UnityEngine;

namespace TNHFramework.Utilities
{
    static class FirearmUtils
    {
        private static readonly FieldInfo fiNumRounds = typeof(FVRFireArmMagazine).GetField("m_numRounds", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Returns a list of magazines, clips, or speedloaders compatible with the firearm, and also within any of the optional criteria
        /// </summary>
        /// <param name="firearm">The FVRObject of the firearm</param>
        /// <param name="minCapacity">The minimum capacity for desired containers</param>
        /// <param name="maxCapacity">The maximum capacity for desired containers. If this values is zero or negative, it is interpreted as no capacity ceiling</param>
        /// <param name="smallestIfEmpty">If true, when the returned list would normally be empty, will instead return the smallest capacity magazine compatible with the firearm</param>
        /// <param name="blacklistedContainers">A list of ItemIDs for magazines, clips, or speedloaders that will be excluded</param>
        /// <returns> A list of ammo container FVRObjects that are compatible with the given firearm </returns>
        public static List<FVRObject> GetCompatibleAmmoContainers(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, bool smallestIfEmpty = true, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            // Refresh the FVRObject to have data directly from object dictionary
            firearm = IM.OD[firearm.ItemID];

            // If the max capacity is zero or negative, we interpret that as no limit on max capacity
            if (maxCapacity <= 0)
                maxCapacity = 9999;

            // Create a list containing all compatible ammo containers
            List<FVRObject> compatibleContainers = [];
            if (firearm.CompatibleSpeedLoaders != null)
                compatibleContainers.AddRange(firearm.CompatibleSpeedLoaders);

            // Go through each magazine and add compatible ones
            foreach (FVRObject magazine in firearm.CompatibleMagazines)
            {
                if (blacklist != null && !blacklist.IsMagazineAllowed(magazine.ItemID))
                    continue;

                if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(magazine.ItemID))
                    continue;

                if (magazine.MagazineCapacity < minCapacity || magazine.MagazineCapacity > maxCapacity)
                    continue;

                compatibleContainers.Add(magazine);
            }

            // Go through each magazine and add compatible ones
            foreach (FVRObject clip in firearm.CompatibleClips)
            {
                if (blacklist != null && !blacklist.IsClipAllowed(clip.ItemID))
                    continue;

                if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(clip.ItemID))
                    continue;

                if (clip.MagazineCapacity < minCapacity || clip.MagazineCapacity > maxCapacity)
                    continue;

                compatibleContainers.Add(clip);
            }

            // If the resulting list is empty, and smallestIfEmpty is true, add the smallest capacity magazine to the list
            if (!compatibleContainers.Any() && smallestIfEmpty && firearm.CompatibleMagazines != null)
            {
                FVRObject magazine = GetSmallestCapacityMagazine(firearm.CompatibleMagazines, globalObjectBlacklist);
                if (magazine != null)
                    compatibleContainers.Add(magazine);
            }

            return compatibleContainers;
        }

        public static List<FVRObject> GetCompatibleMagazines(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, bool smallestIfEmpty = true, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            // Refresh the FVRObject to have data directly from object dictionary
            firearm = IM.OD[firearm.ItemID];

            // If the max capacity is zero or negative, we interpret that as no limit on max capacity
            if (maxCapacity <= 0)
                maxCapacity = 9999;

            // Create a list containing all compatible ammo containers
            List<FVRObject> compatibleMagazines = [];
            if (firearm.CompatibleMagazines != null)
                compatibleMagazines.AddRange(firearm.CompatibleMagazines);

            // Go through these containers and remove any that don't fit given criteria
            for (int i = compatibleMagazines.Count - 1; i >= 0; i--)
            {
                if (blacklist != null && !blacklist.IsMagazineAllowed(compatibleMagazines[i].ItemID))
                    compatibleMagazines.RemoveAt(i);
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(compatibleMagazines[i].ItemID))
                    compatibleMagazines.RemoveAt(i);
                else if (compatibleMagazines[i].MagazineCapacity < minCapacity || compatibleMagazines[i].MagazineCapacity > maxCapacity)
                    compatibleMagazines.RemoveAt(i);
            }

            // If the resulting list is empty, and smallestIfEmpty is true, add the smallest capacity magazine to the list
            if (!compatibleMagazines.Any() && smallestIfEmpty && firearm.CompatibleMagazines is not null)
            {
                FVRObject magazine = GetSmallestCapacityMagazine(firearm.CompatibleMagazines, globalObjectBlacklist, blacklist);
                if (magazine != null)
                    compatibleMagazines.Add(magazine);
            }

            return compatibleMagazines;
        }

        public static List<FVRObject> GetCompatibleClips(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            // Refresh the FVRObject to have data directly from object dictionary
            firearm = IM.OD[firearm.ItemID];

            // If the max capacity is zero or negative, we interpret that as no limit on max capacity
            if (maxCapacity <= 0)
                maxCapacity = 9999;

            // Create a list containing all compatible ammo containers
            List<FVRObject> compatibleClips = [];
            if (firearm.CompatibleClips != null)
                compatibleClips.AddRange(firearm.CompatibleClips);

            // Go through these containers and remove any that don't fit given criteria
            for (int i = compatibleClips.Count - 1; i >= 0; i--)
            {
                if (blacklist != null && !blacklist.IsClipAllowed(compatibleClips[i].ItemID))
                    compatibleClips.RemoveAt(i);
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(compatibleClips[i].ItemID))
                    compatibleClips.RemoveAt(i);
                else if (compatibleClips[i].MagazineCapacity < minCapacity || compatibleClips[i].MagazineCapacity > maxCapacity)
                    compatibleClips.RemoveAt(i);
            }

            return compatibleClips;
        }

        public static List<FVRObject> GetCompatibleRounds(FVRObject firearm, List<TagEra> eras, List<TagSet> sets, List<string> globalBulletBlacklist = null, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            return GetCompatibleRounds(firearm, eras.Select(o => (FVRObject.OTagEra)o).ToList(), sets.Select(o => (FVRObject.OTagSet)o).ToList(), globalBulletBlacklist, globalObjectBlacklist, blacklist);
        }

        public static List<FVRObject> GetCompatibleRounds(FVRObject firearm, List<FVRObject.OTagEra> eras, List<FVRObject.OTagSet> sets, List<string> globalBulletBlacklist = null, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            // Refresh the FVRObject to have data directly from object dictionary
            firearm = IM.OD[firearm.ItemID];

            // Create a list containing all compatible ammo containers
            List<FVRObject> compatibleRounds = [];
            if (firearm.CompatibleSingleRounds != null)
                compatibleRounds.AddRange(firearm.CompatibleSingleRounds);

            // Go through these containers and remove any that don't fit given criteria
            for (int i = compatibleRounds.Count - 1; i >= 0; i--)
            {
                if (compatibleRounds.Count <= 1)
                    break;

                if (!eras.Contains(compatibleRounds[i].TagEra) || !sets.Contains(compatibleRounds[i].TagSet))
                    compatibleRounds.RemoveAt(i);
                else if (blacklist != null && !blacklist.IsRoundAllowed(compatibleRounds[i].ItemID))
                    compatibleRounds.RemoveAt(i);
                else if (globalBulletBlacklist != null && globalBulletBlacklist.Contains(compatibleRounds[i].ItemID))
                    compatibleRounds.RemoveAt(i);
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(compatibleRounds[i].ItemID))
                    compatibleRounds.RemoveAt(i);
            }

            if (AM.STypeDic.ContainsKey(firearm.RoundType))
            {
                // Get a list of ammo types that cost more than 0 and sort them in descending order by cost
                var dogshit = AM.STypeDic[firearm.RoundType].Values
                    .Where(x => x.Cost > 0)
                    .OrderByDescending(x => x.Cost);

                // Remove ammo types starting from highest cost. Don't remove if it's the last one left.
                foreach (var round in dogshit)
                {
                    if (compatibleRounds.Count > 1 && compatibleRounds.Contains(round.ObjectID))
                        compatibleRounds.Remove(round.ObjectID);
                }
            }

            return compatibleRounds;
        }

        /// <summary>
        /// Returns the smallest capacity magazine from the given list of magazine FVRObjects
        /// </summary>
        /// <param name="magazines">A list of magazine FVRObjects</param>
        /// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
        /// <returns>An FVRObject for the smallest magazine. Can be null if magazines list is empty</returns>
        public static FVRObject GetSmallestCapacityMagazine(List<FVRObject> magazines, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            if (magazines == null || !magazines.Any())
                return null;

            List<FVRObject> eligibleMagazines = [.. magazines];
            if (blacklist != null)
                eligibleMagazines.RemoveAll(o => !blacklist.IsMagazineAllowed(o.ItemID));

            if (globalObjectBlacklist != null)
                eligibleMagazines.RemoveAll(o => globalObjectBlacklist.Contains(o.ItemID));

            if (!eligibleMagazines.Any())
                return null;

            int minCapacity = eligibleMagazines.Min(o => o.MagazineCapacity);
            List<FVRObject> smallestMagazines = [.. eligibleMagazines.Where(o => o.MagazineCapacity == minCapacity)];

            if (smallestMagazines.Any())
                return smallestMagazines.GetRandom();

            return null;
        }

        /// <summary>
        /// Returns the smallest capacity magazine that is compatible with the given firearm
        /// </summary>
        /// <param name="firearm">The FVRObject of the firearm</param>
        /// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
        /// <returns>An FVRObject for the smallest magazine. Can be null if firearm has no magazines</returns>
        public static FVRObject GetSmallestCapacityMagazine(FVRObject firearm, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
        {
            // Refresh the FVRObject to have data directly from object dictionary
            firearm = IM.OD[firearm.ItemID];

            return GetSmallestCapacityMagazine(firearm.CompatibleMagazines, globalObjectBlacklist, blacklist);
        }

        /// <summary>
        /// Returns true if the given FVRObject has any compatible rounds, clips, magazines, or speedloaders
        /// </summary>
        /// <param name="item">The FVRObject that is being checked</param>
        /// <returns>True if the FVRObject has any compatible rounds, clips, magazines, or speedloaders. False if it contains none of these</returns>
        public static bool FVRObjectHasAmmoObject(FVRObject item)
        {
            if (item == null)
                return false;

            // Refresh the FVRObject to have data directly from object dictionary
            item = IM.OD[item.ItemID];

            return (item.CompatibleSingleRounds != null && item.CompatibleSingleRounds.Any()) || (item.CompatibleClips != null && item.CompatibleClips.Any()) || (item.CompatibleMagazines != null && item.CompatibleMagazines.Any()) || (item.CompatibleSpeedLoaders != null && item.CompatibleSpeedLoaders.Any());
        }

        /// <summary>
        /// Returns true if the given FVRObject has any compatible clips, magazines, or speedloaders
        /// </summary>
        /// <param name="item">The FVRObject that is being checked</param>
        /// <returns>True if the FVRObject has any compatible clips, magazines, or speedloaders. False if it contains none of these</returns>
        public static bool FVRObjectHasAmmoContainer(FVRObject item)
        {
            if (item == null)
                return false;

            // Refresh the FVRObject to have data directly from object dictionary
            item = IM.OD[item.ItemID];

            return (item.CompatibleClips != null && item.CompatibleClips.Any()) || (item.CompatibleMagazines != null && item.CompatibleMagazines.Any()) || (item.CompatibleSpeedLoaders != null && item.CompatibleSpeedLoaders.Any());
        }

        /// <summary>
        /// Returns the next largest magazine when compared to the current magazine.
        /// </summary>
        /// <param name="currentMagazine">The base magazine FVRObject, for which we are getting the next largest magazine</param>
        /// <param name="globalObjectBlacklist">A global list of ItemIDs that are not allowed to appear</param>
        /// <param name="blacklistedMagazines">A list of ItemIDs for magazines that will be excluded</param>
        /// <returns>An FVRObject for the next largest magazine. Can be null if no next largest magazine is found</returns>
        public static FVRObject GetNextHighestCapacityMagazine(FVRObject currentMagazine, List<string> globalObjectBlacklist = null, List<string> blacklistedMagazines = null)
        {
            currentMagazine = IM.OD[currentMagazine.ItemID];

            if (!IM.CompatMags.ContainsKey(currentMagazine.MagazineType))
            {
                TNHFrameworkLogger.LogError($"Magazine type for ({currentMagazine.ItemID}) is not in compatible magazines dictionary! Will return null");
                return null;
            }

            int magThreshold = Mathf.Max(1, TNHFramework.MagUpgradeThreshold.Value);
            List<FVRObject> eligibleMagazines = [.. IM.CompatMags[currentMagazine.MagazineType].Where(o => o.MagazineCapacity >= currentMagazine.MagazineCapacity + magThreshold)];

            if (blacklistedMagazines != null)
                eligibleMagazines.RemoveAll(o => blacklistedMagazines.Contains(o.ItemID));

            if (globalObjectBlacklist != null)
                eligibleMagazines.RemoveAll(o => globalObjectBlacklist.Contains(o.ItemID));

            if (!eligibleMagazines.Any())
                return null;

            int nextCapacity = eligibleMagazines.Min(o => o.MagazineCapacity);
            List<FVRObject> nextLargestMagazines = [.. eligibleMagazines.Where(o => o.MagazineCapacity == nextCapacity)];

            if (nextLargestMagazines.Any())
                return nextLargestMagazines.GetRandom();

            int largestCapacity = eligibleMagazines.Max(o => o.MagazineCapacity);
            List<FVRObject> largestMagazines = [.. eligibleMagazines.Where(o => o.MagazineCapacity == largestCapacity)];

            if (currentMagazine.MagazineCapacity < largestCapacity && largestMagazines.Any())
                return largestMagazines.GetRandom();

            return null;
        }

        /// <summary>
        /// Returns a list of FVRPhysicalObjects for items that are either in the players hand, or in one of the players quickbelt slots. This also includes any items in a players backpack if they are wearing one
        /// </summary>
        /// <returns>A list of FVRPhysicalObjects equipped on the player</returns>
        public static List<FVRPhysicalObject> GetEquippedItems()
        {
            List<FVRPhysicalObject> heldItems = [];

            FVRInteractiveObject rightHandObject = GM.CurrentMovementManager.Hands[0].CurrentInteractable;
            FVRInteractiveObject leftHandObject = GM.CurrentMovementManager.Hands[1].CurrentInteractable;

            // Get any items in the players hands
            if (rightHandObject is FVRPhysicalObject)
                heldItems.Add((FVRPhysicalObject)rightHandObject);

            if (leftHandObject is FVRPhysicalObject)
                heldItems.Add((FVRPhysicalObject)leftHandObject);

            // Get any items on the players body
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QuickbeltSlots)
            {
                if (slot.CurObject is not null && slot.CurObject.ObjectWrapper is not null)
                    heldItems.Add(slot.CurObject);

                // If the player has a backpack on, we should search through that as well
                if (slot.CurObject is PlayerBackPack && ((PlayerBackPack)slot.CurObject).ObjectWrapper is not null)
                {
                    foreach (FVRQuickBeltSlot backpackSlot in GM.CurrentPlayerBody.QuickbeltSlots)
                    {
                        if (backpackSlot.CurObject is not null)
                            heldItems.Add(backpackSlot.CurObject);
                    }
                }
            }

            return heldItems;
        }

        /// <summary>
        /// Returns a list of FVRObjects for all of the items that are equipped on the player. Items without a valid FVRObject are excluded. There may also be duplicate entries if the player has identical items equipped
        /// </summary>
        /// <returns>A list of FVRObjects equipped on the player</returns>
        public static List<FVRObject> GetEquippedFVRObjects()
        {
            List<FVRObject> equippedFVRObjects = [];

            foreach (FVRPhysicalObject item in GetEquippedItems())
            {
                if (item.ObjectWrapper is null)
                    continue;

                equippedFVRObjects.Add(item.ObjectWrapper);
            }

            return equippedFVRObjects;
        }

        /// <summary>
        /// Returns a random magazine, clip, or speedloader that is compatible with one of the players equipped items
        /// </summary>
        /// <param name="minCapacity">The minimum capacity for desired containers</param>
        /// <param name="maxCapacity">The maximum capacity for desired containers</param>
        /// <param name="blacklistedContainers">A list of ItemIDs for magazines that will be excluded</param>
        /// <returns>An FVRObject for an ammo container. Can be null if no container is found</returns>
        public static FVRObject GetAmmoContainerForEquipped(int minCapacity = 0, int maxCapacity = 9999, List<string> globalObjectBlacklist = null, Dictionary<string, MagazineBlacklistEntry> blacklist = null)
        {
            List<FVRObject> heldItems = GetEquippedFVRObjects();

            // Interpret -1 as having no max capacity
            if (maxCapacity == -1)
                maxCapacity = 9999;

            // Go through and remove any items that have no ammo containers
            heldItems.RemoveAll(o => !FVRObjectHasAmmoContainer(o));

            // Now go through all items that do have ammo containers, and try to get an ammo container for one of them
            heldItems.Shuffle();
            foreach (FVRObject item in heldItems)
            {
                MagazineBlacklistEntry blacklistEntry = null;
                if (blacklist != null && blacklist.ContainsKey(item.ItemID))
                    blacklistEntry = blacklist[item.ItemID];

                List<FVRObject> containers = GetCompatibleAmmoContainers(item, minCapacity, maxCapacity, false, globalObjectBlacklist, blacklistEntry);
                if (containers.Any())
                    return containers.GetRandom();
            }

            return null;
        }

        /// <summary>
        /// Returns a list of all attached objects on the given firearm. This includes attached magazines
        /// </summary>
        /// <param name="fireArm">The firearm that is being scanned for attachmnets</param>
        /// <param name="includeSelf">If true, includes the given firearm as the first item in the list of attached objects</param>
        /// <returns>A list containing every attached item on the given firearm</returns>
        public static List<FVRPhysicalObject> GetAllAttachedObjects(FVRFireArm fireArm, bool includeSelf = false)
        {
            List<FVRPhysicalObject> detectedObjects = [];

            if (includeSelf)
                detectedObjects.Add(fireArm);

            if (fireArm.Magazine is not null && !fireArm.Magazine.IsIntegrated && fireArm.Magazine.ObjectWrapper is not null)
                detectedObjects.Add(fireArm.Magazine);

            foreach (FVRFireArmAttachment attachment in fireArm.Attachments)
            {
                if (attachment.ObjectWrapper is not null)
                    detectedObjects.Add(attachment);
            }

            return detectedObjects;
        }

        public static FVRFireArmMagazine SpawnDuplicateMagazine(TNH_Manager M, FVRFireArmMagazine magazine, Vector3 position, Quaternion rotation)
        {
            FVRObject objectWrapper = magazine.ObjectWrapper;
            GameObject gameObject = Object.Instantiate(objectWrapper.GetGameObject(), position, rotation);
            M.AddObjectToTrackedList(gameObject);

            FVRFireArmMagazine component = gameObject.GetComponent<FVRFireArmMagazine>();
            for (int i = 0; i < Mathf.Min(magazine.LoadedRounds.Length, component.LoadedRounds.Length); i++)
            {
                if (magazine.LoadedRounds[i] != null && magazine.LoadedRounds[i].LR_Mesh != null)
                {
                    component.LoadedRounds[i].LR_Class = magazine.LoadedRounds[i].LR_Class;
                    component.LoadedRounds[i].LR_Mesh = magazine.LoadedRounds[i].LR_Mesh;
                    component.LoadedRounds[i].LR_Material = magazine.LoadedRounds[i].LR_Material;
                    component.LoadedRounds[i].LR_ObjectWrapper = magazine.LoadedRounds[i].LR_ObjectWrapper;
                }
            }

            //component.m_numRounds = magazine.m_numRounds;
            fiNumRounds.SetValue(component, fiNumRounds.GetValue(magazine));
            component.UpdateBulletDisplay();

            return component;
        }

        public static Speedloader SpawnDuplicateSpeedloader(TNH_Manager manager, Speedloader speedloader, Vector3 position, Quaternion rotation)
        {
            FVRObject objectWrapper = speedloader.ObjectWrapper;
            GameObject gameObject = Object.Instantiate(objectWrapper.GetGameObject(), position, rotation);
            manager.AddObjectToTrackedList(gameObject);
            
            Speedloader component = gameObject.GetComponent<Speedloader>();
            for (int i = 0; i < speedloader.Chambers.Count; i++)
            {
                if (speedloader.Chambers[i].IsLoaded)
                    component.Chambers[i].Load(speedloader.Chambers[i].LoadedClass, false);
                else
                    component.Chambers[i].Unload();
            }

            return component;
        }
    }
}
