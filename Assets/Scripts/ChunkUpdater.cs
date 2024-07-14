using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkUpdater : MonoBehaviour
{
    public Transform player;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;

    private int _offsetPropertyId, _sizePropertyId, _playerPosId;

    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb);

        _offsetPropertyId = Shader.PropertyToID("_Origin");
        _sizePropertyId = Shader.PropertyToID("_Size");
        _playerPosId = Shader.PropertyToID("_PlayerPos");
    }

    void Update()
    {
        _mpb.SetVector(_playerPosId, player.position);

        if (transform.hasChanged)
        {
            _mpb.SetVector(_offsetPropertyId, transform.localPosition);
            _mpb.SetVector(_sizePropertyId, transform.localScale);
            GetComponent<MeshRenderer>().SetPropertyBlock(_mpb);
            transform.hasChanged = false;
        }
    }
}
