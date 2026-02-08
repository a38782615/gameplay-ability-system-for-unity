# EX Gameplay Ability System (EX-GAS) 架构文档

> 本文档描述 EX-GAS 项目的整体架构设计，帮助开发者快速理解系统结构。

## 目录

- [项目概述](#项目概述)
- [目录结构](#目录结构)
- [核心架构设计](#核心架构设计)
- [核心模块详解](#核心模块详解)
- [类关系图](#类关系图)
- [关键设计模式](#关键设计模式)
- [使用流程](#使用流程)
- [依赖项](#依赖项)

---

## 项目概述

EX-GAS 是对 **Unreal Engine 的 Gameplay Ability System** 的 Unity 实现。

**核心定位**：一套游戏能力系统框架，为开发者提供灵活而强大的框架，用于实现和管理游戏中的各种角色能力、技能和效果。

**一句话概括**：**WHO DO WHAT**
- **Who**：AbilitySystemComponent (ASC) - GAS的实例对象，体系运转的基础单位
- **Do**：Ability - 游戏中可以触发的一切行为和技能  
- **What**：GameplayEffect (GE) - 掌握游戏内元素的属性实际控制权

---

## 目录结构

```
mygameplay-ability-system-for-unity/
├── Assets/
│   ├── GAS/                          # 核心GAS框架（作为Unity Package）
│   │   ├── Runtime/                  # 运行时核心代码
│   │   │   ├── Core/                 # 核心系统
│   │   │   │   ├── GameplayAbilitySystem.cs  # GAS单例管理器
│   │   │   │   ├── GasHost.cs                # GAS宿主（MonoBehaviour）
│   │   │   │   └── GasCache.cs               # 缓存工具
│   │   │   ├── Component/            # ASC组件
│   │   │   │   ├── AbilitySystemComponent.cs
│   │   │   │   └── AbilitySystemComponentPreset.cs
│   │   │   ├── Ability/              # 能力系统
│   │   │   │   ├── AbstractAbility.cs        # 能力数据基类
│   │   │   │   ├── AbilitySpec.cs            # 能力运行实例基类
│   │   │   │   ├── AbilityAsset.cs           # 能力配置基类
│   │   │   │   ├── AbilityContainer.cs       # 能力容器
│   │   │   │   ├── TimelineAbility/          # 时间轴能力
│   │   │   │   └── TargetCatcher/            # 目标捕获器
│   │   │   ├── Effects/              # 游戏效果
│   │   │   │   ├── GameplayEffect.cs         # 效果数据类
│   │   │   │   ├── GameplayEffectSpec.cs     # 效果运行实例
│   │   │   │   ├── GameplayEffectAsset.cs    # 效果配置
│   │   │   │   ├── GameplayEffectContainer.cs
│   │   │   │   ├── GameplayEffectStacking.cs # 堆叠系统
│   │   │   │   ├── Modifier/                 # 修改器（MMC）
│   │   │   │   └── Execution/                # 执行计算
│   │   │   ├── Attribute/            # 属性系统
│   │   │   │   ├── AttributeBase.cs          # 属性基类
│   │   │   │   ├── AttributeAggregator.cs    # 属性聚合器
│   │   │   │   └── Value/                    # 属性值类型
│   │   │   ├── AttributeSet/         # 属性集
│   │   │   │   ├── AttributeSet.cs           # 属性集基类
│   │   │   │   └── AttributeSetContainer.cs
│   │   │   ├── Tags/                 # 标签系统
│   │   │   │   ├── GameplayTag.cs            # 标签类
│   │   │   │   ├── GameplayTagSet.cs         # 不可变标签集
│   │   │   │   ├── GameplayTagContainer.cs   # 可变标签集
│   │   │   │   └── GameplayTagAggregator.cs  # 标签聚合器
│   │   │   ├── Cue/                  # 游戏提示
│   │   │   └── Utils/                # 工具类
│   │   ├── Editor/                   # 编辑器扩展
│   │   │   ├── Ability/              # 能力编辑器
│   │   │   ├── Effect/               # 效果编辑器
│   │   │   ├── Tags/                 # 标签管理器
│   │   │   ├── Attributes/           # 属性编辑器
│   │   │   ├── AttributeSet/         # 属性集编辑器
│   │   │   └── GASProjectSettings/   # 项目设置
│   │   └── General/                  # 通用工具（ObjectPool等）
│   │
│   ├── Demo/                         # 示例项目
│   │   ├── Script/
│   │   │   ├── GAS/                  # GAS相关实现
│   │   │   │   ├── Ability/          # 自定义能力
│   │   │   │   │   ├── Asset/        # AbilityAsset配置类
│   │   │   │   │   ├── Move.cs
│   │   │   │   │   ├── Jump.cs
│   │   │   │   │   └── NormalFire.cs
│   │   │   │   ├── AbilityTask/      # 能力任务
│   │   │   │   ├── Cue/              # 自定义Cue
│   │   │   │   └── TargetCatcher/    # 自定义目标捕获器
│   │   │   ├── Gen/                  # 自动生成的代码
│   │   │   │   ├── GTagLib.gen.cs    # 标签库
│   │   │   │   ├── GAttrLib.gen.cs   # 属性库
│   │   │   │   ├── GAttrSetLib.gen.cs # 属性集库
│   │   │   │   └── GAbilityLib.gen.cs # 能力库
│   │   │   ├── Element/              # 游戏元素
│   │   │   ├── Fight/                # 战斗系统
│   │   │   └── UI/                   # UI系统
│   │   ├── Resources/                # 资源配置
│   │   └── Scene/                    # 场景
│   │
│   └── Plugins/                      # 第三方插件
│       ├── Sirenix/                  # Odin Inspector（必需）
│       └── ProCamera2D/              # 摄像机插件
│
└── ProjectSettings/                  # Unity项目设置（含GAS配置）
    ├── AttributeAsset.asset
    ├── AttributeSetAsset.asset
    ├── GameplayTagsAsset.asset
    └── GASSettingAsset.asset
```

---

## 核心架构设计

### 1. 系统总览

```
┌─────────────────────────────────────────────────────────────────┐
│                    GameplayAbilitySystem (GAS)                  │
│                         单例管理器                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   WHO (谁)              DO (做)              WHAT (什么)         │
│   ┌─────────┐          ┌─────────┐          ┌─────────────┐    │
│   │   ASC   │ ──────▶  │ Ability │ ──────▶  │ GameplayEffect│   │
│   │ (单位)  │          │ (能力)  │          │   (效果)      │   │
│   └─────────┘          └─────────┘          └─────────────┘    │
│       │                     │                      │            │
│       ▼                     ▼                      ▼            │
│   ┌─────────┐          ┌─────────┐          ┌─────────────┐    │
│   │  Tags   │          │AbilitySpec│        │   Modifier   │   │
│   │Attribute│          │ (运行实例)│         │    (MMC)     │   │
│   └─────────┘          └─────────┘          └─────────────┘    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2. AbilitySystemComponent (ASC) 结构

ASC 是 GAS 的核心运行单位，每个游戏单位（玩家、敌人等）都需要挂载 ASC 组件。

```
AbilitySystemComponent (MonoBehaviour)
├── GameplayTagAggregator      // 标签聚合器
│   ├── FixedTags              // 固有标签（种族、职业等）
│   └── DynamicTags            // 动态标签（由GE/Ability添加）
│
├── AttributeSetContainer      // 属性集容器
│   └── AttributeSet[]         // 属性集（如 AS_Fight: HP, MP, ATK）
│       └── AttributeBase[]    // 属性（BaseValue + CurrentValue）
│
├── AbilityContainer           // 能力容器
│   └── AbilitySpec[]          // 能力运行实例（Move, Attack, Jump等）
│
├── GameplayEffectContainer    // 效果容器
│   └── GameplayEffectSpec[]   // 效果运行实例（Buff/Debuff）
│
└── Level                      // 等级
```

### 3. 数据流与生命周期

```
┌──────────────────────────────────────────────────────────────────┐
│                        数据配置层 (Editor)                        │
├──────────────────────────────────────────────────────────────────┤
│  AbilityAsset    GameplayEffectAsset    AttributeSet配置          │
│  (ScriptableObject)                     (ProjectSettings)         │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼ 代码生成
┌──────────────────────────────────────────────────────────────────┐
│                        生成代码层 (Gen)                           │
├──────────────────────────────────────────────────────────────────┤
│  GTagLib.gen.cs   GAttrSetLib.gen.cs   GAbilityLib.gen.cs        │
│  (标签常量)        (属性集类)           (能力映射)                 │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼ 运行时实例化
┌──────────────────────────────────────────────────────────────────┐
│                        运行时层 (Runtime)                         │
├──────────────────────────────────────────────────────────────────┤
│  AbstractAbility → AbilitySpec (运行实例)                         │
│  GameplayEffect  → GameplayEffectSpec (运行实例)                  │
│  AttributeSet    → AttributeBase (运行实例)                       │
└──────────────────────────────────────────────────────────────────┘
```

---

## 核心模块详解

### 1. GameplayTag 系统

**用途**：替代布尔值/枚举，控制游戏逻辑

**特点**：
- 树形层级结构（如 `State.Debuff.Stun`）
- 支持父子级匹配（`Debuff.Poison` 持有 `Debuff`）

**核心类**：

| 类名 | 说明 |
|------|------|
| `GameplayTag` | 单个标签 |
| `GameplayTagSet` | 不可变标签集合（用于配置数据） |
| `GameplayTagContainer` | 可变标签集合（用于运行时） |
| `GameplayTagAggregator` | ASC专用标签管理器 |

**示例**：
```csharp
// 检查标签
if (asc.HasTag(GTagLib.State_Debuff_Stun)) { ... }

// 检查是否有任意Debuff
if (asc.HasAnyTags(new GameplayTagSet(GTagLib.State_Debuff))) { ... }
```

### 2. Attribute 系统

**用途**：管理游戏单位的数值属性

**核心概念**：
- **AttributeBase**：单个属性（包含 BaseValue 和 CurrentValue）
- **AttributeSet**：属性集合（如 AS_Fight 包含 HP, MP, ATK）
- **AttributeAggregator**：处理 Modifier 叠加计算

**属性值结构**：
```csharp
AttributeValue
├── BaseValue      // 基础值（被Instant GE修改）
└── CurrentValue   // 当前值（= BaseValue + Modifier叠加）
```

### 3. GameplayEffect 系统

**用途**：修改属性、授予标签、触发Cue

**持续策略**：

| 策略 | 说明 |
|------|------|
| `Instant` | 瞬时执行，立即修改BaseValue |
| `Duration` | 限时持续，到期自动移除 |
| `Infinite` | 永久持续，需手动移除 |

**核心功能**：
- **Modifier**：属性修改器（Add/Multiply/Override）
- **MMC**：修改器幅度计算（ScalableFloat/AttributeBased/SetByCaller/Custom）
- **Stacking**：堆叠系统（按来源/按目标叠加）
- **GrantedAbility**：授予能力
- **GameplayCue**：游戏提示（特效、音效等）

**Tag控制**：

| Tag类型 | 说明 |
|---------|------|
| `AssetTags` | 描述性标签 |
| `GrantedTags` | 授予目标的标签 |
| `ApplicationRequiredTags` | 应用要求（目标必须有所有这些标签） |
| `OngoingRequiredTags` | 持续要求（失去标签则失活） |
| `ApplicationImmunityTags` | 免疫标签（目标有任意则免疫） |
| `RemoveGameplayEffectsWithTags` | 移除带这些标签的GE |

### 4. Ability 系统

**用途**：定义游戏中的能力/技能

**三层结构**：

```
AbilityAsset (ScriptableObject)     ← 配置数据
       ↓
AbstractAbility                      ← 运行时数据类
       ↓
AbilitySpec                          ← 运行实例（实际逻辑）
```

**生命周期**：
```
TryActivateAbility() → CanActivate() → ActivateAbility()
                                              ↓
                                        AbilityTick()
                                              ↓
                              TryEndAbility() / TryCancelAbility()
                                              ↓
                                EndAbility() / CancelAbility()
```

**TimelineAbility**：
- 可视化时间轴编辑器
- 支持轨道：Instant Cue、Release Effect、Instant Task、Durational Cue、Buff、Ongoing Task

---

## 类关系图

```
                    ┌─────────────────────┐
                    │GameplayAbilitySystem│ (单例)
                    │     (GAS核心)        │
                    └──────────┬──────────┘
                               │ 管理
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                  AbilitySystemComponent (ASC)                 │
│                      (MonoBehaviour)                          │
├──────────────────────────────────────────────────────────────┤
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │GameplayTag     │  │AttributeSet    │  │AbilityContainer│  │
│  │Aggregator      │  │Container       │  │                │  │
│  │                │  │                │  │ ┌────────────┐ │  │
│  │ FixedTags      │  │ AS_Fight       │  │ │AbilitySpec │ │  │
│  │ DynamicTags    │  │  - HP          │  │ │ (Move)     │ │  │
│  │                │  │  - MP          │  │ │ (Jump)     │ │  │
│  │                │  │  - ATK         │  │ │ (Attack)   │ │  │
│  └────────────────┘  └────────────────┘  └────────────────┘  │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │              GameplayEffectContainer                    │  │
│  │  ┌──────────────────┐  ┌──────────────────┐            │  │
│  │  │GameplayEffectSpec│  │GameplayEffectSpec│  ...       │  │
│  │  │ (Buff_PowerUp)   │  │ (Debuff_Poison)  │            │  │
│  │  └──────────────────┘  └──────────────────┘            │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

---

## 关键设计模式

| 模式 | 应用 | 说明 |
|------|------|------|
| **单例模式** | `GameplayAbilitySystem.GAS` | 全局唯一的GAS管理器 |
| **对象池模式** | `ObjectPool` | Spec实例复用，减少GC |
| **规格模式** | Asset → Data → Spec | 配置与运行时分离 |
| **聚合器模式** | `GameplayTagAggregator`, `AttributeAggregator` | 统一管理和计算 |
| **容器模式** | 各种Container类 | 管理运行时实例集合 |
| **代码生成** | `*.gen.cs` | 编辑器生成类型安全代码 |

---

## 使用流程

### 1. 配置阶段（Editor）

1. **配置 GameplayTag**：在 Tag Manager 中创建标签树
2. **配置 Attribute**：定义属性名称（HP, MP, ATK等）
3. **配置 AttributeSet**：创建属性集，选择包含的属性
4. **创建 GameplayEffect**：配置效果（Modifier、Tag、Cue等）
5. **创建 Ability**：实现 AbilityAsset + AbstractAbility + AbilitySpec
6. **创建 ASC Preset**：配置单位的初始属性集、标签、能力

### 2. 代码生成

每次配置修改后，点击对应的"生成"按钮：
- `GTagLib.gen.cs` - 标签常量
- `GAttrLib.gen.cs` - 属性常量
- `GAttrSetLib.gen.cs` - 属性集类
- `GAbilityLib.gen.cs` - 能力映射

### 3. 运行时初始化

```csharp
// 1. 初始化GAS缓存（游戏启动时）
GasCache.CacheAttributeSetName(GAttrSetLib.TypeToName);

// 2. ASC自动注册（OnEnable时自动调用）
// GameplayAbilitySystem.GAS.Register(asc);

// 3. 初始化ASC（使用Preset或手动）
asc.InitWithPreset(level, preset);
```

### 4. 运行时使用

```csharp
// 激活能力
asc.TryActivateAbility("Attack", targetArg);

// 施加效果
asc.ApplyGameplayEffectTo(damageEffect, targetAsc);

// 检查标签
if (asc.HasTag(GTagLib.State_Invincible)) { ... }

// 获取属性值
float hp = asc.GetAttributeCurrentValue("Fight", "HP") ?? 0;
```

---

## 依赖项

| 依赖 | 版本要求 | 说明 |
|------|----------|------|
| **Odin Inspector** | 3.2+ | **必需**，付费插件，用于编辑器UI |
| Unity | 2021+ | 推荐版本 |

---

## 参考资料

- [UE GAS 中文文档](https://github.com/BillEliot/GASDocumentation_Chinese)
- [EX-GAS 入门教程系列](https://zhuanlan.zhihu.com/p/688111182)
- 项目 README.md

---

*文档生成时间：2026-02-07*
