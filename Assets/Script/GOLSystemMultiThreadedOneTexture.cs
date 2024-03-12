using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
partial class GOLSystemMultiThreadedOneTexture : SystemBase
{
    private NativeArray<bool> _cellStates;
    private NativeArray<bool> _cellNewStates;
    private CGOFGridComponent _config;
    private NativeArray<Color32> _colors;
    private static readonly int Texture = Shader.PropertyToID("_BaseMap");
    private BatchMaterialID _materialId;
    
    // Has to be SystemBase because of this ...
    private Texture2D _texture;

    
    protected override void OnCreate()
    {
        RequireForUpdate<CGOFGridComponent>();
        RequireForUpdate<MultiThreadedOneTexture>();
    }

    protected override void OnStartRunning()
    {

        _config=  SystemAPI.GetSingleton<CGOFGridComponent>();
        
        var random = Random.CreateFromIndex(_config.Seed);

        var cellCount = _config.Width * _config.Height;
        
        _cellStates = new NativeArray<bool>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _cellNewStates = new NativeArray<bool>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        Entity grid = EntityManager.Instantiate(_config.QuadCellPrefab);
        var lt = EntityManager.GetComponentData<LocalTransform>(grid);
        lt.Rotation = TransformHelpers.LookAtRotation(lt.Position,lt.Position-math.up(),lt.Up());
        lt.Scale = _config.Width;
        EntityManager.SetComponentData(grid,lt);
        
        
        EntitiesGraphicsSystem hybridRendererSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        
        MaterialMeshInfo materialMeshInfo= EntityManager.GetComponentData<MaterialMeshInfo>(grid);
        
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _materialId = hybridRendererSystem.RegisterMaterial(material);
        materialMeshInfo.MaterialID = _materialId;  
        
        _texture = new Texture2D(_config.Width, _config.Height, TextureFormat.RGBA32, 1, true);
        _texture.filterMode = FilterMode.Point;
        _colors = _texture.GetRawTextureData<Color32>();
        material.SetTexture(Texture, _texture);
        
        
        EntityManager.SetComponentData(grid, materialMeshInfo);
        

        for (int i = 0; i < cellCount; i++)
        {
            unsafe
            {
                bool alive = random.NextBool();
           
                _cellStates[i] = alive;
                byte val =  (byte)( *((int*)&alive)*255);
                _colors[i] = new Color32(val,val,val,255) ;
            }
        }
        _texture.Apply();
    }

    protected override void OnUpdate()
    {
        unsafe
        {
            // Applying the texture with a 1 frame delay...
            _texture.Apply();
            
             
            Dependency = new GOLSystemThreadedJob
            {
                CellNewStates = _cellNewStates,
                CellStates = _cellStates,
                Config = _config,
                Colors = _colors
            }.ScheduleParallel(_cellStates.Length,_config.Width,Dependency);

            Dependency = new GolUtilities.GolSystemSwapJob
            {
                CellNewStatesPointer = UnsafeUtility.AddressOf(ref _cellNewStates),
                CellStatesPointer = UnsafeUtility.AddressOf(ref _cellStates)
            }.Schedule(Dependency);
        }
    }

    [BurstCompile]
    public struct GOLSystemThreadedJob : IJobFor
    {
        [WriteOnly]public NativeArray<bool> CellNewStates;
        [WriteOnly]public NativeArray<Color32> Colors;
        [ReadOnly]public NativeArray<bool> CellStates;
        [ReadOnly]public CGOFGridComponent Config;
        
        public void Execute(int index)
        {
            
            int x = index % Config.Width;
            int y = index / Config.Width;
            var n1 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y+1,Config.Width,Config.Height)]; 
            var n4 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y,Config.Width,Config.Height)];
            var n6 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y-1,Config.Width,Config.Height)];
            var n2 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x,y+1,Config.Width,Config.Height)];
            var n7 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x,y-1,Config.Width,Config.Height)];
            var n3 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y+1,Config.Width,Config.Height)];
            var n5 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y,Config.Width,Config.Height)];
            var n8 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y-1,Config.Width,Config.Height)];
            unsafe
            {
                var nbNeighboursAlive = *((int*)&n1) + *((int*)&n2) + *((int*)&n3) + *((int*)&n4) + *((int*)&n5) + *((int*)&n6) + *((int*)&n7) + *((int*)&n8);

                bool alive = (CellStates[index] && nbNeighboursAlive == 2) || nbNeighboursAlive == 3;
                CellNewStates[index] = alive;
                byte val =  (byte)( *((int*)&alive)*255);
                Colors[index] = new Color32(val, val, val, 255);
            }
            ;
        }
    }

    
    public void OnStopRunning(ref SystemState state)
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>().UnregisterMaterial(_materialId);
        _cellStates.Dispose();
        _cellNewStates.Dispose();
    }


}
