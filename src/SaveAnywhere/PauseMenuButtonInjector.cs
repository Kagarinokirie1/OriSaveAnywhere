using System;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace SaveAnywhere
{
    /// <summary>
    /// 往暂停（ESC）菜单里追加一个「存档」菜单项。
    ///
    /// 反编译依据（场景 `Assets/scenes/inGameUI.unity` = `oriwotw_Data/level303`）：
    /// - 暂停界面根节点为 `pauseScreen2`，挂 `PauseScreen` + `CleverMenuItemSelectionManager`
    ///   （对应 `PauseScreen.NavigationManager`）。
    /// - 菜单项统一挂在 `pauseScreen2/faderRoot/items` 下，`items` 上有
    ///   `CleverMenuItemLayout` 负责纵向排布（`Sort()` 会把可见项按 `Space` 依次往下摆）。
    /// - 每个菜单项 GameObject 只挂一个 `CleverMenuItem`，它的子物体挂
    ///   `MessageBox` + `CatlikeCoding.TextBox` 负责文字。
    /// - `CleverMenuItem.OnPressed()` 会先执行 `Pressed`（ActionMethod），
    ///   再调用 `PressedCallback`（System.Action）；把克隆体的 `Pressed` 置空、
    ///   只挂 `PressedCallback` 即可让新按钮只触发存档
    ///   （`Decompiled/ISIL/IsilDump/__mainWisp/CleverMenuItem.txt`，OnPressed）。
    /// - `CleverMenuItemSelectionManager.MoveSelection` 完全按下标在 `m_menuItems`
    ///   里前后移动，因此 `AddMenuItem` 之后手柄/键盘也能选中新按钮
    ///   （`.../CleverMenuItemSelectionManager.txt`，MoveSelection）。
    /// </summary>
    public class PauseMenuButtonInjector : MonoBehaviour
    {
        /// <summary>克隆出来的菜单项名称，便于在层级里辨认。</summary>
        private const string ItemName = "SaveAnywhere_SaveItem";

        private IntPtr _injectedPausePtr = IntPtr.Zero;
        private CleverMenuItemLayout _layout;
        private bool _wasVisible;

        public PauseMenuButtonInjector(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            if (!ModConfig.Enabled.Value || !ModConfig.AddPauseMenuButton.Value)
            {
                return;
            }

            var pause = PauseScreen.Instance;
            if (pause == null)
            {
                // 暂停界面被销毁（回主菜单等），允许下次重新注入。
                _injectedPausePtr = IntPtr.Zero;
                _layout = null;
                _wasVisible = false;
                return;
            }

            if (!pause.gameObject.activeInHierarchy)
            {
                _wasVisible = false;
                return;
            }

            if (_injectedPausePtr != pause.Pointer)
            {
                if (!TryInject(pause))
                {
                    return; // 时机不成熟，下一帧再试
                }

                _injectedPausePtr = pause.Pointer;
                _wasVisible = true;
                return;
            }

            // 每次重新打开菜单时重排一次，保证新按钮位置正确。
            if (!_wasVisible)
            {
                _layout?.Sort();
                _wasVisible = true;
            }
        }

        private bool TryInject(PauseScreen pause)
        {
            GameObject clone = null;
            try
            {
                var manager = pause.NavigationManager;
                if (manager == null || manager.MenuItemsCount <= 0)
                {
                    return false;
                }

                var template = manager.GetMenuItem(0);
                if (template == null)
                {
                    return false;
                }

                var parent = template.transform.parent;
                if (parent == null)
                {
                    return false;
                }

                clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
                if (clone == null)
                {
                    return false;
                }
                clone.name = ItemName;

                var item = clone.GetComponent<CleverMenuItem>();
                if (item == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    clone = null;
                    Plugin.Logger.LogWarning("克隆出来的菜单项上没有 CleverMenuItem，已放弃注入。");
                    return false;
                }

                // 只保留外观与高亮动画，清掉模板自带的条件与动作。
                item.Pressed = null;
                item.Activated = null;
                item.Visible = null;
                item.SetSelectionManager(manager);
                item.PressedCallback = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(
                    new Action(OnSaveItemPressed));

                ApplyLabel(clone);

                manager.AddMenuItem(item);

                _layout = parent.GetComponent<CleverMenuItemLayout>();
                if (_layout != null)
                {
                    _layout.MenuItems.Add(item);
                    _layout.Sort();
                }
                else
                {
                    // 找不到布局组件时退化为手工摆放，保证至少不会和模板重叠。
                    var t = template.transform;
                    item.transform.localPosition = t.localPosition + new Vector3(0f, -1.62f, 0f);
                }

                Plugin.Logger.LogInfo("已在 ESC 菜单中注入「存档」按钮。");
                return true;
            }
            catch (Exception e)
            {
                if (clone != null)
                {
                    // 注入中途失败时把半成品删掉，避免菜单里多出一个错位按钮。
                    UnityEngine.Object.Destroy(clone);
                }
                Plugin.Logger.LogError($"注入 ESC 菜单存档按钮失败：{e}");
                return false;
            }
        }

        /// <summary>
        /// 把克隆项里的文字改成配置的按钮文字。
        /// `MessageBox` 在 `MessageProvider` 为空时会直接显示 `OverrideText`
        /// （`Decompiled/ISIL/IsilDump/__mainWisp/MessageBox.txt`，RefreshText）。
        /// </summary>
        private static void ApplyLabel(GameObject clone)
        {
            try
            {
                var box = clone.GetComponentInChildren<MessageBox>(true);
                if (box == null)
                {
                    Plugin.Logger.LogWarning("菜单项里没有找到 MessageBox，按钮文字可能仍是模板文字。");
                    return;
                }

                box.MessageProvider = null;
                box.OverrideText = ModConfig.MenuLabel.Value;
                box.RefreshText();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"设置菜单项文字时出现异常：{e.Message}");
            }
        }

        private static void OnSaveItemPressed()
        {
            SaveService.TrySave("ESC菜单");
        }
    }
}
