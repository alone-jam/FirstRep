# 官方 Prefab 接入与主菜单闭环

基线 FirstRep `23c280e0b43efc0b0fd702f82b8e923e42adf5e8`，fork `093a175b60a196e2ad864a01ace49e90f58aacc3`。用户补充明确：Framework 已提供的 Prefab 和完整流程也必须优先复用，缺陷在 fork 修复并向上游提交 PR，本项目固定采用验证后的修复提交，不等待维护者合并。

## 实际差距与本批范围

Framework README Quick Start（当前固定包 README 第 136 行起）规定完整启动 Prefab；运行包提供 `Runtime/Prefabs/EjoyFramework.prefab`，自带模块、UIComponent、UIRootHelper、标准 UI 分组、相机与 EventSystem。FirstRep 的 SliceBootstrap 却手工 new GameObject/AddComponent 创建部分模块、UI group 和 EventSystem，并直接初始化 ProcedureManager，确实没有遵循完整接入流程，前几批的“API 接入通过”不能代替这一项。

先验证已发现的通用问题：包内 Prefab 序列化 helper 类型仍指向旧 EjoyFramework.Runtime 命名空间，部分 Canvas 变换 scale 为零；EditorSimulationLoader 的 Load/UnloadSceneAsync 仍自行挂载/卸载原生场景，而 SceneComponent 默认 DefaultSceneHelper 也拥有原生挂载/卸载，需通过回归确认并在 fork 修复。此前 FirstRep 给 DefaultSceneHelper 传 null 资源管理器，绕开了这条官方路径，不能继续用这种方式掩盖问题。

本批使用修复后的官方包 Prefab，仅用 FirstRep Prefab variant 配置自己的 Procedure 入口/列表，保留完整模块和 UI 栈；由 ProcedureComponent 启动、GameEntry 获取组件，采用标准 HUD/Window 分组。游戏自己的 HUD、暂停与主菜单文案/布局属于业务内容，不伪称 Framework 已提供特定 FPS 菜单。新增主菜单 Start/Exit，关卡暂停或加载失败时提供返回主菜单；旧资源/场景/会话完整释放后才进入新菜单会话，与 Retry 的所有权一致。

## 验收条件

- 先建立 fork 通用回归：原 Prefab/helper 和 EditorSimulation 场景所有权在修复前失败，修复后通过；上游 PR 不夹带既有 PR #19/#20，不合并。
- 运行时来自官方包 Prefab 的已配置 variant，所有标准组件/8 个 UI groups、一个 EventSystem 正常初始化，ProcedureComponent 配置驱动入口；移除项目手工基础设施接线，不能以删掉问题组件规避。
- 启动主菜单不加载关卡场景；按钮进入现有玩法，暂停返回菜单，再进入至少两轮，不出现并行会话、残留场景或重复 HUD 订阅。
- 加载失败后能返回菜单再进入；菜单资源本身不可用时明确报告失败，不假装成功或创建后备 UI。菜单可用后可经受控重试恢复。
- 原有消费者回归（含 Retry/Exit）通过；记录新 fork pin、原始证据与已标记脱敏副本，提交推送并取回核验。

不新增玩法、不替换原美术，不合并 PR；Release 继续暂停，不做无必要的大构建。先前 Player 不覆盖新 Prefab/流程/pin；真实渲染、音频、性能仍需独立验收。原件本地存储不是持久性或外部备份保证。

## 已执行的整改

