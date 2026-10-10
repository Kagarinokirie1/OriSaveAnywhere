using System;
using UnityEngine;

namespace SaveAnywhere
{
    /// <summary>
    /// 快捷键存档（默认 F1）。ESC 菜单里的按钮和这里共用同一套存档逻辑。
    /// </summary>
    public class HotkeyListener : MonoBehaviour
    {
        private float _lastSaveTime = -10f;

        public HotkeyListener(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            if (!ModConfig.Enabled.Value)
            {
                return;
            }

            var hotkey = ModConfig.Hotkey.Value;
            if (hotkey == KeyCode.None || !Input.GetKeyDown(hotkey))
            {
                return;
            }

            // 同一帧内只处理一次，避免按键抖动触发多次存档。
            if (Time.unscaledTime - _lastSaveTime < 0.5f)
            {
                return;
            }
            _lastSaveTime = Time.unscaledTime;

            SaveService.TrySave("快捷键");
        }
    }
}
