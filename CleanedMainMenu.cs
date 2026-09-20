using BepInEx;
using BepInEx.Configuration;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CleanedMainMenu
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class CleanedMainMenu : BaseUnityPlugin
    {
        public const string PluginGUID = "ewigl.ValheimCleanedMainMenu";
        public const string PluginName = "Cleaned Main Menu";
        public const string PluginVersion = "0.1.0";

        private static readonly string ConfigDir = Path.Combine(Paths.ConfigPath, "CleanedMainMenu");
        private static readonly string CustomLogoPath = Path.Combine(ConfigDir, "logo.png");

        public class UIElement
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string Section { get; set; }
            public string Path { get; set; }
            public string Description { get; set; }
            public int Order { get; set; }
            public bool DefaultValue { get; set; }
            public ConfigEntry<bool> Config { get; set; }
            public GameObject GameObject { get; set; }

            public UIElement(string key, string name, string section, string path, string description, int order, bool defaultValue = false)
            {
                Key = key;
                Name = name;
                Section = section;
                Path = path;
                Description = description;
                Order = order;
                DefaultValue = defaultValue;
            }
        }

        public static UIElement[] UIElements { get; private set; }

        private GameObject menuRoot;
        private bool isMenuScene;
        private bool isCached;

        private void Awake()
        {
            if (!Directory.Exists(ConfigDir))
            {
                Directory.CreateDirectory(ConfigDir);
            }

            InitUIElements();

            SceneManager.sceneLoaded += OnSceneLoaded;
            Logger.LogInfo($"{PluginName} v{PluginVersion} Loaded.");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void InitUIElements()
        {
            UIElements = new[]
            {
                new UIElement("HideLogo",              "Hide Logo",                 "Main",         "Logo",                            "Hide the Valheim logo",               100),
                new UIElement("HideMenuList",          "Hide Menus",                "Main",         "MenuList",                        "Are you sure?",                       90),

                new UIElement("HideBottomLeftButtons", "Hide Bottom-Left Buttons",  "Bottom Left",  "BottomLeftButtons",               "Hide all buttons at bottom-left",      80, true),
                new UIElement("HideChangelog",         "Hide Changelog",            "Bottom Left",  "BottomLeftButtons/showChangelog", "Hide the CHANGELOG button",          79),
                new UIElement("HideEula",              "Hide EULA",                 "Bottom Left",  "BottomLeftButtons/showEula",      "Hide the EULA button",               78),
                new UIElement("HideLog",               "Hide Player Log",           "Bottom Left",  "BottomLeftButtons/showlog",       "Hide the PLAYER.LOG button",          77),

                new UIElement("HideModdedText",        "Hide Modded Warning",       "Bottom Right", "modded_text",                     "Hide the modded game warning text",   70, true),
                new UIElement("HideTopRight",          "Hide Merch Store",          "Bottom Right", "TopRight",                        "Hide the merch store button",         60, true),
                new UIElement("HideVersionText",       "Hide Version Text",         "Bottom Right", "version_text",                    "Hide game version text",              50, true),
            };

            foreach (var elem in UIElements)
            {
                var configDesc = new ConfigDescription(elem.Description, null, new ConfigurationManagerAttributes
                {
                    Order = elem.Order,
                    DispName = elem.Name
                });

                elem.Config = Config.Bind(elem.Section, elem.Key, elem.DefaultValue, configDesc);
                elem.Config.SettingChanged += (s, a) => ApplySingleElement(elem);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isMenuScene = scene.name == "mainmenu" || scene.name.Equals("start", StringComparison.OrdinalIgnoreCase);
            menuRoot = null;
            isCached = false;

            foreach (var elem in UIElements)
            {
                elem.GameObject = null;
            }
        }

        private void Update()
        {
            if (!isMenuScene) return;

            if (!isCached)
            {
                menuRoot ??= GameObject.Find("StartGUI/Menu") ?? GameObject.Find("Menu");
                if (menuRoot == null) return;

                CacheReferences(menuRoot);
                isCached = true;
                ApplyAllElements();

                TryApplyCustomLogo();
            }

            foreach (var elem in UIElements)
            {
                if (elem.Config.Value && elem.GameObject != null && elem.GameObject.activeSelf)
                {
                    elem.GameObject.SetActive(false);
                }
            }
        }

        private void TryApplyCustomLogo()
        {
            if (!File.Exists(CustomLogoPath))
            {
                return;
            }

            foreach (var elem in UIElements)
            {
                if (elem.Key == "HideLogo" && elem.GameObject != null)
                {
                    Transform logoRoot = elem.GameObject.transform;
                    Image targetLogoImage = null;

                    for (int i = 0; i < logoRoot.childCount; i++)
                    {
                        Transform child = logoRoot.GetChild(i);

                        if (!child.gameObject.activeSelf) continue;
                        if (child.name.IndexOf("Embers", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                        Image img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            targetLogoImage = img;
                            break;
                        }
                    }

                    if (targetLogoImage != null)
                    {
                        try
                        {
                            Sprite customSprite = Jotunn.Utils.AssetUtils.LoadSprite(CustomLogoPath);
                            if (customSprite != null)
                            {
                                targetLogoImage.sprite = customSprite;
                                targetLogoImage.overrideSprite = customSprite;
                                targetLogoImage.preserveAspect = true;

                                Transform logoTransform = targetLogoImage.transform;
                                int disabledSnowCount = 0;

                                for (int j = 0; j < logoTransform.childCount; j++)
                                {
                                    Transform innerChild = logoTransform.GetChild(j);

                                    if (innerChild.name.IndexOf("Snow", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        innerChild.gameObject.SetActive(false);
                                        disabledSnowCount++;
                                    }
                                }

                                Logger.LogInfo($"[Cleaned Main Menu] Custom logo applied. Disabled {disabledSnowCount} Snow effect nodes.");
                            }
                            else
                            {
                                Logger.LogError($"[Cleaned Main Menu] Loaded image file, but failed to create Sprite: {CustomLogoPath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"[Cleaned Main Menu] Exception replacing logo: {ex.Message}");
                        }
                        return;
                    }

                    Logger.LogWarning("[Cleaned Main Menu] Could not locate active main logo node.");
                    break;
                }
            }
        }

        private static void CacheReferences(GameObject root)
        {
            foreach (var elem in UIElements)
            {
                Transform target = root.transform.Find(elem.Path) ?? root.transform.Find(elem.Key);
                elem.GameObject = target?.gameObject;
            }
        }

        public static void ApplyAllElements()
        {
            foreach (var elem in UIElements)
            {
                ApplySingleElement(elem);
            }
        }

        public static void ApplySingleElement(UIElement elem) => elem.GameObject?.SetActive(!elem.Config.Value);
    }

    internal class ConfigurationManagerAttributes
    {
        public int? Order;
        public string DispName;
    }
}