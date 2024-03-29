using System;using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

class CGOFGrid : MonoBehaviour
{
    public uint Seed;
    public int Width;
    public int Height;
    public GameObject CubeCellPrefab;
    public GameObject QuadCellPrefab;
    public Color Alive;
    public Color Dead;
    public Version Version;
}

class CGOFGridBaker : Baker<CGOFGrid>
{
    public override void Bake(CGOFGrid authoring)
    {
        Entity bakingEntity = GetEntity(authoring, TransformUsageFlags.WorldSpace);

        AddComponent(bakingEntity, new CGOFGridComponent
        {
            Seed = authoring.Seed,
            Width = authoring.Width,
            Height = authoring.Height,
            CubeCellPrefab = GetEntity(authoring.CubeCellPrefab, TransformUsageFlags.Renderable),
            QuadCellPrefab = GetEntity(authoring.QuadCellPrefab, TransformUsageFlags.Renderable),
            Alive = new float4(authoring.Alive.r, authoring.Alive.g, authoring.Alive.b, authoring.Alive.a),
            Dead =  new float4(authoring.Dead.r, authoring.Dead.g, authoring.Dead.b, authoring.Dead.a),
        });


        switch (authoring.Version)
        {   
            case Version.MainThread:
                AddComponent<MainThread>(bakingEntity);
                break;
            case Version.Threaded:
                AddComponent<Threaded>(bakingEntity);
                break;
            case Version.MultiThreaded:
                AddComponent<MultiThreaded>(bakingEntity);
                break;
            case Version.MultiThreadedOneTexture:
                AddComponent<MultiThreadedOneTexture>(bakingEntity);
                break;
        }
    }
}


public struct CGOFGridComponent : IComponentData
{
    public uint Seed;
    public int Width;
    public int Height;
    public Entity CubeCellPrefab;
    public Entity QuadCellPrefab;
    public float4 Alive;
    public float4 Dead;
}

public struct CellIndex : IComponentData
{
    public int Value;
}


public struct MainThread : IComponentData
{
}
public struct Threaded : IComponentData
{
}
public struct MultiThreaded : IComponentData
{
}
public struct MultiThreadedOneTexture : IComponentData
{
}
public enum Version{
    MainThread,
    Threaded,
    MultiThreaded,
    MultiThreadedOneTexture
}
