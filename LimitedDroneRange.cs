﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("Limited Drone Range", "WhiteThunder", "0.1.0")]
    [Description("Limits how far RC drones can be controlled from computer stations.")]
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

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                var station = player.GetMounted() as ComputerStation;
                if (station == null)
                    continue;

                var drone = station.currentlyControllingEnt.Get(serverside: true) as Drone;
                if (drone == null)
                    continue;

                OnBookmarkControlStarted(station, player, string.Empty, drone);
            }
        }

        private void Unload()
        {
            RangeChecker.DestroyAll();
            _pluginConfig = null;
            _pluginInstance = null;
        }

        private bool? OnBookmarkControl(ComputerStation station, BasePlayer player, string bookmarkName, Drone drone)
        {
            int maxRange;
            if (!ShouldLimitRange(station, drone, player, out maxRange))
                return null;

            if (!IsWithinRange(station, drone, maxRange))
            {
                UI.CreateOutOfRangeUI(player);
                return false;
            }

            return null;
        }

        private void OnBookmarkControlStarted(ComputerStation station, BasePlayer player, string bookmarkName, Drone drone)
        {
            int maxRange;
            if (!ShouldLimitRange(station, drone, player, out maxRange))
                return;

            RangeChecker.Create(player, station, drone, maxRange);
        }

        private void OnBookmarkControlEnded(ComputerStation station, BasePlayer player, Drone drone)
        {
            RangeChecker.Destroy(player);
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

        private static bool ShouldLimitRange(ComputerStation station, Drone drone, BasePlayer player, out int maxRange)
        {
            maxRange = _pluginConfig.GetMaxRangeForPlayer(player);
            if (maxRange <= 0)
                return false;

            return !LimitRangeWasBlocked(station, drone, player);
        }

        #endregion

        private class RangeChecker : EntityComponent<BasePlayer>
        {
            public static RangeChecker Create(BasePlayer player, ComputerStation station, Drone drone, int maxDistance) =>
                player.GetOrAddComponent<RangeChecker>().Init(station, drone, maxDistance);

            public static void Destroy(BasePlayer player)
            {
                var component = player.GetComponent<RangeChecker>();
                if (component != null)
                    DestroyImmediate(component);
            }

            public static void DestroyAll()
            {
                foreach (var player in BasePlayer.activePlayerList)
                    Destroy(player);
            }

            private ComputerStation _station;
            private Drone _drone;
            private int _maxDistance;

            private int _previousDisplayedDistance;

            private int GetDistance() =>
                Mathf.CeilToInt(_station.Distance(_drone));

            public RangeChecker Init(ComputerStation station, Drone drone, int maxDistance)
            {
                _station = station;
                _drone = drone;
                _maxDistance = maxDistance;

                var secondsBetweenUpdates = _pluginConfig.UISettings.SecondsBetweenUpdates;

                InvokeRandomized(CheckRange, 0, secondsBetweenUpdates, secondsBetweenUpdates * 0.1f);
                UI.CreateDistanceUI(baseEntity, GetDistance(), _maxDistance);

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
                    UI.CreateOutOfRangeUI(baseEntity);
                    return;
                }

                UI.CreateDistanceUI(baseEntity, distance, _maxDistance);
                _previousDisplayedDistance = distance;
            }

            public void OnDestroy() => UI.Destroy(baseEntity);
        }

        #region UI

        private static class UI
        {
            private const string DistanceUI = "LimitedDroneRange.Distance";
            private const string OutOfRangeUI = "LimitedDroneRange.OutOfRange";

            private static void CreateLabel(BasePlayer player, string uiName, string label, string color)
            {
                Destroy(player, uiName);

                var cuiElements = new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = _pluginConfig.UISettings.AnchorMin,
                                AnchorMax = _pluginConfig.UISettings.AnchorMax,
                                OffsetMin = _pluginConfig.UISettings.OffsetMin,
                                OffsetMax = _pluginConfig.UISettings.OffsetMax,
                            }
                        },
                        "Overlay",
                        uiName
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
                            FontSize = _pluginConfig.UISettings.TextSize,
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = $"{_pluginConfig.UISettings.TextSize * -3} 0",
                            OffsetMax = $"{_pluginConfig.UISettings.TextSize * 3} {_pluginConfig.UISettings.TextSize * 1.5f}",
                        }
                    },
                    uiName
                );

                CuiHelper.AddUi(player, cuiElements);
            }

            public static void CreateDistanceUI(BasePlayer player, int distance, int maxDistance)
            {
                Destroy(player, DistanceUI);
                CreateLabel(
                    player,
                    DistanceUI,
                    _pluginInstance.GetMessage(player, Lang.Distance, distance, maxDistance),
                    _pluginConfig.UISettings.GetDynamicColor(distance, maxDistance)
                );
            }

            public static void CreateOutOfRangeUI(BasePlayer player)
            {
                Destroy(player, OutOfRangeUI);
                CreateLabel(
                    player,
                    OutOfRangeUI,
                    _pluginInstance.GetMessage(player, Lang.OutOfRange),
                    _pluginConfig.UISettings.OutOfRangeColor
                );
                player.Invoke(() => Destroy(player, OutOfRangeUI), 1);
            }

            public static void Destroy(BasePlayer player, string uiName = DistanceUI)
            {
                CuiHelper.DestroyUi(player, uiName);
            }

            public static void DestroyAll()
            {
                foreach (var player in BasePlayer.activePlayerList)
                    Destroy(player, DistanceUI);
            }
        }

        #endregion

        #region Configuration

        private class RangeProfile
        {
            [JsonProperty("PermissionSuffix")]
            public string PermissionSuffix;

            [JsonProperty("MaxRange")]
            public int MaxRange;

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

        private class ColorConfig
        {
            [JsonProperty("DistanceFromMax")]
            public int DistanceFromMax;

            [JsonProperty("Color")]
            public string Color;
        }

        private class UISettings
        {
            [JsonProperty("AnchorMin")]
            public string AnchorMin = "0.5 0";

            [JsonProperty("AnchorMax")]
            public string AnchorMax = "0.5 0";

            [JsonProperty("OffsetMin")]
            public string OffsetMin = "0 75";

            [JsonProperty("OffsetMax")]
            public string OffsetMax = "0 75";

            [JsonProperty("TextSize")]
            public int TextSize = 24;

            [JsonProperty("DefaultColor")]
            public string DefaultColor = "0.2 0.75 0.2 1";

            [JsonProperty("OutOfRangeColor")]
            public string OutOfRangeColor = "1 0.2 0.2 1";

            [JsonProperty("DynamicColors")]
            public ColorConfig[] DynamicColors = new ColorConfig[]
            {
                new ColorConfig
                {
                    DistanceFromMax = 50,
                    Color = "1 0.2 0.2 1",
                },
                new ColorConfig
                {
                    DistanceFromMax = 100,
                    Color = "1 0.5 0 1",
                },
            };

            [JsonProperty("SecondsBetweenUpdates")]
            public float SecondsBetweenUpdates = 0.5f;

            public string GetDynamicColor(int distance, int maxDistance)
            {
                var distanceFromMax = maxDistance - distance;

                foreach (var colorConfig in DynamicColors)
                {
                    if (distanceFromMax < colorConfig.DistanceFromMax)
                        return colorConfig.Color;
                }

                return DefaultColor;
            }
        }

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("DefaultMaxRange")]
            public int DefaultMaxRange = 500;

            [JsonProperty("ProfilesRequiringPermission")]
            public RangeProfile[] ProfilesRequiringPermission = new RangeProfile[]
            {
                new RangeProfile()
                {
                    PermissionSuffix = "short",
                    MaxRange = 250,
                },
                new RangeProfile()
                {
                    PermissionSuffix = "medium",
                    MaxRange = 500,
                },
                new RangeProfile()
                {
                    PermissionSuffix = "long",
                    MaxRange = 1000,
                },
                new RangeProfile()
                {
                    PermissionSuffix = "unlimited",
                    MaxRange = 0,
                },
            };

            [JsonProperty("UISettings")]
            public UISettings UISettings = new UISettings();

            public void Init(LimitedDroneRange pluginInstance)
            {
                foreach (var profile in ProfilesRequiringPermission)
                    profile.Init(pluginInstance);
            }

            public int GetMaxRangeForPlayer(string userId)
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

            public int GetMaxRangeForPlayer(BasePlayer player) =>
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

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private string GetMessage(BasePlayer player, string messageName, params object[] args) =>
            GetMessage(player.UserIDString, messageName, args);

        private class Lang
        {
            public const string OutOfRange = "UI.OutOfRange";
            public const string Distance = "UI.Distance";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.Distance] = "{0}m / {1}m",
                [Lang.OutOfRange] = "OUT OF RANGE",
            }, this, "en");
        }

        #endregion
    }
}
