# Framework 接入审查与整改

恢复执行前先读 [存储与恢复规则](STORAGE-AND-RECOVERY.md)。下文安装、Reports 和 Player 路径是历史现场；当前缺失状态及官方保留范围以该规则和只读审计为准。本地副本校验不是跨恢复持久性保证。

当前依赖已改为 alone-jam fork 的固定修复提交，Unity 消费者回归待验；下文旧官方 pin 的测试和 Player 均为历史证据。历史实际 Unity 6000.6.5f1：FirstRep EditMode 3/3、PlayMode 5/5 通过。上游相关测试 22/23 通过，一项历史碰撞测试断言失败，详见下文。生产 Player 构建与画面验收单独记录，不能由无图形测试推断。

## 依赖来源

`Packages/manifest.json` 与 `packages-lock.json` 固定 `https://github.com/alone-jam/EjoyFramework.git#093a175b60a196e2ad864a01ace49e90f58aacc3`，对应 fork 的 `firstrep-integration` 分支。以完整 SHA 固定，不随分支移动自动升级。无嵌入 Framework，无包缓存补丁。许可保留在 `ThirdPartyNotices/EjoyFramework/LICENSE.md`。

该提交基于上游 `3d00472607e4b7d180358a571e79aa7bee6ddfa4`，包含非凸静态重叠查询测试几何修正（[PR #19](https://github.com/one2world/EjoyFramework/pull/19)）和弹道重力、引信、碰撞半径、复制纠正修复（[PR #20](https://github.com/one2world/EjoyFramework/pull/20)）。两项 PR 尚未合并；本地采用不等待维护者批准。

后续流程：在 fork 分支修复并添加回归，独立向上游提交 PR；把验证过的修复集成到 `firstrep-integration`，再同步更新 FirstRep 的 manifest、lock、依赖检查常量及来源记录。保留 PR 与提交映射，后续按需同步上游，避免重复应用已合并补丁。

本次记录见 [fork-adoption-validation.json](fork-adoption-validation.json)。此次验证：真实 Framework 纯 .NET Combat 回归 47/47 通过，不使用 Unity stub；同一测试集合对修复前代码有 14 项失败。该结果不能证明 Unity 物理测试、UPM 实际解析、FirstRep EditMode/PlayMode 或新 Player 通过。Unity 6000.6.5f1/revision 3ff58d469c8a Editor 随后已恢复，但实际依赖验证启动因无有效许可证退出 198，未完成这些检查（见 [最新证据](editor-recovery-validation-20261020.json)）；下文 3/3、5/5、22/23、构建与 GIF 结果仅属于旧官方 pin。框架升级后须重新生成 AssetBundles 并完整构建 Player，不能用旧 Player 或仅 BuildScriptsOnly 作为新依赖的验证。


## 具体对应清单

旧实现指已推送的 `1ca0af8eec36a5c5f801df5e1c528d7edd7c7a29`；整改指当前工作区。不能用运行成功代替下列模块接入证据。

| 范围 | 旧文件及实现 | 当前接入的真实 Framework 能力 | 验证边界 |
|---|---|---|---|
| 输入 | `SliceBootstrap.Awake` 选 LegacyInputHelper；`FpsPlayer.Start` 已使用 IInputManager，原本并非手写输入管理器 | 默认 InputComponent/NewInputSystemHelper；绑定、Gameplay/Menu context 仍由 IInputManager 处理 | Mouse0/1 的实际 Input System 设备 held/down/up EditMode 回归通过；OS 录屏输入待后续验证 |
| 场景 | `SliceBootstrap` 已通过 ISceneManager/DefaultSceneHelper 加载，手写 started/failed 聚合推进 | SceneLease 持有真实 additive 场景；持久 SliceSessionHost 等待终态后才释放 AssetBatch/Framework | 原版三个场景真实加载；生产退出两轮后回到基线场景数，加载中销毁 Bootstrap 的晚到加载也正确卸载 |
| 流程/暂停 | `SliceBootstrap` 的布尔推进；`FpsPlayer.Pause/Resume` 自管 paused 与 Time.timeScale | IProcedureManager/ProcedureBase：Load/Play/Pause/Failed；BaseComponent.PauseGame/ResumeGame | 真实流程切换、时间缩放及 UI 状态断言通过；保留实例别名 fail-fast |
| 玩家/靶子 | `SliceBootstrap.OnLoaded` 直接 new GameObject、Instantiate；TargetPractice 自管 Renderer/Collider 显隐 | EntityComponent/IEntityManager + FpsPlayer/TargetPractice : EntityLogic，OnInit/OnShow/OnUpdate/OnHide/OnRecycle | 单靶子隐藏/池复用复活通过；三个相同预制体物理实例独立、隐藏其中一个与空闲池复用通过 |
| 命中特效 | `FpsPlayer.Fire` 直接 Instantiate，延时 Destroy | ImpactEffect : EntityLogic，ShowEntity/HideEntity；ITimerManager 管 expiry | 并发同预制体特效的独立 Transform、隐藏与生命周期通过 |
| 资源 | SliceContent 的直接 prefab/audio 引用被业务 Instantiate/PlayOneShot 消费 | ResourceComponent/IResourceManager + AssetBatch 预载并 Pin；Entity/UI/Sound 从路径加载 | 单靶子测试确认 CachedAssetCount 至少 6；序列化原引用保留为作者工具/迁移审查输入，非实例化后备路径 |
| UI | `FpsPlayer.OnGUI` 同时承担 HUD、菜单和暂停状态 | FpsHudForm/FpsPauseForm : MvvmView<TViewModel>；BindableObject、TextBinder、ButtonBinder、RelayCommand 与已有 Bind/Unbind 管理文本/命令 | 真正绑定的 uGUI 文本与按钮通过；关闭后的旧模型不再写入 View，复用生成新模型，RELOADING 显示/清除通过 |
| 音频 | `FpsPlayer.Start/Fire` 直接新建 AudioSource/PlayOneShot | SoundComponent/ISoundManager 与 Weapons 组加载播放 | 单次射击调用路径通过；环境无实际音频输出设备，听感未验 |
| 移动 | `FpsPlayer.Update` 手写速度、重力、跳跃及 CharacterController.Move | ThreeC.ThirdPersonMotor 复用加减速/重力/跳跃；CharacterControllerStepper 处理 Unity 移动/位置恢复 | 单靶子实际原场景的移动与跳跃断言通过；FPS 仅决定输入缩放和相机朝向 |
| 计时/事件/属性 | 原本已使用 ITimerManager、EventChannel<TargetHit>、AttributeSet | 保留并在 EntityLogic 生命周期解绑/回收 | 单靶子真实射击伤害、计时换弹和复活通过 |
| FPS 专有业务 | WeaponMagazine 弹匣规则、Unity 物理遮挡射线、相机瞄准/原 rig 对齐、原动画 clip 切换 | 保留领域规则与 Unity 物理/动画适配；未再建立输入、资源、实体池、事件总线、流程机或 UI 栈 | 不宣称这些业务函数本身来自 Framework，也不把它们作为泛用基础设施替代 Framework |

作者侧预制体配置在 `FrameworkAssetSetup` 中创建，使用原版 rig/粒子/Robot，未替换模型、贴图或音频。Player 构建前调用上游 `AssetBundleBuilder.BuildAll`，保留原有材质组合策略；未降低美术资源质量。生成的 AssetBundles 不进入 Git。

## 官方修复与实际结果

官方提交 [a5e1ec6](https://github.com/one2world/EjoyFramework/commit/a5e1ec6f3472f741815108509ba303ce39b933c3) 使用独占实例池修复 [#17](https://github.com/one2world/EjoyFramework/issues/17)，以 Collider 身份键修复 Unity 6000.6 编译兼容，并明确拒绝未支持的 Resources 模式（[#16](https://github.com/one2world/EjoyFramework/issues/16)）。FirstRep 继续使用上游 EditorSimulation/AssetBundle 路径。

消费者 PlayMode 5 项均通过：单靶子模块/玩法与 MVVM；三个同预制体实体并发、独立隐藏和空闲池复用；并发命中特效独立 Transform/生命周期；实际生产退出和两轮重进；原生加载中销毁 Bootstrap 后卸载。EditMode 的显式实体 ID、缺失 ID 拒绝及实际 Input System 鼠标设备 3 项通过。测试使用真实原场景、Unity 物理及 Framework；`-nographics` 不验证画面或听感。

独立项目运行同一官方 Git 包的 EntityManager、DefaultEntityHelper、ResourceComponentMode、StaticPhysicsHistoricalQuery 相关测试：22/23 通过。`UnregisteredColliderInStaticLayerIsRejected` 第 66 行射线拒绝断言通过，第 67 行从非凸 MeshCollider 中心出发的小球查询未抛出异常。未修改上游缓存或跳过该测试；额外实际 Unity 探针确认：非凸网格中心的 OverlapSphere=0/Sweep=Miss；表面相交或改为凸碰撞体时均正确抛出 InvalidOperationException，指向该断言的几何假设，而非身份键修复回归。已补充 [现有 #3](https://github.com/one2world/EjoyFramework/issues/3#issuecomment-6195904536)。FirstRep 当前射击使用 Unity 实时物理射线，不调用 StaticPhysicsHistoricalQuery；其消费者测试已独立通过。

## 可复验的消费者验证

结构化结果及每个 XML 的 SHA-256 见 [official-framework-validation.json](official-framework-validation.json)。完整原始 XML/日志保存在本地 Reports：`official-a5e1ec6-editmode.xml`、`official-a5e1ec6-playmode-preserved.xml`、`official-a5e1ec6-upstream-unity.xml`。无凭据、许可证、缓存或构建二进制进入源码。

## 历史官方 pin 的生产构建与画面验证（2026-10-16）

在原环境 `/workspace/FirstRep` 只读核对路径后重新生成了 2 个 bundle、9 项资源和 manifest。同一官方 pin 的完整 Linux Player 已成功构建：Errors=0、Warnings=86、Time=01:23:35.0681140，Editor 正常退出 0。此前失败没有被当作成功，新结果见 [current-player-validation.json](current-player-validation.json)，当前 bundle 大小和哈希见 [production-validation.json](production-validation.json)。完整构建之后仅用 Unity 支持的 BuildScriptsOnly 更新脚本；`data.unity3d`、UnityPlayer、Ejoy 程序集和 bundle manifest 的哈希保持一致。

新可执行文件发现应用退出时 Framework 已关闭而 Bootstrap 再请求模块的异常；已增加应用退出标志，常规生产退出与场景重进路径保留。实际 PlayMode 回归 5/5，新 Player 无图形 smoke 退出 0、无该异常，并确认 4 场景、3 靶子与地面；Null Device 报告不证明画面。

新 Player 最初在实际 X11 中显示黑色三维场景加可见 HUD。逐项诊断确认仅关闭 Bloom 即能显示场景；最终修复采用 HDRP 自带清理通道：同时启用相机 `stopNaNs` 和 `FrameSettingsField.StopNaN`，清理 NaN/Infinity 后再执行后处理。仅有帧设置开关并不会启用该通道。最终正常配置不带效果禁用参数，已实际显示场景和武器，Bloom、自动曝光、ACES、调色、运动模糊、次表面散射均保留。

该修复消除的是整屏黑传播，未定位全部无效颜色的原始着色原因；已有粉绿材质和武器条纹仍在，不能声称还原画质验收通过。更高精度、调色、曝光等试验均未被当作最终修复；临时精度/纹理读回实验代码已移除。真实窗口 GIF 已嵌入 README：96 秒录制、640×360、8 倍速播放；实际 OS 输入触发 30→28 发、RELOADING、30/177 与移动，所有正常效果保留。原始状态轨迹和 GIF 哈希见 [runtime-capture-validation.json](runtime-capture-validation.json)。软件渲染原生约 0.2 fps，不能当作性能验收。音频听感仍因无输出设备而未验。

`Builds` 仍指向 `/tmp/firstrep-build-output`，临时源保留；新版已另存到真正的工作区目录 `Deliverables/FirstRep-Linux-6000.6.5f1-b5c35ad`，321 个构建文件共 2,073,935,781 字节，逐文件 SHA-256 与临时源一致。启动脚本优先使用新版持久路径；完整哈希与检查结果见 [persistent-player-validation.json](persistent-player-validation.json)。

用户分别授权删除旧 Player 目录及 `CachePreservation~/PlayerDataCache-before-a5e1ec6/Linux642/Data/data.unity3d`，两者原本共用主数据 inode，只有两处都删除才释放该数据占用。实际仅删除这两个精确路径；原始资源、代码、其他缓存和新版临时源保留。更早获准清理的两个 `/tmp` 路径在环境恢复时已不存在，与本次实际删除不是同一次操作。

## 历史失败与复验入口

早期完整构建在 HDRP Forward 阶段遇到 OOM，之后低并发重试通过该阶段却在 LZ4HC 打包时耗尽临时盘；这些是历史尝试，详见 [low-memory-build-validation.json](low-memory-build-validation.json)。不能将删除磁盘缓存等同于解决内存上限。新完整构建实际观察到最多两个 ShaderCompiler 进程；cgroup current 含可回收文件缓存，不能据峰值声称最低所需内存。

`Tools/run_low_memory_build.py --stage bundles` 生成资源；`--stage player` 验证当前 bundle 平台、大小、MD5 和当前固定 pin 后完整构建；`--stage scripts` 仅用于已有完整 Player 且没有资源变更的脚本更新。完整构建仍执行项目 Validate，脚本入口不会冒称重新验证/打包全部美术资源。参数依据 [Unity 官方命令行文档](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)；脚本更新机制见 [BuildScriptsOnly](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/BuildOptions.BuildScriptsOnly.html)。
