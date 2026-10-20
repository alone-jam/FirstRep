# 开发收尾审计（2026-10-20 UTC）

本轮只核对复用和验收入口，不新增玩法、安装图形工具、启动显示服务或执行大型构建。审计基线为 FirstRep `51b1eb7870b15de86ded08343458506bd75934bc`；实际测试源码提交及逐文件绑定见 [结构化结果](development-closeout-validation-20261020.json)。固定 fork 为 `c36cc4fff162994ec7f05f47a2f30cc0ec4e414c`，四处来源记录一致。

## 实际复用和生命周期

- `FirstRepSession.prefab` 是包内完整 `EjoyFramework.prefab` 的 variant，父 GUID `0611ba15bbadc0f4d9f81b77d03fe431`。保留全部 20 个组件、8 个标准 UI 组、相机和 EventSystem；没有移除父组件或对象。仅配置游戏流程并添加业务会话宿主。
- Bootstrap 实例化该 Prefab，由 ProcedureComponent 启动 7 个业务流程；GameEntry 获取服务。UI 使用官方绑定命名约定、绑定工具和生成的工厂/访问器，资源使用 AssetBatch、默认 SceneComponent/SceneLease。此前手工模块、分组、EventSystem、流程初始化和空资源场景 helper 绕行已移除。详细对应关系见 [复用矩阵](FRAMEWORK-PREFAB-MENU-20261020.md)。
- 当前缓存的 1,876 个 Runtime/Editor 文件（5,070,548 字节）逐一与 fork Git blob 比较，缺失/修改均为零。package.json 原字段一致，额外 `_fingerprint` 为同一固定 SHA，属于 UPM 元数据，未当成手改缓存。
- 主菜单关闭 UI 后释放资源批次；失败页保留所需资源。加载失败事件及时复制池化参数并解绑；退出等待原生 SceneLease 终态，再释放批次和 Framework。返回/重试等待旧会话完整关闭，新会话由 Bootstrap 生命周期启动；重复或过期命令失效。
- HUD 的 EventChannel 订阅由视图释放解绑，玩家身份和 EntityId 双重筛选；实体回收解绑旧 Unit 事件并取消旧复活计时器。现有重进、失败恢复、暂停和关闭视图用例覆盖这些路径。本轮没有确认新的生命周期缺陷，未凭空重构。
- FPS 弹匣/备用弹药转移、瞄准射线、原 rig 和布局属于游戏业务，没有再造通用总线、池、FSM 或 UI 栈。Framework 的业务 OnInit/OnShow 异常契约不在本轮已验证范围。

`Tools/BuildSmokeProbe.cs` 和 `Tools/SliceWorkbenchNext.cs` 是 Assets 外的历史模板，没有活动引用，也不参与 Unity 编译。保留历史文件；不要将它们覆盖回活动实现。

## 两处真实问题与回归

1. 旧启动脚本会优先选择历史 Player。现在默认使用 `Builds/Linux/FirstRep.x86_64`，启动前必须通过同目录 `PLAYER-MANIFEST.json` 核验：完整源码/fork/Unity 版本、当前 Assets/Packages/ProjectSettings 输入和清单文件大小/SHA256。文档后续提交无需重建；游戏输入变化必须重建。可用 `FIRSTREP_PLAYER` 指定另一个已核验包。实际默认启动已在执行二进制前拒绝现存旧 Player，未伪造新清单。软件渲染入口也先检查实际 ICD 与 DISPLAY。
2. opt-in smoke 探针仍假设立即出现玩家，主菜单上线后会超时。现在通过就绪菜单的真实 StartCommand 进入，只触发一次；加载失败直接报告原因。普通游戏和窗口报告不会自动点击菜单。

Unity 6000.6.5f1 / revision `3ff58d469c8a` 本轮依赖检查、EditMode **9/9**、PlayMode **20/20** 全部退出 0；新用例覆盖 smoke 的真实菜单命令及重复触发保护。启动守卫的 6 项独立检查覆盖匹配包、纯文档后续提交、游戏输入变化、fork 不符、文件篡改和越界路径。之前同一 fork 的独立 Framework 工作流 **7/7** 保留为之前结果，本轮未重跑，也未跑完整上游测试套件。

本轮 [5 份脱敏日志/XML 及原件/副本哈希](evidence/development-closeout-20261020/manifest.json) 明确标记为副本；原件仍仅本地，外部备份未配置。headless 测试不证明生产 AssetBundle IO、当前 Player、实际画面、物理鼠标、听感或性能。

## PR 状态

[PR #21](https://github.com/one2world/EjoyFramework/pull/21) 当前 head `cb2aa4194a0c4d15faab4dfc33228355484e53e3`，open、未合并、未收到代码审查。采样时 check-runs 和 jobs 均为零。[Unity package policy run 39427736403](https://github.com/one2world/EjoyFramework/actions/runs/39427736403) 为 `action_required`，页面明确显示 “This workflow is awaiting approval from a maintainer in #21”。这是等待维护者批准后才能运行 CI，不能报作测试失败或 CI 通过。本轮未批准、重跑或合并 PR，未改 #19/#20 说明。项目继续使用已本地验证的 fork。

## 已安装图形能力：只读结果

已安装 Xorg、dummy 显示驱动、Mesa GL/EGL 和软件 GL 驱动、Vulkan loader、ffmpeg、xwininfo。没有活动显示服务器、DISPLAY/Wayland 配置、X11 socket、GPU `/dev/dri` 或音频 `/dev/snd`；没有 Vulkan ICD JSON 或 Mesa Vulkan 驱动。Xvfb、vulkaninfo、glxinfo、xdotool、pactl、aplay 不可用，旧 render-support 目录也不存在。已有部分工具不等于已具备可用的 Unity Vulkan/HDRP 渲染和录音环境；本轮没有尝试渲染或安装补齐。

## 下一轮明确验收条件

1. 先明确并验证显示与 Vulkan 设备/ICD、真实键鼠输入和音频输出路线；本轮不自动安装或启动。软件 Vulkan 仅可用于功能取证，性能必须在代表性硬件上测量。
2. 保留至少 1 GiB 空间保护，确认完整 bundles/Player 构建及暂存空间；重建当前 pin 的全部资源和 Player，生成真实源码/fork/Unity/文件哈希清单，通过启动守卫。不能沿用旧菜单/场景 manifest 或历史 Player。
3. 真实窗口逐项验主菜单文字/缩放/按钮、Start、暂停/Resume、返回菜单、连续重进、失败/Retry/Exit和应用关闭；检查无重复事件/残留场景、游标锁定、焦点切换和真实鼠标射击/瞄准。
4. 在正常 HDRP 配置下检查场景/武器/靶子、阴影、曝光、Bloom/ACES/调色/运动模糊/SSS；保留 NaN 清理，修复历史粉绿材质、武器条纹和过强高光。保存原速截图/录屏及精确构建来源，不能用旧 GIF 替代。
5. 有实际音频设备后验原射击音、暂停/恢复、重进及退出无残留声源。代表性硬件记录分辨率/画质/预热、CPU/GPU 帧耗时与帧率分布、GC 分配和内存，并比较多轮重进；先确定预算再判定性能达标。历史软件渲染约 0.2 fps 不作为性能依据。
6. 原始日志和大型产物仍需可取回校验的授权存储。旧 Release 附件上传 401 尚未解决，未更换目的地或发布空草稿；旧包及证据保留。源码推送成功不等于大型产物备份/交付完成。
