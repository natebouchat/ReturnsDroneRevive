using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;

namespace ReturnsDroneRevive
{
    internal class InventoryManager
    {
        private static ItemDef droneCompartmentItem;
        private static List<ItemIndex> savedInventory = new List<ItemIndex>();
        private static List<int> savedInventoryStacks = new List<int>();

        internal static void Init(string filePath) {
            CreateItem(filePath);
        }

        private static void CreateItem(string filePath) {
            droneCompartmentItem = ScriptableObject.CreateInstance<ItemDef>();

            droneCompartmentItem.name = "DRONECOMPARTMENT_NAME";
            droneCompartmentItem.nameToken = "DRONECOMPARTMENT_NAME";
            droneCompartmentItem.pickupToken = "DRONECOMPARTMENT_PICKUP";
            droneCompartmentItem.descriptionToken = "DRONECOMPARTMENT_DESC";
            droneCompartmentItem.loreToken = "DRONECOMPARTMENT_LORE";
            
            // Force NoTier so it doesn't end up in item pool
            #pragma warning disable Publicizer001
            #pragma warning disable CS0618
                droneCompartmentItem.deprecatedTier = ItemTier.NoTier;
            #pragma warning restore Publicizer001
            #pragma warning restore CS0618

            var assets = AssetBundle.LoadFromFile(filePath);
            var chestIcon = assets.LoadAsset<Sprite>("Assets/chest.png");
            droneCompartmentItem.pickupIconSprite = chestIcon;

            droneCompartmentItem.canRemove = false;
            droneCompartmentItem.hidden = false;
            var displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(droneCompartmentItem, displayRules));
        }

        internal static void StoreDroneCollectedItem(CharacterMaster playerInstance) {
            ItemIndex addedItem = playerInstance.inventory.itemAcquisitionOrder[1];
            for(int i = 0; i < savedInventory.Count; i++) {
                if(savedInventory[i] == addedItem) {
                    savedInventoryStacks[i]++;
                    playerInstance.inventory.RemoveItem(addedItem);
                    return;
                }
            }
            savedInventory.Add(addedItem);
            savedInventoryStacks.Add(1);
            playerInstance.inventory.RemoveItem(addedItem);
        }

        internal static void SaveAndRemoveInventory(CharacterMaster playerInstance) {
            int inventoryItemCount = playerInstance.inventory.itemAcquisitionOrder.Count;
            for(int i = 0; i < inventoryItemCount; i++) {
                savedInventory.Add(playerInstance.inventory.itemAcquisitionOrder[0]);
                savedInventoryStacks.Add(playerInstance.inventory.GetItemCount(savedInventory[i]));
                for(int j = 0; j < savedInventoryStacks[i]; j++) {
                    playerInstance.inventory.RemoveItem(savedInventory[i]);
                }
            }
               
            playerInstance.inventory.GiveItem(droneCompartmentItem.itemIndex);           
        }

        internal static void AddBackInventory(CharacterMaster playerInstance) {
            playerInstance.inventory.RemoveItem(droneCompartmentItem.itemIndex);

            for(int i = 0; i < savedInventory.Count; i++) {
                for(int j = 0; j < savedInventoryStacks[i]; j++) {
                    playerInstance.inventory.GiveItem(savedInventory[i]);
                }
            }
            ClearSavedInventory();
        }

        internal static void ClearSavedInventory() {
            savedInventory.Clear();
            savedInventoryStacks.Clear();
        }
    }
}