using System;
using UnityEngine;

namespace SaveAnywhere
{
    /// <summary>
    /// 存档核心逻辑：完全复用游戏自带的存档管线。
    ///
    /// 反编译依据：
    /// - `GameController.CreateCheckpoint(doPerformSave, respectRestrictCheckpointZone)`
    ///   在 `doPerformSave = true` 时会调用 `SaveGameController.PerformSave()`
    ///   （`Decompiled/ISIL/IsilDump/__mainWisp/GameController.txt`，CreateCheckpoint）。
    /// - `SaveGameController.PerformSave()` 内部会调用
    ///   `SaveSlotBackupsManager.CreateCurrentBackup()` 生成「备份存档」，
    ///   再 `SaveToFile(当前槽位, -1, 当前 UberState)` 落盘
    ///   （`.../SaveGameController.txt`，PerformSave）。
    ///   因此本 Mod 的存档与游戏自带存档点写出的文件完全一致，也会出现在
    ///   ESC 菜单的「备份游戏存档」列表里。
    /// - 游戏自带存档点的复活点记录方式见 `CheckpointFunctionality.CreateCheckpoint`：
    ///   先用 `InvisibleCheckpoint.GetSeinGroundPosition` 找到脚下地面，
    ///   再调用 `PlatformMovement.OverridePositionNextSave`，最后才创建检查点
    ///   （`.../CheckpointFunctionality.txt`）。本 Mod 的「更新重生点」即照此复刻。
    /// </summary>
    internal static class SaveService
    {
        /// <summary>
        /// 执行一次存档。返回是否成功发起存档。
        /// </summary>
        /// <param name="source">调用来源，仅用于日志。</param>
        internal static bool TrySave(string source)
        {
            try
            {
                if (!ModConfig.Enabled.Value)
                {
                    return false;
                }

                var gameController = global::GameController.Instance;
                if (gameController == null)
                {
                    Plugin.Logger.LogWarning($"[{source}] 存档失败：GameController 尚未初始化。");
                    return false;
                }

                var saveController = gameController.SaveGameController;
                if (saveController == null)
                {
                    Plugin.Logger.LogWarning($"[{source}] 存档失败：SaveGameController 不存在。");
                    return false;
                }

                if (!saveController.CanPerformSave())
                {
                    Plugin.Logger.LogWarning(
                        $"[{source}] 存档失败：当前状态不允许存档" +
                        "（读档中、存档占用中或处于禁止存档的流程里）。");
                    return false;
                }

                if (global::SaveSlotsManager.CurrentSaveSlot == null)
                {
                    Plugin.Logger.LogWarning($"[{source}] 存档失败：当前没有选中的存档槽位。");
                    return false;
                }

                ForceBackupIfNeeded(source);

                if (ModConfig.UpdateRespawnPoint.Value)
                {
                    TryUpdateRespawnPoint(source);
                }

                // 第二个参数为 true 时游戏会遵循「禁止存档区域」限制；
                // 配置开启「忽略禁止存档区域」时传 false，做到真正随时可存。
                bool respectRestrictZone = !ModConfig.IgnoreRestrictZone.Value;
                gameController.CreateCheckpoint(true, respectRestrictZone);

                int slotIndex = global::SaveSlotsManager.CurrentSlotIndex;
                Plugin.Logger.LogInfo(
                    $"[{source}] {DateTime.Now:HH:mm:ss} 已发起存档（槽位 {slotIndex}，" +
                    $"遵循区域限制：{respectRestrictZone}，备份数 {GetBackupCount()}）。");

                PlayConfirmSound(source);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"[{source}] 存档过程抛出异常：{e}");
                return false;
            }
        }

