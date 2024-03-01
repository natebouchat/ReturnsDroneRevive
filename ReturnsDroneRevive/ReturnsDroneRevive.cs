using System.Collections.Generic;
using System.Collections;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using IL.RoR2.ContentManagement;

namespace ReturnsDroneRevive
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
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

        public void Awake()
        {
            Log.Init(Logger);
            CreateItem();

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Stage.onServerStageBegin += Stage_onServerStageBegin;
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            Log.Info(nameof(Awake) + " done.");
        }

        private void Run_onRunStartGlobal(Run obj) {
            playerInstance = PlayerCharacterMasterController.instances[0].master;
        }

        private void Stage_onServerStageBegin(Stage obj) {
            Log.Info($"New Stage: {nameof(Stage_onServerStageBegin)}");
            Log.Info($"Prefab Body: {playerInstance.bodyPrefab.name}");
            if(playerInstance.bodyPrefab.name.Equals("Drone1Body")) {
                //pawnAsCharacter();
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            TrySpawnAsDrone();
        }

        private IEnumerator TrySpawnAsDrone() {
            yield return new WaitForSeconds(1.5f);
            int num;
            if (playerInstance == null) {
                num = 0;
            }
            else {
                num = (((playerInstance != null) ? new bool?(playerInstance.IsDeadAndOutOfLivesServer()) : null) == false) ? 1 : 0;
            }
            if (num != 0 || Run.instance.isGameOverServer) {
                yield break;
            }
            SpawnAsDrone();
        }

        private void CreateItem() {
            droneCompartmentItem = ScriptableObject.CreateInstance<ItemDef>();

            droneCompartmentItem.name = "DRONECOMPARTMENT_NAME";
            droneCompartmentItem.nameToken = "DRONECOMPARTMENT_NAME";
            droneCompartmentItem.pickupToken = "DRONECOMPARTMENT_PICKUP";
            droneCompartmentItem.descriptionToken = "DRONECOMPARTMENT_DESC";
            droneCompartmentItem.loreToken = "DRONECOMPARTMENT_LORE";

            //NoTier
            droneCompartmentItem.deprecatedTier = ItemTier.NoTier;

            string bundleName = "dronereviveassets";
            var assets = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), bundleName));
            var chestIcon = assets.LoadAsset<Sprite>("Assets/chest.png");

            droneCompartmentItem.pickupIconSprite = chestIcon;
            //droneCompartmentItem.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");

            droneCompartmentItem.canRemove = false;
            droneCompartmentItem.hidden = false;
            var displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(droneCompartmentItem, displayRules));
        }

        private void SpawnAsDrone() {
            Log.Info("Player pressed F2. Spawning as Drone");
            playerSelectedCharacterBody = playerInstance.bodyPrefab;
            SaveAndRemoveInventory();

            CharacterMaster master = playerInstance;
            master.bodyPrefab = BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex("Drone1Body"));
            ChangePlayerPrefab(master);
            isTransformed = true;
        }

        private void SpawnAsCharacter() {
            Log.Info("Player pressed F2. Spawning as Character");
            CharacterMaster master = playerInstance;
            master.bodyPrefab = playerSelectedCharacterBody;
            ChangePlayerPrefab(master);

            AddBackInventory();
            isTransformed = false;
        }

        private void ChangePlayerPrefab(CharacterMaster master) {
            #pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
                var pcmc = master.playerCharacterMasterController;
                master.playerCharacterMasterController = null; // prevent metamorphosis rerolling the body for players
                master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
                master.playerCharacterMasterController = pcmc;
            #pragma warning restore Publicizer001
        }

        private void SaveAndRemoveInventory() {
            int inventoryItemCount = playerInstance.inventory.itemAcquisitionOrder.Count;
            for(int i = 0; i < inventoryItemCount; i++) {
                Log.Info($"Inventory Count: {playerInstance.inventory.itemAcquisitionOrder.Count}");
                for(int k = 0; k < playerInstance.inventory.itemAcquisitionOrder.Count; k++) {
                    Log.Info($"Item: {ItemCatalog.GetItemDef(playerInstance.inventory.itemAcquisitionOrder[k]).name}");
                }
                savedInventory.Add(playerInstance.inventory.itemAcquisitionOrder[0]);
                savedInventoryStacks.Add(playerInstance.inventory.GetItemCount(savedInventory[i]));
                for(int j = 0; j < savedInventoryStacks[i]; j++) {
                    playerInstance.inventory.RemoveItem(savedInventory[i]);
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
            }
            savedInventory.Clear();
            savedInventoryStacks.Clear();
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if(!isTransformed) {
                    SpawnAsDrone();
                }
                else {
                    SpawnAsCharacter();
                }
            }
            if (Input.GetKeyDown(KeyCode.F3)) {
                Log.Info("Giving Player Goathoof");
                playerInstance.inventory.GiveItem(ItemCatalog.FindItemIndex("Hoof"));
            }
            if(Input.GetKeyDown(KeyCode.F4)) {
                Log.Info("Removing Goathoof From Player");
                playerInstance.inventory.RemoveItem(ItemCatalog.FindItemIndex("Hoof"));
            }
        }
    }

}
