# Start Screen UI Appearance and Functionality

本次实现对应截图中的第 10.5 项。标题场景：`Assets/Scenes/StartScene.unity`。

| 要求 | 实现 |
| --- | --- |
| 新建 StartScene，作为游戏第一个场景 | Build Settings 的第一个场景为 StartScene，第二个为 RecreatedLevel。 |
| 自己的标题页风格 | 深蓝校园夜间街机风格，配薄荷绿和杏色，不复刻原版 Pacman 标题页。 |
| PacStudent 标题和创意副标题 | 标题为 PacStudent，副标题为 MIDNIGHT ON CAMPUS。 |
| 展示 PacStudent 与 Ghost 精灵 | 使用项目原有角色精灵，通过 Canvas Image 展示一个学生和四只幽灵。 |
| 明显动画的 Canvas 边框 | 16 个发光方块沿边框持续移动；边框及方块均为 Canvas 下的 RectTransform，按实际边框宽高计算路线。 |
| Level 1 按钮 | 加载 Assessment 3 的 RecreatedLevel 场景。 |
| Level 2 按钮 | 当前未创建 Design Iteration 场景，因此按截图允许的方式保留占位按钮，点击显示即将开放的提示。若之后建立 DesignIteration 场景，重新构建标题页时会绑定并加入场景列表。 |
| 高亮与点击反馈 | 鼠标悬停、键盘选择时变色并放大；按下时缩小并播放点击音效。支持方向键选择和 Enter 确认。 |
| 每关上次最高分和时间 | 两关各有独立显示，初始分数 000000（零），时间 00:00:00。已预留从 PlayerPrefs 读取数据的入口。 |
| Start Scene 背景音乐循环 | 使用 Assets/Audio Clips/StartScene.wav，自动播放、循环，作为 2D 菜单音乐；进入关卡后随标题场景结束。 |
| 16:9 与 4:3 横屏适配 | CanvasScaler 按屏幕尺寸缩放，参考尺寸 1600×900；内容、卡片、角色展示区和边框使用比例锚点。 |

在 Unity 6000.4.11f1 中打开 StartScene 后点击 Play。构建游戏时将自动从 StartScene 开始。`Assets/Editor/StartScenePlayBootstrap.cs` 会在编辑器从其他场景点击 Play 时先切换到 StartScene；项目首次打开时也会默认恢复 StartScene。

场景已保存，可直接在 Hierarchy 中编辑文字、颜色、按钮和音量。菜单 **Tools → PacStudent → Build Start Screen** 可重新生成整个标题页，因此重建前应保存或备份自行调整的版本。所需 Unity UI 包为 com.unity.ugui 2.0.0，已写入 manifest.json 和 packages-lock.json。

最高分读取键为 Level1HighScore / Level2HighScore；时间读取键为 Level1BestTime / Level2BestTime，单位为秒。本次只实现显示和读取，不会把占位零分写回存档。

验证已完成：Unity 编译通过；在 Play Mode 中检查循环音乐、边框随时间移动、按钮悬停和按下反馈、键盘导航与确认、Level 2 占位提示，以及 Level 1 实际进入 RecreatedLevel；未出现运行时错误。Unity 原生相机分别以 1600×900 和 1200×900 渲染，确认 Canvas 比例正确、所有文字矩形都处于可见范围，并目视核对了两个比例的布局。

验证日志为 Logs/startscene-build.log、Logs/startscene-playmode.log；预览为 Logs/startscene-16x9.png 与 Logs/startscene-4x3.png。Logs 不纳入 Git。
