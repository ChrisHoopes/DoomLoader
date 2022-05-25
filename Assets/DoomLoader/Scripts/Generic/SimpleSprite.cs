using UnityEngine;

public class SimpleSprite : MonoBehaviour
{
    public Sector currentSector;
    public string spriteName;
    public bool alwaysBright;

    protected MeshRenderer mr;
    protected MaterialPropertyBlock materialProperties;

    public Vector2 pivot = new Vector2(.5f, .5f);

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();

        if (mr == null)
        {
            Destroy(gameObject);
            return;
        }
        
        materialProperties = new MaterialPropertyBlock();

        Triangle sectorTriangle = TheGrid.GetExactTriangle(transform.position);
        if (sectorTriangle != null)
        {
            currentSector = sectorTriangle.sector;
            mr.GetPropertyBlock(materialProperties);
            materialProperties.SetFloat("_SectorLight", alwaysBright ? 1f : currentSector.brightness);
            mr.SetPropertyBlock(materialProperties);
        }

        CreateBillboard();
    }

    protected virtual void CreateBillboard()
    {
        if (mr == null)
            return;

        if (!string.IsNullOrEmpty(spriteName))
        {
            Texture tex = TextureLoader.Instance.GetSpriteTexture(spriteName);
            Mesh mesh = Mesher.CreateBillboardMesh((float)tex.width / MapLoader.sizeDividor, (float)tex.height / MapLoader.sizeDividor, pivot.x, pivot.y);
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
