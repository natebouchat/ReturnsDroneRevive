using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using Mono.Security.Authenticode;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace ReturnsDroneRevive
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class ReturnsDroneRevive : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Burnt";
        public const string PluginName = "ReturnsDroneRevive";
        public const string PluginVersion = "1.0.0";

        private static bool isTransformed = false;
        private static GameObject playerSelectedCharacterBody;
        private static CharacterMaster playerInstance;
        private List<ItemIndex> savedInventory = new List<ItemIndex>();
        private List<int> savedInventoryStacks = new List<int>();
        private static ItemDef droneCompartmentItem;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);

            droneCompartmentItem = ScriptableObject.CreateInstance<ItemDef>();

            droneCompartmentItem.name = "DRONECOMPARTMENT_NAME";
            droneCompartmentItem.nameToken = "DRONECOMPARTMENT_NAME";
            droneCompartmentItem.pickupToken = "DRONECOMPARTMENT_PICKUP";
            droneCompartmentItem.descriptionToken = "DRONECOMPARTMENT_DESC";
            droneCompartmentItem.loreToken = "DRONECOMPARTMENT_LORE";

#pragma warning disable Publicizer001
            droneCompartmentItem._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/NoTierDef.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            string bundleName = "dronereviveassets";
            var assets = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), bundleName));
            var chestIcon = assets.LoadAsset<Sprite>("Assets/chest.png");

            droneCompartmentItem.pickupIconSprite = chestIcon;
            droneCompartmentItem.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");

            droneCompartmentItem.canRemove = false;
            droneCompartmentItem.hidden = false;
            var displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(droneCompartmentItem, displayRules));

            Log.Info(nameof(Awake) + " done.");
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            //If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }
            var attackerCharacterBody = report.attackerBody;

            //We need an inventory to do check for our item
            if (attackerCharacterBody.inventory)
            {
                
            }
        }

        private void SpawnAsDrone() {
            Log.Info("Player pressed F2. Spawning as Drone");
            CharacterMaster master = playerInstance;
            master.bodyPrefab = BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex("Drone1Body"));
            ChangePlayerPrefab(master);
        }

        private void SpawnAsCharacter() {
            Log.Info("Player pressed F2. Spawning as Charracter");
            CharacterMaster master = playerInstance;
            master.bodyPrefab = playerSelectedCharacterBody;
            ChangePlayerPrefab(master);
        }

        private void ChangePlayerPrefab(CharacterMaster master) {
            #pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
                RoR2.ConVar.BoolConVar stage1pod = Stage.stage1PodConVar;
                bool oldVal = stage1pod.value;
                stage1pod.SetBool(false);
                var pcmc = master.playerCharacterMasterController;
                master.playerCharacterMasterController = null; // prevent metamorphosis rerolling the body for players
                master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
                master.playerCharacterMasterController = pcmc;
                stage1pod.SetBool(oldVal); 
            #pragma warning restore Publicizer001
        }

        private void SaveAndRemoveInventory() {
           for(int i = 0; i < playerInstance.inventory.itemAcquisitionOrder.Count; i++) {
                savedInventory.Add(playerInstance.inventory.itemAcquisitionOrder[i]);
                savedInventoryStacks.Add(playerInstance.inventory.GetItemCount(savedInventory[i]));
                for(int j = 0; j < savedInventoryStacks[i]; j++) {
                    playerInstance.inventory.RemoveItem(playerInstance.inventory.itemAcquisitionOrder[i]);
                }
            }   
            playerInstance.inventory.GiveItem(droneCompartmentItem.itemIndex);           
        }

        private void AddBackInventory() {
            playerInstance.inventory.RemoveItem(droneCompartmentItem.itemIndex);           
            for(int i = 0; i < savedInventory.Count; i++) {
                for(int j = 0; j < savedInventoryStacks[i]; j++) {
                    playerInstance.inventory.GiveItem(savedInventory[i]);
                }
                savedInventory.Clear();
                savedInventoryStacks.Clear();
            } 
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                playerInstance = PlayerCharacterMasterController.instances[0].master;
                if(!isTransformed) {
                    playerSelectedCharacterBody = playerInstance.bodyPrefab;
                    SaveAndRemoveInventory();
                    SpawnAsDrone();
                    isTransformed = true;
                }
                else {
                    SpawnAsCharacter();
                    AddBackInventory();
                    isTransformed = false;
                }
            }
            if (Input.GetKeyDown(KeyCode.F3)) {
                playerInstance = PlayerCharacterMasterController.instances[0].master;
                Log.Info("Giving Player Goathoof");
                playerInstance.inventory.GiveItem(ItemCatalog.FindItemIndex("Hoof"));
            }
            if(Input.GetKeyDown(KeyCode.F4)) {
                playerInstance = PlayerCharacterMasterController.instances[0].master;
                Log.Info("Removing Goathoof From Player");
                playerInstance.inventory.RemoveItem(ItemCatalog.FindItemIndex("Hoof"));
            }
        }
    }

}
