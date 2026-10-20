> 当前使用 alone-jam fork 固定提交 `093a175b60a196e2ad864a01ace49e90f58aacc3`。Personal 许可经用户授权激活后，真实 Unity 6000.6.5f1 依赖核验、EditMode 3/3、PlayMode 5/5 和框架原生物理查询 7/7 均通过，见 [当前 fork 验证](current-fork-validation.json)。新 Player 完整构建与实际无图形启动已通过，但 Release 附件上传 401，仍未交付，见 [本轮构建记录](fork-player-validation-20261020.json)；画面及听感尚未复验；以下旧官方 pin 的构建和录屏均为历史。遵循 [存储与恢复规则](STORAGE-AND-RECOVERY.md)：LFS 与 Editor 已恢复，旧 Player 未恢复，原始新日志仅本地暂存，尚无外部产物备份或目录持久保证。

## 历史检查点记录

# FirstRep：首版运行说明（构建已交付，画面仍有差异）

项目目录：`/workspace/FirstRep`。Unity 6000.6.5f1，HDRP 17.6，使用 Git UPM EjoyFramework v2.1.0（当前源码固定 revision 641b43dc75f6fdae9acecc37d1c1e632d7f0aff4）。此版本保留原版 Level_01，提供单人移动、瞄准、射击、换弹与机器人靶子复活；不是原版网络游戏的完整移植。

## 运行

运行 `./Tools/run-firstrep.sh`。需要 Linux x86_64、Vulkan 图形驱动及桌面显示。云端验证采用 Xvfb 和 llvmpipe 软件 Vulkan，仅用于验证，不代表普通显卡上的性能。

Unity 内打开 `Assets/FirstRep/Scenes/Bootstrap.unity` 并进入 Play Mode。Bootstrap 的生成与构建状态以 `Reports/current-progress.json` 为准。

- WASD：移动；Shift：冲刺；Space：跳跃。
- 鼠标：视角；左键：射击；右键：瞄准；R：换弹。
- Esc：暂停；点击 Resume：恢复。
- 每个靶子四枪击倒，4 秒后复活；弹匣 30 发，初始储备 180 发。

## 原资源与改动

保留完整原版背景、主场景和玩法场景的可恢复对象，原版 Terraformer 手臂、武器、Idle/Fire/Reload 动画、Robot 模型、原射击音频与粒子。1,191 个模型/贴图/音频文件按原始哈希校验，不引入外部替代美术。

原 importer 的 `collision_*` 节点规则已恢复。17 个原 LOD00 上指向不存在 fileID 的碰撞组件被禁用，不猜测跨节点绑定。当前有效碰撞与引用检查见 `Reports/collision-runtime-audit.txt` 和 `Reports/unity-reference-check.txt`。

额外恢复官方原版散射配置（SHA-256 与 Git LFS 指针一致），将 15 组参数转成现代 HDRP 配置并重绑 35 处材质。详细记录见 `Reports/diffusion-profile-recovery.json`、`Reports/diffusion-profile-migration.txt`。

## 明确差异

用户已允许缺失原资源时尽可能复刻。公开源码中无法恢复的闪电 VFX、部分材质、体积雾纹理、嵌套粉尘预制体、爆炸效果和 Fidget_01 动作不提供替代美术；受影响内容省略/禁用，逐项证据保存在 Reports。旧网络/ECS/Timeline 音频绑定没有移植；当前玩法使用 Ejoy 的场景、输入、计时器、事件和属性模块。

旧 HDRP 4 到 HDRP 17 的后处理路径在当前软件 Vulkan 上产生全黑画面，首版显式关闭后处理和屏幕空间次表面散射，并校准曝光。光照、材质和部分效果与原版仍有差异。当前环境没有音频输出设备，已验证音频资源加载和触发，未验证实际听感。

## 验证与框架问题

真实 Unity 编译、EditMode/PlayMode、视觉截图、引用检查和 Linux 构建分别有独立报告；不能把其中一项通过视为全部完成。Linux 构建已成功并持久保存；摘要仍含 3 条错误、70 条警告。正常窗口运行验证通过，批处理截图为黑。软件 Vulkan 首次渲染约需 6 分钟，且存在明显色彩/材质异常，不能视为完整画质验收。

已报告 Ejoy 上游问题：[#1](https://github.com/one2world/EjoyFramework/issues/1) 鼠标输入、[#2](https://github.com/one2world/EjoyFramework/issues/2) 无效 GUID、[#3](https://github.com/one2world/EjoyFramework/issues/3) Unity 6000.6 EntityId 兼容。使用现有 LegacyInputHelper，并保留一处要求显式 EntityData.EntityId 的最小兼容补丁，见 `Docs/Ejoy-Unity6000.6.patch`。

历史记录中的“未推送”状态已过期；当前源码在 FirstRep work 分支发布。


## 构建设置

`FirstRep.Fps.Editor.SliceWorkbench.BuildLinux` 生成 Linux x86_64 Development 构建，使用 Vulkan 和无损 LZ4HC 包压缩。`StaticMaterialVariantStripper` 仅处理 HDRP/Lit、HDRP/LayeredLit 及其对应 Tessellation 着色器：保留本次构建依赖（含 Resources）及 HDRP 默认材质实际使用且启用相应 Pass 的 shader_feature 组合，保留这些组合的所有 multi_compile 状态。当前游戏代码不在运行时切换这些着色器的材质关键字；如果以后添加这种行为，应扩充白名单或禁用该裁剪器。逐 pass 的裁剪记录在 Reports/shader-variant-pruning.txt。

构建验证探针仅在 Development 构建中通过 `-firstrep-smoke-test <JSON报告路径>` 显式启用。它确认入口与三个关卡场景、三个靶子、出生点地面及非黑画面，保存截图后退出；普通游戏不会创建此探针。玩法细节另由 Unity PlayMode 测试覆盖。

## 持久交付与存储恢复

可执行文件位于 `Deliverables/FirstRep-Linux-6000.6.5f1/FirstRep.x86_64`，此旧检查点已在 2026-10-16 经授权删除；当前脚本优先使用 `Deliverables/FirstRep-Linux-6000.6.5f1-b5c35ad` 的新版持久副本，仅在缺失时提示并回退到 `Builds/Linux`。以下 314 文件数字仅描述已删除的旧检查点。314 个构建文件共 2,008,081,592 字节，逐文件与临时输出 SHA-256 一致；包内 `SHA256SUMS` 可复验，`Licenses` 保留原许可。

`Reports/PlayerWindow/player-smoke.json` 和同目录截图记录实际独立程序验证。正常窗口通过 4 场景、3 靶子、地面和非黑画面检查；这不等于材质忠实度验证通过。

为保留磁盘余量，项目的 PlayerDataCache、PackageCache、Bee 缓存已完整迁至 `/tmp/firstrep-intermediates` 并保留符号链接。临时区丢失后需恢复/重新生成这些缓存；持久可执行文件不依赖这些目录。中止构建的中间数据已无损归档并逐文件校验，记录见 `Reports/intermediate-preservation.json`。

后续构建可设置 `FIRSTREP_BUILD_TEMP=/tmp/firstrep-intermediates/Temp-active`；BuildLinux 会在 Unity 初始化后重定向 Temp，避免启动时链接被替换。磁盘保护仍保留 1 GiB 阈值，同时检查工作区和重定向的中间/输出目录。
