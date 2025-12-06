using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Effect")]
public class PowerUpEffect : ScriptableObject
{
    public string powerUpName;
    public string description;
    public float moveSpeed, baseMeleeDamage, damageMeleeModifier, baseRangeDamage, baseRangeModifier, meleeCooldown, hitboxRange, health, maxHealth;
    public Sprite itemSprite;
    public bool isMeleeWeapon, isRangeWeapon;
    
    public virtual void Apply(Player player)
    {
        if (isMeleeWeapon) {
            player.baseMeleeDamage = baseMeleeDamage;
        } else if (isRangeWeapon) {
            player.baseRangeDamage = baseRangeDamage;
        }
        player.damageMeleeModifier += damageMeleeModifier;
        player.baseRangeModifier += baseRangeModifier;
        player.maxHealth += (int)maxHealth;
        if (player.maxHealth >= player.maxMaxHealth) {
            player.maxHealth = player.maxMaxHealth;
        }
        if (player.health != player.maxHealth && maxHealth != 0f); {
            player.health = player.maxHealth;
        }
        player.health += (int)health;
        if (player.health <= 0) {
            player.health = 1;
        } else if (player.health > player.maxHealth) {
            player.health = player.maxHealth;
        }
        Health.Instance.UpdateHealthSprites();
        player.moveSpeed += moveSpeed;
        player.meleeCooldown += meleeCooldown;
        player.hitboxRange += hitboxRange;
    }
}
