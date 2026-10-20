# FirstRep 存储、恢复与执行规则

本规则是项目策略；平台行为只以下列适用官方说明为依据。核验日期：2026-10-20 UTC。
`/workspace`、`Deliverables` 等名称不是持久性证明。当前没有已确认的独立持久目录，也没有已配置并验证的外部产物备份。不要迁移到一个自命名的“持久目录”后宣布完成备份。

## 官方依据与适用范围

- [现行 Codex Cloud 环境文档](https://learn.chatgpt.com/docs/environments/cloud-environments)，`Reuse and update saved state`：已有任务继续自己的保存状态，包括未提交文件和已安装工具；新任务从发布的 prepared filesystem 开始。`Create and publish an environment` 区分保存配置与发布文件系统；任务内改动不会自动成为后续新任务的环境基线。
- 同页 `VM specifications` 的原文是 “recoverable for up to seven days”，即 saved VM state 默认最长七天可恢复，时间从最后开始一轮或恢复任务计；不是至少七天的 SLA，也不是长期备份。文档要求另行保存重要工作。
- [dots 计算机与应用文档](https://learn.chatgpt.com/docs/dots/computers-and-apps)，`Cloud threads` 明确编码线程可以使用已配置的 Codex cloud environment，建立本类委派工作与环境文档的产品关联。它不证明本次环境的发布版本、快照或具体恢复过程。
- 当前上下文提供的是所选执行环境与文件访问权限，没有逐目录保留契约。以上官方页面没有分别保证 `/workspace`、`/tmp`，也没有覆盖本次每种断连/恢复情形；不得从 overlay、tmpfs、可写权限、目录名或“同一环境 ID”推导保留保证。不套用 Codex Cloud Legacy、Agents API 或普通 ChatGPT 上传文件的规则。

当前实测只是文件可见性：主机 `8d483965f866`，`/workspace` 为 overlay，`/tmp` 为 tmpfs；这不是保留承诺。10 月 19 日已观察到原安装和产物路径缺失，原因未知；不能用七天上限解释本次缺失。平台环境发布记录及任务恢复事件需由平台界面/支持核对，不能靠修改项目恢复。

## 分类与必须保存的内容（项目策略）

“可靠保存”指现有授权远端中已核对的版本/对象，或另获授权、带保留范围与期限且完成取回校验的备份。当前执行器的任何唯一副本不满足这个定义。下表的暂存路径只是工作用途，不是平台保证。

| 类别 | 允许临时/本地存放 | 必须可靠保存与保存时机 | 哈希、恢复验证及失败处理 |
|---|---|---|---|
| 源码、原始资产和 `.meta` | 工作区编辑副本；不以 `/tmp` 作为唯一副本 | 每个可审查里程碑/暂停前提交并推送到既有 `alone-jam/FirstRep` 的授权 `work` 分支；原始大资产保留 Git LFS，`.meta` 与其资产一起版本化 | 对比本地与远端完整 Git SHA。LFS 指针/OID 不等于已备份 payload；核验远端对象可取回、大小和 SHA256，恢复后无指针、GUID 不变。取回失败保留现场并报告，禁止造替代资产或宣布恢复完成 |
| Unity 安装与安装清单 | 安装二进制可位于经过授权的执行器工具路径；下载包仅作暂存 | 必须版本化版本、revision、目标/模块、官方来源、精确 CLI 版本及安装包/二进制 hash、可复现步骤；软件本体只按许可保存到已确认的环境发布或授权备份，不将 Editor 塞进 Git | 清单见 `Tools/unity-environment.json`；未知字段保持 null。恢复后核对实际 Editor 版本/revision、模块、动态库与可用许可证。文件缺失先查任务/发布状态；新登录或条款确认停止对应操作，不读取/复制凭据 |
| Library、UPM、Shader、下载/构建中间缓存 | `Library/`、项目 `Temp/`、本地工具缓存和显式 scratch 可再生，不进 Git | 无唯一数据时不要求长期备份；缓存应按 Editor、平台、源码/锁文件及资产版本隔离。可选择缓存备份，但不得与源码/交付的备份混淆 | 冷恢复须用真实资源重建。缺失缓存不能说明源码丢失，也不证明环境整体重置；磁盘不足即停，保留日志，不能自动删缓存/降低资源品质 |
| 原始测试日志/XML、构建日志 | 唯一 run ID 的 `Reports/<run-id>/` 可作运行暂存；不能覆盖旧日志 | 每次运行结束（包括失败/中断）保存原始 XML/日志及 SHA256 清单至已授权并可取回的产物存储；暂停/切换前完成。小型脱敏摘要与清单可提交 Git；原始日志默认不强制加入源码仓库 | 摘要记录源码/fork SHA、Unity/CLI 版本、环境、命令、退出码、每项测试状态、原始证据定位/hash。无外部存储时标“仅本地、备份未配置”，保留现有文件，不宣称已交付；有 hash 但无原始文件只能算历史摘要 |
| Player、AssetBundles、录屏 | `Builds/`、`Deliverables/` 及录制 scratch 仅本地暂存；目录名不改变性质 | 宣布交付或结束长期工作前，保存到用户确认的产物目的地，记录保留期/权限、版本或对象 ID、完整 SHA256 清单并取回验证。大型包不直接提交 Git；小型录屏可按已有仓库策略提交 | 清单绑定源码、fork、Unity 版本及构建命令，验证大小/hash与实际启动；录屏说明采集/播放速度。新 fork 必须重建并复验，旧 Player 不冒充新结果。上传/取回失败则交付未完成，不删除本地源 |
| 凭据、许可证、认证会话/keyring | 仅平台批准的 secrets/vault 或软件专用凭据存储；不要另存到 scratch | 不进 Git、测试日志、普通压缩备份、Player 或共享 prepared filesystem。只记录不含身份/密钥的许可检查结果；环境发布前核查文件范围 | 恢复通过正规登录/许可流程，绝不把历史会话复制当恢复方案。遇登录/授权/条款界面停止对应步骤交用户处理；不得输出 env 全量或读取 token/许可证内容 |

每次备份需记录：授权目的地与可见范围、保留期限、源 revision、文件相对路径/字节数/SHA256、远端对象/版本 ID、取回校验时间与结果。未经这些证据，状态保持“未验证”。同一个 overlay 上复制两份只证明当时副本一致，不能证明跨任务/跨恢复保存。任何删除仍按用户批准的精确范围执行，不从“可再生”推导删除授权。

## 当前可恢复范围与缺口

2026-10-20 核查时，远端 `work` 与本地 `155f110f5446e66ea99b6dbf7a783661b7dbabe7` 一致；fork `firstrep-integration` 为 `093a175b60a196e2ad864a01ace49e90f58aacc3`。这些提交及跟踪的摘要/GIF可从既有 Git 目的地定位，但不是所有文件的独立备份证明。

`git lfs ls-files --json` 报告 1,193 个资产、声明大小 12,985,685,428 字节，checkout/downloaded 均为 0。本地仅有这些资产的指针；尚未核验远端 LFS payload 可恢复。不要将工程目录存在表述成美术资源完整可用。

原 Editor 路径、`Library`、`Reports`、`Builds`、`Deliverables/FirstRep-Linux-6000.6.5f1-b5c35ad` 当前不可见。历史 JSON 和 GIF保留；原始 XML、Player 和许可证的恢复情况未知。已有记录见 [只读审计](environment-recovery-audit-20261020.json)，历史 [官方版本测试](official-framework-validation.json)、[Player 构建](current-player-validation.json)、[当时副本哈希](persistent-player-validation.json)。保留这些历史记录原文；其中“persisted”只描述当时本地复制，不升级为平台保证。

## 恢复顺序与验证入口

1. **先定位原状态，不切换执行器。** 在同一任务核对环境配置、已发布基线、恢复记录及文件路径；如需平台支持，提供环境 ID、时间、主机和缺失路径，不提供凭据。本任务没有权限接口证明原发布状态，不能自行发布/重建新环境。
2. **先完成存储配置核验。** 当前 `verifiedPersistentDirectory`、`configuredExternalArtifactBackup` 均为 null。待配置项是用户确认的产物目的地、权限/保留期和取回验证方式；若平台提供目录契约，需记录官方出处、适用环境与范围后再用。配置前可做安全源码工作，但不能将新大型唯一产物宣称已可靠保存。
3. **检查源码与资源。** `python3 Tools/check_environment.py --verify-remote` 只读输出状态，不安装、不下载、不登录、不读密钥。若需恢复 LFS，先核对既有 `origin`，在授权和空间允许时运行 `git lfs pull origin work`；之后逐项校验 payload 的 SHA256/OID。该下载本次未执行；13 GB 原始资产之外还需 Editor、Library 和打包空间，不能仅看当前剩余盘就宣称完整构建可行。
4. **恢复工具。** 用户原先已明确要求安装 Unity CLI 及 Unity 6000.6.5f1；但本次没有再次安装，当前 CLI 版本/认证状态未知。依据 [Unity CLI 官方安装说明](https://docs.unity.com/en-us/unity-cli/use-unity-cli) 恢复 CLI 后先读 `unity --help`、`unity install --help`、`unity install-path --help`，记录精确 CLI 版本/hash。确认安装路径后使用 `unity install 6000.6.5f1`；feed 找不到版本时按 [官方参考](https://docs.unity.com/en-us/unity-cli/unity-cli-reference) 核对 changeset，项目记录为 `3ff58d469c8a`，不改装 latest。模块先按该版本官方清单核验，只恢复现有 Linux 目标所需模块。不要自动接受条款、购买席位或提升系统权限。
5. **认证与版本。** 用户通过正规私密流程确认当前账号/许可证适用及需要的新条款；遵守 [Unity Editor 条款](https://unity.com/legal/editor-terms-of-service/software)。可用 CLI 的 `auth status` 只在授权上下文中检查且不提交身份输出；遇新登录/许可界面停下交用户处理。本机 Debian 13/glibc 2.41；现行 CLI 文档列举 Linux RHEL 9/Ubuntu 22.04+，不能由 glibc 满足就声称本发行版受支持，须实际兼容性检查。不要为此静默换机器。
6. **预检和真实回归。** 使用 `python3 Tools/check_environment.py --require-ready`；仅检查存在与 pin一致性，不能代替许可和真实版本检查。Editor 路径默认来自安装清单，可用 `FIRSTREP_UNITY_EDITOR` 指向同版本的合法安装；此变量不改变存储契约。启动入口 `run_unity_checked.py`、`run_low_memory_build.py` 在缺失 Editor/LFS 时先退出，不创建 Temp/Reports或假装完成验证。

恢复后，在工程根目录给每次运行分配新的 `<run-id>`（不是重复使用历史日志名），再创建本地暂存 `Reports/<run-id>`。下列为待执行命令模板，不是本次通过结果：

```bash
python3 Tools/run_unity_checked.py -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod FirstRep.Fps.Editor.FrameworkDependencyCheck.Run \
  -logFile "$PWD/Reports/<run-id>/dependency.log"
python3 Tools/run_unity_checked.py -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode -assemblyNames FirstRep.Fps.Tests \
  -testResults "$PWD/Reports/<run-id>/editmode.xml" -logFile "$PWD/Reports/<run-id>/editmode.log"
python3 Tools/run_unity_checked.py -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform PlayMode -assemblyNames FirstRep.Fps.PlayTests \
  -testResults "$PWD/Reports/<run-id>/playmode.xml" -logFile "$PWD/Reports/<run-id>/playmode.log"
```

确认 UPM 实际来自 fork 固定 SHA、编译成功、XML 各项测试与退出码后，重建资源再完整构建：`python3 Tools/run_low_memory_build.py --stage bundles`，然后 `--stage player`；首次恢复/升级框架不能只跑 `--stage scripts`。该入口默认使用新的时间戳日志且拒绝覆盖已有前缀。仍须验证新的 Player、真实画面、退出与录屏，保留 NaN 清理及现有美术资源；headless 不证明画面/听感。

成功/失败均记录原始证据 hash及备份状态。若不能恢复任何前提，停止对应导入/构建而继续不依赖它的源码工作；报告准确缺口，不删文件、不伪造通过、不扩展上传目的地。
