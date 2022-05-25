using System.Collections.Generic;
using UnityEngine;

public class SpectreBehavior : PinkyBehavior
{
    public override void Init()
    {
        base.Init();

        MeshRenderer mr = owner.GetComponent<MeshRenderer>();
        MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
        mr.GetPropertyBlock(materialProperties);
        materialProperties.SetFloat("_Alpha", .02f);
        mr.SetPropertyBlock(materialProperties);
    }
}