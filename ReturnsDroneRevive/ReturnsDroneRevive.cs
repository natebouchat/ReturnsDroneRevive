using System.Collections.Generic;
using BepInEx;
using IL.RoR2;
using IL.RoR2.UI;
using R2API;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        private static RoR2.CharacterMaster playerInstance;
        private List<ItemIndex> savedInventory = new List<ItemIndex>();
        private List<int> savedInventoryStacks = new List<int>();

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);

            Log.Info(nameof(Awake) + " done.");
        }

        private void GlobalEventManager_onCharacterDeathGlobal(RoR2.DamageReport report)
        {
            //If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }
            var attackerCharacterBody = report.attackerBody;

            //attackerCharacterBody.inventory.RemoveItem();
            //attackerCharacterBody.inventory.ResetItem();

            //We need an inventory to do check for our item
            if (attackerCharacterBody.inventory)
            {
                
            }
        }

        private void SpawnAsDrone() {
            Log.Info("Player pressed F2. Spawning as Drone");
            RoR2.CharacterMaster master = playerInstance;
            master.bodyPrefab = RoR2.BodyCatalog.GetBodyPrefab(RoR2.BodyCatalog.FindBodyIndex("Drone1Body"));
            ChangePlayerPrefab(master);
        }

        private void SpawnAsCharacter() {
            Log.Info("Player pressed F2. Spawning as Charracter");
            RoR2.CharacterMaster master = playerInstance;
            master.bodyPrefab = playerSelectedCharacterBody;
            ChangePlayerPrefab(master);
        }

        private void ChangePlayerPrefab(RoR2.CharacterMaster master) {
            #pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
                RoR2.ConVar.BoolConVar stage1pod = RoR2.Stage.stage1PodConVar;
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
        }

        private void AddBackInventory() {
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
                playerInstance = RoR2.PlayerCharacterMasterController.instances[0].master;
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
                playerInstance = RoR2.PlayerCharacterMasterController.instances[0].master;
                Log.Info("Giving Player Goathoof");
                playerInstance.inventory.GiveItem(RoR2.ItemCatalog.FindItemIndex("Hoof"));
            }
            if(Input.GetKeyDown(KeyCode.F4)) {
                playerInstance = RoR2.PlayerCharacterMasterController.instances[0].master;
                Log.Info("Removing Goathoof From Player");
                playerInstance.inventory.RemoveItem(RoR2.ItemCatalog.FindItemIndex("Hoof"));
            }
            if(Input.GetKeyDown(KeyCode.F5)) {
                playerInstance = RoR2.PlayerCharacterMasterController.instances[0].master;
                Log.Info("Changing inventory Color");
                for(int i = 0; i < playerInstance.inventory.itemAcquisitionOrder.Count; i++) {
                    SpriteRenderer changeColor = new SpriteRenderer();
                    changeColor.sprite = RoR2.ItemCatalog.GetItemDef(playerInstance.inventory.itemAcquisitionOrder[i]).pickupIconSprite;
                    changeColor.color = new Color(1,1,1,0.5f);
                }
            }
        }
    }
}
