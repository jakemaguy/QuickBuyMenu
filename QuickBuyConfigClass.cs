using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using CSync.Util.Types;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuickBuyMenu
{
    [DataContract]
    public class QuickBuyModConfig : SyncedConfig<QuickBuyModConfig>
    {
        [DataMember] public SyncedEntry<bool> allowQuickBuyOffShip { get; private set; }
        public ConfigEntry<int> quickBuyMessagesFadeDelay { get; private set; }
        [DataMember] public SyncedEntry<string> quickBuyItemBlacklist { get; private set; }

        [DataMember] public SyncedEntry<float> EXAMPLE_VAR { get; private set; }
        public QuickBuyModConfig(ConfigFile cfg) : base("Quick Buy Menu")
        {
            ConfigManager.Register(this);

            allowQuickBuyOffShip = cfg.BindSyncedEntry
            (
                "General",
                "AllowQuickBuyOffShip",
                false,
                "Boolean that lets you buy items off ship.  For balancing this is set to false as default."
            );

            quickBuyMessagesFadeDelay = cfg.Bind
            (
                "General",
                "quickBuyMessagesFadeDelay",
                5,
                "Timer in seconds for how long the Quick Buy chat messages should appear before they are deleted."
            );

            quickBuyItemBlacklist = cfg.BindSyncedEntry
            (
                "General",
                "QuickBuyItemBlacklist",
                "",
                "Comma Seperated List of Items to be blacklisted by Quick Buy.  Example: 'Walkie-Talkie,FlashLight,Boombox'"
            );

        }
    }
}
