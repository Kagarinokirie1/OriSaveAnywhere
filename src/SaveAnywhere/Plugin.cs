using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SaveAnywhere
{
    /// <summary>
    /// BepInEx 插件入口：注册并挂载「随时存档」相关行为组件。
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "oriwotw.saveanywhere";
        public const string PluginName = "随时存档";
        public const string PluginVersion = "0.2.0";

        internal static ManualLogSource Logger;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint codePageId);

        public override void Load()
        {
            TryFixConsoleEncoding();
            Logger = Log;
            ModConfig.Initialize(Config);

            // IL2CPP 下自定义 MonoBehaviour 必须先注册类型再挂载。
            ClassInjector.RegisterTypeInIl2Cpp<PauseMenuButtonInjector>();
            ClassInjector.RegisterTypeInIl2Cpp<HotkeyListener>();

            var host = new GameObject("SaveAnywhere");
            Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<PauseMenuButtonInjector>();
            host.AddComponent<HotkeyListener>();

            Log.LogInfo($"{PluginName} v{PluginVersion} 已加载");
        }

        /// <summary>
        /// BepInEx 控制台默认沿用系统代码页（简中为 GBK），显示 UTF-8 的中文日志会变成乱码。
        /// 这里把控制台输出代码页切成 UTF-8（65001），让中文日志正常显示。
        /// </summary>
        private static void TryFixConsoleEncoding()
        {
            try
            {
                SetConsoleOutputCP(65001);
                System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                // 没有控制台或不允许修改时忽略即可
            }
        }
    }
}
