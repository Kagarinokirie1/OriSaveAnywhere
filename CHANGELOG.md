# 更新记录

## 0.2.0

- 修复「ESC 菜单里点了存档却看不到备份」。
  - 原因：`SaveSlotBackupsManager.CreateCurrentBackup()` 在 `Instance.m_gameJustLoadedDelay > 0`
    时直接返回（这是该方法唯一会跳过备份的分支），而该延迟在备份成功后被重置为 10 秒、
    读档时还会被 `SetupGameLoadedDelay` 拉长，所以刚读档那段时间手动存档不会生成备份。
  - 修复：存档前把 `m_gameJustLoadedDelay` 清零，绕过该节流；新增配置项 `每次存档都生成备份存档`。
- 新增存档反馈：存档成功后播放游戏的菜单确认音（`UISoundSettingsAsset.ConfirmItem`），
  解决「点了没反应、以为没生效」；新增配置项 `存档后播放提示音`。
- 修复复活点在部分地形下不更新：`InvisibleCheckpoint.GetSeinGroundPosition` 在部分位置返回
  `false`，导致只保存了进度、复活点没变。现在按
  「贴地查找 → 角色地面射线落点 `CharacterPlatformMovement.GroundRayPoint` → 玩家当前位置」依次兜底。
- 日志增强：存档日志带上本地时间与当前备份数量，便于和存档文件时间戳对照。

## 0.1.0

- 首个版本。
- ESC 暂停菜单内注入「存档」按钮，支持手柄/键盘导航。
- 支持 `F1` 快捷键存档。
- 存档复用游戏自带的 `GameController.CreateCheckpoint` 管线，主存档与「备份游戏存档」都会正常生成。
- 新增「忽略禁止存档区域」「同时把重生点更新到当前位置」两个可配置开关。
