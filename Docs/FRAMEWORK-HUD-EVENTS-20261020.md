# HUD 状态事件增量

实施前基线：FirstRep `9f879904b1b51b92a138481a032ce1dd778ee89d`，Framework fork `093a175b60a196e2ad864a01ace49e90f58aacc3`，Unity 6000.6.5f1 / 3ff58d469c8a。已先读存储规则并运行只读远端预检，源码与远端相同、工具和 1,193 个 LFS 工作副本存在；存在性不代替许可或 payload hash 验证。

## 实施前实际调用链

- `FpsHudForm.OnUpdate → FpsHudViewModel.Refresh → FpsPlayer.HudText/HitMarker`：每帧插值 Header，MVVM SetField 最后才判等。Header 来自弹药、备用弹药、换弹和击杀数；Crosshair 来自短暂命中标记。
- `FpsPlayer.OnUpdate → WeaponMagazine.TryFire → AbilitySystem.TryActivate`：接受射击后消耗弹药；`Reload → BeginReload` 设置标签；Framework TimerManager 回调执行 CompleteReload。应在这些成功状态转移处通知。
- `TargetPractice.UnitModel.OnDamaged → EventChannel<TargetHit>.FireNow → FpsPlayer.OnHit`：命中开启标记，死亡增加击杀数；靶子隐藏时解绑旧 UnitModel，池复用建立新模型。
- `PlayOriginalLevel.OnEnter/OnLeave → UIManager.OpenUIForm/CloseUIForm → MvvmView.CreateViewModel/OnViewModelUnbinding`：HUD 在暂停时关闭，恢复时重建模型。关闭和重新进入关卡必须解绑旧订阅，重新打开需要初始快照，不能依赖错过的事件。

## 本批范围与验证计划

使用 IEventManager 管理的类型化 EventChannel 发送玩家 HUD 状态快照；模型订阅后读取初始快照，通过 BindableObject 和现有 TextBinder 显示。事件按来源玩家和实体身份过滤，关闭时退订。移除 HUD Form 的逐帧 Refresh，不建立另一套事件总线。

命中标记仍使用既有非缩放时间语义；玩家更新只检查有活动标记时是否到期，到期发送一次状态变化。角色移动、FOV 插值、动画和技能冷却继续按帧推进。弹匣转移和击杀计数属于游戏业务。

回归重点：初始状态、空闲无重复通知、射击和拒绝射击、换弹开始/完成、UI 关闭期间发生的状态变化、反复暂停/恢复、标记到期、靶子复用和旧模型隔离、生产退出/重进后的订阅数量。运行完整消费者 EditMode/PlayMode；不做大型 Player 构建、图形配置、Release 排障或清理，不修改正式美术资源和既有 PR 说明。若实测需要 Framework 修复/补齐，依照 fork + 回归 + 上游 PR + 固定 SHA 规则处理。

旧 Player 不覆盖本批变更。

## 累计 Framework 使用矩阵

以下是当前消费者实际调用链，不按 API 数量或百分比推测使用程度。每项只代表列出的能力，没有宣称使用整个子系统。

| 领域 | 实际接入链 | FirstRep 保留的业务/适配边界 |
|---|---|---|
| 输入 | FpsPlayer → IInputManager 绑定/查询；暂停与失败菜单 → InputContext 栈 | FPS 按键、视角灵敏度、瞄准时速度比例 |
| 角色移动 | FpsPlayer → ThirdPersonMotor.Step → CharacterControllerStepper.Move/RestorePosition | 相机朝向、出生点和掉落重生条件；Unity CharacterController 碰撞适配 |
| 资源 | LoadOriginalLevel → ResourceComponent.InitializeAsync → IResourceManager.AssetBatch 预载/Pin → Entity/UI/Sound 加载 | SliceContent 的正式资源路径、预载集合和场景列表 |
| 场景和退出 | SceneLease 加载/释放；SliceSessionHost 等待终态后 AssetBatch.Release/Framework.Shutdown | 会话资源所有权与退出顺序，未自建场景加载器 |
| 流程与暂停 | ProcedureBase/ChangeState：Load、Play、Pause、Failed、Exit；BaseComponent.PauseGame/ResumeGame | 关卡状态转换、失败原因、菜单入口 |
| 实体及池 | EntityComponent/IEntityManager.ShowEntity/HideEntity → FpsPlayer、TargetPractice、ImpactEffect : EntityLogic | 玩家/靶子位置、复活规则、命中特效位置和寿命；无消费者实例池 |
| 武器 | WeaponMagazine → AbilityDefinition.WithCooldown/AddCost/BlockedBy → AbilitySystem.TryActivate/Tick、GameplayAttribute、GameplayTagSet | 弹匣容量、备用弹药、换弹转移、开火输入；未接入网络弹道/历史回溯 |
| 单位伤害 | TargetPractice → UnitModel.ConfigureHealth/TakeDamage → OnDamaged/OnDied → TargetHit 通道/EntityManager | 靶子阵营配置、击杀计数、复活条件；未使用完整 UnitWorld/索敌 |
| HUD 状态事件 | FpsPlayer → IEventManager.GetChannel<PlayerHudChanged>().FireNow → FpsHudViewModel → BindableObject.SetField | HUD 快照、玩家身份过滤、命中标记的短暂显示；无额外事件总线 |
| UI/命令 | IUIManager → MvvmView、PropertyAccessor、TextBinder、ButtonBinder、RelayCommand | HUD 文案及布局、Resume/Exit 行为和命令可执行条件 |
| 计时 | ITimerManager 管换弹完成、靶子复活和特效隐藏 | 1.8 秒换弹、4 秒复活等游戏时长；命中标记只在活动期检查非缩放时间到期 |
| 音频 | SoundComponent/ISoundManager.PlaySound，Weapons 声音组 | 射击时选择音效和音量配置；本轮不验证实际听感 |
| 演员加载失败 | IEntityManager.ShowEntityFailure → 复制池化参数 → FailedOriginalLevel → UIManager/MVVM/生产 Exit | 所属会话及演员组过滤、失败文案和安全退出；未覆盖业务 OnInit/OnShow 异常契约 |
| FPS 瞄准及原版表现 | Unity 双射线遮挡、FirstPersonPresentation 原 rig/动画/粒子、PresentationSettings HDRP 适配 | 游戏/引擎适配，不能计为 Framework 提供的弹道、动画或渲染系统；继续保留正式原资源 |

