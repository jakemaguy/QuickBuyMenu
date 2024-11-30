using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace QuickBuyMenu.NetworkHandler
{
    public class QuickBuyNetworkHandler : NetworkBehaviour
    {
        public static event Action<string> LevelEvent;
        public static QuickBuyNetworkHandler Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            LevelEvent = null;
            bool flag = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
            if (flag)
            {
                QuickBuyNetworkHandler instance = Instance;
                if (instance != null)
                {
                    instance.gameObject.GetComponent<NetworkObject>().Despawn(true);
                }
            }
            Instance = this;
            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void EventClientRpc(ulong clientId, NetworkObjectReference obj = default(NetworkObjectReference), ClientRpcParams clientRpcParams = default(ClientRpcParams))
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                PlayerControllerB playerController = 
                    GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts[clientId];
                
                Plugin.Log.LogDebug(string.Format("Running EventClientRPC method\n${0}, client ID: {1}\n", playerController.playerUsername, playerController.actualClientId));
                NetworkObject targetObject;

                if (obj.TryGet(out targetObject))
                {
                    GrabbableObject clientItem = targetObject.GetComponent<GrabbableObject>();

                    // This prevents overlapping items when multiple items are purhchased in terminal session without exiting.
                    // editing currentlyHeldObjectServer prevents other spawned item meshes to dissapear when its set to render false.
                    playerController.currentlyHeldObjectServer?.EnableItemMeshes(false);

                    clientItem.EnableItemMeshes(true);
                    clientItem.playerHeldBy = playerController;
                    clientItem.playerHeldBy.currentlyGrabbingObject = clientItem;
                    clientItem.playerHeldBy.currentlyHeldObjectServer = clientItem;
                    clientItem.parentObject = playerController.localItemHolder;


                    switch (clientItem.itemProperties.itemName)
                    {
                        case "Lockpicker":
                            clientItem.GetComponent<LockPicker>().lockPickerAudio = clientItem.gameObject.GetComponent<AudioSource>();
                            break;
                        case "Shotgun":
                            clientItem.GetComponent<ShotgunItem>().previousPlayerHeldBy = playerController;
                            clientItem.GetComponent<ShotgunItem>().hasBeenHeld = true;
                            clientItem.GetComponent<ShotgunItem>().previousPlayerHeldBy.equippedUsableItemQE = true;
                            clientItem.GetComponentInParent<Component>().gameObject.GetComponentInChildren<ScanNodeProperties>().nodeType = 2;
                            break;
                        default:
                            break;
                    }

                    if (!(NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer))
                    {
                        GrabObjectSyncServerRpc(clientId, clientItem.GetComponent<NetworkObject>());
                    }
                    else
                    {
                        clientItem.playerHeldBy.GrabObjectServerRpc(clientItem.GetComponent<NetworkObject>());
                    }

                    Coroutine coroutine = clientItem.playerHeldBy.grabObjectCoroutine;
                    if (coroutine != null)
                    {
                        GameNetworkManager.Instance.StopCoroutine(coroutine);
                    }
                    clientItem.playerHeldBy.grabObjectCoroutine = GameNetworkManager.Instance.StartCoroutine(clientItem.playerHeldBy.GrabObject());
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EventServerRpc(int itemID, ulong clientId, int itemIndex,ServerRpcParams serverRpcParams = default)
        {
            var clientIdTest = serverRpcParams.Receive.SenderClientId;
            if (NetworkManager.ConnectedClients.ContainsKey(clientIdTest))
            {
                var client = NetworkManager.ConnectedClients[clientIdTest];
                PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts[clientIdTest];
                Plugin.Log.LogDebug(string.Format("Running EventServerRPC method\n${0}, client ID: {1}\n", playerController.playerUsername, playerController.actualClientId));

                Terminal __terminal = FindObjectOfType<Terminal>();

                bool inventoryFull = checkInventoryFull(playerController) == -1;
                Vector3 itemSpawn = playerController.transform.position;

                if (inventoryFull)
                {
                    itemSpawn.x = (float)(playerController.transform.position.x + (itemIndex * -0.5));
                }

                GameObject gameObject = Instantiate(
                    __terminal.buyableItemsList[itemID].spawnPrefab,
                    itemSpawn,
                    Quaternion.identity);

                NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
                netObj.Spawn(false);
                netObj.ChangeOwnership(clientIdTest);

                Plugin.Log.LogDebug($"Inventory Full Boolean: {inventoryFull}");
                if (!inventoryFull)
                {
                    EventClientRpc(clientIdTest, netObj);
                }
            }
            
        }

        private int checkInventoryFull(PlayerControllerB player)
        {
            int result = -1;
            //int length = player.ItemSlots.Length > 4 ? 5 : 4;
            for (int i = 0; i < 4; i++)
            {
                if (player.ItemSlots[i] == null)
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        [ServerRpc(RequireOwnership = false)]
        public void GrabObjectSyncServerRpc(ulong clientId, NetworkObjectReference obj)
        {
            PlayerControllerB playerController = 
                GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts[clientId];
            Plugin.Log.LogDebug(string.Format("Running GrabObjectSyncServerRpc method\n${0}, client ID: {1}\n", playerController.playerUsername, playerController.actualClientId));
            
            NetworkObject clientItem = obj;
            playerController.GrabObjectClientRpc(true, clientItem);
        }


        [ServerRpc(RequireOwnership = false)]
        public void SyncGroupCreditsServerRpc(int groupCredits, int numItemsInDropShip)
        {
            Terminal __terminal = FindObjectOfType<Terminal>();

            __terminal.SyncGroupCreditsServerRpc(groupCredits, numItemsInDropShip);
        }
    }
}
