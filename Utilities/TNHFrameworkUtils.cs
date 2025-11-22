using Deli.VFS;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TNHFramework.ObjectTemplates;
using UnityEngine;
using EquipmentGroup = TNHFramework.ObjectTemplates.EquipmentGroup;

namespace TNHFramework.Utilities
{
    static class TNHFrameworkUtils
    {
        public static bool IsSpawning = false;
        public static GameObject LastSpawnedGun;

        public static void RunCoroutine(IEnumerator routine, Action<Exception> onError = null)
        {
            AnvilManager.Run(RunAndCatch(routine, onError));
        }

        public static IEnumerator RunAndCatch(IEnumerator routine, Action<Exception> onError = null)
        {
            bool more = true;
            while (more)
            {
                try
                {
                    more = routine.MoveNext();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e);
                    yield break;
                }

                if (more)
                {
                    yield return routine.Current;
                }
            }
        }

        public static void RemoveUnloadedObjectIDs(EquipmentGroup group)
        {
            if (group.IDOverride != null)
                RemoveMissingObjectIDs(group.IDOverride);
        }

        public static void RemoveUnloadedObjectIDs(ObjectTemplates.V1.EquipmentGroup group)
        {
            if (group.IDOverride != null)
                RemoveMissingObjectIDs(group.IDOverride);
        }

        public static void RemoveMissingObjectIDs(List<string> IDOverride)
        {
            for (int i = IDOverride.Count - 1; i >= 0; i--)
            {
                if (!IM.OD.ContainsKey(IDOverride[i]))
                {
                    // If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                    if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(IDOverride[i]))
                    {
                        if (!LoadedTemplateManager.LoadedLegacyVaultFiles[IDOverride[i]].AllComponentsLoaded())
                            IDOverride.RemoveAt(i);
                    }

                    // If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                    else if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(IDOverride[i]))
                    {
                        if (!VaultFileComponentsLoaded(LoadedTemplateManager.LoadedVaultFiles[IDOverride[i]]))
                            IDOverride.RemoveAt(i);
                    }

                    // If this is not a vaulted gun, remove it
                    else
                    {
                        TNHFrameworkLogger.LogWarning($"Object in table not loaded, removing it from object table! ObjectID: {IDOverride[i]}");
                        IDOverride.RemoveAt(i);
                    }
                }
            }
        }

        public static bool VaultFileComponentsLoaded(VaultFile template)
        {
            bool result = true;
            List<string> missing = [];

            foreach (VaultObject vaultObject in template.Objects)
            {
                foreach (VaultElement vaultElement in vaultObject.Elements)
                {
                    if (!IM.OD.ContainsKey(vaultElement.ObjectID))
                    {
                        missing.Add(vaultElement.ObjectID);
                        result = false;
                    }
                }
            }

            if (!result)
                TNHFrameworkLogger.LogWarning($"Vaulted gun in table does not have all components loaded, removing it! VaultID: {template.FileName}, Missing ID(s): {string.Join(", ", [.. missing])}");

            return result;
        }

        public static void RemoveUnloadedObjectIDs(SosigTemplate template)
        {
            // Loop through all outfit configs and remove any clothing objects that don't exist
            foreach (OutfitConfig config in template.OutfitConfigs)
            {
                for (int i = config.Headwear.Count - 1; i >= 0 ; i--)
                {
                    if (!IM.OD.ContainsKey(config.Headwear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Headwear[i]);
                        config.Headwear.RemoveAt(i);
                    }
                }

                if (config.Headwear.Count == 0)
                    config.Chance_Headwear = 0;

                for (int i = config.Facewear.Count - 1; i >= 0 ; i--)
                {
                    if (!IM.OD.ContainsKey(config.Facewear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Facewear[i]);
                        config.Facewear.RemoveAt(i);
                    }
                }
                
                if (config.Facewear.Count == 0)
                    config.Chance_Facewear = 0;

                for (int i = config.Eyewear.Count - 1; i >= 0 ; i--)
                {
                    if (!IM.OD.ContainsKey(config.Eyewear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Eyewear[i]);
                        config.Eyewear.RemoveAt(i);
                    }
                }
                
                if (config.Eyewear.Count == 0)
                    config.Chance_Eyewear = 0;

                for (int i = config.Torsowear.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Torsowear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Torsowear[i]);
                        config.Torsowear.RemoveAt(i);
                    }
                }
                
