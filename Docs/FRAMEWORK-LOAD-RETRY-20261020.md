# 加载失败后的可操作重试

实施前基线 FirstRep `dba96d84e65db9e2d857c592d7cda353557d2101`；Framework fork `093a175b60a196e2ad864a01ace49e90f58aacc3`；Unity 6000.6.5f1 / 3ff58d469c8a。先读存储规则并运行远端预检：本地与 work 相同，Editor 和 1,193 个 LFS 工作副本存在；没有重装、切换环境或删除文件。

## 当前链路与选择范围

LoadOriginalLevel 已把 AssetBatch 失败、SceneLease 失败和 IEntityManager 报告的演员显示失败转入 FailedOriginalLevel；它取消加载中的演员/批次、隐藏已加载演员、停止声音，在资源已缓存时由 UIManager 打开 FpsPauseForm。失败菜单显示原因，Resume 不可执行，Exit 能走生产释放流程；重进仅有 Enter API，没有可点击 Retry。

现有 ExitOriginalLevel → SliceSessionHost.BeginExit → SceneLease.Dispose/等待终态 → AssetBatch.Release → Framework.Shutdown/服务对象销毁；SliceExitStatus.Completed 在全部结束后为 true。ResourceComponent 的 InitializeAsync 每个实例只能触发一次，因此直接在旧 ResourceComponent 重跑 LoadOriginalLevel 不符合现有契约。

本批选择：失败菜单 Retry 复用现有 RelayCommand、ButtonBinder、VisibilityBinder；先调用生产 Exit，再由仍存活的 SliceBootstrap 等待旧 ExitStatus.Completed，最后 Enter 新会话。该短协程只协调跨 Framework 关停的生命周期，不保存第二套关卡状态机，也不接管资源、场景加载或模块更新。重复请求不启动第二个重试；外部 Exit/销毁 Bootstrap 取消待重进动作。

本批覆盖正常菜单资源可加载时的演员失败及真实资源路径缺失。资源初始化本身失败、失败菜单自身资源缺失、场景释放失败的人机恢复界面以及主菜单仍是独立产品缺口；不添加绕过 Framework 的临时 UI、直接 Resources/Instantiate 后备路径或替代资产。

## 验收条件

1. 正常进入仍为 Play，HUD 和实际演员数量正确；正常暂停显示 Resume/Exit，Retry 隐藏且不可执行。
2. 加载失败显示原因和 Retry/Exit，Resume 隐藏且不可执行；按钮通过真正的 Framework MVVM 绑定触发生产路径。
3. 连续缺失同一测试资源时，重复 Retry 仍得到可操作失败菜单，每次旧场景/批次/UI/事件订阅都完成清理；双击/旧模型调用不产生并发会话。
4. 将测试配置恢复为真实资源路径后点击 Retry，成功回到 Play，清除旧 Failure、时间缩放和旧模型命令，不复制场景、演员或 HUD 订阅。
5. 演员失败也能通过 Retry 成功进入；手动 Exit 或销毁 Bootstrap 在等待清理期间不会自动重进。
6. 完整消费者 Unity EditMode/PlayMode 回归通过，证据绑定测试源文件、fork pin 和生成 UI。没有大型 Player 构建、Release 操作、图形配置、数据删除或既有 PR 说明修改。若证实 Framework 缺陷/通用缺失，单独走 fork 修复、回归、上游 PR、固定已验证提交的流程。

