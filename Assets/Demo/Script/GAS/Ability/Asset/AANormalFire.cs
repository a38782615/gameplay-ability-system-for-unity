using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS.Runtime
{
    public class AANormalFire : AbilityAsset
    {
        [BoxGroup("子弹配置")]
        [LabelText("子弹预制体")]
        [Required("请设置子弹预制体")]
        public GameObject bulletPrefab;

        [BoxGroup("子弹配置")]
        [LabelText("子弹速度")]
        [Range(1f, 100f)]
        public float bulletSpeed = 20f;

        [BoxGroup("子弹配置")]
        [LabelText("子弹存活时间(秒)")]
        [Range(0.1f, 10f)]
        public float bulletLifetime = 3f;

        [BoxGroup("子弹配置")]
        [LabelText("发射点偏移")]
        [Tooltip("相对于玩家中心的发射点偏移")]
        public Vector2 firePointOffset = new Vector2(0.5f, 0f);

        [BoxGroup("伤害配置")]
        [LabelText("伤害效果")]
        [Tooltip("子弹命中时应用的GameplayEffect")]
        public GameplayEffectAsset damageEffect;

        [BoxGroup("伤害配置")]
        [LabelText("基础伤害")]
        [Tooltip("如果不使用MMC，则使用此固定伤害值")]
        [Range(1f, 100f)]
        public float baseDamage = 10f;

        public override Type AbilityType()
        {
            return typeof(NormalFire);
        }
    }
}