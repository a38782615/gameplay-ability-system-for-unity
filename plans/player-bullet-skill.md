# 玩家子弹技能实现计划

> **状态**: 待开始  
> **优先级**: 高  
> **预估工时**: 4-6 小时  
> **创建日期**: 2026-02-07

---

## 目录

- [1. 背景与动机](#1-背景与动机)
- [2. 需求分析](#2-需求分析)
- [3. 技术设计](#3-技术设计)
- [4. 实现阶段](#4-实现阶段)
- [5. 验收标准](#5-验收标准)
- [6. 替代方案](#6-替代方案)
- [7. 风险评估](#7-风险评估)
- [8. 依赖关系](#8-依赖关系)
- [9. 任务清单](#9-任务清单)

---

## 1. 背景与动机

### 1.1 当前状态

项目已有部分子弹技能的基础代码：
- `AANormalFire.cs` - AbilityAsset 配置类（已存在，包含 bulletPrefab 字段）
- `NormalFire.cs` - Ability 和 AbilitySpec 类（已存在，但逻辑未完成）
- `NormalFire.asset` - 能力配置文件（已存在）
- `AS_Bullet` - 子弹属性集（已存在，包含 ATK、SPEED）
- `GTagLib.Ability_NormalFire` - 能力标签（已存在）
- `GTagLib.CD_FireBullet` - 冷却标签（已存在）

### 1.2 目标

实现一个完整的玩家子弹射击技能，包括：
- 按键触发射击
- 生成子弹并向前飞行
- 子弹碰撞敌人造成伤害
- 支持冷却和消耗（可选）
- 视觉/音效反馈（可选）

### 1.3 参考

- 现有 `Attack` 能力（TimelineAbility 模式）
- `BossAttack00` 能力（自定义 Ability + AbilitySpec 模式）
- ProCamera2D 示例中的 `Bullet.cs`

---

## 2. 需求分析

### 2.1 功能需求

| ID | 需求 | 优先级 | 说明 |
|----|------|--------|------|
| F1 | 按键触发射击 | P0 | 玩家按下射击键（如鼠标左键或 F 键）触发 |
| F2 | 生成子弹 | P0 | 在玩家位置生成子弹 Prefab |
| F3 | 子弹飞行 | P0 | 子弹向玩家朝向方向飞行 |
| F4 | 碰撞检测 | P0 | 子弹碰撞敌人触发伤害 |
| F5 | 伤害计算 | P0 | 基于玩家 ATK 属性计算伤害 |
| F6 | 子弹销毁 | P0 | 碰撞后或超时后销毁子弹 |
| F7 | 冷却机制 | P1 | 射击有冷却时间 |
| F8 | 消耗机制 | P2 | 射击消耗 MP 或 STAMINA |
| F9 | 射击特效 | P2 | 枪口火焰、子弹拖尾等 |
| F10 | 射击音效 | P2 | 射击和命中音效 |

### 2.2 非功能需求

| ID | 需求 | 说明 |
|----|------|------|
| NF1 | 性能 | 子弹使用对象池，避免频繁 Instantiate/Destroy |
| NF2 | 可配置 | 子弹速度、伤害、存活时间等可在 Inspector 配置 |
| NF3 | 可扩展 | 支持未来添加不同类型子弹（散弹、追踪弹等） |

---

## 3. 技术设计

### 3.1 架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                        射击流程                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [Input]        [Ability]           [Bullet]        [Effect]   │
│     │               │                   │               │       │
│  按下射击键 ──▶ NormalFire ──▶ 生成 Bullet ──▶ 碰撞敌人       │
│                 Ability          Prefab              │         │
│                     │               │                │         │
│                 检查冷却/消耗    飞行+检测      ApplyGE伤害    │
│                     │               │                │         │
│                 DoCost()        OnTrigger      GE_BulletDamage │
│                                     │                │         │
│                                  销毁子弹       扣除敌人HP     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 类设计

#### 3.2.1 Ability 层

```
AANormalFire (AbilityAsset)          ← 配置数据
├── bulletPrefab: GameObject         ← 子弹预制体
├── bulletSpeed: float               ← 子弹速度
├── bulletLifetime: float            ← 子弹存活时间
├── firePoint: Vector2               ← 发射点偏移
└── damageEffect: GameplayEffectAsset ← 伤害效果

NormalFire (AbstractAbility)         ← 运行时数据
└── 从 AANormalFire 读取配置

NormalFireSpec (AbilitySpec)         ← 运行实例
├── ActivateAbility()                ← 执行射击
│   ├── DoCost()                     ← 消耗冷却
│   └── SpawnBullet()                ← 生成子弹
├── CancelAbility()                  ← 取消（空实现）
└── EndAbility()                     ← 结束（立即结束）
```

#### 3.2.2 Bullet 层

```
Bullet (MonoBehaviour)
├── 配置
│   ├── speed: float
│   ├── lifetime: float
│   └── damageEffect: GameplayEffect
├── 运行时
│   ├── owner: AbilitySystemComponent  ← 发射者
│   ├── direction: Vector2             ← 飞行方向
│   └── timer: float                   ← 存活计时
├── 方法
│   ├── Init(owner, direction, speed, lifetime, damageEffect)
│   ├── Update()                       ← 移动 + 计时
│   └── OnTriggerEnter2D()             ← 碰撞检测
└── 伤害逻辑
    └── owner.ApplyGameplayEffectTo(damageEffect, target)
```

#### 3.2.3 GameplayEffect 层

```
GE_BulletDamage (GameplayEffectAsset)
├── DurationPolicy: Instant
├── Modifiers:
│   └── [0] AS_Fight.HP, Minus, MMC_BulletAttr_Attack
└── Tags:
    └── AssetTags: [Damage.Bullet]
```

### 3.3 数据流

```
1. 玩家按下射击键
   └── Player.OnFire() → ASC.TryActivateAbility("NormalFire")

2. NormalFireSpec.ActivateAbility()
   ├── CanActivate() 检查
   │   ├── 冷却检查 (CD_FireBullet)
   │   └── 消耗检查 (MP/STAMINA)
   ├── DoCost() 扣除消耗 + 启动冷却
   └── SpawnBullet()
       ├── Instantiate(bulletPrefab)
       ├── bullet.Init(owner, direction, ...)
       └── TryEndAbility() 立即结束

3. Bullet.Update()
   ├── transform.Translate(direction * speed * deltaTime)
   └── timer += deltaTime → 超时销毁

4. Bullet.OnTriggerEnter2D(other)
   ├── 获取 target ASC
   ├── 检查阵营 (Faction_Enemy)
   ├── owner.ApplyGameplayEffectTo(damageEffect, target)
   └── Destroy(gameObject)
```

---

## 4. 实现阶段

### 阶段 1：基础射击功能 (P0)

**目标**：实现最基本的射击和伤害

#### 1.1 完善 AANormalFire.cs
- [ ] 添加 bulletSpeed 字段
- [ ] 添加 bulletLifetime 字段
- [ ] 添加 firePointOffset 字段
- [ ] 添加 damageEffect 字段

#### 1.2 完善 NormalFire.cs
- [ ] 完善 NormalFire 类，读取配置
- [ ] 完善 NormalFireSpec.ActivateAbility()
  - [ ] 获取玩家位置和朝向
  - [ ] 实例化子弹
  - [ ] 初始化子弹参数
  - [ ] 调用 TryEndAbility()

#### 1.3 创建 Bullet.cs
- [ ] 创建 `Assets/Demo/Script/Element/Bullet.cs`
- [ ] 实现 Init() 方法
- [ ] 实现 Update() 移动逻辑
- [ ] 实现 OnTriggerEnter2D() 碰撞检测
- [ ] 实现伤害应用逻辑

#### 1.4 创建子弹 Prefab
- [ ] 创建 `Assets/Demo/Resources/Prefabs/Bullet.prefab`
- [ ] 添加 SpriteRenderer（子弹外观）
- [ ] 添加 Rigidbody2D（Kinematic）
- [ ] 添加 Collider2D（Trigger）
- [ ] 添加 Bullet 脚本

#### 1.5 创建伤害 GameplayEffect
- [ ] 创建 `GE_BulletDamage.asset`
- [ ] 配置为 Instant 类型
- [ ] 添加 Modifier：AS_Fight.HP, Minus
- [ ] 创建 MMC：基于发射者 ATK 计算伤害

#### 1.6 添加输入绑定
- [ ] 在 DemoController 中添加 Fire 输入
- [ ] 在 Player.cs 中添加 OnFire 处理

### 阶段 2：冷却与消耗 (P1)

**目标**：添加冷却和消耗机制

#### 2.1 创建冷却 GameplayEffect
- [ ] 创建 `GE_CD_NormalFire.asset`
- [ ] 配置为 Duration 类型
- [ ] 设置 GrantedTags: CD_FireBullet

#### 2.2 创建消耗 GameplayEffect（可选）
- [ ] 创建 `GE_Cost_NormalFire.asset`
- [ ] 配置为 Instant 类型
- [ ] 添加 Modifier：AS_Fight.MP 或 STAMINA, Minus

#### 2.3 配置 NormalFire.asset
- [ ] 设置 Cooldown 为 GE_CD_NormalFire
- [ ] 设置 CooldownTime
- [ ] 设置 Cost 为 GE_Cost_NormalFire（可选）

### 阶段 3：视觉与音效 (P2)

**目标**：添加游戏反馈

#### 3.1 创建射击 Cue
- [ ] 创建枪口火焰特效
- [ ] 创建 GameplayCueInstant 播放特效

#### 3.2 创建命中 Cue
- [ ] 创建命中特效
- [ ] 创建命中音效

#### 3.3 子弹拖尾
- [ ] 添加 TrailRenderer 到子弹 Prefab

### 阶段 4：优化与扩展 (P2)

**目标**：性能优化和可扩展性

#### 4.1 对象池
- [ ] 实现子弹对象池
- [ ] 修改 Bullet 支持复用

#### 4.2 可扩展设计
- [ ] 抽象 BulletBase 基类
- [ ] 支持不同子弹类型

---

## 5. 验收标准

### 5.1 功能验收

| ID | 验收项 | 验收标准 |
|----|--------|----------|
| A1 | 射击触发 | 按下射击键，玩家发射子弹 |
| A2 | 子弹飞行 | 子弹向玩家朝向方向匀速飞行 |
| A3 | 碰撞伤害 | 子弹碰撞敌人，敌人 HP 减少 |
| A4 | 子弹销毁 | 碰撞后或超时后子弹消失 |
| A5 | 冷却生效 | 冷却期间无法再次射击 |
| A6 | 阵营判断 | 子弹不伤害玩家自己 |

### 5.2 技术验收

| ID | 验收项 | 验收标准 |
|----|--------|----------|
| T1 | 无报错 | 射击过程无 Console 错误 |
| T2 | 性能 | 连续射击不造成明显卡顿 |
| T3 | GAS 集成 | 伤害通过 GameplayEffect 应用 |

### 5.3 测试用例

```
TC1: 基础射击
  前置: 玩家面向右侧，前方有敌人
  操作: 按下射击键
  预期: 子弹向右飞行，命中敌人，敌人 HP 减少

TC2: 冷却测试
  前置: 刚射击完毕
  操作: 立即再次按下射击键
  预期: 无法射击（冷却中）

TC3: 子弹超时
  前置: 玩家面向空旷区域
  操作: 按下射击键
  预期: 子弹飞行一段距离后自动消失

TC4: 阵营测试
  前置: 子弹飞行路径上有玩家自己
  操作: 射击
  预期: 子弹穿过玩家，不造成伤害
```

---

## 6. 替代方案

### 6.1 Ability 实现方式

| 方案 | 优点 | 缺点 | 推荐度 |
|------|------|------|--------|
| **A: 自定义 AbilitySpec** | 灵活，代码清晰 | 需要手写逻辑 | ⭐⭐⭐⭐⭐ |
| B: TimelineAbility | 可视化编辑 | 射击逻辑简单，不需要时间轴 | ⭐⭐ |
| C: 纯脚本（不用 GAS） | 简单直接 | 不符合项目架构 | ⭐ |

**选择方案 A**：自定义 AbilitySpec，因为射击是瞬时动作，不需要时间轴。

### 6.2 子弹实现方式

| 方案 | 优点 | 缺点 | 推荐度 |
|------|------|------|--------|
| **A: MonoBehaviour + Trigger** | 简单，易于理解 | 需要 Rigidbody | ⭐⭐⭐⭐⭐ |
| B: Raycast 射线检测 | 性能好，无延迟 | 无法看到子弹飞行 | ⭐⭐⭐ |
| C: DOTween 动画 | 平滑 | 依赖第三方库 | ⭐⭐ |

**选择方案 A**：MonoBehaviour + Trigger，直观且符合 2D 游戏惯例。

### 6.3 伤害计算方式

| 方案 | 优点 | 缺点 | 推荐度 |
|------|------|------|--------|
| **A: MMC 基于发射者 ATK** | 符合 GAS 设计 | 需要创建 MMC | ⭐⭐⭐⭐⭐ |
| B: 固定伤害值 | 简单 | 不灵活 | ⭐⭐ |
| C: 子弹自带 ASC | 完整 GAS 流程 | 过于复杂 | ⭐⭐ |

**选择方案 A**：使用 MMC 基于发射者 ATK 计算，已有 `AS_Bullet` 和 `MMC_BulletAttr_Attack`。

---

## 7. 风险评估

### 7.1 技术风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 子弹碰撞检测不准确 | 中 | 中 | 使用 Continuous 碰撞检测，调整 Collider 大小 |
| 子弹穿透多个敌人 | 低 | 低 | 碰撞后立即销毁或禁用 Collider |
| 性能问题（大量子弹） | 低 | 中 | 实现对象池，限制同屏子弹数量 |
| GE 应用失败 | 低 | 高 | 添加空值检查和日志 |

### 7.2 依赖风险

| 依赖 | 风险 | 缓解措施 |
|------|------|----------|
| 敌人必须有 ASC | 低 | 碰撞时检查 ASC 是否存在 |
| 敌人必须有 AS_Fight | 低 | 使用 GetAttributeCurrentValue 安全获取 |
| 输入系统配置 | 低 | 参考现有 Attack 输入配置 |

---

## 8. 依赖关系

### 8.1 前置依赖

- [x] GAS 框架已集成
- [x] Player 类已实现
- [x] 敌人有 ASC 和 AS_Fight
- [x] 输入系统已配置
- [x] AS_Bullet 属性集已存在
- [x] GTagLib.Ability_NormalFire 已存在
- [x] GTagLib.CD_FireBullet 已存在

### 8.2 文件依赖

```
需要修改的文件:
├── Assets/Demo/Script/GAS/Ability/Asset/AANormalFire.cs  ← 添加配置字段
├── Assets/Demo/Script/GAS/Ability/NormalFire.cs          ← 完善逻辑
├── Assets/Demo/Script/Element/Player.cs                  ← 添加射击输入
└── Assets/Demo/Resources/GAS_Setting/Config/GameplayAbilityLib/Player/NormalFire.asset ← 配置

需要创建的文件:
├── Assets/Demo/Script/Element/Bullet.cs                  ← 子弹脚本
├── Assets/Demo/Resources/Prefabs/Bullet.prefab           ← 子弹预制体
├── Assets/Demo/Resources/GAS_Setting/Config/GameplayEffectLib/GE_BulletDamage.asset ← 伤害GE
└── Assets/Demo/Resources/GAS_Setting/Config/GameplayEffectLib/Cooldown/GE_CD_NormalFire.asset ← 冷却GE
```

---

## 9. 任务清单

### 阶段 1：基础射击功能

- [x] **1.1** 完善 `AANormalFire.cs` - 添加配置字段
- [x] **1.2** 完善 `NormalFire.cs` - 实现射击逻辑
- [x] **1.3** 创建 `Bullet.cs` - 子弹脚本
- [ ] **1.4** 创建子弹 Prefab （需在Unity编辑器中完成）
- [ ] **1.5** 创建 `GE_BulletDamage.asset` - 伤害效果 （需在Unity编辑器中完成）
- [ ] **1.6** 创建/配置 MMC - 伤害计算 （需在Unity编辑器中完成）
- [x] **1.7** 修改 `Player.cs` - 添加射击输入
- [x] **1.8** 添加 Fire 输入动作到 `DemoController.inputactions`
- [ ] **1.9** 配置 `NormalFire.asset` - 关联 Prefab 和 GE （需在Unity编辑器中完成）
- [ ] **1.10** 重新生成 `DemoController.cs` （需在Unity编辑器中完成）
- [ ] **1.11** 测试基础射击功能

### 阶段 2：冷却与消耗

- [ ] **2.1** 创建 `GE_CD_NormalFire.asset` - 冷却效果
- [ ] **2.2** 配置 NormalFire.asset 的 Cooldown
- [ ] **2.3** 测试冷却功能
- [ ] **2.4** (可选) 创建消耗效果

### 阶段 3：视觉与音效

- [ ] **3.1** 创建射击特效
- [ ] **3.2** 创建命中特效
- [ ] **3.3** 添加子弹拖尾
- [ ] **3.4** 添加音效

### 阶段 4：优化

- [ ] **4.1** 实现子弹对象池
- [ ] **4.2** 性能测试

---

## 附录

### A. 相关文件路径

```
GAS 框架:
  Assets/GAS/Runtime/Ability/
  Assets/GAS/Runtime/Effects/

Demo 代码:
  Assets/Demo/Script/GAS/Ability/
  Assets/Demo/Script/Element/

配置文件:
  Assets/Demo/Resources/GAS_Setting/Config/

生成代码:
  Assets/Demo/Script/Gen/
```

### B. 参考代码

**现有 NormalFire.cs（待完善）**:
```csharp
public class NormalFireSpec : AbilitySpec
{
    public override void ActivateAbility()
    {
        // TODO: 实现射击逻辑
        var ab = Ability as NormalFire;
    }
}
```

**参考 ProCamera2D Bullet.cs**:
```csharp
void Update()
{
    _transform.Translate(Vector3.right * BulletSpeed * Time.deltaTime);
    if (Time.time - _startTime > BulletDuration)
        Disable();
}
```

---

*计划创建时间: 2026-02-07*  
*最后更新: 2026-02-07*
