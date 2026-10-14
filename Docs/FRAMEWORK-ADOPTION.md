# Framework 接入审查与整改

状态：**三靶子实例隔离仍被上游 #17 阻塞，不能宣称完整整改验收通过。** 旧 Linux Player 的可运行结果不证明新源码的架构或画面已验收。

## 依赖来源

`Packages/manifest.json` 采用 `https://github.com/one2world/EjoyFramework.git#641b43dc75f6fdae9acecc37d1c1e632d7f0aff4`。实际 Unity UPM 已解析为 Git 包，`packages-lock.json` 的 `source=git` 与 `hash` 均已验证；当前 Git 索引中的完整嵌入包文件为 0。包缓存未打补丁，未创建替代 Framework 或推送上游代码。原嵌入版本可从此前提交恢复，MIT 许可保留在 `ThirdPartyNotices/EjoyFramework/LICENSE.md`。

当前上游 HEAD `051bce991579dc59e1364eb3669858e00e17190b` 实测在 `StaticPhysicsHistoricalQuery.cs:36/73` 报 CS0619，已补充 [#3](https://github.com/one2world/EjoyFramework/issues/3#issuecomment-6165533368)。固定 revision 位于该回归引入前，并包含此前 Mouse0/1、GUID 和 DefaultEntityHelper 的上游修复。

## 具体对应清单

旧实现指已推送的 `1ca0af8eec36a5c5f801df5e1c528d7edd7c7a29`；整改指当前工作区。不能用运行成功代替下列模块接入证据。

| 范围 | 旧文件及实现 | 当前接入的真实 Framework 能力 | 验证边界 |
|---|---|---|---|
| 输入 | `SliceBootstrap.Awake` 选 LegacyInputHelper；`FpsPlayer.Start` 已使用 IInputManager，原本并非手写输入管理器 | 默认 InputComponent/NewInputSystemHelper；绑定、Gameplay/Menu context 仍由 IInputManager 处理 | Mouse0/1 的实际 Input System 设备 held/down/up EditMode 回归通过；OS 录屏输入待后续验证 |
| 场景 | `SliceBootstrap` 已通过 ISceneManager/DefaultSceneHelper 加载，手写 started/failed 聚合推进 | ISceneManager 保留；协调迁入 `LevelProcedures.LoadOriginalLevel` | 原版三个内容场景在实际 Editor Play Mode 加载 |
| 流程/暂停 | `SliceBootstrap` 的布尔推进；`FpsPlayer.Pause/Resume` 自管 paused 与 Time.timeScale | IProcedureManager/ProcedureBase：Load/Play/Pause/Failed；BaseComponent.PauseGame/ResumeGame | 单靶子流程切换、时间缩放及 UI 状态断言通过；实例别名明确 fail-fast |
| 玩家/靶子 | `SliceBootstrap.OnLoaded` 直接 new GameObject、Instantiate；TargetPractice 自管 Renderer/Collider 显隐 | EntityComponent/IEntityManager + FpsPlayer/TargetPractice : EntityLogic，OnInit/OnShow/OnUpdate/OnHide/OnRecycle | 单靶子隐藏/池复用复活通过；三个相同预制体的物理实例隔离失败，#17 |
| 命中特效 | `FpsPlayer.Fire` 直接 Instantiate，延时 Destroy | ImpactEffect : EntityLogic，ShowEntity/HideEntity；ITimerManager 管 expiry | 单次射击及效果路径在单靶子测试中执行；并发同预制体特效同样受 #17 影响 |
| 资源 | SliceContent 的直接 prefab/audio 引用被业务 Instantiate/PlayOneShot 消费 | ResourceComponent/IResourceManager + AssetBatch 预载并 Pin；Entity/UI/Sound 从路径加载 | 单靶子测试确认 CachedAssetCount 至少 6；序列化原引用保留为作者工具/迁移审查输入，非实例化后备路径 |
| UI | `FpsPlayer.OnGUI` 同时承担 HUD、菜单和暂停状态 | FpsHudForm/FpsPauseForm : UIFormBehaviour，经 IUIManager、DefaultUIFormHelper、UIGroupCanvasHelper 管理打开/关闭/复用 | HUD/Pause 的真实 IUIManager 表项及流程断言通过；OnGUI 仅绘制 FPS 文案/按钮，不充当 UI 管理器 |
| 音频 | `FpsPlayer.Start/Fire` 直接新建 AudioSource/PlayOneShot | SoundComponent/ISoundManager 与 Weapons 组加载播放 | 单次射击调用路径通过；环境无实际音频输出设备，听感未验 |
| 移动 | `FpsPlayer.Update` 手写速度、重力、跳跃及 CharacterController.Move | ThreeC.ThirdPersonMotor 复用加减速/重力/跳跃；CharacterControllerStepper 处理 Unity 移动/位置恢复 | 单靶子实际原场景的移动与跳跃断言通过；FPS 仅决定输入缩放和相机朝向 |
| 计时/事件/属性 | 原本已使用 ITimerManager、EventChannel<TargetHit>、AttributeSet | 保留并在 EntityLogic 生命周期解绑/回收 | 单靶子真实射击伤害、计时换弹和复活通过 |
| FPS 专有业务 | WeaponMagazine 弹匣规则、Unity 物理遮挡射线、相机瞄准/原 rig 对齐、原动画 clip 切换 | 保留领域规则与 Unity 物理/动画适配；未再建立输入、资源、实体池、事件总线、流程机或 UI 栈 | 不宣称这些业务函数本身来自 Framework，也不把它们作为泛用基础设施替代 Framework |

作者侧预制体配置在 `FrameworkAssetSetup` 中创建，使用原版 rig/粒子/Robot，未替换模型、贴图或音频。Player 构建前调用上游 `AssetBundleBuilder.BuildAll`，保留原有材质组合策略；未降低美术资源质量。生成的 AssetBundles 不进入 Git。

## 上游问题与实际结果

- [#1](https://github.com/one2world/EjoyFramework/issues/1)、[#2](https://github.com/one2world/EjoyFramework/issues/2)、[#3](https://github.com/one2world/EjoyFramework/issues/3) 沿用已有问题，未重复创建。旧 DefaultEntityHelper 修复已通过真实消费者测试；最新 HEAD 的新增同类 CS0619 已补充 #3。
- [#16](https://github.com/one2world/EjoyFramework/issues/16)：Resources 枚举宣称 Resources.LoadAsync，但组件静默选 AssetBundleLoader。消费者选择现有 EditorSimulation/AssetBundle 正式路径，不自写 Resources loader。
- [#17](https://github.com/one2world/EjoyFramework/issues/17)：EntityManager 用 MultiSpawn 实例池，多个同时在用的同 prefab 实体共享 GameObject。实测实体总数 4，靶子 ID 100/101/102，独立靶子句柄及活动 TargetPractice 均只有 1。建议上游改为独占实例池并验证同时实例、隐藏独立性、池复用和关闭顺序。消费者明确拒绝实例别名，没有附加手写池来掩盖问题。

真实 Unity 6000.6.5f1 编译与固定 Git 依赖核验通过。EditMode 3/3 通过；PlayMode 2 项中 1 项单靶子集成通过，1 项三靶子隔离回归失败。PlayMode 使用 `-nographics`：验证真实加载、Unity 物理、Framework 模块行为，但不验证新画面、听感或独立 Player。

未构建新生产 AssetBundles/Linux Player，未发布新的运行 GIF。原因是生产三靶子生命周期被 #17 阻塞；原版资源、既有 Linux 检查点与录屏待办保留。待上游修复可消费后，更新固定 revision、重跑隔离/玩法回归，再构建真实 Player、录制 GIF 与更新 README。不能用旧构建录制证明本次源码已验证。

## 本地证据

`Reports/framework-dependency-resolution.json`、`framework-refactor-editmode-final.xml`、`framework-refactor-playmode-final.xml`、`framework-entity-isolation.json` 及对应 Unity 日志。Reports 保持本地，不包含在发布的源码中。旧独立 .NET 检查脚本已将嵌入路径改为可指定的 Git 缓存路径，但本轮证据来自真实 Unity，未重跑那些旧检查脚本。
