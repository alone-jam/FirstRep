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
