using UnityEngine;

public class ThingController : MonoBehaviour 
{
    public string thingName;
    public int thingID;
    public string spriteName;
    public bool alwaysBright;
    public bool dynamic;
    public Sector currentSector;

    protected MeshRenderer mr;
    protected MaterialPropertyBlock materialProperties;

    public BehaviorBase.Behaviors BehaviorType = BehaviorBase.Behaviors.None;
    public BehaviorBase CurrentBehavior;

    public enum ThingType
    {
        Decor, //non-blocking, non-interactive
        Neutral, //blocking or interactive
        Item,
        Monster
    }
    public ThingType thingType = ThingType.Decor;

    public enum SpriteType
    {
        Omni,
        Axis,
        TransparentOmni,
        TransparentAxis
    }
    public SpriteType spriteType = SpriteType.Omni;

    public float gravityAccumulator;

    public void ApplySimpleGravity()
    {
        if (currentSector == null)
            return;

        if (transform.position.y == currentSector.floorHeight)
            return;

        gravityAccumulator += Time.deltaTime * GameManager.Instance.gravity;
        if (gravityAccumulator > GameManager.Instance.terminalVelocity)
            gravityAccumulator = GameManager.Instance.terminalVelocity;

        transform.position += Vector3.down * gravityAccumulator * Time.deltaTime;

        if (transform.position.y < currentSector.floorHeight)
            transform.position = new Vector3(transform.position.x, currentSector.floorHeight, transform.position.z);
    }

    public Vec2I cell;

    protected virtual void OnAwake()
    {
        mr = GetComponent<MeshRenderer>();

        if (mr != null)
        {
            switch(spriteType)
            {
                case SpriteType.Omni:
                    mr.material = MaterialManager.Instance.omniBillboardMaterial;
                    break;

                case SpriteType.Axis:
                    mr.material = MaterialManager.Instance.axisBillboardMaterial;
                    break;

                case SpriteType.TransparentOmni:
                    mr.material = MaterialManager.Instance.transparentOmniBillboardMaterial;
                    break;

                case SpriteType.TransparentAxis:
                    mr.material = MaterialManager.Instance.transparentAxisBillboardMaterial;
                    break;
            }
        }

        if (BehaviorType != BehaviorBase.Behaviors.None)
        {
            CurrentBehavior = BehaviorBase.Instantiate(BehaviorType);
            CurrentBehavior.owner = this;
            CurrentBehavior.Init();
        }
    }

    void Awake()
    {
        OnAwake();
    }

    public void AddToGrid()
    {
        if (!TheGrid.existenceBox.GetValue(cell))
            return;

        int x = cell.x - TheGrid.origoX;
        int y = cell.y - TheGrid.origoY;

        switch (thingType)
        {
            case ThingType.Decor:
                TheGrid.decorThings[x, y].InsertFront(this);
                break;

            case ThingType.Neutral:
                TheGrid.neutralThings[x, y].InsertFront(this);
                break;

            case ThingType.Item:
                TheGrid.itemThings[x, y].InsertFront(this);
                break;

            case ThingType.Monster:
                TheGrid.monsterThings[x, y].InsertFront(this);
                break;
        }
    }

    public void RemoveFromGrid()
    {
        int x = cell.x - TheGrid.origoX;
        int y = cell.y - TheGrid.origoY;

        if (TheGrid.existenceBox.GetValue(cell))
        {
            switch (thingType)
            {
                case ThingType.Decor:
                    TheGrid.decorThings[x, y].DestroyContainingNode(this);
                    break;

                case ThingType.Neutral:
                    TheGrid.neutralThings[x, y].DestroyContainingNode(this);
                    break;

                case ThingType.Item:
                    TheGrid.itemThings[x, y].DestroyContainingNode(this);
                    break;

                case ThingType.Monster:
                    TheGrid.monsterThings[x, y].DestroyContainingNode(this);
                    break;
            }
        }
    }

    public virtual void Init()
    {
        materialProperties = new MaterialPropertyBlock();

        cell.x = Mathf.FloorToInt(transform.position.x);
        cell.y = Mathf.FloorToInt(transform.position.z);

        AddToGrid();

        Triangle sectorTriangle = TheGrid.GetExactTriangle(transform.position);
        if (sectorTriangle == null)
        {
            Debug.Log("Thing \"" + thingName + "\" no sector found.");
            Destroy(gameObject);
            return;
        }

        currentSector = sectorTriangle.sector;
        transform.position = new Vector3(transform.position.x, currentSector.floorHeight, transform.position.z);

        if (mr != null)
        {
            mr.GetPropertyBlock(materialProperties);
            materialProperties.SetFloat("_SectorLight", alwaysBright ? 1f : currentSector.brightness);
            mr.SetPropertyBlock(materialProperties);
        }

        CreateBillboard();

        if (currentSector.Dynamic)
            dynamic = true; //things need to update when on elevators, crushers, etc
        
        if (!dynamic)
            enabled = false;

        SectorController sc = currentSector.floorObject;
        if (dynamic)
            sc.DynamicThings.AddFirst(this);
        else
            sc.StaticThings.Add(this);
    }

    void OnDestroy()
    {
        SectorController sc = null;
        if (currentSector != null)
            if (currentSector.floorObject != null)
                 sc = currentSector.floorObject;

        if (sc != null)
            if (dynamic)
                sc.DynamicThings.Remove(this);
            else
                sc.StaticThings.Remove(this);

        RemoveFromGrid();
    }

    void Update()
    {
        if (currentSector != null)
            transform.position = new Vector3(transform.position.x, currentSector.floorHeight, transform.position.z);
    }

    protected virtual void CreateBillboard()
    {
        if (mr == null)
            return;

        if (!string.IsNullOrEmpty(spriteName))
        {
            Texture tex = TextureLoader.Instance.GetSpriteTexture(spriteName);
            Mesh mesh = Mesher.CreateBillboardMesh((float)tex.width / MapLoader.sizeDividor, (float)tex.height / MapLoader.sizeDividor, .5f, 0);
            GetComponent<MeshFilter>().mesh = mesh;

            materialProperties.SetTexture("_MainTex", tex);
            mr.SetPropertyBlock(materialProperties);
        }
    }

    public void SetBrightness(float value)
    {
        if (mr == null)
            return;

        if (alwaysBright)
            return;

        mr.GetPropertyBlock(materialProperties);
        materialProperties.SetFloat("_SectorLight", value);
        mr.SetPropertyBlock(materialProperties);
    }
}
