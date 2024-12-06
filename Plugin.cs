using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using LethalNetworkAPI;
using System.Threading;
using System.Xml.Linq;
using LC_API;
using System.Diagnostics;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace QuickBuyMenu
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("LethalNetworkAPI")]
    public class Plugin : BaseUnityPlugin
    {
        private static Plugin Instance;
        internal static ManualLogSource Log;
        LNetworkMessage<string> clientItemSpawnRequest;
        LNetworkMessage<int> syncCredits;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Log = Logger;

            //NetcodeWeaver();

            Log.LogDebug("Plugin QuickBuyMenu is loaded!");

            LC_API.ClientAPI.CommandHandler.RegisterCommand("quickbuy", new List<string> { "qb", "buy", "qbuy", }, RunQuickBuy);

            clientItemSpawnRequest = LNetworkMessage<string>.Create(identifier: "clientItemSpawnRequest", (itemName, clientId) =>
            {

                Logger.LogDebug($"Message Handler: {itemName}");
                // get player controller reference based on request client id
                PlayerControllerB playerController =
                    GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts[clientId];

                // spawn object via LC API
                //LC_API.GameInterfaceAPI.Features.Item.CreateAndGiveItem(itemName,
                //    LC_API.GameInterfaceAPI.Features.Player.Get(playerController));

                GameObject buyableItem = FindObjectOfType<Terminal>().buyableItemsList.FirstOrDefault(
                    kw => kw.name.Equals(itemName)).spawnPrefab;

                if (buyableItem != null)
                {
                    GameObject obj = UnityEngine.Object.Instantiate(buyableItem, Vector3.zero, default(Quaternion));
                    obj.GetComponent<NetworkObject>().Spawn();
                    LC_API.GameInterfaceAPI.Features.Item component = obj.GetComponent
                        <LC_API.GameInterfaceAPI.Features.Item>();

                    component.GiveTo(
                        LC_API.GameInterfaceAPI.Features.Player.Get(playerController), 
                        true);
                }
            });

            syncCredits = LNetworkMessage<int>.Create("syncCredits", (credits, clientId) =>
            {
                Logger.LogDebug($"Server received update credits: {credits}");
                FindObjectOfType<Terminal>().groupCredits = credits;
            }, (credits) =>
            {
                Logger.LogDebug($"Client received update credits: {credits}");
                FindObjectOfType<Terminal>().groupCredits = credits;
            },
            (credits, clientId) => {
                Logger.LogDebug($"Client - client received update credits: {credits}");
                FindObjectOfType<Terminal>().groupCredits = credits;
            });

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void RunQuickBuy(string[] obj)
        {
            Terminal __terminal = FindObjectOfType<Terminal>();

            // prevent people from using commands while off ship or in buildings - balancing
            // TODO config file option to enable thiss
            if (!GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
            {
                chatMessageHandler("You can only use quick buy commands on the ship.", true);
                return;
            }

            if (obj.Length > 0)
            {
                int quantity = 1;
                string itemKeyword = obj[0].Trim();
                Logger.LogDebug($" item arg: {obj[0]}");
                Item itemMatch = __terminal.buyableItemsList.FirstOrDefault(kw =>
                        kw.name.Contains(itemKeyword, StringComparison.OrdinalIgnoreCase));

                if (itemMatch == null)
                {
                    itemMatch = __terminal.buyableItemsList.FirstOrDefault(kw =>
                        kw.name.Equals(itemKeyword, StringComparison.OrdinalIgnoreCase));
                }

                if (obj.Length == 2 && int.TryParse(obj[1], out int parsedQuantity))
                {
                    quantity = parsedQuantity;
                }

                // check if cost and determine if theres enough money
                var itemCost = itemMatch.creditsWorth * (__terminal.itemSalesPercentages[
                    Array.IndexOf(__terminal.buyableItemsList, itemMatch)] / 100f) * quantity;
                int adjustedCredits = __terminal.groupCredits - (int)itemCost;

                if (adjustedCredits < 0)
                {
                    chatMessageHandler($"Not Enough Credits\n\nTotal Cost: ${itemCost}\nCredits: ${__terminal.groupCredits}", true);
                    return;
                }

                Logger.LogDebug($"Run quick buy: Item match name: {itemMatch.name}");
                clientItemSpawnRequest.SendServer(itemMatch.name);

                //update the terminal credits and rpc sync
                __terminal.groupCredits = adjustedCredits;
                // sync the credit count over network
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    syncCredits.SendClients(adjustedCredits);
                }
                else
                {
                    syncCredits.SendOtherClients(adjustedCredits);
                }
                chatMessageHandler($"Order Summary\n\n{itemMatch.itemName}\nQuantity: {quantity}\nTotal: ${(int)itemCost}\nCredits: ${adjustedCredits}"); 
            }
        }

        private void chatMessageHandler(string message, bool isWarning = false)
        {
            string color = isWarning ? "red" : "green";
            string chatMessage = $"<color=\"{color}\"><b>Quick Buy</b>\n{message}</color>";

            if (HUDManager.Instance.lastChatMessage == chatMessage)
            {
                HUDManager.Instance.lastChatMessage = "";
            }

            HUDManager.Instance.AddChatMessage(chatMessage);

            var audioClip = isWarning ? HUDManager.Instance.warningSFX : HUDManager.Instance.tipsSFX;
            RoundManager.PlayRandomClip(HUDManager.Instance.UIAudio, audioClip, randomize: false);

            quickBuyChatMessageDelayHandler();
        }


        private async void quickBuyChatMessageDelayHandler(int seconds = 5)
        {
            await Task.Delay(seconds * 1000);

            int chatIndex = HUDManager.Instance.ChatMessageHistory.FindIndex(chat => chat.Contains("<b>Quick Buy</b>"));

            if (chatIndex != -1)
            {
                HUDManager.Instance.ChatMessageHistory.RemoveAt(chatIndex);
                HUDManager.Instance.chatText.text = "";
                for (int i = 0; i < HUDManager.Instance.ChatMessageHistory.Count; i++)
                {
                    TextMeshProUGUI textMeshProUGUI = HUDManager.Instance.chatText;
                    textMeshProUGUI.text = textMeshProUGUI.text + "\n" + HUDManager.Instance.ChatMessageHistory[i];
                }
            }
        }
    }
}
