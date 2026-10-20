# Framework 实际调用审计与本轮开发目标

基线：FirstRep `1831c48d8c3b2b0cfb561c8caf289a8c6ec9a686`；Git UPM fork `093a175b60a196e2ad864a01ace49e90f58aacc3`；Unity 6000.6.5f1 / 3ff58d469c8a。2026-10-20 11:43 UTC 原环境预检通过，1,193 个 LFS 工作副本存在、Editor 存在，原 PTY 的 auth/license status 均退出 0（loggedIn/active/signedIn=true）。没有从断连推断丢失、重装或切换环境。Release 认证排障暂停；现有包和草稿保留。

以下位置是基线代码位置，按真实调用链审查，不按引用/using 数量评分。

| 功能 | 实际调用位置与 Framework API | 分类及判断 |
|---|---|---|
| 输入与角色运动 | `Assets/FirstRep/Runtime/FpsPlayer.cs:38–45,70–73`：IInputManager.SetBinding/GetAxis/GetButton；ThirdPersonMotor.Step、CharacterControllerStepper.Move | 已实际使用；Unity 相机朝向、瞄准缩放和掉落重生规则是 FPS 业务 |
| 资源与关卡 | `Runtime/LevelProcedures.cs:29,38–49`：ResourceComponent.InitializeAsync、IResourceManager.CreateAssetBatch/Add/Start、SceneLease；`Runtime/SliceSessionHost.cs:34–65`：Dispose/终态等待、AssetBatch.Release | 已实际使用；SessionHost 协调场景释放后才关闭服务，是消费者所有权边界，不是自建 SceneManager |
| 实体与特效 | `Runtime/SliceBootstrap.cs:42–45,68–71`：EntityComponent 分组/ShowEntity；`Runtime/ImpactEffect.cs:11–19`：EntityLogic、ITimerManager、IEntityManager.HideEntity | 已实际使用真实加载/独占实例池，无业务 Instantiate 后备池 |
| 状态流程、暂停 | `Runtime/LevelProcedures.cs:14–19,75–110`：ProcedureBase/ChangeState；BaseComponent.PauseGame/ResumeGame；IInputManager.PushContext/PopContext | 已实际使用；暂停菜单和关卡退出是业务状态 |
| UI 与音频 | `Runtime/FpsHudForm.cs:4–14`、`Runtime/FpsPauseForm.cs:4–10`、`Runtime/FpsViewModels.cs:4–37`：MvvmView/BindableObject/PropertyAccessor/RelayCommand；`Runtime/FpsPlayer.cs:84`：SoundComponent.PlaySound | 已实际使用；HUD 每帧 Refresh/字符串生成仍可后续改为状态变更驱动，本轮不扩展到另一套通知系统 |
| 武器射速与弹药消费 | `Runtime/FpsPlayer.cs:29,51,77–79` 手工 nextShot/Time.time；`Runtime/WeaponMagazine.cs:17–20` 手工扣减弹药 | 高价值重复：Framework AbilitySystem 已有成本校验/扣除、冷却和标签门槛；应进入实际射击路径，而不是只注册一个未调用的技能 |
| 靶子生命与死亡 | `Runtime/TargetPractice.cs:20,25,28–33` 手工减血、钳制、判死 | 高价值重复：UnitModel.ConfigureHealth/TakeDamage 已管理生命钳制、伤害来源、一次死亡事件；当前仅用 AttributeSet 储存数字，没有复用这些语义 |
| 瞄准与表现 | `Runtime/FpsPlayer.cs:85–95`：相机射线加枪口遮挡；`Runtime/FirstPersonPresentation.cs:16–33`：原版 rig/动画/粒子；`Runtime/PresentationSettings.cs:9–58`：HDRP 迁移配置 | 合理 Unity/FPS 适配；不能为了增加 API 数量把离线实时射击强行改为历史回溯/网络弹道，或替换正式资源 |

Framework 实现依据：fork `Runtime/GamePlay/Combat/Abilities/AbilitySystem.cs:159–204,300–360`（TryActivate、Cooldown、Tick）；`AbilityDefinition.cs:209–280`（WithCooldown/AddCost/BlockedBy）；`Runtime/GamePlay/Units/UnitModel.cs:209–230,275–302,466–479`（生命、伤害、事件）。这些均为现有真实实现，不需要在 FirstRep 新造模块。

## 下一批实施目标

1. 将 WeaponMagazine 保留为弹匣容量/备用弹药/换弹转移的业务适配层；弹药资源消费、0.12 秒射速和换弹阻挡委托给真实 AbilitySystem、AbilityCost、GameplayTagSet。FpsPlayer 的 EntityLogic 更新推进该拥有者的 AbilitySystem 时间，移除 nextShot；暂停不推进、池复用重新开始。
2. TargetPractice 使用 UnitModel 的生命和伤害/死亡事件，保留 EntityManager 隐藏和 TimerManager 复活。将射手的 Framework Entity.Id 传为伤害来源；重复致死不重复计数；提前重显时取消上一条复活计时器，避免旧回调再次显示同 ID。
3. 增加弹药/冷却/换弹交互的 Editor 回归和真实实体生命周期的 PlayMode 回归，再运行现有消费者套件。固定 fork 无需因纯消费者接入变化而移动；若实测框架缺陷，再在 fork 修复并按既有 PR 流程处理。

目前未确认需要新增 Framework 修复。已知历史几何/弹道问题由当前组合 fork 和既有 PR #19/#20 承载，本轮不改这些 PR 的说明，也不合并。框架没有直接提供 FirstRep 弹匣转移规则、原版 rig 和相机—枪口双射线适配；保留业务适配并不等于重复造框架。

## 验证边界与资源约束

工作区仅约 1.51 GiB 可用，继续保留 1 GiB 保护阈值；本轮做源码和现有工程的增量测试，不做大型 Player 重建、不删除缓存/已有包。原版 FPSSample 资产及 `.meta` 保持。先前已构建 Player 对本轮后续代码变化不提供验证；新的测试证据须单独记录版本、命令、结果、原件 hash 与备份/脱敏状态。无图形测试不证明画面、听感或性能。
