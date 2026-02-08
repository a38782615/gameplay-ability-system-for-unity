using Demo.Script.Element;
using UnityEngine;

namespace GAS.Runtime
{
    public class NormalFire : AbstractAbility<AANormalFire>
    {
        public GameObject BulletPrefab { get; }
        public float BulletSpeed { get; }
        public float BulletLifetime { get; }
        public Vector2 FirePointOffset { get; }
        public GameplayEffect DamageEffect { get; }
        public float BaseDamage { get; }

        public NormalFire(AANormalFire abilityAsset) : base(abilityAsset)
        {
            BulletPrefab = abilityAsset.bulletPrefab;
            BulletSpeed = abilityAsset.bulletSpeed;
            BulletLifetime = abilityAsset.bulletLifetime;
            FirePointOffset = abilityAsset.firePointOffset;
            BaseDamage = abilityAsset.baseDamage;

            // 如果配置了伤害效果，创建 GameplayEffect 实例
            if (abilityAsset.damageEffect != null)
            {
                DamageEffect = new GameplayEffect(abilityAsset.damageEffect);
            }
        }

        public override AbilitySpec CreateSpec(AbilitySystemComponent owner)
        {
            return new NormalFireSpec(this, owner);
        }
    }

    public class NormalFireSpec : AbilitySpec<NormalFire>
    {
        private readonly FightUnit _unit;
        private readonly Transform _transform;

        public NormalFireSpec(NormalFire ability, AbilitySystemComponent owner) : base(ability, owner)
        {
            _unit = owner.GetComponent<FightUnit>();
            _transform = owner.transform;
        }

        public override void ActivateAbility()
        {
            // 执行消耗和冷却
            DoCost();

            // 生成子弹
            SpawnBullet();

            // 射击是瞬时动作，立即结束能力
            TryEndAbility();
        }

        private void SpawnBullet()
        {
            if (Data.BulletPrefab == null)
            {
                Debug.LogError("[NormalFire] 子弹预制体未设置！");
                return;
            }

            // 获取玩家朝向（基于 Renderer 的 localScale.x）
            float facingDirection = _unit != null ? Mathf.Sign(_unit.Renderer.localScale.x) : 1f;
            Vector2 direction = new Vector2(facingDirection, 0f);

            // 计算发射位置
            Vector2 firePointOffset = Data.FirePointOffset;
            firePointOffset.x *= facingDirection; // 根据朝向调整偏移
            Vector3 spawnPosition = _transform.position + (Vector3)firePointOffset;

            // 实例化子弹
            GameObject bulletObj = Object.Instantiate(Data.BulletPrefab, spawnPosition, Quaternion.identity);

            // 初始化子弹
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Init(
                    owner: Owner,
                    direction: direction,
                    speed: Data.BulletSpeed,
                    lifetime: Data.BulletLifetime,
                    damageEffect: Data.DamageEffect,
                    baseDamage: Data.BaseDamage
                );
            }
            else
            {
                Debug.LogError("[NormalFire] 子弹预制体缺少 Bullet 组件！");
                Object.Destroy(bulletObj);
            }
        }

        public override void CancelAbility()
        {
            // 射击是瞬时动作，无需取消逻辑
        }

        public override void EndAbility()
        {
            // 射击是瞬时动作，无需结束逻辑
        }
    }
}