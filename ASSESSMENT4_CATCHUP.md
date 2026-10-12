# Assessment 4：Catch-up from Assessment 3

本次修改对应提供截图中的第 7 项。正式关卡为 `Assets/Scenes/RecreatedLevel.unity`，已加入 Build Settings，作为第一个且唯一启用的场景。

| 截图要求 | 已完成的修改 |
| --- | --- |
| 补齐至手工关卡布局 | 保留已保存的 `Level01_Manual` 墙体、普通豆和四颗能量豆；修正墙体转角与直线方向；移除重复布局和会在运行时删除手工关卡的生成器组件。 |
| 幽灵放在中央出生区 | `GhostGroup` 中保留四只可见幽灵，位置分别为 `(-1.5, 0.5)`、`(-0.5, 0.5)`、`(0.5, 0.5)`、`(1.5, 0.5)`。 |
| PacStudent 放在左上角 | `PacStudent` 起点为 `(-12.5, 13.5)`，位于左上角通道内。 |
| 能量豆闪烁动画播放 | 手工关卡中的四颗 `PowerPellet` 均启用 `PowerPelletAnimator.controller`，默认播放循环的 `Pellet_Flash.anim`。 |
| 移除 Assessment 3 的移动组件 | 场景不再挂载 `PacStudentMovement`；原脚本保留于 Project，方便参考。 |
| 移除幽灵动画循环，保留动画资源 | 场景幽灵不再挂载 Animator；`Assets/Animation` 中的幽灵动画、Controller 与 Override Controller 均保留。PacStudent 也使用静态起始精灵，避免旧演示 Controller 自动切换方向和死亡状态。 |
| 移除多余示例精灵 | 移除正式关卡中的 `VisualAnimations` 演示组、樱桃和生命图标示例，以及重复墙体和豆子；`VisualAssets_Showcase.unity` 作为历史资源展示保留，不加入正式场景列表。 |

用 Unity 6000.4.11f1 打开项目，打开 `RecreatedLevel.unity` 后点击 Play 即可查看。角色停留在起点，四颗能量豆持续闪烁。

如需重新应用本次准备步骤，可使用菜单 **Tools → PacStudent → Prepare Assessment 4 Catch-up**。该工具修改并保存正式关卡；会先提示保存当前未保存的场景。后续实现 Assessment 4 新移动功能后，不应再次运行这个准备工具，以免移除角色上的旧移动组件或重置起点。

验证：Unity 编译通过；静态场景检查通过；实际 Play Mode 中观察两秒，PacStudent 与四只幽灵位置保持不变，四颗能量豆均切换过两个有效闪烁帧，手工关卡保持存在。检查日志在 `Logs/catchup-unity.log` 和 `Logs/catchup-playmode.log`，Unity 相机预览在 `Logs/catchup-preview.png`（Logs 不纳入 Git）。

截图未包含 Assessment 3 的完整评分细则，因此本说明仅记录本次截图条目及实际验证结果。
