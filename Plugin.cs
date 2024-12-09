using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using LethalNetworkAPI;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace QuickBuyMenu
{
    [BepInPlugin("Quick.Buy.Menu", "Quick Buy Meny", "2.0.0")]
    [BepInDependency("LethalNetworkAPI")]
    [BepInDependency("io.github.CSync")]
    public class Plugin : BaseUnityPlugin
    {
        private static Plugin Instance;
        internal static ManualLogSource Log;
        LNetworkMessage<ClientItemRequestParams> clientItemSpawnRequest;
        LNetworkMessage<int> syncCredits;
        List<string> blackListedItems;

        public static new QuickBuyModConfig Config { get; private set; }

        public struct ClientItemRequestParams : INetworkSerializeByMemcpy
        {
            public string ItemName;
            public int clientIndex;

            void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ItemName);
                serializer.SerializeValue(ref clientIndex);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Log = Logger;
            Log.LogDebug("Plugin QuickBuyMenu is loaded!");

            Config = new(base.Config);

            blackListedItems = Config.quickBuyItemBlacklist.Value
                .Split(',')
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToList();

            LC_API.ClientAPI.CommandHandler.RegisterCommand("quickbuy", new List<string> { "qb", "buy", "qbuy", }, RunQuickBuy);
            LC_API.ClientAPI.CommandHandler.RegisterCommand("price-check", new List<string> { "qc", "inquire", "check", "pc" }, RunPriceCheck);

            clientItemSpawnRequest = LNetworkMessage<ClientItemRequestParams>.Create(identifier: "clientItemSpawnRequest", (clientRequest, clientId) =>
            {
                // get player controller reference based on client index in request
                LC_API.GameInterfaceAPI.Features.Player playerController =
                    LC_API.GameInterfaceAPI.Features.Player
                    .Get(GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts[clientRequest.clientIndex]);
                

                GameObject buyableItem = FindObjectOfType<Terminal>().buyableItemsList.FirstOrDefault(
                    kw => kw.name.Equals(clientRequest.ItemName)).spawnPrefab;

                if (buyableItem != null)
                {
                    GameObject obj = UnityEngine.Object.Instantiate(buyableItem, Vector3.zero, default(Quaternion));
                    obj.GetComponent<NetworkObject>().Spawn();
                    LC_API.GameInterfaceAPI.Features.Item component = obj.GetComponent
                        <LC_API.GameInterfaceAPI.Features.Item>();

                    component.GiveTo(playerController, true);
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
            Terminal terminal = FindObjectOfType<Terminal>();
            
            if (!IsQuickBuyAllowed())
            {
                chatMessageHandler("You can only use quick buy commands on the ship.", true);
                return;
            }

            if (obj.Length > 0)
            {
                int quantity = GetQuantity(obj);
                string itemKeyword = obj[0].Trim();
                Logger.LogDebug($" item arg: {obj[0]}");

                Item itemMatch = FindItemMatch(terminal, itemKeyword);

                if (itemMatch == null)
                {
                    chatMessageHandler($"Item {itemKeyword} not found.", true);
                    return;
                }

                if (blackListedItems.Exists(item => item.Equals(itemMatch.itemName, StringComparison.OrdinalIgnoreCase)))
                {
                    chatMessageHandler($"{itemMatch.itemName} is blacklisted.", true);
                    return;
                }

                if (!CheckPlayerInventory(quantity))
                {
                    return;
                }

                if (!HasEnoughCredits(terminal, itemMatch, quantity, out var itemCost, out var adjustedCredits))
                {
                    chatMessageHandler($"Not Enough Credits\n\nTotal Cost: ${itemCost}\nCredits: ${terminal.groupCredits}", true);
                    return;
                }

                ProcessQuickBuy(terminal, itemMatch, quantity, itemCost, adjustedCredits);
            }
        }
        private void RunPriceCheck(string[] obj)
        {
            Terminal terminal = FindObjectOfType<Terminal>();

            if (!IsQuickBuyAllowed())
            {
                chatMessageHandler("You can only use quick buy commands on the ship.", true);
                return;
            }

            if (obj.Length > 0)
            {
                string itemKeyword = obj[0].Trim();
                int quantity = GetQuantity(obj);

                Item itemMatch = FindItemMatch(terminal, itemKeyword);
                
                if (itemMatch == null)
                {
                    chatMessageHandler($"Item {itemKeyword} not found.", true);
                    return;
                }

                if (blackListedItems.Exists(item => item.Equals(itemMatch.itemName, StringComparison.OrdinalIgnoreCase)))
                {
                    chatMessageHandler($"{itemMatch.itemName} is blacklisted.", true);
                    return;
                }

                if (!HasEnoughCredits(terminal, itemMatch, quantity, out var itemCost, out var adjustedCredits))
                {
                    chatMessageHandler($"Price Check\nNot Enough Credits\n\nItem: {itemMatch.itemName}\nQuantity: {quantity}\nTotal Cost: ${itemCost}\nGroup Credits: ${terminal.groupCredits}", true);
                    return;
                }

                chatMessageHandler($"Price Check\n\nItem: {itemMatch.itemName}\nQuantity: {quantity}\nTotal Cost: ${itemCost}\nGroup Credits: ${terminal.groupCredits}");
            }       
        }

        private bool IsQuickBuyAllowed()
        {
            return Config.allowQuickBuyOffShip.Value || GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom;
        }

        private int GetQuantity(string[] obj)
        {
            return obj.Length == 2 && int.TryParse(obj[1], out int parsedQuantity) ? parsedQuantity : 1;
        }

        private Item FindItemMatch(Terminal terminal, string itemKeyword)
        {
            return terminal.buyableItemsList.FirstOrDefault(kw =>
                       kw.name.Contains(itemKeyword, StringComparison.OrdinalIgnoreCase)) ??
                   terminal.buyableItemsList.FirstOrDefault(kw =>
                       kw.name.Equals(itemKeyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasEnoughCredits(Terminal terminal, Item itemMatch, int quantity, out float itemCost, out int adjustedCredits)
        {
            itemCost = itemMatch.creditsWorth * (terminal.itemSalesPercentages[
                Array.IndexOf(terminal.buyableItemsList, itemMatch)] / 100f) * quantity;
            adjustedCredits = terminal.groupCredits - (int)itemCost;
            return adjustedCredits >= 0;
        }

        private bool CheckPlayerInventory(int quantity)
        {
            LC_API.GameInterfaceAPI.Features.Player player =
                    LC_API.GameInterfaceAPI.Features.Player.Get(GameNetworkManager.Instance.localPlayerController);
            
            if (player.Inventory.GetFirstEmptySlot() == -1)
            {
                chatMessageHandler($"Inventory is Full.", true);
                return false;
            }

            Log.LogDebug($"Item slots length: {player.PlayerController.ItemSlots.Length}");

            int InventoryItemsCount = player.Inventory.Items.Where(item => item != null).ToArray().Length;
            int freeSlotsAvailable = player.PlayerController.ItemSlots.Length - InventoryItemsCount;

            if (quantity > (player.PlayerController.ItemSlots.Length - InventoryItemsCount))
            {
                chatMessageHandler($"Not enough inventory space for this order.", true);
                return false;
            }

            return true;
        }

        private void ProcessQuickBuy(Terminal terminal, Item itemMatch, int quantity, float itemCost, int adjustedCredits)
        {
            // get current clientIndex in connected players array to retrieve the clientId
            int clientIndex = Array.FindIndex(GameNetworkManager.Instance.localPlayerController.playersManager.allPlayerScripts,
                player => player.actualClientId.Equals(GameNetworkManager.Instance.localPlayerController.actualClientId));
            
            for (int i = 0; i < quantity; i++)
            {
                ClientItemRequestParams clientRequest = new ClientItemRequestParams();
                clientRequest.clientIndex = clientIndex;
                clientRequest.ItemName = itemMatch.name;
                clientItemSpawnRequest.SendServer(clientRequest);
            }

            terminal.groupCredits = adjustedCredits;
            SyncCredits(adjustedCredits);

            chatMessageHandler($"Order Summary\n\n{itemMatch.itemName}\nQuantity: {quantity}\nTotal: ${(int)itemCost}\nCredits: ${adjustedCredits}");
        }

        private void SyncCredits(int adjustedCredits)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                syncCredits.SendClients(adjustedCredits);
            }
            else
            {
                syncCredits.SendOtherClients(adjustedCredits);
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

            quickBuyChatMessageDelayHandler(Config.quickBuyMessagesFadeDelay.Value);
        }
        private async void quickBuyChatMessageDelayHandler(int seconds)
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
