using System.Collections;
using BepInEx;
using BepInEx.Configuration;
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

        private static ConfigFile configFile;
        private static ConfigEntry<bool> disableInventory;
        private static KeyCode keybindForceSwap;
        private static KeyCode keybindAscend;
        private static KeyCode keybindDescend;


        public void Awake()
        {
            Log.Init(Logger);

            SetFromConfig();
            if(disableInventory.Value) {
                InventoryManager.Init(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "dronereviveassets"));
            }

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Stage.onServerStageBegin += Stage_onServerStageBegin;
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            Log.Info(nameof(Awake) + " done.");
        }

        private void SetFromConfig() {
            configFile = new ConfigFile(Paths.ConfigPath + "//ReturnsDroneRevive.cfg", true);
            
            disableInventory = configFile.Bind("DroneRevive", "Disable Inventory as Drone", true, "All collected items will be disabled as a drone. Items are returned next stage.");
            ConfigEntry<string> keybindForceSwapString = configFile.Bind("DroneRevive", "Force Swap Keybind", "None", "Keybind for force swap transform to Drone/Character. Will also respawn if player is dead.");
            ConfigEntry<string> keybindAscendString = configFile.Bind("DroneRevive", "Ascend as Drone Keybind", "Space", "Keybind to ascend as a drone.");
            ConfigEntry<string> keybindDescendString = configFile.Bind("DroneRevive", "Descend as Drone Keybind", "LeftShift", "Keybind to descend as a drone");
       
            if(!System.Enum.TryParse(keybindForceSwapString.Value, out keybindForceSwap)) {
                Log.Error("Failed Parse on \"Force Swap Keybind\". Defaulting to \"None\".");
                keybindForceSwap = KeyCode.None;
            }
            if(!System.Enum.TryParse(keybindAscendString.Value, out keybindAscend)) {
                Log.Error("Failed Parse on \"Ascend as Drone Keybind\". Defaulting to \"Space\".");
                keybindAscend = KeyCode.Space;
            }
            if(!System.Enum.TryParse(keybindDescendString.Value, out keybindDescend)) {
                Log.Error("Failed Parse on \"Descend as Drone Keybind\". Defaulting to \"LeftShift\".");
                keybindDescend = KeyCode.LeftShift;
            }
        }

        private void Run_onRunStartGlobal(Run obj) {
            playerInstance = PlayerCharacterMasterController.instances[0].master;
            isTransformed = false;
            if(disableInventory.Value) {
                InventoryManager.ClearSavedInventory();
                playerInstance.inventory.onInventoryChanged += DroneInventoryChanged;
            }
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
            if(isTransformed && playerInstance.inventory.itemAcquisitionOrder.Count > 1 && disableInventory.Value) {
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
            if(disableInventory.Value) {
                InventoryManager.SaveAndRemoveInventory(playerInstance);
            }

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
            if(disableInventory.Value) {
                InventoryManager.AddBackInventory(playerInstance);
            }
        }

        private void ChangePlayerPrefab(CharacterMaster master) {
            #pragma warning disable Publicizer001
                var pcmc = master.playerCharacterMasterController;
                master.playerCharacterMasterController = null; // prevent metamorphosis rerolling the body for players
                master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
                master.playerCharacterMasterController = pcmc;
            #pragma warning restore Publicizer001
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            if (Input.GetKeyDown(keybindForceSwap))
            {
                if(!isTransformed) {
                    SpawnAsDrone();
                }
                else {
                    SpawnAsCharacter(true);
                }
            }
            if(isTransformed) {
                // Using Time.deltaTime causes jittery acsension
                if (Input.GetKey(keybindAscend)) {
                    playerInstance.GetBodyObject().transform.Translate(Vector3.up * 0.2f);
                }
                else if (Input.GetKey(keybindDescend)){
                    playerInstance.GetBodyObject().transform.Translate(Vector3.down * 0.2f);
                }
            }
        }
    }
}