累计实际使用矩阵延续 [HUD 批次矩阵](FRAMEWORK-HUD-EVENTS-20261020.md#累计-framework-使用矩阵)，本批只扩展既有流程、资源、场景与 MVVM 菜单的消费者使用；结果与剩余核心缺口如下。

## 完成结果与验证边界

Prepare/UPM 依赖核验、Unity EditMode **9/9**、PlayMode **15/15** 均退出 0。本地运行目录为 `Reports/load-retry-20261020T124141Z`，详细命令、测试项、版本与文件 hash 见 [结构化结果](framework-load-retry-validation-20261020.json) 和 [已标记脱敏证据](evidence/framework-load-retry-20261020/manifest.json)。75 个 Runtime/Test/Editor 的源码、程序集描述及 meta 与测试启动快照逐字节一致；测试后的 Pause prefab 仅清理行尾空格，原生成字节另存并记录 hash，没有更换 GUID/字段/对象 ID。

新增 4 项 PlayMode 用例，分别覆盖：真实缺失靶子资源连续三次失败后成功重试；真实 DefaultEntityHelper 的创建点注入失败后通过已绑定 Retry 按钮恢复；显式 Exit 取消待重试；销毁 Bootstrap 取消待重试。原有 11 项 PlayMode 与全部 9 项 EditMode 继续通过。

重复资源失败用例逐次验证旧 SceneLease 终态、AssetBatch 句柄失效、旧 ResourceManager 缓存和 EntityManager 计数归零、旧事件通道关停；重复按钮/旧命令/API 调用不能创建并行会话。成功恢复后确认正常时间缩放、正确场景/演员数、一个服务宿主和一个 HUD 订阅；正常进入和暂停仍工作，Retry 只在失败菜单可见/可执行。生产资源/场景/UI/模块均实际参与；没有删除或伪造资产，只修改测试用 SliceContent 克隆的路径。

EditorSimulation 的资源卸载是编辑器模式行为；这些结果证明 Framework 批次句柄、缓存、模块所有权和真实 Unity 场景清理，不证明生产 AssetBundle 的 I/O、bundle 引用计数或实际 Player 重试。没有大型构建，也没有新的图形、物理鼠标、音频设备或性能验收。已有许可证 active=true，CLI 未登录没有阻止实际测试；本批没有新登录、激活、安装或条款操作。

当前 fork 的 API 足够，本批没有修改 Framework 源码、pin 或既有 PR，也没有新建不必要的上游 PR。Release 继续暂停，既有 Player/唯一压缩包保留；没有删除数据，工作区仍保留超过 1 GiB 空闲。原始日志仍仅本地保存，公开的是带原件/副本 hash 的脱敏副本，不宣称完成独立外部备份。

| 本批扩展的累计接入 | 已使用的真实 Framework API | 游戏侧边界 |
|---|---|---|
| 失败重试菜单 | UIManager → MvvmView → RelayCommand/ButtonBinder/VisibilityBinder/PropertyAccessor | ShowResume/CanRetry、Retry/Exit 的游戏语义 |
| 重试前清理 | ExitOriginalLevel/ProcedureBase → SceneLease 释放 → AssetBatch.Release → Framework.Shutdown | SliceBootstrap 跨关停等待同一个生产 ExitStatus 完成；未增加关卡状态机或加载器 |
| 重新进入 | 原 Enter → LoadOriginalLevel → ResourceComponent.InitializeAsync/AssetBatch → 原生场景/EntityManager → Play | 同关卡重新开始、配置修复后再尝试；不自动下载或替代缺失资源 |

## 剩余核心玩法和验收缺口

- 主菜单、关卡选择和返回入口尚未提供；Exit 结束会话，Retry 只重新加载同一关卡。
- Resource 初始化失败、失败菜单自身缺失时仍没有产品恢复界面；场景释放失败保留旧服务等待恢复，不能强行启动新会话，其用户恢复入口仍未实现。
- 当前可玩内容仍是单人移动、瞄准、射击、换弹及复活靶子的切片；没有宣称完成对抗敌人、任务目标或关卡胜败闭环。
- DefaultEntityHelper 对 EntityLogic.OnInit/OnShow 异常的契约仍须单独复现和确认；确认通用缺陷后应在 fork 修复、回归并提交新上游 PR，FirstRep 固定已验证 SHA。
- 最新源码需要后续独立 Player/AssetBundle 重建与真实运行；HDRP 材质迁移、画面、实际输入/听感及性能仍待验收。旧 Player/录屏不覆盖这批代码。
