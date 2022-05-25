using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        Bullets,
        Shells,
        Rockets,
        Health,
        Armor,
        Weapon,
        Keycard,
        Backpack,
        Invisibility,
        TacticalMap,
        NightVision,
        RadiationSuit
    }

    public ItemType itemType;

    public int amount;
    public bool bonus;
    public int givesWeapon = -1;
    public int givesKeycard = -1;

    public string PickupSound;

    void OnTriggerEnter(Collider other)
    {
        PlayerThing player = other.GetComponent<PlayerThing>();

        if (player == null)
            return;

        bool destroy = false;

        switch (itemType)
        {
            default:
                break;

            case ItemType.Bullets:
                if (PlayerInfo.Instance.Ammo[0] == PlayerInfo.Instance.MaxAmmo[0])
                    break;
                PlayerInfo.Instance.Ammo[0] += amount;
                if (PlayerInfo.Instance.Ammo[0] > PlayerInfo.Instance.MaxAmmo[0])
                    PlayerInfo.Instance.Ammo[0] = PlayerInfo.Instance.MaxAmmo[0];
                destroy = true;
                break;

            case ItemType.Shells:
                if (PlayerInfo.Instance.Ammo[1] == PlayerInfo.Instance.MaxAmmo[1])
                    break;
                PlayerInfo.Instance.Ammo[1] += amount;
                if (PlayerInfo.Instance.Ammo[1] > PlayerInfo.Instance.MaxAmmo[1])
                    PlayerInfo.Instance.Ammo[1] = PlayerInfo.Instance.MaxAmmo[1];
                destroy = true;
                break;

            case ItemType.Rockets:
                if (PlayerInfo.Instance.Ammo[2] == PlayerInfo.Instance.MaxAmmo[2])
                    break;
                PlayerInfo.Instance.Ammo[2] += amount;
                if (PlayerInfo.Instance.Ammo[2] > PlayerInfo.Instance.MaxAmmo[2])
                    PlayerInfo.Instance.Ammo[2] = PlayerInfo.Instance.MaxAmmo[2];
                destroy = true;
                break;

            case ItemType.Health:
                if (bonus)
                {
                    if (player.hitpoints == PlayerInfo.Instance.MaxBonusHealth)
                        break;

                    player.hitpoints += amount;
                    if (player.hitpoints > PlayerInfo.Instance.MaxBonusHealth)
                        player.hitpoints = PlayerInfo.Instance.MaxBonusHealth;
                }
                else
                {
                    if (player.hitpoints >= PlayerInfo.Instance.MaxHealth)
                        break;

                    player.hitpoints += amount;
                    if (player.hitpoints > PlayerInfo.Instance.MaxHealth)
                        player.hitpoints = PlayerInfo.Instance.MaxHealth;
                }
                destroy = true;
                break;

            case ItemType.Armor:
                if (bonus)
                {
                    if (player.armor == PlayerInfo.Instance.MaxBonusArmor)
                        break;

                    player.armor += amount;
                    if (player.armor > PlayerInfo.Instance.MaxBonusArmor)
                        player.armor = PlayerInfo.Instance.MaxBonusArmor;
                }
                else
                {
                    if (player.armor >= PlayerInfo.Instance.MaxArmor)
                        break;

                    player.armor += amount;
                    if (player.armor > PlayerInfo.Instance.MaxArmor)
                        player.armor = PlayerInfo.Instance.MaxArmor;
                }

                destroy = true;
                break;

            case ItemType.Keycard:
                if (PlayerInfo.Instance.Keycards[givesKeycard])
                    break;

                PlayerInfo.Instance.Keycards[givesKeycard] = true;
                
                //to allow making of skullkeys eventually
                if (givesKeycard == 0)
                    PlayerInfo.Instance.bluekeyTexture = TextureLoader.Instance.GetSpriteTexture("BKEYA0");
                if (givesKeycard == 1)
                    PlayerInfo.Instance.yellowkeyTexture = TextureLoader.Instance.GetSpriteTexture("YKEYA0");
                if (givesKeycard == 2)
                    PlayerInfo.Instance.redkeyTexture = TextureLoader.Instance.GetSpriteTexture("RKEYA0");

                destroy = true;
                break;

            case ItemType.Backpack:
                PlayerInfo.Instance.MaxAmmo[0] = 400;
                PlayerInfo.Instance.MaxAmmo[1] = 100;
                PlayerInfo.Instance.MaxAmmo[2] = 100;

                PlayerInfo.Instance.Ammo[0] += 10;
                if (PlayerInfo.Instance.Ammo[0] > PlayerInfo.Instance.MaxAmmo[0])
                    PlayerInfo.Instance.Ammo[0] = PlayerInfo.Instance.MaxAmmo[0];
                PlayerInfo.Instance.Ammo[1] += 4;
                if (PlayerInfo.Instance.Ammo[1] > PlayerInfo.Instance.MaxAmmo[1])
                    PlayerInfo.Instance.Ammo[1] = PlayerInfo.Instance.MaxAmmo[1];
                PlayerInfo.Instance.Ammo[2] += 1;
                if (PlayerInfo.Instance.Ammo[2] > PlayerInfo.Instance.MaxAmmo[2])
                    PlayerInfo.Instance.Ammo[2] = PlayerInfo.Instance.MaxAmmo[2];

                destroy = true;
                break;
        }

        if (givesWeapon != -1)
        {
            if (givesWeapon >= 0 && givesWeapon < PlayerInfo.Instance.Weapon.Length)
            {
                if (!PlayerInfo.Instance.Weapon[givesWeapon])
                {
                    PlayerInfo.Instance.Weapon[givesWeapon] = true;
                    PlayerControls.Instance.TrySwapWeapon(givesWeapon);
                }
            }

            destroy = true;
        }

        if (destroy)
        {
            PlayerInfo.Instance.pickupFlashTime = 1f;

            if (!string.IsNullOrEmpty(PickupSound))
                GameManager.Create3DSound(transform.position, PickupSound, 1f);

            Destroy(gameObject);
        }
    }
}
