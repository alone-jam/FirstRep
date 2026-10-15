# Framework 接入审查与整改

官方修复已消费。实际 Unity 6000.6.5f1：FirstRep EditMode 3/3、PlayMode 5/5 通过。上游相关测试 22/23 通过，一项历史碰撞测试断言失败，详见下文。生产 Player 构建与画面验收单独记录，不能由无图形测试推断。

## 依赖来源

`Packages/manifest.json` 与 `packages-lock.json` 固定官方 Git SHA `a5e1ec6f3472f741815108509ba303ce39b933c3`，实际 UPM `source=git`、解析 hash 已核验。无嵌入 Framework，无包缓存补丁。许可保留在 `ThirdPartyNotices/EjoyFramework/LICENSE.md`。

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

## 生产构建与剩余阻碍

同一官方 SHA 的上游 AssetBundleBuilder 已生成 2 个 bundle、9 项资源及 manifest，完整 SHA-256/大小/资源清单见 [production-validation.json](production-validation.json)。Linux Player 构建在 HDRP 着色器 Forward 变体处理阶段被 16 GiB cgroup 内存限制终止；oom_kill 计数从 1 增至 2。工作区约剩 1.5 GiB、临时区约剩 3.3 GiB，同时保留 1 GiB 磁盘阈值。未产出新可执行文件，未进行新 Player 的画面验收或 GIF 录制，README 未插入旧构建冒充的 GIF。

已完成的空间恢复是逐文件哈希验证后的无损迁移/保留；原资源、旧持久 Player、历史中间数据均保留。继续重建需要释放额外构建内存/存储余量；若依靠删除缓存或历史数据，需先取得用户授权，不能将对已校验重复旧构建的单次删除许可扩大到其他目录。
