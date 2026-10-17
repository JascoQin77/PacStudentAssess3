# In-game UI

本次实现对应图片中的第 14 项，正式关卡为 `Assets/Scenes/RecreatedLevel.unity`。

`HUD` 是一个 Screen Space Camera Canvas，使用 `CanvasScaler.ScaleWithScreenSize`，参考分辨率为 1600x900，并在 16:9 和 4:3 横屏下验证。顶部 HUD 包含：

- `Lives`：场景中保存 3 个分开排列的生命图标，不使用数字显示生命。`life indicator full.png` 是 Assessment 3 原始 `life indicator.png` 的第一个完整红心帧（左上角 36×32 像素）；原图保持不变。
- `Score`：六位零填充数字，初始为 `000000`。
- `Game Timer`：`mm:ss:msms` 格式，初始文本为 `00:00:00`，运行时从零开始累计。
- `Ghost Scared Timer`：默认隐藏；通过 `InGameHUDController.BeginScaredTimer(seconds)` 开始显示整数秒倒计时，归零后再次隐藏。
- `Exit Button`：返回 Build Settings 中的 `StartScene`。
- `Level Name`：显示 `LEVEL 1`。

四个幽灵分别有一个子 Canvas：`Ghost1Canvas`、`Ghost2Canvas`、`Ghost3Canvas`、`Ghost4Canvas`。它们均为 World Space Canvas，直接挂在 `GhostRed`、`GhostPink`、`GhostBlue`、`GhostOrange` 下，每个 Canvas 只有一个可读的数字文本，分别显示 1、2、3、4。

运行控制脚本为 `Assets/Scripts/InGameHUDController.cs`，编辑器生成工具为 `Assets/Editor/BuildInGameHUD.cs`。可通过 Unity 菜单 **Tools → PacStudent → Build In-Game HUD** 重建 HUD；重建前请保存当前场景，因为该工具会删除旧 HUD 和旧幽灵标签后重新生成。

其他玩法脚本可以直接调用：

```csharp
var hud = FindFirstObjectByType<InGameHUDController>();
hud.AddScore(100);
hud.SetLives(2);
hud.BeginScaredTimer(10f);
hud.ClearScaredTimer();
```

`Assets/Editor/HUDVerification.cs` 提供 Unity Batch Mode / Play Mode 集成检查（`-executeMethod HUDVerification.Run`，不加 `-quit`，检查结束会自动退出）。验证了保存场景中的初始文本与隐藏状态、三个独立完整红心及其不重叠的位置、六位分数、百分之一秒时间格式、生命数更新、Scared Timer 倒计时与归零隐藏、四个幽灵的子 Canvas 和单个数字文本，以及鼠标射线首先命中 EXIT 后通过指针事件加载 StartScene。

画面检查使用 Unity 相机分别渲染 1600×900 和 1200×900 的 RenderTexture，并按 CanvasScaler 相同的公式设置目标画布缩放。检查了迷宫边界完整位于视口内且在 HUD 下方，以及生命图标和幽灵数字的屏幕尺寸；同时人工检查两张渲染图。预览文件在 `Logs/hud-16x9.png` 和 `Logs/hud-4x3.png`，通过日志在 `Logs/hud-verified.log`。

开发修改原先保存在桌面的 `C:\Users\Lenovo\Desktop\Introduction  to computer game development\Labs\PacStudentAssess3`，现已按要求同步到 `D:\Introduction to computer game development\PacStudentAssess3`。日常可直接打开 D 盘项目中的 `Assets/Scenes/StartScene.unity` 或 `Assets/Scenes/RecreatedLevel.unity`。两份项目有独立的 Git 仓库和 Unity 缓存，后续修改应以实际打开的项目路径为准。
