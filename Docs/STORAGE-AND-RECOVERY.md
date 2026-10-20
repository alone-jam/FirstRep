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

2026-10-20 恢复前核查时，远端 `work` 与本地 `155f110f5446e66ea99b6dbf7a783661b7dbabe7` 一致；fork `firstrep-integration` 为 `093a175b60a196e2ad864a01ace49e90f58aacc3`。这些提交及跟踪的摘要/GIF可从既有 Git 目的地定位，但不是所有文件的独立备份证明。

恢复前 `git lfs ls-files --json` 报告 1,193 个资产、声明大小 12,985,685,428 字节，checkout/downloaded 均为 0。随后已从既有 origin 全部下载、展开，并对每个工作副本及缓存对象核对大小/SHA256，全部匹配；资产及 `.meta` 无 Git 差异。见 [本次恢复记录](recovery-progress-20261020.json)。这证明此时取回和本地完整性，不构成远端长期保留承诺或独立备份。

恢复前原 Editor 可执行文件、`Library`、`Builds`、`Deliverables/FirstRep-Linux-6000.6.5f1-b5c35ad` 均不可见；Editor 现已通过官方 CLI 恢复，旧 Player 尚未恢复。旧 `Reports` 原始证据未恢复；本次另建了唯一 run ID 的恢复校验报告。历史 JSON 和 GIF保留；原始 XML、Player 和许可证的恢复情况未知。已有记录见 [只读审计](environment-recovery-audit-20261020.json)，历史 [官方版本测试](official-framework-validation.json)、[Player 构建](current-player-validation.json)、[当时副本哈希](persistent-player-validation.json)。保留这些历史记录原文；其中“persisted”只描述当时本地复制，不升级为平台保证。

## 恢复顺序与验证入口

