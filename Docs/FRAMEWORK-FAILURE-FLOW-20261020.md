# 下一批：实体加载失败进入可退出的失败流程

基线 FirstRep `0593211fcd9a1fa4a2c00c75d3fb5ae824e32035`，Git UPM fork 仍固定 `093a175b60a196e2ad864a01ace49e90f58aacc3`，Unity 6000.6.5f1。原环境 preflight 在 2026-10-20 12:02 UTC 通过；原 PTY 可用。Release 排障继续暂停，不重复安装或构建 Player。

## 为什么优先

`Assets/FirstRep/Runtime/LevelProcedures.cs` 中 LoadOriginalLevel 请求演员后，只等待 EntityCount 等于玩家加靶子数；没有订阅失败通知。fork `Runtime/Core/Entity/EntityManager.cs` 的 OnEntityLoadCompleted/InternalShow 已会通过 `IEntityManager.ShowEntityFailure` 报告资源失败、InstantiateEntity/CreateEntity 失败，事件参数还会立即回收到 ReferencePool。消费者不接通知会永久停在 Load，已有 FailedOriginalLevel 也只暂停，没有可操作的失败界面。这是实际玩法阻碍，优先级高于 HUD 无状态变化时仍逐帧生成字符串的问题。

## 明确实施范围

1. LoadOriginalLevel 在 OnEnter/OnLeave 订阅/解除真实 IEntityManager.ShowEntityFailure，只接受本 bootstrap 的 ActorSpawn 请求；立即复制错误文字，不能保留池化事件参数。将同步 FrameworkException 请求拒绝同样转入现有 FailedOriginalLevel，不另造加载器或事件总线。
2. 失败后取消加载中的演员并隐藏已加载演员，保留现有 SceneLease/AssetBatch 所有权直到用户 Exit，再由 SliceSessionHost 完成真实场景卸载与 Framework 关闭。
3. 复用 IUIManager、FpsPauseForm、MvvmView、TextBinder、ButtonBinder 和 RelayCommand：显示失败原因，Resume 不可执行，Exit 可用；正常暂停/恢复语义不变。只调整 FirstRep 自有 UI prefab 和生成入口，不动正式 FPSSample 资源。
4. 故障注入仅在测试的 IEntityHelper 装饰器中发生，正常操作委托 DefaultEntityHelper；使用真实资源、场景、EntityManager 失败事件和生产 Exit。验证失败不再卡 Load、按钮可操作、清理后可重进，以及离开 Load 后后续特效失败不会触发过期监听。

## 边界

本轮不创建 Framework 替代模块、不移动 fork pin。只有确认框架缺陷才在既有 fork 修复；不修改 PR #19/#20 说明、不合并。资源初始化或失败菜单自身不可加载时，不提供绕过资源模块的临时 UI；保留 Failure 与 Exit 入口并准确记录未覆盖范围。当前正式内容已有菜单预载，实体失败场景会实际验证菜单缓存路径。

HUD 事件驱动刷新仍是下一批候选。本轮只做增量消费者测试，不做大型构建；新代码不由旧 Player/录屏验证。无图形测试不证明画面、听感或性能。原始证据唯一目录保存，公开副本须脱敏并保留原件/副本 hash。

## 本轮结果与剩余项

- 新增 LoadOriginalLevel 的真实 ShowEntityFailure 订阅与解除订阅，复制池化参数里的错误文字；同步 FrameworkException 请求拒绝也会进入现有 FailedOriginalLevel。失败状态取消未完成 AssetBatch/实体请求，隐藏演员并停止声音，场景和资源所有权仍留到生产 Exit。
- 失败菜单使用原 IUIManager/MvvmView/TextBinder/ButtonBinder/RelayCommand 路径，显示原因、禁用 Resume、允许 Exit；普通暂停仍显示 PAUSED 并允许 Resume。FirstRep 自有 Pause.prefab 新增 Status 绑定，正式 FPSSample 资源不变。
- Unity 6000.6.5f1 / 固定 fork：Prepare（含依赖核验）退出 0，EditMode 9/9、PlayMode 10/10，均退出 0。新增实际 EntityManager 实例化/创建失败、生产 Exit 后 Enter 重进、Play 后特效失败隔离；故障注入只覆盖指定 helper 调用，其余调用委托 DefaultEntityHelper。两次先前编译失败均保留（缺少 namespace、测试装饰器接口签名），当时未运行测试，不能算通过。
- 两次失败没有修改 Framework API、包缓存或 pin。成功轮次的运行/测试/Editor 脚本与测试快照一致；仅生成 UI 中空 YAML 字段的行尾空格在测试后规范化，保留了测试原件与新旧 hash，未改变引用或值。

剩余高价值项：HUD 的事件驱动刷新；资源初始化失败/失败菜单自身缺失时的产品流程；可操作的重试/主菜单（本轮重进由现有 Enter API 验证，未新增 Retry 按钮）；单独验证 DefaultEntityHelper 目前捕获 OnInit/OnShow 业务异常并记录日志时，是否会把半初始化实体当作成功，以及应有的框架失败契约。本轮只覆盖 IEntityManager 已报告的显示失败，不把这些边界宣称为已解决。

已测功能路径当前没有阻塞。视觉验收有明确环境缺口：12:17 UTC 未发现 /dev/dri、DISPLAY、Xvfb 或先前 render-support；本轮没有安装替代环境或创建新图形配置。Headless 验证了界面文字和命令绑定，但未验证实际渲染、物理鼠标交互、音频设备或性能；既有材质/颜色差异也未解决。没有大型构建，旧 Player 不覆盖本批增量。Release 继续暂停，PR #19/#20 说明未动。
