using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

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

        public class PlayerStorage
        {
            public CharacterMaster playerInstance = null;
            public GameObject savedCharacterBody = null;
            public List<ItemIndex> savedInventory = new List<ItemIndex>();
            public List<int> savedInventoryStacks = new List<int>();
        }

        public List<PlayerStorage> playerStorage = new List<PlayerStorage>();
        private static InventoryManager inventoryManager;

        private static ConfigFile configFile;
        private static ConfigEntry<bool> disableInventory;

        public void Awake()
        {
            Log.Init(Logger);

            SetFromConfig();
            if(disableInventory.Value) {
                inventoryManager = new InventoryManager(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "dronereviveassets"));
            }
            Run.onRunStartGlobal += Run_onRunStartGlobal;

            Log.Info(nameof(Awake) + " done.");
        }

        private void Run_onRunStartGlobal(Run obj) {
            if(NetworkServer.active) {
                Stage.onServerStageBegin += Stage_onServerStageBegin;
                On.RoR2.GlobalEventManager.OnPlayerCharacterDeath += new On.RoR2.GlobalEventManager.hook_OnPlayerCharacterDeath(OnPlayerDeath);
            }
        }

        private void SetFromConfig() {
            configFile = new ConfigFile(Paths.ConfigPath + "//ReturnsDroneRevive.cfg", true);
            disableInventory = configFile.Bind("DroneRevive", "Disable Inventory as Drone", true, "All collected items will be disabled as a drone. Items are returned next stage.");
        }

        private void Stage_onServerStageBegin(Stage obj) {
            ResetToCharacters();
        }
        
        private void OnPlayerDeath(On.RoR2.GlobalEventManager.orig_OnPlayerCharacterDeath orig, GlobalEventManager self, DamageReport damagereport, NetworkUser networkuser)
        {
            orig.Invoke(self, damagereport, networkuser);
            Log.Info($"Player has died: {networkuser.master.bodyPrefab.name}");

            if(!networkuser.master.bodyPrefab.name.Equals("Drone1Body")) {
                PlayerStorage deadPlayer = new PlayerStorage();
                deadPlayer.playerInstance = networkuser.master;
                deadPlayer.savedCharacterBody = networkuser.master.bodyPrefab;
                playerStorage.Add(deadPlayer);
                StartCoroutine(TrySpawnAsDrone(deadPlayer));
            }
        }

        private void DroneInventoryChanged() {
            if(!disableInventory.Value) {return;}

            for(int i = 0; i < playerStorage.Count; i++) {
                if(playerStorage[i].playerInstance.inventory.itemAcquisitionOrder.Count > 1) {
                    inventoryManager.StoreDroneCollectedItem(playerStorage[i]);
                }
            }
        }

        private IEnumerator TrySpawnAsDrone(PlayerStorage deadPlayer) {
            Log.Info("Attempting to Spawn as Drone");
            yield return new WaitForSeconds(1.5f);
            int num;
            if (deadPlayer.playerInstance == null) {
                num = 0;
            }
            else {
                num = (((deadPlayer.playerInstance != null) ? new bool?(deadPlayer.playerInstance.IsDeadAndOutOfLivesServer()) : null) == false) ? 1 : 0;
            }
            if (num != 0 || Run.instance.isGameOverServer) {
                Log.Info("Player is not out of lives");
                yield break;
            }
            SpawnAsDrone(deadPlayer.playerInstance.deathFootPosition, deadPlayer);
        }

        private void SpawnAsDrone(Vector3 spawnPosition, PlayerStorage deadPlayer) {
            Log.Info("Spawning as Drone");

            if(disableInventory.Value) {
                inventoryManager.SaveAndRemoveInventory(deadPlayer);
                deadPlayer.playerInstance.inventory.onInventoryChanged += DroneInventoryChanged;
            }
            deadPlayer.playerInstance.bodyPrefab = BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex("Drone1Body"));
            ChangePlayerPrefab(deadPlayer.playerInstance, spawnPosition);
        }

        private void ResetToCharacters() {
            Log.Info("Resetting character bodies");
            for(int i = 0; i < playerStorage.Count; i++) {
                if(playerStorage[i].playerInstance != null && playerStorage[i].playerInstance.bodyPrefab.name.Equals("Drone1Body")) {
                    playerStorage[i].playerInstance.bodyPrefab = playerStorage[i].savedCharacterBody;
                    if(disableInventory.Value) {
                        playerStorage[i].playerInstance.inventory.onInventoryChanged -= DroneInventoryChanged;
                        inventoryManager.AddBackInventory(playerStorage[i]);
                        inventoryManager.ResetRegenScrap(playerStorage[i]);
                    }
                }
            }
            playerStorage.Clear();
        }

        private void ChangePlayerPrefab(CharacterMaster master, Vector3 spawnPosition, Quaternion spawnRotation = default) {
            #pragma warning disable Publicizer001
                var pcmc = master.playerCharacterMasterController;
                master.playerCharacterMasterController = null; // prevent metamorphosis rerolling the body for players
                master.Respawn(spawnPosition, spawnRotation);
                master.playerCharacterMasterController = pcmc;
            #pragma warning restore Publicizer001
        }
    }
}
