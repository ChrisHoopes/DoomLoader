using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    public PlayerThing playerThing;

    public Texture bluekeyTexture;
    public Texture yellowkeyTexture;
    public Texture redkeyTexture;

    GUIStyle centerMessageStyle;
    GUIStyle whiteGuiStyle;
    GUIStyle ammoGuiStyle;
    GUIStyle healthGuiStyle;

    void Awake()
    {
        Instance = this;
        playerThing = GetComponent<PlayerThing>();

        centerMessageStyle = new GUIStyle();
        centerMessageStyle.font = GuiFont;
        centerMessageStyle.normal.textColor = new Color(1, .75f, .5f, 1f);
        centerMessageStyle.fontSize = 20;
        centerMessageStyle.alignment = TextAnchor.MiddleCenter;

        whiteGuiStyle = new GUIStyle();
        whiteGuiStyle.font = GuiFont;
        whiteGuiStyle.normal.textColor = new Color(1, 1, 1, .5f);
        whiteGuiStyle.fontSize = 10;
        whiteGuiStyle.alignment = TextAnchor.MiddleLeft;

        ammoGuiStyle = new GUIStyle();
        ammoGuiStyle.font = GuiFont;
        ammoGuiStyle.normal.textColor = new Color(1, .5f, 0, .5f);
        ammoGuiStyle.fontSize = 10;
        ammoGuiStyle.alignment = TextAnchor.MiddleRight;

        healthGuiStyle = new GUIStyle();
        healthGuiStyle.font = GuiFont;
        healthGuiStyle.normal.textColor = new Color(1, 0, 0, .5f);
        healthGuiStyle.fontSize = 10;
        healthGuiStyle.alignment = TextAnchor.MiddleLeft;
    }

    public Font GuiFont;
    public Texture pickupTexture;
    public Texture painTexture;

    public int[] Ammo = new int[3] { 50, 0, 0 }; //bullets, shells, rockets
    public bool[] Weapon = new bool[6] { true, false, true, false, false, false }; //fist, chainsaw, pistol, shotgun, chaingun, rocket
    public int[] MaxAmmo = new int[3] { 200, 50, 50 };
    public bool[] Keycards = new bool[3];

    public int MaxHealth = 100;
    public int MaxBonusHealth = 200;
    public int MaxArmor = 100;
    public int MaxBonusArmor = 200;

    public PlayerWeapon[] WeaponPrefabs = new PlayerWeapon[5];

    public float pickupFlashTime;
    public float painFlashTime;

    public List<Sector> unfoundSecrets = new List<Sector>();
    public List<Sector> foundSecrets = new List<Sector>();

    public void Reset()
    {
        Ammo = new int[3] { 50, 0, 0 };
        Weapon = new bool[6] { true, false, true, false, false, false };
        MaxAmmo = new int[3] { 200, 50, 50 };
        Keycards = new bool[3];

        pickupFlashTime = 0f;
        painFlashTime = 0f;

        PlayerControls.Instance.CurrentWeapon = -1;
        PlayerControls.Instance.SwapWeapon = -1;
        PlayerControls.Instance.SwapToBestWeapon();
    }

    void Update()
    {
        if (painFlashTime > 0f) painFlashTime -= Time.deltaTime;
        if (painFlashTime < 0f) painFlashTime = 0f;

        if (pickupFlashTime > 0f) pickupFlashTime -= Time.deltaTime;
        if (pickupFlashTime < 0f) pickupFlashTime = 0f;
    }

    public static bool GameFinished = false;

    void OnGUI()
    {
        if (GameFinished)
        {
            GameManager.PauseForever();
            GUI.Label(new Rect(0,0, Screen.width, Screen.height), "Game Over?\n\nThis is the end of the shareware campaign!\n\nThank you for playing!", centerMessageStyle);
            return;
        }

        if (Keycards[0])
            GUI.DrawTexture(new Rect(20, Screen.height - 80, bluekeyTexture.width, bluekeyTexture.height), bluekeyTexture);

        if (Keycards[1])
            GUI.DrawTexture(new Rect(40, Screen.height - 80, yellowkeyTexture.width, yellowkeyTexture.height), yellowkeyTexture);

        if (Keycards[2])
            GUI.DrawTexture(new Rect(60, Screen.height - 80, redkeyTexture.width, redkeyTexture.height), redkeyTexture);

        GUI.Label(new Rect(Screen.width - 130, Screen.height - 55, 40, 20), "BULL", whiteGuiStyle);
        GUI.Label(new Rect(Screen.width - 130, Screen.height - 40, 40, 20), "SHEL", whiteGuiStyle);
        GUI.Label(new Rect(Screen.width - 130, Screen.height - 25, 40, 20), "RCKT", whiteGuiStyle);

        GUI.Label(new Rect(Screen.width - 80, Screen.height - 55, 60, 20), Ammo[0] + "/" + MaxAmmo[0], ammoGuiStyle);
        GUI.Label(new Rect(Screen.width - 80, Screen.height - 40, 60, 20), Ammo[1] + "/" + MaxAmmo[1], ammoGuiStyle);
        GUI.Label(new Rect(Screen.width - 80, Screen.height - 25, 60, 20), Ammo[2] + "/" + MaxAmmo[2], ammoGuiStyle);

        GUI.Label(new Rect(20, Screen.height - 55, 40, 20), "HEALTH", whiteGuiStyle);
        GUI.Label(new Rect(100, Screen.height - 55, 40, 20), playerThing.hitpoints.ToString(), healthGuiStyle);

        GUI.Label(new Rect(20, Screen.height - 35, 40, 20), "ARMOR", whiteGuiStyle);
        GUI.Label(new Rect(100, Screen.height - 35, 40, 20), playerThing.armor.ToString(), whiteGuiStyle);

        //pickup flash
        if (pickupFlashTime > 0f)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pickupTexture, ScaleMode.StretchToFill, true, 0, Color.white * Mathf.Min(pickupFlashTime, 1f) * .5f, 0, 0);

        //pain flash
        if (painFlashTime > 0f)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), painTexture, ScaleMode.StretchToFill, true, 0, Color.white * Mathf.Min(painFlashTime, 1f) * .5f, 0, 0);
    }
}
