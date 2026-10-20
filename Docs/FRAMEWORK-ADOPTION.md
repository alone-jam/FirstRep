# Framework 接入审查与整改

恢复执行前先读 [存储与恢复规则](STORAGE-AND-RECOVERY.md)。下文安装、Reports 和 Player 路径是历史现场；当前缺失状态及官方保留范围以该规则和只读审计为准。本地副本校验不是跨恢复持久性保证。

当前固定 alone-jam fork `c36cc4fff162994ec7f05f47a2f30cc0ec4e414c`，含 [Prefab/场景职责修复 PR #21](https://github.com/one2world/EjoyFramework/pull/21)。FirstRep 现采用官方完整 Prefab 的配置 variant、ProcedureComponent 启动、GameEntry、8 个标准 UI 分组，以及官方按钮自动绑定和流程/MVVM 生成工具；此前手工模块/根节点/事件系统/分组接线及手写访问器登记已移除。现有玩法具备启动菜单 → 进入 → 暂停或加载失败 → 返回菜单 → 再进入闭环，Retry/Exit 保留。真实 Unity 6000.6.5f1 EditMode **9/9**、PlayMode **19/19**；见 [本批复用清单、累计矩阵与边界](FRAMEWORK-PREFAB-MENU-20261020.md)、[证据](framework-prefab-menu-validation-20261020.json) 和 [脱敏清单](evidence/framework-prefab-menu-20261020/manifest.json)。

此前 093a175 fork 的原生物理查询 7/7、源码 `9a7c412` 的完整 Player/无图形启动保持为历史结果，本轮没有重复上游全套测试或大型构建。旧 Player 不覆盖本轮增量。画面、实际听感和性能未验收；Release 上传排障已按优先级暂停，原包保留。下文旧官方 pin 的结果均为历史。

## 依赖来源

manifest、lock、依赖守卫和来源记录固定 `https://github.com/alone-jam/EjoyFramework.git#c36cc4fff162994ec7f05f47a2f30cc0ec4e414c`，对应 firstrep-integration。未嵌入 Framework、未改包缓存；官方包提供的根 Prefab 以配置 variant 引用，GUID/来源可追溯。

组合提交保留 PR [#19](https://github.com/one2world/EjoyFramework/pull/19)、[#20](https://github.com/one2world/EjoyFramework/pull/20) 的原修复，并集成 [#21](https://github.com/one2world/EjoyFramework/pull/21)：Prefab 的旧 helper 引用/零缩放、EditorSimulation 与默认场景 helper 的重复原生场景职责，以及异步启动可查询的 IsInitialized。修复分支独立基于上游基础提交，不夹带 #19/#20；没有合并 PR。

用户流程规则：先核对 Framework 的 Prefab、作者工具及完整使用路径，再决定业务配置；缺陷和缺失通用能力在 fork 修复、补回归、推送并提交上游 PR。FirstRep 直接采用验证后的完整 fork SHA，同步四处来源记录，不等待维护者批准。FPS 专用资源、文案、弹匣和瞄准规则留在游戏侧，不另造通用框架设施。

下文旧 pin 的物理/.NET 和 Player 结果保持历史，不能当新 pin 全套上游或新 Player 的结果。

先前纯 .NET 验证见 [fork-adoption-validation.json](fork-adoption-validation.json)：真实 Framework Combat 回归 47/47 通过，不使用 Unity stub；同一集合对修复前代码有 14 项失败。随后 Editor 恢复后的无许可证退出 198 已保留在 [历史恢复证据](editor-recovery-validation-20261020.json)。用户完成官方登录并明确授权 Personal 激活后，本次真实 Unity 依赖及消费者、原生物理回归全部通过。原生 7 项包含非凸中心无表面重叠控制、表面重叠的未注册碰撞体拒绝及凸碰撞体包含起点的拒绝，补验了 PR #19 的几何修正。没有修改包缓存或跳过该测试。

首次许可恢复后的运行因默认 UPM 缓存 `/home/agent/.cache/Unity/upm/db` 不可写退出 1；启动器现按清单设置可写 `UPM_CACHE_ROOT` 及 Unity 专用 XDG 默认值，并保留调用方覆盖。成功运行和失败尝试的命令、退出码、XML 各项状态和原始证据 hash 均在 [current-fork-validation.json](current-fork-validation.json)。PlayMode 自动清空 HDRP 全局设置的运行时引用列表，已另存生成文件和差异，再还原为测试前的原始版本；Assets 无 Git 差异。原始日志/XML 仅本地暂存；经用户明确授权的八份脱敏副本及原件/副本哈希见 [公开证据清单](evidence/fork-093a175-unity-20261020/manifest.json)，副本明确标记脱敏范围，不能冒充原件。原件外部备份未配置。框架升级后仍须重新生成 AssetBundles 并完整构建 Player，不能用旧 Player 或仅 BuildScriptsOnly 验证新依赖。


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
| UI/HUD 事件 | `FpsPlayer.OnGUI` 同时承担 HUD、菜单和暂停状态；后续迁入 MVVM 后仍逐帧 Refresh | FpsHudForm/FpsPauseForm : MvvmView<TViewModel>；IEventManager.EventChannel<PlayerHudChanged> 快照驱动 BindableObject/TextBinder；ButtonBinder/RelayCommand 管理命令 | 初始/关闭期间状态快照、射击/换弹/击杀/标记到期均更新真实 uGUI；空闲无事件、拒绝动作无重复通知、三轮暂停恢复及两轮生产重进订阅清理通过 |
| 音频 | `FpsPlayer.Start/Fire` 直接新建 AudioSource/PlayOneShot | SoundComponent/ISoundManager 与 Weapons 组加载播放 | 单次射击调用路径通过；环境无实际音频输出设备，听感未验 |
| 移动 | `FpsPlayer.Update` 手写速度、重力、跳跃及 CharacterController.Move | ThreeC.ThirdPersonMotor 复用加减速/重力/跳跃；CharacterControllerStepper 处理 Unity 移动/位置恢复 | 单靶子实际原场景的移动与跳跃断言通过；FPS 仅决定输入缩放和相机朝向 |
| 计时/事件/属性 | 原本已使用 ITimerManager、EventChannel<TargetHit>、AttributeSet | 保留并在 EntityLogic 生命周期解绑/回收 | 单靶子真实射击伤害、计时换弹和复活通过 |
| 演员加载失败 | LoadOriginalLevel 只轮询 EntityCount，失败时可能永远等候；失败流程仅暂停 | 订阅/解绑 IEntityManager.ShowEntityFailure，立即复制池化参数；ProcedureBase 失败状态复用 UIManager、MvvmView 和命令/文本绑定器，取消实体/批次并保留生产 Exit 所有权；RelayCommand/ButtonBinder/VisibilityBinder 提供 Retry | InstantiateEntity/CreateEntity 和真实预载路径缺失均显示原因；连续失败可重试，恢复资源配置后可成功进入；重复请求/取消/旧会话清理通过。菜单自身缺失和业务 OnInit/OnShow 异常契约尚未覆盖 |
| 武器消费/冷却/换弹门槛 | FpsPlayer 用 nextShot/Time.time 控制射速，WeaponMagazine 直接 --Rounds | WeaponMagazine 组合 AbilityDefinition.WithCooldown/AddCost/BlockedBy、AbilitySystem.TryActivate/Tick、GameplayTagSet；EntityLogic 更新拥有者时间 | 拒绝不扣弹、暂停冻结、实际换弹持续开火阻挡、实体池复用重置均通过；容量/备用弹药/换弹转移仍为 FPS 业务 |
| 靶子生命/伤害/死亡 | 用 AttributeSet 存数字，TargetPractice 自行减血/钳制/判死 | UnitModel.ConfigureHealth/TakeDamage/OnDamaged/OnDied，桥接既有 EventChannel、EntityManager 和 TimerManager | 真实射击携带 Entity.Id、过量伤害钳制、死亡计数、旧模型解绑和旧复活计时器取消通过；没有宣称接入完整 UnitWorld |
| FPS 专有业务 | WeaponMagazine 弹匣规则、Unity 物理遮挡射线、相机瞄准/原 rig 对齐、原动画 clip 切换 | 保留领域规则与 Unity 物理/动画适配；未再建立输入、资源、实体池、事件总线、流程机或 UI 栈 | 不宣称这些业务函数本身来自 Framework，也不把它们作为泛用基础设施替代 Framework |

作者侧预制体配置在 `FrameworkAssetSetup` 中创建，使用原版 rig/粒子/Robot，未替换模型、贴图或音频。Player 构建前调用上游 `AssetBundleBuilder.BuildAll`，保留原有材质组合策略；未降低美术资源质量。生成的 AssetBundles 不进入 Git。

## 历史官方修复与实际结果

官方提交 [a5e1ec6](https://github.com/one2world/EjoyFramework/commit/a5e1ec6f3472f741815108509ba303ce39b933c3) 使用独占实例池修复 [#17](https://github.com/one2world/EjoyFramework/issues/17)，以 Collider 身份键修复 Unity 6000.6 编译兼容，并明确拒绝未支持的 Resources 模式（[#16](https://github.com/one2world/EjoyFramework/issues/16)）。FirstRep 继续使用上游 EditorSimulation/AssetBundle 路径。

消费者 PlayMode 5 项均通过：单靶子模块/玩法与 MVVM；三个同预制体实体并发、独立隐藏和空闲池复用；并发命中特效独立 Transform/生命周期；实际生产退出和两轮重进；原生加载中销毁 Bootstrap 后卸载。EditMode 的显式实体 ID、缺失 ID 拒绝及实际 Input System 鼠标设备 3 项通过。测试使用真实原场景、Unity 物理及 Framework；`-nographics` 不验证画面或听感。

独立项目运行同一官方 Git 包的 EntityManager、DefaultEntityHelper、ResourceComponentMode、StaticPhysicsHistoricalQuery 相关测试：22/23 通过。`UnregisteredColliderInStaticLayerIsRejected` 第 66 行射线拒绝断言通过，第 67 行从非凸 MeshCollider 中心出发的小球查询未抛出异常。未修改上游缓存或跳过该测试；额外实际 Unity 探针确认：非凸网格中心的 OverlapSphere=0/Sweep=Miss；表面相交或改为凸碰撞体时均正确抛出 InvalidOperationException，指向该断言的几何假设，而非身份键修复回归。已补充 [现有 #3](https://github.com/one2world/EjoyFramework/issues/3#issuecomment-6195904536)。FirstRep 当前射击使用 Unity 实时物理射线，不调用 StaticPhysicsHistoricalQuery；其消费者测试已独立通过。

## 历史官方 pin 的消费者验证

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
