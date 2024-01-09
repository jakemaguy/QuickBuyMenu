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
        public void EventClientRpc(int eventName, ulong clientId, NetworkObjectReference obj = default(NetworkObjectReference), ClientRpcParams clientRpcParams = default(ClientRpcParams))
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
                    clientItem.EnableItemMeshes(true);
                    clientItem.playerHeldBy = playerController;
                    clientItem.playerHeldBy.currentlyGrabbingObject = clientItem;
                    playerController.currentlyHeldObjectServer = clientItem;

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
                    clientItem.playerHeldBy.grabObjectCoroutine = GameNetworkManager.Instance.StartCoroutineManaged2(clientItem.playerHeldBy.GrabObject());
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EventServerRpc(int itemID, ulong clientId, ServerRpcParams serverRpcParams = default(ServerRpcParams))
        {
            PlayerControllerB playerController = GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts[clientId];
            Plugin.Log.LogDebug(string.Format("Running EventServerRPC method\n${0}, client ID: {1}\n", playerController.playerUsername, playerController.actualClientId));
            
            Terminal __terminal = FindObjectOfType<Terminal>();
            
            GameObject gameObject = Instantiate(
                __terminal.buyableItemsList[itemID].spawnPrefab, 
                playerController.transform.position, 
                Quaternion.identity);

            NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
            netObj.Spawn(false);
            netObj.ChangeOwnership(clientId);
            
            EventClientRpc(itemID, clientId, netObj);
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
