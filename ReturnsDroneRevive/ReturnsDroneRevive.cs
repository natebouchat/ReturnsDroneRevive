using System.Collections;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;

namespace ReturnsDroneRevive
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class ReturnsDroneRevive : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Hyenate";
        public const string PluginName = "ReturnsDroneRevive";
        public const string PluginVersion = "1.0.0";

        private static bool isTransformed = false;
        private static GameObject playerSelectedCharacterBody;
        private static CharacterMaster playerInstance;

        public void Awake()
        {
            Log.Init(Logger);
            InventoryManager.Init(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "dronereviveassets"));

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Stage.onServerStageBegin += Stage_onServerStageBegin;
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            Log.Info(nameof(Awake) + " done.");
        }

        private void Run_onRunStartGlobal(Run obj) {
            playerInstance = PlayerCharacterMasterController.instances[0].master;
            InventoryManager.ClearSavedInventory();
            playerInstance.inventory.onInventoryChanged += DroneInventoryChanged;
        }

        private void Stage_onServerStageBegin(Stage obj) {
            if(isTransformed) {
                SpawnAsCharacter(false);
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            if(!isTransformed) {
                TrySpawnAsDrone();
            }
        }

        private void DroneInventoryChanged() {
            if(isTransformed && playerInstance.inventory.itemAcquisitionOrder.Count > 1) {
                InventoryManager.StoreDroneCollectedItem(playerInstance);
            }
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

        private void SpawnAsDrone() {
            Log.Info("Spawning as Drone");
            playerSelectedCharacterBody = playerInstance.bodyPrefab;
            InventoryManager.SaveAndRemoveInventory(playerInstance);

            CharacterMaster master = playerInstance;
            master.bodyPrefab = BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex("Drone1Body"));
            ChangePlayerPrefab(master);
            isTransformed = true;
        }

        private void SpawnAsCharacter(bool forceRespawn) {
            Log.Info("Spawning as Character");
            if(forceRespawn) {
                CharacterMaster master = playerInstance;
                master.bodyPrefab = playerSelectedCharacterBody;
                ChangePlayerPrefab(master);
            }
            else {
                playerInstance.bodyPrefab = playerSelectedCharacterBody;
            }
            isTransformed = false;
            InventoryManager.AddBackInventory(playerInstance);
        }

        private void ChangePlayerPrefab(CharacterMaster master) {
            #pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
                var pcmc = master.playerCharacterMasterController;
                master.playerCharacterMasterController = null; // prevent metamorphosis rerolling the body for players
                master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
                master.playerCharacterMasterController = pcmc;
            #pragma warning restore Publicizer001
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
                    SpawnAsCharacter(true);
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
