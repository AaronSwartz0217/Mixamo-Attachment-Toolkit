# 小怪模型挂点与预制体交付标准

本标准用于将新导入的 Humanoid 小怪整理为与已确认的 ClayHusk 一致的挂点和资源配置。相同标准指挂点语义、参考空间姿态、层级和配置规则一致，不要求不同角色的骨架尺寸、骨骼局部轴或碰撞体数值完全相同。

标准版本：1.0。确认日期：2026-10-23。适用插件：`com.codex.mixamo-attachment-toolkit` 0.3.3。适用编辑器基线：Unity 2022.3 LTS。

## 参考资源和保护范围

项目内以已经人工确认的 `ClayHusk.prefab` 及 `ClayHusk/ClayHusk.fbx` 为唯一主参考。`Enemy.prefab` 和 `Human_WithReferenceBones.fbx` 可以辅助对照，但不得在批处理中自动替换主参考，更不能为匹配另一参考而改变已确认的轴向。

- 保留主参考的 FBX、Prefab、材质及其 `.meta`，批处理前后校验内容不变。
- 原文件备份放在项目目录之外，不得进入 `Assets`、插件包或交付提交。
- 保留目标角色的网格、蒙皮、原有骨骼、比例和资产 GUID。新增挂点不等于替换整套骨架。
- 项目内部移动和重命名使用 Unity AssetDatabase；不得重新生成已有资源的 `.meta`。
- 不复制 ClayHusk 的完整可见内容来代替目标角色。新小怪不得因此携带参考角色的武器、网格或贴图。

## 插件职责和制作顺序

必须先使用插件配置挂点，再整理 FBX、Prefab、胶囊体和材质。不能将直接复制标准 Prefab、手工追加节点或编辑模型二进制作为插件绑定的替代方案。

1. 在工作副本中加载目标角色，确认 Humanoid Avatar 有效，并显式选择已批准的 ClayHusk 参考资源。
2. 调用插件创建或修复全部标准 Socket，再同步参考挂点和手部辅助节点。保持手掌长度适配及参考空间补偿开启。
3. 检查父子关系、位置和旋转；重复执行不得创建重名挂点。
4. 若交付要求 FBX 自带节点，将工作副本另存为带节点的 FBX，重新导入后与插件结果比较；正式替换必须保留资产 GUID，并复查蒙皮、比例和引用。
5. 由目标角色生成或更新正式 Prefab，保存插件挂点标记，配置胶囊体、材质及业务组件，然后保存并重新加载验收。

插件 0.3.3 负责挂点、辅助节点、装备预览和可选 FBX 导出，不会自动完成本标准中的胶囊体体型适配、贴图打包或整批资源迁移。FBX 导出需要 Unity 官方 FBX Exporter；正常创建挂点不需要重新导出原始模型。只有要求节点写入 FBX 本体时才需要该步骤。

FBX 可以保存 Transform 层级和姿态，不能保存 Unity 的 `AttachmentSocket` MonoBehaviour。使用插件运行时 API 的 Prefab 必须保留对应组件和插件运行时依赖；仅将 FBX 交给不安装插件的其他项目时，普通 Transform 挂点仍可使用，但不包含插件运行时功能。

## 标准挂点

每个角色包含以下 16 个唯一 Socket。表中为 Humanoid 语义骨骼及 Mixamo 回退名称，具体定位由插件解析。

| 节点名 | Humanoid 骨骼 | Mixamo 回退名称 |
|---|---|---|
| Socket_RightHandWeapon | RightHand | RightHand |
| Socket_LeftHandWeapon | LeftHand | LeftHand |
| Socket_TwoHandWeapon | RightHand | RightHand |
| Socket_LeftHandShield | LeftHand | LeftHand |
| Socket_LeftForearmShield | LeftLowerArm | LeftForeArm |
| Socket_RightHandShield | RightHand | RightHand |
| Socket_NeckAccessory | Neck | Neck |
| Socket_HeadAccessory | Head | Head |
| Socket_Backpack | UpperChest | Spine2 |
| Socket_BackWeapon | UpperChest | Spine2 |
| Socket_CapeRoot | UpperChest | Spine2 |
| Socket_ChestAccessory | UpperChest | Spine2 |
| Socket_Belt | Hips | Hips |
| Socket_LeftHip | Hips | Hips |
| Socket_RightHip | Hips | Hips |
| Socket_RearHip | Hips | Hips |

另有 4 个唯一手部辅助节点，父子关系必须为：

```text
RightHand
└── transform1
    └── joint1
LeftHand
└── transform2
    └── joint2
```

