using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float reTriggerDelay = 1f; //쿨
    [SerializeField] private Color zoneColor = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] private bool hideVisualOnPlay = false;
    
    
    public event Action<GameObject> OnEnterZone;
    
    private int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly Dictionary<GameObject, float> _lastTriggerTime = new Dictionary<GameObject, float>();

    private BoxCollider _box;
    private MeshRenderer _renderer;

    private void Awake()
    {
        _box = GetComponent<BoxCollider>();
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null)
        {
            ApplyColor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 콜라이더가 여러개면 가장 위에있는거만 당하게 하라고 깡통이 조언함
        GameObject victim = other.transform.root.gameObject;

        if (_lastTriggerTime.TryGetValue(victim, out float last) &&
            Time.time - last < reTriggerDelay) return;
        _lastTriggerTime[victim] = Time.time;

        OnEnterZone?.Invoke(victim); // ########## 닿음 이벤트 #############
        Debug.Log($"{name} --사망--> {victim.name}  ");
    }

    private void ApplyColor()
    {
        _renderer = GetComponent<MeshRenderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, zoneColor);
        _renderer.SetPropertyBlock(block);
    }

    private void OnValidate()
    {
        ApplyColor();
    }
}
