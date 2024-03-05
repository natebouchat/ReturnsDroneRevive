using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;

namespace ReturnsDroneRevive
{
    internal class InventoryManager
    {
        private static ItemDef droneCompartmentItem;

        public InventoryManager(string filePath) {
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

        public void StoreDroneCollectedItem(ReturnsDroneRevive.PlayerStorage playerStorage) {
            ItemIndex addedItem = playerStorage.playerInstance.inventory.itemAcquisitionOrder[1];
            for(int i = 0; i < playerStorage.savedInventory.Count; i++) {
                if(playerStorage.savedInventory[i] == addedItem) {
                    playerStorage.savedInventoryStacks[i]++;
                    playerStorage.playerInstance.inventory.RemoveItem(addedItem);
                    return;
                }
            }
            playerStorage.savedInventory.Add(addedItem);
            playerStorage.savedInventoryStacks.Add(1);
            playerStorage.playerInstance.inventory.RemoveItem(addedItem);
        }

        public void SaveAndRemoveInventory(ReturnsDroneRevive.PlayerStorage playerStorage) {
            int inventoryItemCount = playerStorage.playerInstance.inventory.itemAcquisitionOrder.Count;
            for(int i = 0; i < inventoryItemCount; i++) {
                playerStorage.savedInventory.Add(playerStorage.playerInstance.inventory.itemAcquisitionOrder[0]);
                playerStorage.savedInventoryStacks.Add(playerStorage.playerInstance.inventory.GetItemCount(playerStorage.savedInventory[i]));
                for(int j = 0; j < playerStorage.savedInventoryStacks[i]; j++) {
                    playerStorage.playerInstance.inventory.RemoveItem(playerStorage.savedInventory[i]);
                }
            }
               
            playerStorage.playerInstance.inventory.GiveItem(droneCompartmentItem.itemIndex);           
        }

        public void AddBackInventory(ReturnsDroneRevive.PlayerStorage playerStorage) {
            playerStorage.playerInstance.inventory.RemoveItem(droneCompartmentItem.itemIndex);

            for(int i = 0; i < playerStorage.savedInventory.Count; i++) {
                for(int j = 0; j < playerStorage.savedInventoryStacks[i]; j++) {
                    playerStorage.playerInstance.inventory.GiveItem(playerStorage.savedInventory[i]);
                }
            }

            playerStorage.savedInventory.Clear();
            playerStorage.savedInventoryStacks.Clear();
        }
    }
}