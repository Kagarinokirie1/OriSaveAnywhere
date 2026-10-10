using BepInEx.Configuration;

namespace SaveAnywhere
{
    /// <summary>
    /// 插件配置项。配置文件名由 BepInEx 依据插件 GUID 自动生成。
    /// </summary>
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> AddPauseMenuButton;
        internal static ConfigEntry<string> MenuLabel;
        internal static ConfigEntry<UnityEngine.KeyCode> Hotkey;
        internal static ConfigEntry<bool> IgnoreRestrictZone;
        internal static ConfigEntry<bool> UpdateRespawnPoint;
        internal static ConfigEntry<bool> ForceBackup;
        internal static ConfigEntry<bool> PlayConfirmSound;

        internal static void Initialize(ConfigFile config)
        {
            Enabled = config.Bind(
                "通用", "启用", true,
                "是否启用本 Mod。关闭后不响应快捷键，也不会往 ESC 菜单里添加存档按钮。");

            AddPauseMenuButton = config.Bind(
                "通用", "在ESC菜单中添加存档按钮", true,
                "是否在暂停（ESC）菜单里追加一个「存档」菜单项，可与手柄导航一起使用。");

            MenuLabel = config.Bind(
                "通用", "存档按钮文字", "存档",
                "追加到 ESC 菜单的菜单项显示文字。");

            Hotkey = config.Bind(
                "通用", "存档快捷键", UnityEngine.KeyCode.F1,
                "随时按下即可存档的快捷键，默认 F1。设为 None 可关闭快捷键。");

            IgnoreRestrictZone = config.Bind(
                "存档", "忽略禁止存档区域", true,
                "游戏自带的存档点在 Boss 战、逃亡段等区域会禁止创建检查点。" +
                "开启后本 Mod 无视该限制，随时都能存档。");

            UpdateRespawnPoint = config.Bind(
                "存档", "同时把重生点更新到当前位置", true,
                "开启后存档会把「复活点」一并记录到当前所在位置（自动贴地），" +
                "效果与游戏自带存档点一致；关闭则只保存进度，复活点仍沿用上一个存档点。");

            ForceBackup = config.Bind(
                "存档", "每次存档都生成备份存档", true,
                "游戏本身对「备份游戏存档」有节流：刚读档后一段时间内、或距离上次备份太近时会跳过备份，" +
                "所以手动存档不一定马上能在备份列表里看到。开启本项后每次手动存档都强制生成一条备份。" +
                "注意：备份位只有 10 个且会轮换，频繁存档会把较旧的备份挤掉。");

            PlayConfirmSound = config.Bind(
                "存档", "存档后播放提示音", true,
                "存档成功后播放游戏的菜单确认音，方便确认「确实存上了」。");
        }
    }
}