        /// <summary>
        /// 清掉「刚读档延迟」，让本次存档必定生成一条备份存档。
        ///
        /// 依据：`SaveSlotBackupsManager.CreateCurrentBackup`
        /// （`Decompiled/ISIL/IsilDump/__mainWisp/SaveSlotBackupsManager.txt`）
        /// 唯一会直接跳过备份的路径就是 `Instance.m_gameJustLoadedDelay > 0`
        /// （该方法自身在成功后会把它重置为 10 秒）。
        /// 不清掉的话，读档后的一段时间内手动存档不会产生新备份，
        /// 表现为「ESC 菜单的备份游戏存档里看不到刚存的档」。
        /// </summary>
        private static void ForceBackupIfNeeded(string source)
        {
            if (!ModConfig.ForceBackup.Value)
            {
                return;
            }

            try
            {
                var backups = global::SaveSlotBackupsManager.Instance;
                if (backups == null)
                {
                    return;
                }

                backups.m_gameJustLoadedDelay = 0f;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"[{source}] 重置备份节流失败（不影响存档本身）：{e.Message}");
            }
        }

        private static int GetBackupCount()
        {
            try
            {
                var backups = global::SaveSlotBackupsManager.Instance;
                return backups != null ? backups.SaveSlotBackupsCount : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 存档后播放游戏的菜单确认音，给玩家一个「确实存上了」的反馈。
        /// 存档本身是静默的，没有这个提示很容易误以为没生效。
        /// </summary>
        private static void PlayConfirmSound(string source)
        {
            if (!ModConfig.PlayConfirmSound.Value)
            {
                return;
            }

            try
            {
                var menu = global::Game.UI.Menu;
                var sounds = menu?.Sounds;
                if (sounds == null || sounds.ConfirmItem == null)
                {
                    return;
                }

                sounds.PlaySoundEvent(sounds.ConfirmItem);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"[{source}] 播放存档提示音失败：{e.Message}");
            }
        }

        /// <summary>
        /// 把复活点更新为玩家当前所站的地面位置，效果与游戏自带存档点一致。
        /// 找不到地面时只写日志，不影响后续存档。
        /// </summary>
        private static void TryUpdateRespawnPoint(string source)
        {
            try
            {
                var sein = global::Game.Characters.Sein;
                if (sein == null)
                {
                    Plugin.Logger.LogInfo($"[{source}] 未找到玩家角色，跳过重生点更新。");
                    return;
                }

                var platformMovement = sein.PlatformBehaviour?.PlatformMovement;
                if (platformMovement == null)
                {
                    Plugin.Logger.LogInfo($"[{source}] 未找到平台移动组件，跳过重生点更新。");
                    return;
                }

                var ori = global::Game.Characters.m_ori;
                Vector3 target = ori != null ? ori.transform.position : sein.transform.position;

                // 优先用游戏自带存档点的贴地逻辑。
                if (global::InvisibleCheckpoint.GetSeinGroundPosition(out Vector3 ground, target, 1f))
                {
                    platformMovement.OverridePositionNextSave(ground);
                    Plugin.Logger.LogInfo(
                        $"[{source}] 重生点已更新为 ({ground.x:F1}, {ground.y:F1}, {ground.z:F1})。");
                    return;
                }

                // 贴地射线失败时退而求其次：直接用角色平台移动的地面射线落点。
                if (platformMovement.GroundRayHit)
                {
                    Vector3 rayPoint = platformMovement.GroundRayPoint;
                    platformMovement.OverridePositionNextSave(rayPoint);
                    Plugin.Logger.LogInfo(
                        $"[{source}] 贴地查找失败，改用地面射线落点 ({rayPoint.x:F1}, {rayPoint.y:F1}, {rayPoint.z:F1})。");
                    return;
                }

                // 最后兜底：直接记录玩家当前位置，避免读档后回到很久以前的存档点。
                platformMovement.OverridePositionNextSave(target);
                Plugin.Logger.LogInfo(
                    $"[{source}] 脚下未探到地面，重生点退化为玩家当前位置 " +
                    $"({target.x:F1}, {target.y:F1}, {target.z:F1})。");
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"[{source}] 更新重生点时出现异常，已跳过：{e.Message}");
            }
        }
    }
}
