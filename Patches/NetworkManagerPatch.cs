using HarmonyLib;
using QuickBuyMenu.NetworkHandler;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace QuickBuyMenu.Patches
{
    [HarmonyPatch]
    internal class NetworkManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            bool flag = networkPrefab != null;
            if (!flag)
            {
                AssetBundle MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "QuickBuyMenuAssets"));
                networkPrefab = (GameObject)MainAssetBundle.LoadAsset("QuickBuyNetworkHandler");
                networkPrefab.AddComponent<QuickBuyNetworkHandler>();
                NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        private static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                GameObject networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost?.GetComponent<NetworkObject>().Spawn(false);
            }
        }
        private static GameObject networkPrefab;
    }
}
