using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkUpdater : MonoBehaviour
{
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;

    private int _offsetPropertyId, _sizePropertyId;

    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb);

        _offsetPropertyId = Shader.PropertyToID("_Offset");
        _sizePropertyId = Shader.PropertyToID("_Size");
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            _mpb.SetVector(_offsetPropertyId, transform.localPosition);
            _mpb.SetVector(_sizePropertyId, transform.localScale);
            GetComponent<MeshRenderer>().SetPropertyBlock(_mpb);
            transform.hasChanged = false;
        }
    }
}
