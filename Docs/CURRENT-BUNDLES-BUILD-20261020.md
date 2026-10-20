# 当前源码资源构建结果（2026-10-20 UTC）

固定 FirstRep `ad9f75a996befa57ebf7b0bbfab79f7af25eae38`、Framework `c36cc4fff162994ec7f05f47a2f30cc0ec4e414c`，Unity 6000.6.5f1 / `3ff58d469c8a`。本轮没有新增玩法或修改 Framework；Assets、Packages、ProjectSettings 已复核回到固定提交的字节内容。后续提交只保存本次结果和脱敏证据。

**bundles 构建和生产资源校验通过；完整 Player 因预计空间不足，在启动前暂停，headless smoke 未运行。** 当前没有新可执行文件或伪造的 PLAYER-MANIFEST，不能宣称菜单/Framework Prefab 已通过当前 Player 的集成验证。

## 已实测结果

- 1,193 个原始 LFS 工作副本、12,985,685,428 字节重新通过大小与 SHA256 校验。未重新下载，未主动清理缓存。
- 原 bundles 会话 69475 / Unity PID 60400 持续运行至退出码 **0**，约 13:52–14:31 UTC；中途执行通道断连没有重启构建。耗时的 HDRP/Lit TransparentBackface pass 编译了 640 个变体，后续 Forward pass 命中缓存。
- 官方 Framework builder 生成 **6 个 bundles、14 项资源，总计 1,953,595,217 字节**。manifest 包含 MainMenu、HUD、Pause、Player、Impact、Target、4 项武器音频及 4 个 scene 条目。逐包大小、MD5 与 manifest 匹配，另记录 SHA256。
- 另用真实 Editor 执行 `FrameworkAssetSetup.ValidateBuiltResources`，退出码 **0**；日志确认固定 fork 来源及 `FIRSTREP PREPARED ASSETBUNDLES VERIFIED 14`。这是生产入口的资源覆盖与完整性检查，不是运行时听感/画面或 Player smoke。
- 构建日志保留旧 Timeline playable 缺失脚本和着色器支持警告；没有为了成功而移除原资源或降低品质。bundle 构建成功不消除这些迁移限制。

资源准备工具重新序列化了 Framework variant、HUD、MainMenu、Pause。已保存生成副本、差异及哈希；variant 的差异仅为空白，三个 UI Prefab 按层级/组件身份规范化后字段完全一致，变化来自本地对象 ID、文档顺序和空白。bundle 阶段完成后仅将这 4 个文件恢复为固定提交的精确字节，没有修改业务代码或包缓存。

## Player 空间预算与暂停原因

本轮输出目录在原有 `/tmp/firstrep-intermediates` 暂存区下独立分配，项目的 `Assets/StreamingAssets/AssetBundles` 和 `Builds/Linux` 指向新目录。不是新执行环境或持久备份。Player 目录仍为空。

14:33 UTC 预算采样如下，保留原有 1 GiB 保护：

| 项目 | 字节 |
|---|---:|
| `/tmp` 实测可用 | 5,637,783,552 |
| 历史同内容 Player 去除旧 StreamingAssets | 2,014,546,551 |
| 新 bundles 实测总量 | 1,953,595,217 |
| 据此估计的新 Player | 3,968,141,768 |
| 需重新生成的 PlayerDataCache，按历史 data.unity3d 估计 | 1,821,395,694 |
| 必留余量 | 1,073,741,824 |
| 预计总需求 | 6,863,279,286 |
| **预计缺口** | **1,225,495,734** |

这是容量估算，不是新 Player 实测体积；额外临时峰值尚未计入。当时工作盘也仅余约 1.46 GB，扣除保护后不足以填补缺口。因此未启动 Player，而不是把旧 Player 或脚本增量更新当作新完整构建。构建运行期间 Unity 管理的 PlayerDataCache 大文件已不在，不能按旧缓存仍可直接复用来减去这项预算；本代理没有主动删除它或其他既有文件。

## 保留和启动守卫

旧 Player 保存在 `Builds/Linux-historical-9a7c412-20261020T135205Z`，旧 bundles 保存在 `CachePreservation~/pre-current-player-20261020T135205Z/AssetBundles`。两个原目录仅改名，**336 个文件、2,146,234,262 字节**逐一与改名前的大小/SHA256 比较通过。原始日志、生成副本和新 bundles 均保留本地。

旧 Release 压缩包仍在，1,818,104,222 字节，重新计算 SHA256 为 `13d81e70b907d9165cba9a81d5716891828573b539820ec94c60ef09a9ca2ee3`。这与历史记录一致，不是外部备份或当前版本产物。

实际执行了更新后的守卫：新预留目录没有可执行文件/来源清单，直接 provenance 检查和默认启动入口均拒绝；显式指定保留的历史 Player 时，也在二进制执行前拒绝。三次退出均为 **1**，没有启动任何 Player。没有为绕过守卫创建清单。

## 下一步和交付边界

先解决构建暂存空间预算，继续保留 1 GiB 保护；本轮没有删除旧产物或原始证据，也没有另换环境/目的地。输入不变时使用已校验的这批 bundles，继续完整 Player 构建，生成真实文件哈希和源码/fork/Unity 清单，再通过启动守卫执行 opt-in headless smoke。当前 Player 和 smoke 状态保持“未执行”，不是通过或实际构建失败。

真实显示、键鼠、HDRP 材质/后处理、音频设备和代表性硬件性能仍按 [收尾验收清单](DEVELOPMENT-CLOSEOUT-20261020.md) 逐项验收。本轮未安装图形/音频工具、启动显示服务、提交/合并新的 Framework PR，或恢复 Release 上传。

[结构化结果与逐包哈希](current-bundles-build-validation-20261020.json)、[4 份脱敏日志及原件/副本哈希](evidence/current-bundles-20261020/manifest.json) 保存到既有源码仓库。日志副本明确标记脱敏范围。原始日志和大型产物仍仅本地，外部备份未验证，不能宣称 Player 已交付或构建产物已可靠备份。
