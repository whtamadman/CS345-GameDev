using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Effect")]
public class PowerUpEffect : ScriptableObject
{
    public string powerUpName;
    public string description;
    public float moveSpeed, baseMeleeDamage, damageMeleeModifier, baseRangeDamage, baseRangeModifier, meleeCooldown, hitboxFrames, health, maxHealth, reloadTime;
    public int cost;
    public Sprite itemSprite;
    public bool isMeleeWeapon, isRangeWeapon, isHealth;
    
    public virtual void Apply(Player player)
    {
        if (isMeleeWeapon) {
            player.baseMeleeDamage = baseMeleeDamage;
            player.meleeCooldown = meleeCooldown;
            player.hitboxFrames = hitboxFrames;
            player.hitboxRange = 1f;
        } else if (isRangeWeapon) {
            player.baseRangeDamage = baseRangeDamage;
            player.reloadTime = reloadTime;
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
    }
}
