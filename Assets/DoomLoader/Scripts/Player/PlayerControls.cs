using UnityEngine;

public class PlayerControls : MonoBehaviour 
{
    public AudioSource audioSource;
    public static PlayerControls Instance;
    PlayerThing playerThing;

    public float gravityOofThreshold = 12f;

    public float speed = 8.0f;
    public float rotateSpeed = 1.0f;

    public float centerHeight = .88f;

    public Vector2 viewDirection = new Vector2(0, 0);

    CharacterController controller;

    float pokeSoundTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        playerThing = GetComponent<PlayerThing>();

        Instance = this;
    }

    public float gravityAccumulator;
    float lastFloorHeight;
    public bool lastFrameStickToFloor;
    public float deathTime = 0;

    public int CurrentWeapon = -1;
    public int SwapWeapon = -1;

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (playerThing.Dead)
        {
            if (PlayerCamera.Instance != null)
                PlayerCamera.Instance.bopActive = false;

            if (PlayerWeapon.Instance != null)
                PlayerWeapon.Instance.bopActive = false;

            if (deathTime < 1f)
                deathTime += Time.deltaTime;
            else
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    deathTime = 0;
                    viewDirection = Vector2.zero;
                    playerThing.hitpoints = 100;
                    playerThing.armor = 0;

                    if (PlayerWeapon.Instance != null)
                    {
                        Destroy(PlayerWeapon.Instance.gameObject);
                        PlayerWeapon.Instance = null;
                    }

                    PlayerInfo.Instance.Reset();

                    GameManager.Instance.ChangeMap = MapLoader.CurrentMap;
                }
            }
            return;
        }

        if (pokeSoundTime > 0)
            pokeSoundTime -= Time.deltaTime;

        viewDirection.y += Input.GetAxis("Mouse X") * Options.MouseSensitivity.x;
        viewDirection.x -= Input.GetAxis("Mouse Y") * Options.MouseSensitivity.y;

        //so you don't fall when no-clipping
        bool outerSpace = false;

        //smoother elevator movement
        bool sticktofloor = false;
        {
            Triangle t = TheGrid.GetExactTriangle(transform.position);
            if (t != null)
            {
                if (playerThing.currentSector != t.sector)
                {
                    if (playerThing.currentSector != null)
                        playerThing.currentSector.floorObject.DynamicThings.Remove(playerThing);

                        t.sector.floorObject.DynamicThings.AddLast(playerThing);
                }

                playerThing.LastSector = playerThing.currentSector;
                playerThing.currentSector = t.sector;

                if (playerThing.LastSector != null && playerThing.currentSector != null)
                    if (playerThing.LastSector == playerThing.currentSector)
                    {
                        if (lastFloorHeight != playerThing.currentSector.floorHeight)
                        {
                            lastFloorHeight = playerThing.currentSector.floorHeight;
                            if (controller.isGrounded)
                            {
                                float diff = Mathf.Abs((transform.position.y - centerHeight) - playerThing.currentSector.floorHeight);
                                if (diff <= MapLoader._4units)
                                {
                                    transform.position = new Vector3(transform.position.x, playerThing.currentSector.floorHeight + centerHeight, transform.position.z);
                                    sticktofloor = true;
                                }
                            }
                        }
                    }
                    else
                        lastFloorHeight = playerThing.currentSector.floorHeight;

                //so we can no-clip from lower platform to higher
                if (transform.position.y < t.sector.floorHeight)
                    transform.position = new Vector3(transform.position.x, t.sector.floorHeight, transform.position.z);
            }
            else
                outerSpace = true;
        }

        //read input
        if (Input.GetKey(KeyCode.LeftArrow))
            viewDirection.y -= Time.deltaTime * 90;

        if (Input.GetKey(KeyCode.RightArrow))
            viewDirection.y += Time.deltaTime * 90;

        if (viewDirection.y < -180) viewDirection.y += 360;
        if (viewDirection.y > 180) viewDirection.y -= 360;

        //restricted up/down looking angle as sprites look really bad when looked at steep angle
        //also the game doesn't really require such as originally there was no way to rotate camera pitch
        //if (viewDirection.x < -90) viewDirection.x = -90;
        //if (viewDirection.x > 90) viewDirection.x = 90;
        if (viewDirection.x < -45) viewDirection.x = -45;
        if (viewDirection.x > 45) viewDirection.x = 45;

        transform.rotation = Quaternion.Euler(0, viewDirection.y, 0);

        //qwerty and dvorak combatible =^-^=

        float forwardSpeed = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Comma) || Input.GetKey(KeyCode.UpArrow))
            forwardSpeed += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.O) || Input.GetKey(KeyCode.DownArrow))
            forwardSpeed -= 1f;
        Vector3 forward = transform.TransformDirection(Vector3.forward) * forwardSpeed;

        float sidewaysSpeed = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.E))
            sidewaysSpeed += 1f;
        if (Input.GetKey(KeyCode.A))
            sidewaysSpeed -= 1f;
        Vector3 right = transform.TransformDirection(Vector3.right) * sidewaysSpeed;

        //fall down or hit ground
        if (controller.isGrounded || outerSpace)
        {
            if (!lastFrameStickToFloor)
                if (gravityAccumulator > gravityOofThreshold)
                {
                    audioSource.clip = SoundLoader.Instance.LoadSound("DSOOF");
                    audioSource.Play();
                }

            gravityAccumulator = 0f;
        }
        else
            gravityAccumulator += Time.deltaTime * GameManager.Instance.gravity;

        //terminal velocity or in elevator
        if (gravityAccumulator > GameManager.Instance.terminalVelocity || sticktofloor)
            gravityAccumulator = GameManager.Instance.terminalVelocity;

        //apply move
        Vector3 move = Vector3.down * Time.deltaTime * gravityAccumulator;
        if (sidewaysSpeed != 0f || forwardSpeed != 0f)
            move += (forward + right).normalized * speed * Time.deltaTime;
        controller.Move(move);

        //used so player doesn't hit the elevator floor constantly and make the OOF sound
        lastFrameStickToFloor = sticktofloor;

        //apply bop 
        if (Mathf.Abs(forwardSpeed) + Mathf.Abs(sidewaysSpeed) > .1f)
        {
            if (PlayerCamera.Instance != null)
                PlayerCamera.Instance.bopActive = true;

            if(PlayerWeapon.Instance != null)
                PlayerWeapon.Instance.bopActive = true;
        }
        else
        {
            if (PlayerCamera.Instance != null)
                PlayerCamera.Instance.bopActive = false;

            if (PlayerWeapon.Instance != null)
                PlayerWeapon.Instance.bopActive = false;
        }

        //such vanity
        if (PlayerWeapon.Instance != null)
            if (playerThing.currentSector != null)
            {
                SectorController sc = playerThing.currentSector.floorObject;
                PlayerWeapon.Instance.sectorLight = sc.sector.brightness;
            }

        //poke stuff
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2, ~((1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
            {
                bool noway = true;
                Pokeable lc = hit.collider.gameObject.GetComponent<Pokeable>();
                if (lc != null)
                    if (lc.Poke(gameObject))
                        noway = false;

                if (noway && pokeSoundTime <= 0)
                {
                    audioSource.clip = SoundLoader.Instance.LoadSound("DSNOWAY");
                    audioSource.Play();
                    pokeSoundTime = .175f;
                }
            }
        }

        //use weapon
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
        {
            if (PlayerWeapon.Instance != null)
                if (PlayerWeapon.Instance.Fire())
                    if (PlayerWeapon.Instance.Noise > 0)
                        playerThing.CastNoise(PlayerWeapon.Instance.Noise);
        }

        //swap weapon
        if (PlayerWeapon.Instance == null)
        {
            if (SwapWeapon == -1)
                SwapToBestWeapon();

            if (SwapWeapon > -1)
            {
                PlayerWeapon.Instance = Instantiate(PlayerInfo.Instance.WeaponPrefabs[SwapWeapon]);
                CurrentWeapon = SwapWeapon;
                SwapWeapon = -1;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            if (!TrySwapWeapon(1))
                TrySwapWeapon(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            TrySwapWeapon(2);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            TrySwapWeapon(3);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            TrySwapWeapon(4);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            TrySwapWeapon(5);

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public bool TrySwapWeapon(int weapon)
    {
        if (CurrentWeapon == weapon || SwapWeapon != -1)
            return false;

        if (weapon < 0 || weapon >= PlayerInfo.Instance.Weapon.Length)
            return false;

        if (!PlayerInfo.Instance.Weapon[weapon])
            return false;

        switch(weapon)
        {
            default:
                return false;

            case 0:
                break;

            case 1:
                break;

            case 2:
                if (PlayerInfo.Instance.Ammo[0] <= 0)
                    return false;
                break;

            case 3:
                if (PlayerInfo.Instance.Ammo[1] <= 0)
                    return false;
                break;

            case 4:
                if (PlayerInfo.Instance.Ammo[0] <= 0)
                    return false;
                break;

            case 5:
                if (PlayerInfo.Instance.Ammo[2] <= 0)
                    return false;
                break;
        }

        PlayerWeapon.PutAway();
        SwapWeapon = weapon;
        return true;
    }

    public void SwapToBestWeapon()
    {
        if (TrySwapWeapon(3)) return; //shotgun
        if (TrySwapWeapon(4)) return; //chaingun
        if (TrySwapWeapon(2)) return; //pistol
        if (TrySwapWeapon(1)) return; //cainsaw
        if (TrySwapWeapon(0)) return; //fist
        if (TrySwapWeapon(5)) return; //rocketlauncher
    }
}
