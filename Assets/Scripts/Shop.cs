using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class Shop : MonoBehaviour {

    public GameObject powerUpPrefab;
    private Vector2 itemSpot1;
    private Vector2 itemSpot2;
    private Vector2 itemSpot3;

    public void SpawnInShopItems() {

        Player.Instance.health = Player.Instance.maxHealth;
        Health.Instance.UpdateHealthSprites();

        itemSpot1 = new Vector2(-18.191f,14.192f);
        itemSpot2 = new Vector2(-17.397f,14.192f);
        itemSpot3 = new Vector2(-16.594f,14.192f);

        List<PowerUpEffect> weaponsObtained = Player.Instance.weaponsObtained;

        PowerUpEffect[] rangeWeapons = Resources.LoadAll<PowerUpEffect>("PowerUps/Weapons/Ranged");
        PowerUpEffect[] meleeWeapons = Resources.LoadAll<PowerUpEffect>("PowerUps/Weapons/Melee");
        PowerUpEffect[] bossUpgrades = Resources.LoadAll<PowerUpEffect>("PowerUps/BossUpgrades");

        int randomRangeIndex;
        int randomMeleeIndex;
        int randomBossIndex;

        PowerUpEffect rangeShopWeapon = null;
        PowerUpEffect meleeShopWeapon = null;
        PowerUpEffect bossItem = null;

        while (weaponsObtained.Contains(rangeShopWeapon) || !rangeShopWeapon) {
            randomRangeIndex = Random.Range(0, rangeWeapons.Length);
            // int randomBossItemIndex = Random.Range(0, bossUpgrades.Length);
            rangeShopWeapon = rangeWeapons[randomRangeIndex];
            // PowerUpEffect bossShopItem = meleeWeapons[randomBossItemIndex];
        }

        while (weaponsObtained.Contains(bossItem) || !bossItem) {
            randomBossIndex = Random.Range(0, bossUpgrades.Length);
            bossItem = bossUpgrades[randomBossIndex];
        }

        while (weaponsObtained.Contains(meleeShopWeapon) || !meleeShopWeapon) {
            randomMeleeIndex = Random.Range(0, meleeWeapons.Length);
            meleeShopWeapon = meleeWeapons[randomMeleeIndex];
        }

        GameObject itemToSpawn1 = Instantiate(powerUpPrefab, itemSpot1, Quaternion.identity);
        GameObject itemToSpawn2 = Instantiate(powerUpPrefab, itemSpot2, Quaternion.identity);
        GameObject itemToSpawn3 = Instantiate(powerUpPrefab, itemSpot3, Quaternion.identity);

        itemToSpawn1.name = "RangeShopWeapon";
        itemToSpawn2.name = "MeleeShopWeapon";
        itemToSpawn3.name = "BossItem";

        SpriteRenderer sr1 = itemToSpawn1.GetComponent<SpriteRenderer>();
        SpriteRenderer sr2 = itemToSpawn2.GetComponent<SpriteRenderer>();
        SpriteRenderer sr3 = itemToSpawn2.GetComponent<SpriteRenderer>();

        itemToSpawn1.transform.localScale = new Vector2(1.0f,1.0f);
        itemToSpawn3.transform.localScale = new Vector2(1.0f,1.0f);

        sr1.sortingOrder = 20;
        sr2.sortingOrder = 20;
        sr3.sortingOrder = 20;
        
        BoxCollider2D col1 = itemToSpawn1.GetComponent<BoxCollider2D>();
        // BoxCollider2D col2 = itemToSpawn2.GetComponent<BoxCollider2D>();
        BoxCollider2D col3 = itemToSpawn3.GetComponent<BoxCollider2D>();

        col1.size = new Vector2(0.5f, 0.5f);
        // col2.size = new Vector2(2.0f, 2.0f);
        col3.size = new Vector2(0.5f,0.5f);

        PowerUp p1 = itemToSpawn1.GetComponent<PowerUp>();
        PowerUp p2 = itemToSpawn2.GetComponent<PowerUp>();
        PowerUp p3 = itemToSpawn3.GetComponent<PowerUp>();

        p1.effect = rangeShopWeapon;
        p2.effect = meleeShopWeapon;
        p3.effect = bossItem;
    }

    public void DestroyNotPickedItems() {

        GameObject range = GameObject.Find("RangeShopWeapon");
        GameObject melee = GameObject.Find("MeleeShopWeapon");
        GameObject boss = GameObject.Find("BossItem");

        if (range) Destroy(range);
        if (melee) Destroy(melee);
        if (boss) Destroy(boss);
        
    }
}