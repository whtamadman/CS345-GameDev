using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Effect")]
public class PowerUpEffect : ScriptableObject
{
    public string powerUpName;
    public string description;
    public float moveSpeed, baseMeleeDamage, damageMeleeModifier, baseRangeDamage, baseRangeModifier, meleeCooldown, hitboxFrames, health, maxHealth, reloadTime, invinceTimer, hitboxRange, coinMultiplier;
    public int cost;
    public Sprite itemSprite;
    public Projectile projectile;
    public bool isMeleeWeapon, isRangeWeapon, isHealth, isBossRoomItem;
    
    public virtual void Apply(Player player)
    {
        if (isMeleeWeapon || isRangeWeapon || isBossRoomItem) {
            player.weaponsObtained.Add(this);
        }
        if (isMeleeWeapon) {
            player.baseMeleeDamage = baseMeleeDamage;
            player.meleeCooldown = meleeCooldown;
            player.hitboxFrames = hitboxFrames;
        } else if (isRangeWeapon) {
            player.baseRangeDamage = baseRangeDamage;
            player.reloadTime = reloadTime;
            player.projectile = projectile;
        }
        player.damageMeleeModifier += damageMeleeModifier;
        player.baseRangeModifier += baseRangeModifier;
        player.maxHealth += (int)maxHealth;
        if (player.maxHealth >= player.maxMaxHealth) {
            player.maxHealth = player.maxMaxHealth;
        }
        if (maxHealth != 0f) {
            player.health = player.maxHealth;
        }
        if (isHealth) {
            player.health += (int)health;
            if (player.health <= 0) {
                player.health = 1;
            } else if (player.health > player.maxHealth) {
                player.health = player.maxHealth;
            }
        }
        Health.Instance.UpdateHealthSprites();
        player.moveSpeed += moveSpeed;
        player.invinceTimer += invinceTimer;
        player.hitboxRange += hitboxRange;
        player.coinMultiplier += coinMultiplier;
    }
}