- fork 修复经 [上游 PR #21](https://github.com/one2world/EjoyFramework/pull/21) 提交；既有 PR #19/#20 不修改、不合并。修复包 Prefab 中 3 个旧 helper 类型及 8 个 Canvas 零缩放；EditorSimulation 的资源阶段不再重复挂载/卸载原生场景，由默认 SceneComponent/DefaultSceneHelper 独占该职责。
- 首轮完整 Prefab 接入发现：ProcedureComponent 在 Start 中初始化，而旧调用方立即读取 CurrentProcedure 会按严格契约抛异常。fork 新增 IProcedureManager/ProcedureComponent.IsInitialized，不改变 CurrentProcedure 的严格契约；消费者依据就绪状态查询/退出，不抢先手工 Initialize，也不吞掉日志。该能力和回归也进入 PR #21。
- FirstRep 固定 fork `c36cc4fff162994ec7f05f47a2f30cc0ec4e414c`，manifest、lock、依赖守卫和来源记录一致。启动使用官方 Prefab 的 FirstRepSession variant，仅配置 7 个游戏流程类型/入口并附加游戏会话生命周期组件，保留完整 Framework 模块与 UI 栈。
- 移除 SliceBootstrap 的手工 Base/Input/Resource/Entity/Sound 组件创建、UI helper/group/EventSystem 创建、场景 helper null-resource 绕行及 ProcedureManager.Initialize/StartProcedure。使用 GameEntry 和 Prefab 现成组件；UI 使用标准 HUD/Window 分组。
- HUD/暂停/主菜单遵循 `B_<Property>_<Widget>`，由官方 MvvmBindingsGenerator 自动配置绑定。游戏流程工厂和 MVVM 访问器由官方 HelperFactoryGenerator/MvvmAccessorGenerator 生成，移除手写 FpsUiBindings。官方包内生成文件、Prefab 和 Loader 与 fork 提交字节一致，没有缓存补丁。
- 游戏仍编写 FPS 文案、布局、玩家/特效的原美术包装以及关卡开始/返回规则；Framework 没有交付这些 FirstRep 专有内容。这里没有再造通用根节点、事件总线、流程机或 UI 分组。
- 标准 SceneComponent 使用真实 ResourceManager 后，生产资源配置补齐关卡场景目录、独立 Scene bundle 规则，验证入口拒绝缺少当前菜单/场景的旧 manifest；本批不执行大型资源/Player 构建，因此这些生产配置不冒充已构建验证。

最初的测试脚本曾因内部 CoroutineManager 可见性、随后缺少 EditMode 工厂登记而失败；修正测试宿主后，旧版本 6/6 回归失败、修复版本 6/6 通过。首轮完整消费者 PlayMode 2/18，通过前述就绪状态问题继续修正；该失败及原件保留，不作为通过结果。最终通过结果见下节。

最终 fork pin 的独立回归 **7/7** 通过（`Reports/dropin-final-20261020T132039Z`），其中新增流程初始化/关停状态断言；6 项原缺陷的正确修复前基线为 `Reports/dropin-red-20261020T125808Z`，修复后 6/6 为 `Reports/dropin-green-20261020T130101Z`。没有把最初测试宿主错误算作 Framework 缺陷证据。

## 最终消费者结果

真实 Unity 6000.6.5f1 的准备/依赖核验、EditMode **9/9**、PlayMode **19/19** 全部退出 0，运行目录 `Reports/prefab-menu-20261020T131710Z`。本批结果与源码绑定见 [结构化证据](framework-prefab-menu-validation-20261020.json)，公开的是 [已标记脱敏副本](evidence/framework-prefab-menu-20261020/manifest.json)，原始日志和中间失败尝试均保留本地。

本轮 4 项新增 PlayMode 覆盖：官方包 Prefab variant 的完整组件、8 个标准 UI 分组及唯一 EventSystem，启动菜单不加载关卡，真实绑定按钮进入/暂停返回/再次进入两轮；加载失败返回菜单并修复配置后进入；主菜单资源缺失时明确失败、使用已预载的真实失败菜单重试恢复；Procedure 尚未初始化时退出仍完整清理。原有 15 项玩法、HUD、失败重试和退出回归继续通过。每次返回都等待旧会话完全关闭，新会话只有一个宿主和一个正确的 HUD 订阅，旧批次句柄失效。

第二次消费者尝试为 8/19：就绪问题解决后，剩余失败定位到官方按钮绑定约定自动追加 `Command`，而旧 VM 命名未带该后缀。已按官方约定改为 StartCommand/ResumeCommand/RetryCommand/MainMenuCommand/ExitCommand，没有恢复手写绑定器。改名后一份过期生成访问器曾导致准备阶段编译失败、没有运行测试；保留原文件后临时排除该过期输出，仅让官方生成器重生成。最终访问器完全由官方工具重写，临时编译保护不在最终源码中；源码快照取自准备完成后。

功能提交 `89399de` 的 82 个 Runtime/Editor/Test 源码、程序集描述及 meta 与最终测试快照逐字节一致。随后仅把 3 份新 meta 的行尾空格规范化（其中一份为 Generated 文件夹 meta）；GUID 和所有 token 不变，证据单独记录，未改 C# 或程序集描述。HUD、暂停、主菜单、Framework variant 和 SliceContent 的测试后原件已另存，提交前只去除行尾空格，所有 YAML token/字段/对象 ID/GUID 不变。没有修改正式原美术或丢弃失败证据。没有新的登录、许可激活、安装、条款操作或数据删除。

## 累计使用与剩余边界

| 项目职责 | 当前实际复用 |
|---|---|
| 启动和基础设施 | 官方 EjoyFramework.prefab 的业务配置 variant；GameEntry、完整模块和 UI 栈；不再手工搭接模块/相机/事件系统/分组 |
| 关卡/菜单流程 | ProcedureComponent 配置入口/类型，官方工厂生成；Launch/MainMenu/Load/Play/Pause/Failed/Exit 均为同一个 Framework FSM 的游戏状态 |
| UI | 标准 HUD/Window 分组、MvvmView/BindableObject/RelayCommand、官方命名自动绑定和访问器生成；文案/布局为 FPS 业务 |
| 资源/场景 | ResourceComponent/AssetBatch、SceneComponent 默认 DefaultSceneHelper/SceneLease；游戏会话协调旧引用释放完成后才重进 |
| 输入/运动 | InputComponent/IInputManager、ThreeC motor、CharacterControllerStepper；相机瞄准和出生点为 FPS 规则 |
| 武器/单位/事件/声音 | AbilitySystem/GameplayAttribute/Tags、UnitModel、EventChannel、TimerManager、SoundComponent；弹匣转移、双射线和复活时长为游戏规则 |

前述 [详细累计矩阵](FRAMEWORK-HUD-EVENTS-20261020.md#累计-framework-使用矩阵) 的历史结果不改写，本表补充此前遗漏的 Prefab 和作者工具/完整流程接入。不能以已调用 API 数量宣称完整采用了 Framework。

当前具备现有靶场玩法的“启动菜单 → 进入 → 暂停/失败 → 返回菜单 → 再进入”逻辑闭环，并保留 Retry/Exit；这里只验证 headless 真实模块、文本/命令绑定和原生场景生命周期。菜单与失败菜单资源同时缺失、资源初始化彻底失败，以及场景释放失败时的产品恢复入口仍未完整覆盖；不创建替代资源/UI 强行成功。

仍未完成对抗敌人、任务目标/关卡胜败闭环；业务 OnInit/OnShow 异常的 Framework 契约仍需单独核验。新 pin、Prefab、菜单和生产资源配置必须在后续完整 Player/AssetBundle 重建中重新验证，旧 Player/录屏无覆盖；HDRP 材质、实际画面、物理鼠标输入、听感和性能验收仍阻塞。Release 暂停，唯一旧压缩包保留。独立外部原始日志备份未配置，不能用本地副本推断平台持久性。
