# OriSaveAnywhere

《奥日与精灵意志》（Ori and the Will of the Wisps）的**随时存档** Mod。

不用再跑回存档点：按下 `ESC` 打开暂停菜单，里面会多出一项「存档」，选中按下即可存档；
也可以直接按快捷键 `F1`。

存档完全走游戏自带的存档管线，因此主存档与「备份游戏存档」都会正常生成，
效果和在游戏里踩存档点完全一致。

## 功能特性

- **ESC 菜单里的「存档」按钮**：运行时注入到暂停菜单中，外观、高亮动画、手柄/键盘导航都与原菜单项一致。
- **快捷键存档**：默认 `F1`，可在配置里改成任意按键或关闭。
- **走原生存档管线**：调用游戏自身的 `GameController.CreateCheckpoint`，因此
  - 主存档 `saveFile<N>.uberstate` 正常写入；
  - `SaveSlotBackupsManager.CreateCurrentBackup` 正常执行，会生成「备份游戏存档」；
  - 与游戏自带存档点写出的文件格式完全一致。
- **忽略禁止存档区域**：Boss 战、逃亡段等区域游戏会禁止创建检查点，本 Mod 可绕过（可关闭）。
- **同步更新复活点**：存档时把复活点记录到当前所在位置（自动贴地），读档后从存档地点继续。
  贴地查找失败时依次退回角色地面射线落点、玩家当前位置，尽量避免读档后回到很久以前的存档点。
- **存档反馈**：存档成功后播放游戏的菜单确认音，避免「静默存档、点了以为没反应」。
- **强制生成备份**：绕过游戏「刚读档一段时间内不生成备份」的节流，每次手动存档都写一条备份（可关闭）。

## 环境要求

| 项目 | 要求 |
| --- | --- |
| 游戏 | Ori and the Will of the Wisps（Steam / PC 版） |
| Unity | 2018.4.24f1，IL2CPP |
| Mod 框架 | BepInEx **6.0.0-be.755**（IL2CPP x64 版）+ Il2CppInterop |
| 运行时 | .NET 6.0 |

## 安装

1. 先按 BepInEx 6（IL2CPP）的常规方式给游戏装好 BepInEx，确认 `BepInEx/interop/` 已生成。
2. 从 [Releases](../../releases) 下载 `SaveAnywhere.dll`，放进：

   ```
   <游戏根目录>/BepInEx/plugins/SaveAnywhere.dll
   ```

3. 启动游戏。日志出现 `随时存档 v0.2.0 已加载` 即安装成功。

配置文件在 `<游戏根目录>/BepInEx/config/oriwotw.saveanywhere.cfg`，首次启动后自动生成。

## 使用

1. 进入游戏，按 `ESC` 打开暂停菜单。
2. 菜单里会多出一项「存档」，选中按下即可存档，会听到一声确认音。
3. 也可以随时按 `F1` 存档。
4. 存档后可在 `ESC` 菜单 →「备份游戏存档」里看到新生成的备份条目。

> 备份位只有 10 个且会轮换，频繁存档会把较旧的备份挤掉，这是游戏本身的机制。

## 配置项

配置文件：`BepInEx/config/oriwotw.saveanywhere.cfg`

| 分组 | 配置项 | 默认 | 说明 |
| --- | --- | --- | --- |
| 通用 | `启用` | `true` | 是否启用本 Mod |
| 通用 | `在ESC菜单中添加存档按钮` | `true` | 是否往暂停菜单注入「存档」菜单项 |
| 通用 | `存档按钮文字` | `存档` | 菜单项显示文字 |
| 通用 | `存档快捷键` | `F1` | 随时存档的快捷键，设为 `None` 关闭 |
| 存档 | `忽略禁止存档区域` | `true` | 是否无视 Boss 战等区域的禁止存档限制 |
| 存档 | `同时把重生点更新到当前位置` | `true` | 存档时是否把复活点记录到当前位置 |
| 存档 | `每次存档都生成备份存档` | `true` | 是否绕过游戏的备份节流，每次都写一条备份 |
| 存档 | `存档后播放提示音` | `true` | 存档成功后是否播放菜单确认音 |

## 从源码构建

构建依赖游戏目录下由 BepInEx 生成的 interop 程序集，因此需要本机已装好带 BepInEx 的游戏。

1. 用文本编辑器打开仓库根目录的 `Directory.Build.props`，把 `GameDir` 改成你自己的游戏根目录
   （也可以用环境变量 `ORIWOTW_DIR` 覆盖，避免改文件）。
2. 构建：

   ```powershell
   dotnet build src/SaveAnywhere/SaveAnywhere.csproj -c Release
   ```

3. 产物在 `src/SaveAnywhere/bin/Release/SaveAnywhere.dll`。
   也可以直接用脚本一步构建并复制到游戏目录：

   ```powershell
   pwsh -File scripts/build.ps1
   ```

## 实现原理

反编译结论（含每条结论对应的 ISIL 证据位置）整理在
[`docs/暂停菜单与存档系统.md`](docs/暂停菜单与存档系统.md)。要点：

- **暂停菜单结构**：ESC 菜单不是代码拼的，而是场景 `Assets/scenes/inGameUI.unity`
  （构建索引 `oriwotw_Data/level303`）里的预制实例：`pauseScreen2 → faderRoot → items`，
  `items` 上挂 `CleverMenuItemLayout`，每个菜单项挂一个 `CleverMenuItem`，
  文字是子物体上的 `MessageBox`。
- **注入方式**：克隆一个已有菜单项，清掉模板自带的 `Pressed`/`Visible`/`Activated`，
  只挂 `PressedCallback`，再追加进 `CleverMenuItemSelectionManager` 与 `CleverMenuItemLayout`
  并调用 `Sort()` 重排；文字用 `MessageBox.OverrideText` + `RefreshText()` 覆盖。
- **存档入口**：`GameController.CreateCheckpoint(true, respectRestrictCheckpointZone)`
  → `SaveGameController.PerformSave()` → `SaveSlotBackupsManager.CreateCurrentBackup()` + `SaveToFile(...)`。
- **复活点**：按游戏自带存档点的做法，先 `InvisibleCheckpoint.GetSeinGroundPosition` 贴地，
  再 `PlatformMovement.OverridePositionNextSave`，最后才创建检查点。

## 兼容性说明

- 与同作者的地图双击传送 Mod 可同时使用（日志中可见两者互不干扰）。
- 存档写入路径与游戏本体一致，Steam 云同步不受影响。
- 已知限制：游戏自带存档点在找不到地面时会放弃创建检查点；本 Mod 在这种情况下仍会保存进度，
  但复活点只能退化处理（见上文「同步更新复活点」）。

## 更新记录

见 [CHANGELOG.md](CHANGELOG.md)。
