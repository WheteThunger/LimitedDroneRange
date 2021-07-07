using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Limited Drone Range", "WhiteThunder", "0.1.0")]
    [Description("Allows limiting how far RC drones can be controlled from computer stations.")]
    internal class LimitedDroneRange : CovalencePlugin
    {
        #region Fields

        private static LimitedDroneRange _pluginInstance;

        private static Configuration _pluginConfig;

        private const string PermissionProfilePrefix = "limiteddronerange";

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginConfig.Init(this);

            _pluginInstance = this;
        }

        private void Unload()
        {
            RangeChecker.DestroyAll();

            _pluginInstance = null;
        }

        private bool? OnBookmarkControl(ComputerStation station, BasePlayer player, string bookmarkName, Drone drone)
        {
            if (!ShouldLimitRange(station, drone, player))
                return null;

            var maxRange = _pluginConfig.GetMaxRangeForPlayer(player);
            if (!IsWithinRange(station, drone, maxRange))
            {
                UI.ShowOutOfRange(player);
                return false;
            }

            return null;
        }

        private void OnBookmarkControlStarted(ComputerStation station, BasePlayer player, string bookmarkName, Drone drone)
        {
            if (!ShouldLimitRange(station, drone, player))
                return;

            var maxRange = _pluginConfig.GetMaxRangeForPlayer(player);
            RangeChecker.AddToPlayer(player, station, drone, maxRange);
        }

        private void OnBookmarkControlEnded(ComputerStation station, BasePlayer player, Drone drone)
        {
            RangeChecker.RemoveFromPlayer(player);
        }

        #endregion

        #region Helper Methods

        private static bool LimitRangeWasBlocked(ComputerStation station, Drone drone, BasePlayer player)
        {
            object hookResult = Interface.CallHook("OnDroneRangeLimit", station, drone, player);
            return hookResult is bool && (bool)hookResult == false;
        }

        private static float GetDistance(BaseEntity entity1, BaseEntity entity2) =>
            Vector3.Distance(entity1.transform.position, entity2.transform.position);

        private static bool IsWithinRange(ComputerStation station, Drone drone, float range) =>
            station.Distance(drone) < range;

        private static string GetProfilePermission(string profileSuffix) =>
            $"{PermissionProfilePrefix}.{profileSuffix}";

        private static bool ShouldLimitRange(ComputerStation station, Drone drone, BasePlayer player)
        {
            if (LimitRangeWasBlocked(station, drone, player))
                return  false;

            var maxRange = _pluginConfig.GetMaxRangeForPlayer(player);
            return maxRange > 0;
        }

        #endregion

        private class RangeChecker : EntityComponent<BasePlayer>
        {
            public static RangeChecker AddToPlayer(BasePlayer player, ComputerStation station, Drone drone, float maxDistance) =>
                player.GetOrAddComponent<RangeChecker>().Init(station, drone, maxDistance);

            public static void RemoveFromPlayer(BasePlayer player)
            {
                var component = player.GetComponent<RangeChecker>();
                if (component != null)
                    DestroyImmediate(component);
            }

            public static void DestroyAll()
            {
                foreach (var player in BasePlayer.activePlayerList)
                    RemoveFromPlayer(player);
            }

            private ComputerStation _station;
            private Drone _drone;
            private float _maxDistance;

            private int _previousDisplayedDistance;

            private int GetDistance() =>
                Mathf.CeilToInt(_station.Distance(_drone));

            public RangeChecker Init(ComputerStation station, Drone drone, float maxDistance)
            {
                _station = station;
                _drone = drone;
                _maxDistance = maxDistance;

                InvokeRandomized(CheckRange, 0, 0.25f, 0.025f);
                UI.Create(baseEntity, GetDistance(), _maxDistance);

                return this;
            }

            public void CheckRange()
            {
                var distance = Mathf.CeilToInt(_station.Distance(_drone));
                if (distance == _previousDisplayedDistance)
                    return;

                if (distance > _maxDistance)
                {
                    _station.StopControl(baseEntity);
                    UI.ShowOutOfRange(baseEntity);
                    return;
                }

                UI.Update(baseEntity, distance, _maxDistance);
                _previousDisplayedDistance = distance;
            }

            public void OnDestroy() =>
                UI.Destroy(baseEntity);
        }

        #region UI

        private static class UI
        {
            private const string DistanceUI = "LimitedDroneRange.Distance";
            private const string OutOfRangeUI = "LimitedDroneRange.OutOfRange";

            private static void CreateInternal(BasePlayer player, string name, string label, string color)
            {
                Destroy(player);

                var cuiElements = new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5 0",
                                AnchorMax = "0.5 0",
                                OffsetMin = "0 75",
                                OffsetMax = "0 75",
                            }
                        },
                        "Overlay",
                        name
                    }
                };

                cuiElements.Add(
                    new CuiLabel
                    {
                        Text =
                        {
                            Text = label,
                            Align = TextAnchor.MiddleCenter,
                            Color = color,
                            FontSize = 30,
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "-200 0",
                            OffsetMax = "200 50",
                        }
                    },
                    name
                );

                CuiHelper.AddUi(player, cuiElements);
            }

            private const string SafeRangeColor = "0 0.75 0 1";
            private const string DangeRangerColor = "1 0.2 0.2 1";
            private const string CautionRangeColor = "1 0.5 0 1";
            private const string OutOfRangeColor = "1 0.2 0.2 1";

            public static void Create(BasePlayer player, float distance, float maxDistance)
            {
                var color = SafeRangeColor;

                if (maxDistance - distance < 50)
                    color = DangeRangerColor;
                else if (maxDistance - distance < 100)
                    color = CautionRangeColor;

                CreateInternal(player, DistanceUI, $"{distance}m / {maxDistance}m", color);
            }

            public static void ShowOutOfRange(BasePlayer player)
            {
                Destroy(player, OutOfRangeUI);
                CreateInternal(player, OutOfRangeUI, "OUT OF RANGE", "1 0.2 0.2 1");
                player.Invoke(() => Destroy(player, OutOfRangeUI), 1);
            }

            public static void Update(BasePlayer player, float distance, float maxDistance)
            {
                Destroy(player);
                Create(player, distance, maxDistance);
            }

            public static void Destroy(BasePlayer player, string name = DistanceUI)
            {
                CuiHelper.DestroyUi(player, name);
            }

            public static void DestroyAll()
            {
                foreach (var player in BasePlayer.activePlayerList)
                    Destroy(player);
            }
        }

        #endregion

        #region Configuration

        private class RangeProfile
        {
            [JsonProperty("PermissionSuffix")]
            public string PermissionSuffix;

            [JsonProperty("MaxRange")]
            public float MaxRange;

            [JsonIgnore]
            public string Permission;

            public void Init(LimitedDroneRange pluginInstance)
            {
                if (string.IsNullOrWhiteSpace(PermissionSuffix))
                    return;

                Permission = GetProfilePermission(PermissionSuffix);
                pluginInstance.permission.RegisterPermission(Permission, pluginInstance);
            }
        }

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("DefaultMaxRange")]
            public float DefaultMaxRange = 500;

            [JsonProperty("ProfilesRequiringPermission")]
            public RangeProfile[] ProfilesRequiringPermission = new RangeProfile[]
            {
                new RangeProfile()
                {
                    PermissionSuffix = "shortrange",
                    MaxRange = 250,
                },
                new RangeProfile()
                {
                    PermissionSuffix = "mediumrange",
                    MaxRange = 500,
                },
                new RangeProfile()
                {
                    PermissionSuffix = "longrange",
                    MaxRange = 1000,
                },
                new RangeProfile()
                {
                    PermissionSuffix = "unlimitedrange",
                    MaxRange = 0,
                },
            };

            public void Init(LimitedDroneRange pluginInstance)
            {
                foreach (var profile in ProfilesRequiringPermission)
                    profile.Init(pluginInstance);
            }

            public float GetMaxRangeForPlayer(string userId)
            {
                if (ProfilesRequiringPermission == null)
                    return DefaultMaxRange;

                for (var i = ProfilesRequiringPermission.Length - 1; i >= 0; i--)
                {
                    var profile = ProfilesRequiringPermission[i];
                    if (profile.Permission != null && _pluginInstance.permission.UserHasPermission(userId, profile.Permission))
                        return profile.MaxRange;
                }

                return DefaultMaxRange;
            }

            public float GetMaxRangeForPlayer(BasePlayer player) =>
                GetMaxRangeForPlayer(player.UserIDString);
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion

        #region Localization

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            player.ChatMessage(string.Format(GetMessage(player.IPlayer, messageName), args));

        private string GetMessage(IPlayer player, string messageName, params object[] args) =>
            GetMessage(player.Id, messageName, args);

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {

            }, this, "en");
        }

        #endregion
    }
}
