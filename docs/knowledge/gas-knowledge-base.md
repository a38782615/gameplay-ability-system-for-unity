# EX-GAS 知识库

> 本文档记录 EX-GAS (Gameplay Ability System) 的核心概念和使用方法。

---

## 目录

- [1. 核心概念](#1-核心概念)
- [2. 代码生成机制](#2-代码生成机制)
- [3. 属性集使用](#3-属性集使用)
- [4. 能力触发 GameplayEffect 流程](#4-能力触发-gameplayeffect-流程)
- [5. 自定义能力开发](#5-自定义能力开发)

---

## 1. 核心概念

### 1.1 WHO DO WHAT 模型

```
WHO (谁)           DO (做)            WHAT (什么)
    │                  │                   │
    ▼                  ▼                   ▼
   ASC    ────────▶  Ability  ────────▶  GameplayEffect
 (单位)              (能力)               (效果)
```

- **ASC (AbilitySystemComponent)**: GAS 的运行单位，挂载在 GameObject 上
- **Ability**: 可触发的行为/技能
- **GameplayEffect (GE)**: 实际修改属性的效果

### 1.2 数据与运行时分离

| 层级 | 类型 | 说明 |
|------|------|------|
| **配置层** | `*Asset` (ScriptableObject) | 在编辑器中配置的数据 |
| **数据层** | `Abstract*` / `Gameplay*` | 运行时数据类 |
| **实例层** | `*Spec` | 运行时实例，包含实际逻辑 |

示例：
```
AbilityAsset (配置) → AbstractAbility (数据) → AbilitySpec (实例)
GameplayEffectAsset (配置) → GameplayEffect (数据) → GameplayEffectSpec (实例)
```

---

## 2. 代码生成机制

### 2.1 生成的文件

| 文件 | 内容 | 触发方式 |
|------|------|----------|
| `GTagLib.gen.cs` | 所有 GameplayTag 常量 | Tag Manager → 生成 TagLib |
| `GAttrLib.gen.cs` | 所有 Attribute 常量 | Attribute Manager → 生成 AttrLib |
| `GAttrSetLib.gen.cs` | 所有 AttributeSet 类 | AttributeSet Manager → 生成 AttrSetLib |
| `GAbilityLib.gen.cs` | 所有 Ability 信息 | Ability Overview → Generate Ability Collection |

### 2.2 GAbilityLib 生成流程

```csharp
// AbilityCollectionGenerator.cs 核心逻辑

// 1. 扫描所有 AbilityAsset
var abilityAssets = EditorUtil.FindAssetsByType<AbilityAsset>(GASSettingAsset.GameplayAbilityLibPath);

// 2. 为每个 AbilityAsset 生成 AbilityInfo
foreach (var ability in abilityAssets)
{
    writer.WriteLine(
        $"public static AbilityInfo {ability.UniqueName} = new AbilityInfo {{ " +
        $"Name = \"{ability.UniqueName}\", " +
        $"AssetPath = \"{path}\"," +
        $"AbilityClassType = typeof({ability.InstanceAbilityClassFullName}) }};");
}

// 3. 生成 AbilityMap 字典
```

### 2.3 让新能力出现在 GAbilityLib 中

1. 创建 `AbilityAsset` 子类（如 `AANormalFire.cs`）
2. 重写 `AbilityType()` 返回对应的 Ability 类型
3. 在 Unity 中创建 `.asset` 文件
4. 设置 `UniqueName`
5. **点击 "Generate Ability Collection" 按钮**

---

## 3. 属性集使用

### 3.1 为什么 `ASC.AttrSet<T>()` 返回空？

**原因**: ASC 只能获取**已添加**的属性集。

**解决方案**:

#### 方式 1: 在 ASC Preset 中配置
```
ASC_Player.asset
└── AttributeSets: ["Fight", "Bullet"]  ← 添加需要的属性集
```

#### 方式 2: 代码中动态添加
```csharp
// 先添加属性集
asc.AttributeSetContainer.AddAttributeSet<AS_Bullet>();

// 然后才能获取
var bulletAttrSet = asc.AttrSet<AS_Bullet>();
bulletAttrSet.InitATK(10f);
```

### 3.2 属性操作方法

```csharp
var attrSet = asc.AttrSet<AS_Fight>();

// 初始化（同时设置 BaseValue 和 CurrentValue）
attrSet.InitHP(100f);

// 只设置基础值
attrSet.SetBaseHP(100f);

// 读取值
float currentHP = attrSet.HP.CurrentValue;
float baseHP = attrSet.HP.BaseValue;

// 设置范围限制
attrSet.SetMinMaxHP(0f, 100f);
```

### 3.3 通过 ASC 直接访问

```csharp
// 获取当前值
float? hp = asc.GetAttributeCurrentValue("Fight", "HP");

// 获取基础值
float? baseHp = asc.GetAttributeBaseValue("Fight", "HP");
```

---

## 4. 能力触发 GameplayEffect 流程

### 4.1 TimelineAbility 方式（如 Attack）

```
┌─────────────────────────────────────────────────────────────────┐
│                    TimelineAbility 触发 GE 流程                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. ASC.TryActivateAbility("Attack")                           │
│            ↓                                                    │
│  2. TimelineAbilitySpec.ActivateAbility()                      │
│            ↓                                                    │
│  3. TimelineAbilityPlayer.Play()                               │
│            ↓                                                    │
│  4. TimelineAbilityPlayer.Tick() (每帧)                        │
│            ↓                                                    │
│  5. TickFrame_ReleaseGameplayEffects(frame)                    │
│       ├─ TargetCatcher.CatchTargets()  ← 捕获目标              │
│       └─ ASC.ApplyGameplayEffectTo(GE, target)  ← 应用效果     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**关键代码** (`TimelineAbilityPlayer.cs`):
```csharp
private void TickFrame_ReleaseGameplayEffects(int frame)
{
    foreach (var mark in _cacheReleaseGameplayEffect)
    {
        if (frame == mark.startFrame)
        {
            // 1. 捕获目标
            var catcher = mark.TargetCatcher;
            catcher.CatchTargetsNonAllocSafe(_abilitySpec.Target, _targets);

            // 2. 对每个目标应用 GE
            foreach (var asc in _targets)
            {
                foreach (var gea in mark.gameplayEffectAssets)
                {
                    _abilitySpec.Owner.ApplyGameplayEffectTo(gea.SharedInstance, asc);
                }
            }
        }
    }
}
```

### 4.2 自定义 AbilitySpec 方式（如 NormalFire）

```csharp
public class NormalFireSpec : AbilitySpec<NormalFire>
{
    public override void ActivateAbility()
    {
        DoCost();           // 消耗和冷却
        SpawnBullet();      // 生成子弹
        TryEndAbility();    // 立即结束
    }
}

// 子弹碰撞时应用伤害
public class Bullet : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var targetASC = other.GetComponent<AbilitySystemComponent>();
        if (targetASC != null && !IsSameFaction(targetASC))
        {
            // 应用伤害效果
            _owner.ApplyGameplayEffectTo(_damageEffect, targetASC);
            Destroy(gameObject);
        }
    }
}
```

### 4.3 两种方式对比

| 特性 | TimelineAbility | 自定义 AbilitySpec |
|------|-----------------|-------------------|
| 配置方式 | 可视化时间轴编辑 | 代码实现 |
| 适用场景 | 有前摇/后摇的技能 | 瞬时技能 |
| GE 触发时机 | 指定帧触发 | 代码控制 |
| 目标选择 | TargetCatcher 配置 | 代码逻辑 |
| 灵活性 | 中等 | 高 |

---

## 5. 自定义能力开发

### 5.1 三件套结构

```
1. AAXxx.cs (AbilityAsset)     ← 配置数据，ScriptableObject
2. Xxx.cs (AbstractAbility)    ← 运行时数据
3. XxxSpec.cs (AbilitySpec)    ← 运行实例，实际逻辑
```

### 5.2 完整示例：NormalFire 射击能力

**AANormalFire.cs** - 配置类:
```csharp
public class AANormalFire : AbilityAsset
{
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 3f;
    public GameplayEffectAsset damageEffect;

    public override Type AbilityType() => typeof(NormalFire);
}
```

**NormalFire.cs** - 数据类 + 实例类:
```csharp
public class NormalFire : AbstractAbility<AANormalFire>
{
    public GameObject BulletPrefab { get; }
    public float BulletSpeed { get; }
    public GameplayEffect DamageEffect { get; }

    public NormalFire(AANormalFire asset) : base(asset)
    {
        BulletPrefab = asset.bulletPrefab;
        BulletSpeed = asset.bulletSpeed;
        DamageEffect = asset.damageEffect != null 
            ? new GameplayEffect(asset.damageEffect) 
            : null;
    }

    public override AbilitySpec CreateSpec(AbilitySystemComponent owner)
        => new NormalFireSpec(this, owner);
}

public class NormalFireSpec : AbilitySpec<NormalFire>
{
    public NormalFireSpec(NormalFire ability, AbilitySystemComponent owner) 
        : base(ability, owner) { }

    public override void ActivateAbility()
    {
        DoCost();
        SpawnBullet();
        TryEndAbility();
    }

    private void SpawnBullet()
    {
        var bullet = Object.Instantiate(Data.BulletPrefab, ...);
        bullet.GetComponent<Bullet>().Init(Owner, direction, Data.BulletSpeed, ...);
    }

    public override void CancelAbility() { }
    public override void EndAbility() { }
}
```

### 5.3 开发检查清单

- [ ] 创建 `AA*.cs` (AbilityAsset) 并重写 `AbilityType()`
- [ ] 创建 `*.cs` (AbstractAbility) 并实现 `CreateSpec()`
- [ ] 创建 `*Spec.cs` (AbilitySpec) 并实现 `ActivateAbility()`, `CancelAbility()`, `EndAbility()`
- [ ] 在 Unity 中创建 `.asset` 配置文件
- [ ] 设置 `UniqueName`
- [ ] 点击 "Generate Ability Collection" 重新生成 GAbilityLib
- [ ] 将能力添加到 ASC Preset 或代码中授予

---

## 附录

### A. 常用 API

```csharp
// ASC 操作
asc.TryActivateAbility("AbilityName");      // 激活能力
asc.TryEndAbility("AbilityName");           // 结束能力
asc.TryCancelAbility("AbilityName");        // 取消能力
asc.ApplyGameplayEffectTo(ge, target);      // 应用效果到目标
asc.ApplyGameplayEffectToSelf(ge);          // 应用效果到自己
asc.HasTag(GTagLib.State_Buff);             // 检查标签
asc.AttrSet<AS_Fight>();                    // 获取属性集

// AbilitySpec 内部
DoCost();                                    // 执行消耗和冷却
TryEndAbility();                            // 结束能力
Owner;                                       // 获取 ASC
Data;                                        // 获取 Ability 数据
```

### B. 相关文件路径

```
GAS 框架:
  Assets/GAS/Runtime/Ability/
  Assets/GAS/Runtime/Effects/
  Assets/GAS/Editor/Ability/

Demo 代码:
  Assets/Demo/Script/GAS/Ability/
  Assets/Demo/Script/Element/

配置文件:
  Assets/Demo/Resources/GAS_Setting/Config/

生成代码:
  Assets/Demo/Script/Gen/
```

---

*文档创建时间: 2026-02-07*