1. **先定位原状态，不切换执行器。** 在同一任务核对环境配置、已发布基线、恢复记录及文件路径；如需平台支持，提供环境 ID、时间、主机和缺失路径，不提供凭据。本任务没有权限接口证明原发布状态，不能自行发布/重建新环境。
2. **先完成存储配置核验。** 当前 `verifiedPersistentDirectory`、`configuredExternalArtifactBackup` 均为 null。用户已确认 Player 压缩包与校验清单保存到同一公开仓库 alone-jam/FirstRep 的 GitHub Release，但本轮附件上传被 uploads.github.com 以 401 Bad credentials 拒绝，草稿 418223234 无附件，尚未发布或完成取回校验；固定保留期未约定。需先修复该上传主机的认证，才能继续使用已授权目的地；若平台提供目录契约，需记录官方出处、适用环境与范围后再用。配置前可做安全源码工作，但不能将新大型唯一产物宣称已可靠保存。
3. **检查源码与资源。** `python3 Tools/check_environment.py --verify-remote` 只读输出状态，不安装、不下载、不登录、不读密钥。若需恢复 LFS，先核对既有 `origin`，在授权和空间允许时运行 `git lfs pull origin work`；之后逐项校验 payload 的 SHA256/OID。本次已完成下载与校验，使用了本地 `git lfs install --local --skip-repo` 恢复过滤器，未改全局配置或 hooks。`python3 Tools/verify_lfs_payloads.py --include-local-cache` 可只读复验两份内容；不带该开关则只验工作副本。13 GB 原始资产之外还需 Editor、Library 和打包空间，不能仅看当前剩余盘就宣称完整构建可行。
4. **恢复工具。** 用户原先已明确要求安装 Unity CLI 及 Unity 6000.6.5f1；本次已从官方版本 manifest 恢复 CLI `1.0.0-beta.13`，按官方 SHA256 核验，入口为 `python3 Tools/run_unity_cli.py`。其状态存放在安装清单列明的 Unity 专用 XDG 目录；未修改 shell 配置、系统包或凭据。Editor 现已通过官方 CLI 安装成功，实测版本和二进制内 revision 一致；记录见 [最新恢复证据](editor-recovery-validation-20261020.json)。依据 [Unity CLI 官方安装说明](https://docs.unity.com/en-us/unity-cli/use-unity-cli) 恢复 CLI 后先读 `unity --help`、`unity install --help`、`unity install-path --help`，记录精确 CLI 版本/hash。确认安装路径后使用 `unity install 6000.6.5f1`；feed 找不到版本时按 [官方参考](https://docs.unity.com/en-us/unity-cli/unity-cli-reference) 核对 changeset，项目记录为 `3ff58d469c8a`，不改装 latest。模块先按该版本官方清单核验，只恢复现有 Linux 目标所需模块。不要自动接受条款、购买席位或提升系统权限。
5. **认证与版本。** 用户通过正规私密流程确认当前账号/许可证适用及需要的新条款；遵守 [Unity Editor 条款](https://unity.com/legal/editor-terms-of-service/software)。可用 CLI 的 `auth status` 只在授权上下文中检查且不提交身份输出；恢复初次实测 loggedIn=false、license active=false（退出码分别为 3、4）；后续用户已完成官方登录并明确授权 Personal 激活，当前许可检查和真实 Editor 运行均成功，见 [当前 fork 验证](current-fork-validation.json)。不把未登录直接等同于安装命令必需登录。遇新条款/许可确认停下交用户处理。本机 Debian 13/glibc 2.41；现行 CLI 文档列举 Linux RHEL 9/Ubuntu 22.04+，不能由 glibc 满足就声称本发行版受支持，须实际兼容性检查。不要为此静默换机器。
本次容量恢复：用户明确批准清理 `/workspace/FirstRep/.git/lfs/objects`。重新校验 1,193 个工作副本/缓存对象及资产 `.meta` 无差异后，仅删除该目录内的 1,193 个重复对象，根目录及其他目录保留；实际释放 12,988,174,336 字节。删除后再次逐项核对工作副本大小/SHA256，全部通过。缓存将来需要时须从既有 origin 再下载；缺少缓存不阻止 Unity 使用完整工作副本。这项精确批准不授权后续其他清理。

随后 `python3 Tools/run_unity_cli.py install 6000.6.5f1 --non-interactive --format ndjson` 成功退出 0，官方 CLI 完成下载校验与安装；未使用 `--accept-eula`。Editor `-version` 输出 6000.6.5f1，二进制含 `3ff58d469c8a`，本地 SHA256 已记录。安装后约剩 8.2 GiB，额外导入/构建空间仍须实测监控，不预先保证充足。

以下为已解决的历史许可阻碍：用实际工程启动依赖验证入口，Editor 在 2026-10-20 07:11 UTC 退出 **198**，提示 `No valid Unity Editor license found. Please activate your license.`，没有执行到成功的依赖验证，也没有完成 EditMode/PlayMode。原始日志仅保存在当前任务的唯一 run 目录，脱敏摘要及各文件 hash见 [editor-recovery-validation-20261020.json](editor-recovery-validation-20261020.json)；旧审计与旧测试 JSON 未覆盖。

当时后续步骤是在**这个任务的正规私密认证上下文**中完成 `python3 Tools/run_unity_cli.py auth login` 的官方浏览器登录流程，再检查 `license status` 并激活适用的已有许可。`auth login --help`、`license activate --help` 已核对；选择 Personal 时 CLI 明确要求接受其条款，不能自动替用户选择或执行 `--accept-eula`。不购买席位，不在聊天索要密码、token 或许可证文件，也不把用户其他设备已登录等同于本执行器已认证。若平台未提供对应私密浏览器/终端入口，该入口本身需先由平台确认。参考 [Unity CLI 官方登录流程](https://docs.unity.com/en-us/unity-cli/use-unity-cli#sign-in)。

6. **预检和真实回归。** 使用 `python3 Tools/check_environment.py --require-ready`；仅检查存在与 pin一致性，不能代替许可和真实版本检查。Editor 路径默认来自安装清单，可用 `FIRSTREP_UNITY_EDITOR` 指向同版本的合法安装；此变量不改变存储契约。启动入口 `run_unity_checked.py`、`run_low_memory_build.py` 在缺失 Editor/LFS 时先退出，不创建 Temp/Reports或假装完成验证。

当前用户已完成官方登录并明确授权 Personal 激活（含披露的现行条款），CLI 激活与许可检查退出 0。真实 UPM fork 来源核验、FirstRep EditMode 3/3、PlayMode 5/5、框架原生物理查询 7/7 均通过，见 [当前 fork 验证](current-fork-validation.json)。旧退出 198 的 JSON 不覆盖。默认 HOME 下 UPM 缓存不可写的问题已通过清单中的 `upmCacheRoot` 与启动器的 `UPM_CACHE_ROOT` 默认值修复；Unity XDG 默认路径同 CLI，保留调用方覆盖，不复制或读取凭据。

工程根目录每次运行分配新的 `<run-id>`（不是重复使用历史日志名），再创建本地暂存 `Reports/<run-id>`。下列为后续复验模板；本轮实际命令、退出码与 XML 见上述新记录：

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

## 本轮 Player 构建与交付状态（2026-10-20）

固定 fork 的 bundles 与完整 Player 已成功构建，Editor 退出 0，报告 0 错误、85 警告；新 Player 实际无图形启动退出 0，确认 4 场景、3 靶子及地面。仅新生成的 PlayerDataCache 放入 `/tmp/firstrep-intermediates`，没有删除既有文件或切换环境；原有 1 GiB 磁盘保护继续生效。构建生成的 Pause prefab 已保留副本和差异，并还原源码。

压缩包 1,818,104,222 字节，SHA256 `13d81e70b907d9165cba9a81d5716891828573b539820ec94c60ef09a9ca2ee3`，包内 230 个文件大小/hash 全部核验通过。包仅含运行文件、软件版权/许可说明和校验清单；不包含私密日志、凭据或 Editor 激活文件。上传因 401 失败，原始 Player、唯一压缩包、校验清单和本轮私密日志仍仅本地保留，不能宣称完成备份或公开交付。没有换上传目的地、重试被拒绝的附件上传或发布空草稿。完整状态见 [fork-player-validation-20261020.json](fork-player-validation-20261020.json)。
