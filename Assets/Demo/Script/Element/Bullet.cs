using UnityEngine;
using GAS.Runtime;

namespace Demo.Script.Element
{
    /// <summary>
    /// 子弹组件 - 处理子弹的飞行、碰撞和伤害
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        #region 配置参数

        [Header("调试信息（运行时只读）")]
        [SerializeField] private float _speed;
        [SerializeField] private float _lifetime;
        [SerializeField] private float _baseDamage;
        [SerializeField] private Vector2 _direction;

        #endregion

        #region 运行时状态

        private AbilitySystemComponent _owner;
        private GameplayEffect _damageEffect;
        private float _timer;
        private bool _isInitialized;
        private Rigidbody2D _rb;

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // 确保 Rigidbody2D 设置正确
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // 移动子弹
            transform.Translate(_direction * _speed * Time.deltaTime, Space.World);

            // 检查存活时间
            _timer += Time.deltaTime;
            if (_timer >= _lifetime)
            {
                DestroySelf();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized) return;

            // 尝试获取目标的 ASC
            var targetASC = other.GetComponent<AbilitySystemComponent>();
            if (targetASC == null)
            {
                // 尝试从父对象获取
                targetASC = other.GetComponentInParent<AbilitySystemComponent>();
            }

            if (targetASC == null)
            {
                // 碰到非 ASC 对象（如墙壁），销毁子弹
                if (!other.isTrigger)
                {
                    DestroySelf();
                }
                return;
            }

            // 不伤害自己
            if (targetASC == _owner)
            {
                return;
            }

            // 检查阵营 - 不伤害同阵营
            if (IsSameFaction(targetASC))
            {
                return;
            }

            // 应用伤害
            ApplyDamage(targetASC);

            // 销毁子弹
            DestroySelf();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化子弹
        /// </summary>
        /// <param name="owner">发射者的 ASC</param>
        /// <param name="direction">飞行方向（归一化）</param>
        /// <param name="speed">飞行速度</param>
        /// <param name="lifetime">存活时间</param>
        /// <param name="damageEffect">伤害效果（可选）</param>
        /// <param name="baseDamage">基础伤害（当没有 damageEffect 时使用）</param>
        public void Init(
            AbilitySystemComponent owner,
            Vector2 direction,
            float speed,
            float lifetime,
            GameplayEffect damageEffect = null,
            float baseDamage = 10f)
        {
            _owner = owner;
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _damageEffect = damageEffect;
            _baseDamage = baseDamage;
            _timer = 0f;
            _isInitialized = true;

            // 设置子弹朝向
            if (_direction.x < 0)
            {
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查是否同阵营
        /// </summary>
        private bool IsSameFaction(AbilitySystemComponent target)
        {
            // 检查发射者是否是玩家阵营
            bool ownerIsPlayer = _owner.HasTag(GTagLib.Faction_Player);
            bool targetIsPlayer = target.HasTag(GTagLib.Faction_Player);

            // 检查发射者是否是敌人阵营
            bool ownerIsEnemy = _owner.HasTag(GTagLib.Faction_Enemy);
            bool targetIsEnemy = target.HasTag(GTagLib.Faction_Enemy);

            // 同阵营不伤害
            if (ownerIsPlayer && targetIsPlayer) return true;
            if (ownerIsEnemy && targetIsEnemy) return true;

            return false;
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        private void ApplyDamage(AbilitySystemComponent target)
        {
            if (_damageEffect != null)
            {
                // 使用配置的 GameplayEffect
                _owner.ApplyGameplayEffectTo(_damageEffect, target);
                Debug.Log($"[Bullet] 子弹命中 {target.name}，应用伤害效果");
            }
            else
            {
                // 没有配置 GE，直接扣血（备用方案）
                var targetAttrSet = target.AttrSet<AS_Fight>();
                if (targetAttrSet != null)
                {
                    float currentHP = targetAttrSet.HP.CurrentValue;
                    float newHP = currentHP - _baseDamage;
                    targetAttrSet.SetBaseHP(newHP);
                    Debug.Log($"[Bullet] 子弹命中 {target.name}，造成 {_baseDamage} 点伤害，HP: {currentHP} -> {newHP}");
                }
            }
        }

        /// <summary>
        /// 销毁子弹
        /// </summary>
        private void DestroySelf()
        {
            _isInitialized = false;
            // TODO: 未来可以改为对象池回收
            Destroy(gameObject);
        }

        #endregion
    }
}
