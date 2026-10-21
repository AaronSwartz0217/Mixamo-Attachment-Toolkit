# Mixamo Attachment Toolkit

面向 Unity 2022.3 LTS 的 Mixamo/Humanoid 装备挂载工具。

## 功能

- 优先通过 Humanoid Avatar 定位骨骼。
- 无有效 Avatar 时按 Mixamo 骨骼名称回退查找。
- 一键创建武器、盾牌、项链、背包、披风和腰部 Socket。
- 重复执行不会重复创建已经标记的 Socket。
- 根据组件、目录或名称前缀识别装备类型。
- 在场景中预览装备并把局部姿态保存回 Prefab。
- 提供运行时装备、挂载和卸载 API。
- 使用 `WeaponAttachmentProfile` 保存基础恒定偏移和动作状态偏移。
- 支持 Default、Idle、Walk、Run、Sprint、Attack、Block、Holster 状态。
- 运行时状态切换采用平滑位置、旋转和缩放过渡。
- 自动查找 `Human_WithReferenceBones.fbx`（兼容旧 `T-pose-W.fbx`），同步完整 16 个标准挂点，并复制手部辅助骨骼。
- 编辑器场景修改支持 Undo。

## 安装

1. 打开 `Window > Package Manager`。
2. 点击 `+`。
3. 选择 `Add package from disk...`。
4. 选择本包根目录的 `package.json`。

## 快速开始

1. 打开 `Tools > Mixamo Attachment Toolkit`。
2. 选择 Project 中的角色 Prefab/FBX，点击“从角色资源创建场景工作副本”；也可以直接选择已有场景角色。
3. 扫描骨架并创建所需 Socket。
4. 选择装备 Prefab，创建角色与装备的同场景预览。
5. 在 Scene 中调整并保存装备局部姿态。
6. 点击“另存为带挂点角色 Prefab”，生成新的完整角色 Prefab。

插件不会直接修改原始模型 FBX。若只想保存空挂点，请在另存角色 Prefab 前删除装备预览；若希望生成默认带武器的角色，则保留装备后保存。

## 0.2 偏移 Profile 工作流

1. 创建角色工作副本、Socket 和装备预览。
2. 点击“创建偏移 Profile”，将 `.asset` 保存到项目目录。
3. 调整武器到正确握持位置，点击“保存为基础恒定偏移”。
4. 将“编辑状态”改为 Run、Sprint 等状态。
5. 在基础姿态上继续调整武器，点击“保存为当前状态偏移”。
6. 点击“应用当前 Profile”检查各状态结果。
7. 运行时调用 `attachmentController.SetState(AttachmentState.Run)` 切换状态。

最终姿态由基础偏移和状态偏移组合而成；状态偏移不会重复包含基础偏移。

## 导出为 FBX 源文件

场景工作副本创建挂点后，可点击“导出带参考骨骼的角色 FBX”。该功能需要 Unity 官方
FBX Exporter（`com.unity.formats.fbx`），且不会覆盖原始 FBX。

- 网格、身高和整体比例来自当前目标角色。
- `transform1/joint1`、`transform2/joint2` 的层级和局部姿态来自 T-pose-W。
- 开启手掌长度修正时，新增骨骼位置会适配目标角色手掌尺寸。
- 导出到 `Assets` 内时自动配置为 Humanoid、关闭动画导入，导入比例设为 1。
- 默认排除当前预览装备；需要时可勾选“FBX 中包含当前挂载的装备”。

## T-pose-W 参考骨骼

“参考模型的标准挂点和手部辅助骨骼”默认勾选。插件会在 `Assets` 下自动查找，优先使用：

```text
Human_WithReferenceBones.fbx
```

创建 Socket 时会：

1. 将参考模型中的 16 个 `Socket_*` 局部姿态同步到目标角色对应骨骼。
2. 在右手下复制 `transform1/joint1`，在左手下复制 `transform2/joint2`。
3. 左右手武器、双手武器及手持盾牌的位置按对应手掌长度比例修正。
4. 参考模型缺少手部 Socket 时，兼容使用 `joint1/joint2` 姿态。
5. 已有同名辅助节点和 Socket 时只更新姿态，不重复创建。

取消该选项后，插件继续使用标准零偏移 Socket。参考姿态仍可通过装备 Profile 和 Scene 手工调整覆盖。

## 命名前缀

| 前缀 | 类型 |
|---|---|
| `WPN_RH_` | 右手武器 |
| `WPN_LH_` | 左手武器 |
| `WPN_2H_` | 双手武器 |
| `WPN_BACK_` | 背部武器 |
| `SHIELD_LH_` | 左手盾牌 |
| `SHIELD_LA_` | 左前臂盾牌 |
| `SHIELD_RH_` | 右手盾牌 |
| `NECK_` | 项链 |
| `HEAD_` | 头部附件 |
| `BACKPACK_` | 背包 |
| `CAPE_` | 披风根部 |
| `CHEST_` | 胸前附件 |
| `BELT_` | 腰带 |
| `HIP_L_` | 左腰附件 |
| `HIP_R_` | 右腰附件 |
| `HIP_REAR_` | 后腰附件 |

## 文件夹自动识别

插件会读取 Prefab 在 Unity Project 中的资产路径，当前支持以下目录片段：

| 目录片段 | 类型 |
|---|---|
| `Weapons/RightHand/` | 右手武器 |
| `Weapons/LeftHand/` | 左手武器 |
| `Weapons/TwoHanded/` | 双手武器 |
| `Weapons/Back/` | 背部武器 |
| `Shields/Hand/` | 左手盾牌 |
| `Shields/Forearm/` | 左前臂盾牌 |
| `Backpacks/` | 背包 |
| `Capes/` | 披风根部 |
| `Neck/` | 项链/颈部附件 |
| `Head/` | 头部附件 |
| `Chest/` | 胸前附件 |
| `Belts/` | 腰带 |
| `HipAccessories/Left/` | 左腰附件 |
| `HipAccessories/Right/` | 右腰附件 |
| `HipAccessories/Rear/` | 后腰附件 |

推荐根目录为 `Assets/Attachments/`，但不是强制要求；只要完整资产路径包含上表中的目录结构即可。目录匹配不区分大小写。

识别优先级：

1. Prefab 已有 `AttachmentItem` 组件。
2. 文件夹路径。
3. 对象名称前缀。
4. 无法识别时由用户手动选择。

## 说明

- 插件不会修改 Mixamo 原始骨骼层级。
- 双手武器只挂到右手 Socket，左右手姿势由专用动画负责。
- 披风和项链的物理摆动不属于本插件首版范围。
- 对于体型差异较大的角色，需要单独检查装备偏移和动画穿模。
