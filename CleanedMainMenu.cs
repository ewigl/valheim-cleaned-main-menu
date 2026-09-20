using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanedMainMenu
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class CleanedMainMenu : BaseUnityPlugin
    {
        public const string PluginGUID = "ewigl.ValheimCleanedMainMenu";
        public const string PluginName = "Cleaned Main Menu";
        public const string PluginVersion = "0.1.0";

        public class UIElement
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string SectionToken { get; set; }
            public string DisplayNameToken { get; set; }
            public string DescriptionToken { get; set; }
            public int Order { get; set; }
            public bool DefaultValue { get; set; }
            public ConfigEntry<bool> Config { get; set; }
            public GameObject GameObject { get; set; }

            public UIElement(string name, string path, string section, string displayName, string desc, int order, bool defaultValue = false)
            {
                Name = name;
                Path = path;
                SectionToken = section;
                DisplayNameToken = displayName;
                DescriptionToken = desc;
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
            RegisterLocalizations();
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
                new UIElement("Logo",              "Logo",                          "$vcmm_section_main",        "$vcmm_logo_name",       "$vcmm_logo_desc",       100),
                new UIElement("MenuList",          "MenuList",                      "$vcmm_section_main",        "$vcmm_menulist_name",   "$vcmm_menulist_desc",   90),

                new UIElement("BottomLeftButtons", "BottomLeftButtons",               "$vcmm_section_bottomleft",  "$vcmm_bottomleft_name", "$vcmm_bottomleft_desc", 80, true),
                new UIElement("showChangelog",     "BottomLeftButtons/showChangelog", "$vcmm_section_bottomleft",  "$vcmm_changelog_name",  "$vcmm_changelog_desc",  79),
                new UIElement("showEula",          "BottomLeftButtons/showEula",      "$vcmm_section_bottomleft",  "$vcmm_eula_name",       "$vcmm_eula_desc",       78),
                new UIElement("showlog",           "BottomLeftButtons/showlog",       "$vcmm_section_bottomleft",  "$vcmm_log_name",        "$vcmm_log_desc",        77),

                new UIElement("modded_text",       "modded_text",                   "$vcmm_section_bottomright", "$vcmm_modded_name",     "$vcmm_modded_desc",     70, true),
                new UIElement("TopRight",          "TopRight",                      "$vcmm_section_bottomright", "$vcmm_topright_name",    "$vcmm_topright_desc",    60, true),
                new UIElement("version_text",      "version_text",                  "$vcmm_section_bottomright", "$vcmm_version_name",    "$vcmm_version_desc",    50, true),
            };

            foreach (var elem in UIElements)
            {
                string section = Localize(elem.SectionToken);
                string name = Localize(elem.DisplayNameToken);
                string desc = Localize(elem.DescriptionToken);

                var configDesc = new ConfigDescription(desc, null, new ConfigurationManagerAttributes { Order = elem.Order });

                elem.Config = Config.Bind(section, name, elem.DefaultValue, configDesc);
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
            }

            foreach (var elem in UIElements)
            {
                if (elem.Config.Value && elem.GameObject != null && elem.GameObject.activeSelf)
                {
                    elem.GameObject.SetActive(false);
                }
            }
        }

        private static void CacheReferences(GameObject root)
        {
            foreach (var elem in UIElements)
            {
                Transform target = root.transform.Find(elem.Path) ?? root.transform.Find(elem.Name);
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

        private string Localize(string token) =>
            string.IsNullOrEmpty(token) ? string.Empty : LocalizationManager.Instance.GetLocalization().TryTranslate(token);

        private void RegisterLocalizations()
        {
            var localization = LocalizationManager.Instance.GetLocalization();

            localization.AddTranslation("Chinese", new Dictionary<string, string>
            {
                ["vcmm_section_main"] = "主要",
                ["vcmm_section_bottomleft"] = "左下角",
                ["vcmm_section_bottomright"] = "右下角",

                ["vcmm_logo_name"] = "隐藏 Logo",
                ["vcmm_logo_desc"] = "隐藏硕大的 Logo",
                ["vcmm_menulist_name"] = "隐藏菜单",
                ["vcmm_menulist_desc"] = "你认真的吗？",

                ["vcmm_bottomleft_name"] = "隐藏左下角全部按钮",
                ["vcmm_bottomleft_desc"] = "隐藏整个左下角按钮区域",
                ["vcmm_changelog_name"] = "隐藏更新日志按钮",
                ["vcmm_changelog_desc"] = "隐藏 更新日志 按钮",
                ["vcmm_eula_name"] = "隐藏 EULA 按钮",
                ["vcmm_eula_desc"] = "隐藏 最终用户许可协议 按钮",
                ["vcmm_log_name"] = "隐藏日志按钮",
                ["vcmm_log_desc"] = "隐藏 PLAYER.LOG 按钮",

                ["vcmm_modded_name"] = "隐藏已修改游戏提示文本",
                ["vcmm_modded_desc"] = "隐藏安装 Mod 后的警告内容",
                ["vcmm_topright_name"] = "隐藏商店",
                ["vcmm_topright_desc"] = "隐藏商店图标",
                ["vcmm_version_name"] = "隐藏版本号",
                ["vcmm_version_desc"] = "隐藏游戏版本号",
            });

            localization.AddTranslation("English", new Dictionary<string, string>
            {
                ["vcmm_section_main"] = "Main",
                ["vcmm_section_bottomleft"] = "Bottom Left",
                ["vcmm_section_bottomright"] = "Bottom Right",

                ["vcmm_logo_name"] = "Hide Logo",
                ["vcmm_logo_desc"] = "Hide the Valheim logo",
                ["vcmm_menulist_name"] = "Hide Menus",
                ["vcmm_menulist_desc"] = "Are you serious?",

                ["vcmm_bottomleft_name"] = "Hide Bottom-Left",
                ["vcmm_bottomleft_desc"] = "Hide the entire bottom-left buttons container",
                ["vcmm_changelog_name"] = "Hide Changelog Button",
                ["vcmm_changelog_desc"] = "Hide the Changelog button",
                ["vcmm_eula_name"] = "Hide EULA Button",
                ["vcmm_eula_desc"] = "Hide the EULA button",
                ["vcmm_log_name"] = "Hide Log Button",
                ["vcmm_log_desc"] = "Hide the PLAYER.LOG button",

                ["vcmm_modded_name"] = "Hide Modded Warning",
                ["vcmm_modded_desc"] = "Hide the modded game warning",
                ["vcmm_topright_name"] = "Hide Merch Store",
                ["vcmm_topright_desc"] = "Hide the store",
                ["vcmm_version_name"] = "Hide Version Text",
                ["vcmm_version_desc"] = "Hide game version text",
            });
        }
    }

    internal class ConfigurationManagerAttributes
    {
        public int? Order;
    }
}