这批角色的 `transform1`、`transform2` 局部位置保持为零，位于各自手骨原点；握持偏移由对应子节点表达。不得在修正父节点位置时擅自调整已确认正确的 `joint1`、`joint2` 朝向。

`joint1` 与 `Socket_RightHandWeapon` 的握持位置重合，`joint2` 与 `Socket_LeftHandWeapon` 的握持位置重合。骨骼局部轴不同时，应通过角色空间转换保持参考效果，不能直接逐项复制欧拉角或根据截图中单条红轴判断对错。手部偏移按左右手各自的手掌长度适配；朝向不能因体型缩放而改变。

## FBX 和 Prefab 要求

### FBX

- 在 FBX 本体中交付全部 20 个节点，名称、对应骨骼和层级完整，不仅存在于场景实例或 Prefab 覆盖中。
- 导入后 Humanoid Avatar 的 `isHuman`、`isValid` 均为真；原网格顶点数量、蒙皮骨骼映射、bindposes 和整体尺寸不得发生非预期变化。
- 原有动画资源和引用不得被无关调整；新增节点不得引入比例翻转、镜像或异常缩放。
- 用插件从目标原骨架重新生成预期节点，比较导入后的 FBX 结果，不以“看起来有节点”作为通过依据。

### Prefab

- 引用各自正式 FBX、Avatar 和外部材质，不能残留 ClayHusk 的模型引用。
- 节点结构和姿态与对应 FBX 及插件预期一致；每个 Socket 有正确的 `AttachmentSocket` 类型标记。
- 原有小怪保留原先已配置的装备和业务引用；新小怪默认空装备，除非另有明确的装备需求。
- 不得因复制模板附带 `SpearObj`、`ShieldObj`、`BowObj`、`PennantObj`、`WhiskObj`。新小怪的 `weaponModels` 应为空。
- 无 Missing Script、丢失本地 fileID 或损坏的 GUID 引用。验收必须针对保存后重新加载的正式 Prefab，而非未保存工作副本。

## 胶囊体配置

以 ClayHusk 的根节点 CapsuleCollider 为配置基线：启用、Y 轴方向、非 Trigger。物理材质、接触偏移、层及接触相关设置沿用主参考；高度、半径和中心按目标体型重新计算，不直接复制固定值。

本批参考值均在 Prefab 根节点局部空间测量：胶囊高度 `1.7762663`，半径 `0.20890318`，中心约为 `(0.00346905, 0.88813317, -0.0000001192)`。参考蒙皮网格高度约 `1.849098`，躯干宽度约 `0.448950`，深度约 `0.321582`。计算时使用未舍入的实际测量值。

测量方法：对角色 SkinnedMeshRenderer 调用 `BakeMesh`，将顶点转到 Prefab 根节点局部空间，合并身体包围盒；排除武器。躯干测量取 Hips 到 Neck 的高度区间，横向限制在两侧 LeftArm、RightArm 之间，并向两侧各扩展身体高度的 2%。测量必须使用一致的静止姿态。若缺少必要骨骼或蒙皮，停止并报告，不以任意网格包围盒代替。

设目标与参考身体高度比为 `sH`，躯干宽度比为 `sW`，深度比为 `sD`：

```text
目标胶囊高度 = 参考胶囊高度 × sH
目标胶囊半径 = min(参考胶囊半径 × max(sW, sD), 目标胶囊高度 / 2)
目标胶囊底部 = 目标身体最低点 + (参考胶囊底部 - 参考身体最低点) × sH
目标中心 Y = 目标胶囊底部 + 目标胶囊高度 / 2
```

中心 X、Z 跟随目标 Hips，保留参考中心相对参考 Hips 的偏移，分别按躯干宽度比和深度比适配。最终检查胶囊落脚高度、躯干覆盖及运行时碰撞，不能用 T Pose 展开的双臂宽度决定半径。

## 材质和贴图配置

采用 ClayHusk 材质的 Shader 和渲染设置，但每个角色必须使用自己的贴图，不能残留参考角色贴图 GUID。本批基线为 URP Lit、不透明材质。

| 材质槽 | 目标贴图 | 导入要求 |
|---|---|---|
| _BaseMap 和 _MainTex | 本角色 BaseColor | sRGB 开启 |
| _BumpMap | 本角色 Normal | Texture Type 为 Normal Map，线性数据 |
| _MetallicGlossMap | 本角色 MetallicSmoothness | sRGB 关闭 |