                if (config.Torsowear.Count == 0)
                    config.Chance_Torsowear = 0;

                for (int i = config.Pantswear.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Pantswear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Pantswear[i]);
                        config.Pantswear.RemoveAt(i);
                    }
                }
                
                if (config.Pantswear.Count == 0)
                    config.Chance_Pantswear = 0;

                for (int i = config.Pantswear_Lower.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Pantswear_Lower[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Pantswear_Lower[i]);
                        config.Pantswear_Lower.RemoveAt(i);
                    }
                }
                
                if (config.Pantswear_Lower.Count == 0)
                    config.Chance_Pantswear_Lower = 0;

                for (int i = config.Backpacks.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Backpacks[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Backpacks[i]);
                        config.Backpacks.RemoveAt(i);
                    }
                }
                
                if (config.Backpacks.Count == 0)
                    config.Chance_Backpacks = 0;
            }
        }

        /// <summary>
        /// Loads a sprite from a file path. Solution found here: https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(FileInfo file)
        {
            Texture2D spriteTexture = LoadTexture(file);

            if (spriteTexture == null)
                return null;
            
            Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), 100f);
            sprite.name = file.Name;
            return sprite;
        }

        /// <summary>
        /// Loads a sprite from a file path. Solution found here: https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(IFileHandle file)
        {
            Texture2D spriteTexture = LoadTexture(file);
            
            if (spriteTexture == null)
                return null;
            
            Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), 100f);
            sprite.name = file.Name;
            return sprite;
        }

        public static Sprite LoadSprite(Texture2D spriteTexture, float pixelsPerUnit = 100f)
        {
            return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit);
        }

        /// <summary>
        /// Loads a texture2D from the sent file. Source: https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Texture2D LoadTexture(FileInfo file)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails
            Stream fileStream = file.OpenRead();
            MemoryStream mem = new();

            CopyStream(fileStream, mem);

            byte[] fileData = mem.ToArray();

            Texture2D tex2D = new(2, 2);
            if (tex2D.LoadImage(fileData))
                return tex2D;

            return null;
        }

        /// <summary>
        /// Loads a texture2D from the sent file. Source: https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Texture2D LoadTexture(IFileHandle file)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails
            Stream fileStream = file.OpenRead();
            MemoryStream mem = new();

            CopyStream(fileStream, mem);

            byte[] fileData = mem.ToArray();

            Texture2D tex2D = new(2, 2);          
            if (tex2D.LoadImage(fileData))
                return tex2D;

            return null;                     
        }

        /// <summary>
        /// Copies the input stream into the output stream. Source: https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] b = new byte[32768];
            int r;

            while ((r = input.Read(b, 0, b.Length)) > 0)
            {
                output.Write(b, 0, r);
            }
        }

        public static IEnumerator SpawnLegacyVaultFile(SavedGunSerializable savedGun, Vector3 position, Quaternion rotation, TNH_Manager M)
        {
            IsSpawning = true;

            List<GameObject> toDealWith = [];
            List<GameObject> toMoveToTrays = [];
            FVRFireArm myGun = null;
            FVRFireArmMagazine myMagazine = null;
            List<int> validIndexes = [];
            Dictionary<GameObject, SavedGunComponent> dicGO = [];
            Dictionary<int, GameObject> dicByIndex = [];
            List<AnvilCallback<GameObject>> callbackList = [];
            SavedGun gun = savedGun.GetSavedGun();

            for (int i = 0; i < gun.Components.Count; i++)
            {
                callbackList.Add(IM.OD[gun.Components[i].ObjectID].GetGameObjectAsync());
                TNHFrameworkLogger.Log($"Loading vault component: {gun.Components[i].ObjectID}", TNHFrameworkLogger.LogType.General);
            }
            yield return callbackList;

            for (int j = 0; j < gun.Components.Count; j++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(IM.OD[gun.Components[j].ObjectID].GetGameObject());
                M.AddObjectToTrackedList(gameObject);

                dicGO.Add(gameObject, gun.Components[j]);
                dicByIndex.Add(gun.Components[j].Index, gameObject);

                if (gun.Components[j].isFirearm)
                {
                    myGun = gameObject.GetComponent<FVRFireArm>();
                    savedGun.ApplyFirearmProperties(myGun);

                    LastSpawnedGun = gameObject;

                    validIndexes.Add(j);
                    gameObject.transform.position = position;
                    gameObject.transform.rotation = Quaternion.identity;
                }
                else if (gun.Components[j].isMagazine)
                {
                    myMagazine = gameObject.GetComponent<FVRFireArmMagazine>();
                    validIndexes.Add(j);
                    
                    if (myMagazine != null)
                    {
                        gameObject.transform.position = myGun.GetMagMountPos(myMagazine.IsBeltBox).position;
                        gameObject.transform.rotation = myGun.GetMagMountPos(myMagazine.IsBeltBox).rotation;
                        myMagazine.Load(myGun);
                        myMagazine.IsInfinite = false;
                    }
                }
                else if (gun.Components[j].isAttachment)
                {
                    toDealWith.Add(gameObject);
                }
                else
                {
                    toMoveToTrays.Add(gameObject);
                    
                    if (gameObject.GetComponent<Speedloader>() != null && gun.LoadedRoundsInMag.Count > 0)
                    {
                        Speedloader component = gameObject.GetComponent<Speedloader>();
                        component.ReloadSpeedLoaderWithList(gun.LoadedRoundsInMag);
                    }
                    else if (gameObject.GetComponent<FVRFireArmClip>() != null && gun.LoadedRoundsInMag.Count > 0)
                    {
                        FVRFireArmClip component2 = gameObject.GetComponent<FVRFireArmClip>();
                        component2.ReloadClipWithList(gun.LoadedRoundsInMag);
                    }
                }
               
                gameObject.GetComponent<FVRPhysicalObject>().ConfigureFromFlagDic(gun.Components[j].Flags);
            }
            
            if (myGun.Magazine != null && gun.LoadedRoundsInMag.Count > 0)
            {
                myGun.Magazine.ReloadMagWithList(gun.LoadedRoundsInMag);
                myGun.Magazine.IsInfinite = false;
            }
            
            int BreakIterator = 200;
            
            while (toDealWith.Count > 0 && BreakIterator > 0)
            {
                BreakIterator--;
                
                for (int k = toDealWith.Count - 1; k >= 0; k--)
                {
                    SavedGunComponent savedGunComponent = dicGO[toDealWith[k]];
                    
                    if (validIndexes.Contains(savedGunComponent.ObjectAttachedTo))
                    {
                        GameObject gameObject2 = toDealWith[k];
                        FVRFireArmAttachment component3 = gameObject2.GetComponent<FVRFireArmAttachment>();
                        FVRFireArmAttachmentMount mount = GetMount(dicByIndex[savedGunComponent.ObjectAttachedTo], savedGunComponent.MountAttachedTo);
                        gameObject2.transform.rotation = Quaternion.LookRotation(savedGunComponent.OrientationForward, savedGunComponent.OrientationUp);
                        gameObject2.transform.position = GetPositionRelativeToGun(savedGunComponent, myGun.transform);
                        
                        if (component3.CanScaleToMount && mount.CanThisRescale())
                            component3.ScaleToMount(mount);
                        
                        component3.AttachToMount(mount, false);
                        
                        if (component3 is Suppressor)
                            (component3 as Suppressor).AutoMountWell();
                        
                        validIndexes.Add(savedGunComponent.Index);
                        toDealWith.RemoveAt(k);
                    }
                }
            }
            
            int trayIndex = 0;
            int itemIndex = 0;
           
            for (int l = 0; l < toMoveToTrays.Count; l++)
            {
                toMoveToTrays[l].transform.position = position + (float)itemIndex * 0.1f * Vector3.up;
                toMoveToTrays[l].transform.rotation = rotation;
                itemIndex++;
                trayIndex++;
                
                if (trayIndex > 2)
                    trayIndex = 0;
            }
            
            myGun.SetLoadedChambers(gun.LoadedRoundsInChambers);
            myGun.transform.rotation = rotation;

            IsSpawning = false;
            yield break;
        }

        public static FVRFireArmAttachmentMount GetMount(GameObject obj, int index)
        {
            return obj.GetComponent<FVRPhysicalObject>().AttachmentMounts[index];
        }

        public static Vector3 GetPositionRelativeToGun(SavedGunComponent data, Transform gun)
        {
            Vector3 a = gun.position;
            a += gun.up * data.PosOffset.y;
            a += gun.right * data.PosOffset.x;
            return a + gun.forward * data.PosOffset.z;
        }

        public static IEnumerator SpawnItemRoutine(TNH_Manager M, Vector3 position, Quaternion rotation, FVRObject o)
        {
            TNHFrameworkLogger.Log($"SpawnItemRoutine: START [{o.ItemID}]", TNHFrameworkLogger.LogType.TNH);

            if (o == null)
                yield break;

            IsSpawning = true;
            yield return o.GetGameObjectAsync();

            TNHFrameworkLogger.Log($"SpawnItemRoutine: Spawning item [{o.ItemID}]", TNHFrameworkLogger.LogType.TNH);
            LastSpawnedGun = UnityEngine.Object.Instantiate(o.GetGameObject(), position, rotation);
            M.AddObjectToTrackedList(LastSpawnedGun);
            LastSpawnedGun.SetActive(true);

            IsSpawning = false;
            yield break;
        }

        /// <summary>
        /// Used to spawn more than one, same objects at a position
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <param name="tolerance"></param>
        // Used to spawn more than one, same objects at a position
        public static IEnumerator InstantiateMultiple(TNH_Manager M, GameObject gameObject, Vector3 position, int count, float tolerance = 1.3f)
        {
            float heightNeeded = (gameObject.GetMaxBounds().size.y / 2) * tolerance;

            for (var index = 0; index < count; index++)
            {
                float current = index * heightNeeded;
                UnityEngine.Object.Instantiate(gameObject, position + (Vector3.up * current), new Quaternion());
                M.AddObjectToTrackedList(gameObject);
                yield return null;
            }
        }

        /// <summary>
        /// Spawns items from the equipment group
        /// </summary>
        /// <param name="selectedGroup"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="callback"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static IEnumerator InstantiateFromEquipmentGroup(EquipmentGroup selectedGroup, Vector3 position, Quaternion rotation, Action<GameObject> callback = null, float tolerance = 1.3f)
        {
            float currentHeight = 0;

            foreach (EquipmentGroup group in selectedGroup.GetSpawnedEquipmentGroups())
            {
                for (int i = 0; i < group.ItemsToSpawn; i++)
                {
                    if (IM.OD.TryGetValue(group.GetObjects().GetRandom(), out FVRObject selectedFVR))
                    {
                        // First, async get the game object to spawn
                        AnvilCallback<GameObject> objectCallback = selectedFVR.GetGameObjectAsync();
                        yield return objectCallback;

                        // Next calculate the height needed for this item
                        GameObject gameObject = selectedFVR.GetGameObject();
                        float heightNeeded = gameObject.GetMaxBounds().size.y / 2 * tolerance;
                        currentHeight += heightNeeded;

                        // Finally spawn the item and call the callback if it's not null
                        GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, position + (Vector3.up * currentHeight), rotation);

                        // This is added to the tracked object list after we return
                        callback?.Invoke(spawnedObject);
                        yield return null;
                    }
                }
            }
        }
    }
}
