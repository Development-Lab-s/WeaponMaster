using UnityEngine;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib
{
    public class PlayerDataStore
    {
        public string PlayerName;
        public string BodyType;
        public string WeaponType;
        public uint BodyColor;
        [Newtonsoft.Json.JsonIgnore]
        public uint AccessoryStyle;

        private bool accessoryStyleLoaded;
        private uint legacyHairStyle;

        [Newtonsoft.Json.JsonProperty("AccessoryStyle")]
        private uint SavedAccessoryStyle
        {
            get => AccessoryStyle;
            set { AccessoryStyle = value <= 2 ? value : 0; accessoryStyleLoaded = true; }
        }

        // Read old profiles, but only write the new key on subsequent saves.
        [Newtonsoft.Json.JsonProperty("HairStyle")]
        private uint LegacyHairStyle { set => legacyHairStyle = value; }

        [System.Runtime.Serialization.OnDeserialized]
        private void MigrateAccessoryStyle(System.Runtime.Serialization.StreamingContext context)
        {
            if (!accessoryStyleLoaded) AccessoryStyle = legacyHairStyle <= 2 ? legacyHairStyle : 0;
        }
        public uint KillCount;
        public uint DeathCount;
        public uint Score;
    }
}