金属度与光滑度打包采用 `R = Metallic 灰度`、`A = 1 - Roughness 灰度`。将 `_Metallic`、`_Smoothness` 设为 1 作为贴图乘数，并启用 `_NORMALMAP`、`_METALLICSPECGLOSSMAP`。数值 1 不代表整张材质统一为金属或全光滑。

本批贴图最大尺寸为 2048、开启 mipmaps、使用压缩，并通过 FBX 导入器关联各自外部材质。制作标准允许从参考材质复制配置后重新匹配自身贴图，但不允许复制参考模型或蒙皮来实现外观匹配。

## 文件命名和交付范围

使用英文资源名，FBX、Prefab、材质名称与角色名一致；目录移动保留 GUID。四个新角色命名为 MudMonk、Wayfarer、Acolyte、Mudling，分别对应泥僧人、行者、童子、泥者。

```text
Enemy/
├── CharacterName.prefab
├── CharacterName.prefab.meta
├── CharacterName.meta
└── CharacterName/
    ├── CharacterName.fbx
    ├── CharacterName.fbx.meta
    ├── CharacterName.mat
    ├── CharacterName.mat.meta
    ├── CharacterName.meta
    └── CharacterName/
        └── CharacterName_BaseColor、Normal、Metallic、Roughness、MetallicSmoothness 及对应 meta
```

所有被引用资源及其 `.meta` 一起提交。不得提交项目外备份、临时批处理脚本、测试场景、Library 缓存或未要求的场景改动。交付这些带 `AttachmentSocket` 的 Prefab 时，还需要项目中的插件依赖。

## 验收规则

以下为本标准 1.0 的资源验收阈值，测量对象和预期对象必须处于相同根节点空间与姿态：

| 检查项 | 通过条件 |
|---|---|
| 节点完整性 | 16 个唯一 Socket 加 4 个唯一辅助节点，父级正确 |
| 插件结果一致性 | 各节点局部位置误差不超过 0.00002 米，旋转误差不超过 0.06 度，缩放无非预期变化 |
| 手部握持位置 | joint1 和右手 Socket、joint2 和左手 Socket 的距离各不超过 0.00002 米 |
| 胶囊体 | 高度和底部位置与体型适配计算值误差不超过 0.00001 米，半径符合上述公式且直径不大于高度 |
| 模型有效性 | Avatar 有效，原有网格与蒙皮数据无非预期变化 |
| 引用和持久化 | 正式 FBX 和保存后重新加载的 Prefab 均通过检查，引用完整且无 Missing Script |
| 主参考保护 | ClayHusk 受保护文件与操作前备份的内容哈希一致 |
| 运行验收 | Idle、Walk、Run、Attack 及实际装备下无明显错位、异常轴向或缩放，碰撞与落脚满足玩法要求 |

资源验收和运行验收分开记录。只有前者通过时，应标记为“资源配置通过，待运行验收”，不能写为全部玩法测试完成。

## 本批资源验收记录

2026-10-23 检查的 11 个小怪为 BambooWayfarer、BrazenAvatar、BronzeWarden、GildedMarshal、Thornfist、VerdigrisSentry、Wickerling、MudMonk、Acolyte、Wayfarer、Mudling。

全部 11 个角色已实际调用插件 0.3.3 配置并比较节点结果，FBX 与插件输出一致，Prefab 保存并重新加载通过；Avatar 有效，节点完整，无丢失脚本。胶囊体已按体型适配，材质引用各自贴图，新 4 个角色不带模板武器。主参考 ClayHusk 内容未变。本批尚待动画装备和战斗碰撞运行验收。

本批项目小怪资源变更计 93 个文件，包含 `.meta`：原有 7 个角色各 3 个文件，共 21 个；新增 4 个角色各 18 个文件，共 72 个。这个数量不包含插件包、包依赖文件及其他项目改动，也不是以后批次必须满足的固定数量。

## 插件包体与追溯

配套离线包位于 [`Distribution~/com.codex.mixamo-attachment-toolkit-0.3.3.tgz`](../Distribution~/com.codex.mixamo-attachment-toolkit-0.3.3.tgz)，源码基线提交为 `00de45f5663c8b2ca09abb8ea790475eaf765c3b`。包体和当前项目已验证使用的包一致；本次上传标准文档不改变插件代码或版本号。

SHA256：`f79d6ddf301461a47bad27a025b24fed24042ba85283b2b966c6d57d6176c2a4`。

Unity Package Manager 中选择 `Add package from tarball...` 安装该文件。多人项目应将包放在项目 `Packages` 目录并通过相对路径安装，提交包体及 Package Manager 生成的依赖清单，避免引用个人电脑上的绝对路径。该包不包含 ClayHusk、小怪模型、贴图或项目专有脚本。
