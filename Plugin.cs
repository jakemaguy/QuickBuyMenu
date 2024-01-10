using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SimpleCommand.API.Classes;
using static SimpleCommand.API.SimpleCommand;
using System.Reflection;
using UnityEngine;
using System.Text;
using static TerminalApi.TerminalApi;
using Unity.Netcode;
using System;
using QuickBuyMenu.NetworkHandler;

namespace QuickBuyMenu
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("atomic.terminalapi", "1.5.0")]
    [BepInDependency("SimpleCommand.API")]
    public class Plugin : BaseUnityPlugin
    {
        private static Plugin Instance;
        internal static ManualLogSource Log;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Log = Logger;

            NetcodeWeaver();

            Log.LogInfo("Plugin QuickBuyMenu is loaded!");

            SimpleCommandModule quickBuyMenu = new SimpleCommandModule()
            {
                DisplayName = "quickbuy",
                Description = "Displays the Quick Buy Menu for purchasing items quickly.",
                HasDynamicInput = false,
                Abbreviations = ["qb", "quickb", "QB", "qbuy", "QBUY"],
                Method = QuickBuyItemList,
            };

            SimpleCommandModule executeQuickBuy = new SimpleCommandModule()
            {
                Method = RunQuickBuy,
                DisplayName = "buy",
                Description = "Purchases the given item based off the items ID number.",
                HasDynamicInput = true,
                Arguments = ["itemID"],
                HideFromCommandList = true,
            };

            AddSimpleCommand(quickBuyMenu);
            AddSimpleCommand(executeQuickBuy);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void NetcodeWeaver()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private static TerminalNode QuickBuyItemList(Terminal __terminal)
        {
            StringBuilder stringBuilder2 = new StringBuilder();
            stringBuilder2.Append("QUICK BUY MENU\n");
            for (int i = 0; i < __terminal.buyableItemsList.Length; i++)
            {
                stringBuilder2.Append("\n* " + __terminal.buyableItemsList[i].itemName + "  //  Price: $" + (__terminal.buyableItemsList[i].creditsWorth * (__terminal.itemSalesPercentages[i] / 100f)).ToString());
                bool flag = __terminal.itemSalesPercentages[i] != 100;
                if (flag)
                {
                    stringBuilder2.Append(string.Format("   ({0}% OFF!)", 100 - __terminal.itemSalesPercentages[i]));
                }
            }
            stringBuilder2.Append("\n\nMake your selection by entering:\nbuy (Item Name)\n");
            return CreateTerminalNode(stringBuilder2.ToString(), true, "");
        }

        private static TerminalNode RunQuickBuy(Terminal __terminal)
        {
            string input = RemovePunctuation(__terminal.screenText.text.Substring(__terminal.screenText.text.Length - __terminal.textAdded));
            input = input.Substring(4); // everything after 'buy '
            Log.LogDebug("Terminal Input from purchase command: " + input);
            int itemIndex = FindItemIndex(__terminal, input);
            bool flag = itemIndex != -1;
            TerminalNode result;
            if (flag)
            {
                Item item = __terminal.buyableItemsList[itemIndex];
                var itemCost = __terminal.buyableItemsList[itemIndex].creditsWorth * (__terminal.itemSalesPercentages[itemIndex] / 100f);

                if (__terminal.groupCredits - itemCost < 0)
                {
                    result = CreateTerminalNode(string.Format("You have insufficient funds to purchase a {0} at a cost of {1}\n", item.itemName, itemCost), true, "");
                }
                else
                {
                    bool flag3 = GameNetworkManager.Instance.localPlayerController.FirstEmptyItemSlot() == -1;
                    if (flag3)
                    {
                        result = CreateTerminalNode("Your inventory is currently full.\n", true, "");
                    }
                    else
                    {
                        var weight = Mathf.Clamp(__terminal.buyableItemsList[itemIndex].weight - 1f, 0f, 10f);
                        Log.LogDebug($"Item carry weight: {weight}\n Current Player weight: {GameNetworkManager.Instance.localPlayerController.carryWeight}");
                        GameNetworkManager.Instance.localPlayerController.carryWeight += weight;

                        QuickBuyNetworkHandler.Instance.EventServerRpc(itemIndex, NetworkManager.Singleton.LocalClientId);
                        
                        __terminal.groupCredits -= (int)itemCost;
                        QuickBuyNetworkHandler.Instance.SyncGroupCreditsServerRpc(__terminal.groupCredits, __terminal.numberOfItemsInDropship);
                        result = CreateTerminalNode(string.Format("You have purchased a {0} at a cost of {1}\n", item.itemName, itemCost), true, "");
                    }
                }
            }
            else
            {
                result = CreateTerminalNode("Item not found in store: " + input + "\n", true, "");
            }
            return result;
        }

        // case insensitive string.Contains() alternative
        // Sanitizes inputs with Hyphens with whitespace for comparison purposes
        // example input: [wallkie talkie] will match item name [walkie-talkie]
        private static int FindItemIndex(Terminal terminal, string input)
        {
            return Array.FindIndex(
                terminal.buyableItemsList, (Item item) => 
                item.itemName.Replace('-', ' ').IndexOf(input.Replace('-', ' '), 
                StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Overriding SimpleAPI RemovePunctuation to allow Hyphens
        private static string RemovePunctuation(string s)
        {
            StringBuilder stringBuilder = new();
            foreach (char c in s)
            {
                if (c.Equals('-') || (!char.IsSymbol(c) && !char.IsPunctuation(c)))
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().ToLower();
        }
    }
}
