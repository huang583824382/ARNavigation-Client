using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arrows : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        None
    }
    public Axis OffsetAxis = Axis.None;
    private MeshRenderer mesh;
    public float Speed = 0.5f;
    public float OffMax = 2.0f;
    public float OffMin = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        ChangeOffset();
    }

    void ChangeOffset()
    {
        Vector2 offset = mesh.material.mainTextureOffset;
        switch (OffsetAxis)
        {
            case Axis.X:
                offset = new Vector2(mesh.material.mainTextureOffset.x > OffMax ? OffMin : mesh.material.mainTextureOffset.x + Time.deltaTime * Speed, 0);
                break;
            case Axis.Y:
                offset = new Vector2(0, mesh.material.mainTextureOffset.y > OffMax ? OffMin : mesh.material.mainTextureOffset.y + Time.deltaTime * Speed);
                break;
        }
        mesh.material.mainTextureOffset = offset;
    }
}