## 验证结果

真实 Unity 6000.6.5f1 的 Prepare/依赖核验、EditMode **9/9**、PlayMode **11/11** 均退出 0。本地运行目录 `Reports/hud-events-20261020T123055Z`；75 个 Runtime/Test/Editor 源码、程序集描述及 meta 文件与启动前测试快照逐字节一致。详细命令、每项测试结果、版本与 hash 见 [本批证据](framework-hud-events-validation-20261020.json)，已标记的脱敏副本见 [清单](evidence/framework-hud-events-20261020/manifest.json)。原始日志保留本地，未配置独立外部备份。

- FpsHudForm 不再逐帧 Refresh；FpsPlayer 在成功射击、开始/完成换弹、击杀和标记状态转换时发布快照，重复命中只延长已亮标记，不重复发布不变的 HUD。模型订阅后读取当前快照，按玩家引用和实体 ID 过滤，关闭时退订缓存的处理器。
- 新用例验证初始真实 TextBinder 文本、空闲零事件/零属性通知、满弹匣拒绝换弹、射击扣弹、换弹状态、换弹期间拒绝射击/再次换弹时无重复事件、三轮暂停/恢复的订阅数量、完成换弹后的弹药和备用数、重复命中、非缩放时间到期、关闭期间的击杀与重新打开快照，以及重复致死无重复更新。
- 现有靶子池复用用例加强了 HUD 断言：旧 UnitModel 的伤害不更新 HUD，复用靶子的新击杀正确累加。两轮生产退出/重进验证每次只有一个 HUD 订阅，暂停退订、关闭通道归零，新会话恢复初始值。已有真实命中、MVVM、失败流程和场景资源生命周期回归全部通过。
- 初次继承的测试脚本因 CLI 未登录而在启动 Editor 前停止；随后只读 license status 显示已有许可证 active=true、signedIn=false。移除本批脚本中过严的登录前提，使用现有有效许可证完成实际测试；没有新登录、激活、安装或接受条款。首次停止没有测试结果，未算作通过。
- 准备步骤重新生成 Pause prefab 的本地对象 ID/文档次序及空白；保留生成副本和差异后，按对象层级、组件类与 script 身份规范化逐字段/引用比较，语义完全一致，恢复了原跟踪字节。没有改变 UI 布局、字段、外部 GUID 或正式 FPSSample 资产。

本批现有 Framework API 足够，未改 fork 源码或 pin、未新增框架修复 PR，也未改既有 PR 说明或合并。工作区仍保留超过 1 GiB 空闲保护，没有删除数据。这里只验证无图形消费者逻辑/绑定和真实场景生命周期；没有测量分配量/性能，也不宣称画面、物理鼠标或音频听感验收通过。旧 Player/压缩包保留，Release 工作暂停。

## 剩余高价值项

- 失败后的 Retry/主菜单产品入口；当前生产 Exit 和 Enter API 重进已验证，尚无 Retry 按钮。
- Resource 初始化失败或失败菜单资源本身不可用时的产品流程，不能加绕过资源/UI 模块的后备实现。
- 单独复现并确认 DefaultEntityHelper 对 EntityLogic.OnInit/OnShow 异常的成功/失败契约，再决定是否在 fork 修复并提交新上游 PR；本批不把待确认的契约当已修复缺陷。
- 当前新代码仍需后续 Player 重建、图形支持和既有 HDRP 材质迁移问题的画面验收；当前 headless 回归不替代这些结果。
