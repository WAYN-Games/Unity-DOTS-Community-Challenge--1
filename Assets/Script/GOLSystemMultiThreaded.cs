using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
partial struct GOLSystemMultiThreaded : ISystem,ISystemStartStop
{
    private NativeArray<bool> _cellStates;
    private NativeArray<bool> _cellNewStates;
    private CGOFGridComponent _config;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CGOFGridComponent>();
        state.RequireForUpdate<MultiThreaded>();
    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {

        _config=  SystemAPI.GetSingleton<CGOFGridComponent>();
        
        var random = Random.CreateFromIndex(_config.Seed);

        var cellCount = _config.Width * _config.Height;
        NativeArray<Entity> cellEntities = new NativeArray<Entity>(cellCount, Allocator.Temp);
        _cellStates = new NativeArray<bool>(cellCount, Allocator.Persistent);
        _cellNewStates = new NativeArray<bool>(cellCount, Allocator.Persistent);

        var alteredPrefab = state.EntityManager.Instantiate(_config.CellPrefab);
        state.EntityManager.AddComponent<CellIndex>(alteredPrefab);
        state.EntityManager.Instantiate(alteredPrefab, cellEntities);
        
        for (int i = 0; i < cellCount; i++)
        {
            _cellStates[i] = random.NextFloat(0,1) < 0.5f;
            
            state.EntityManager.SetComponentData(cellEntities[i], new URPMaterialPropertyBaseColor
            {
                Value = _cellStates[i] ? _config.Alive : _config.Dead
            });
            
            int x = i % _config.Width;
            int y = i / _config.Height;
            
            state.EntityManager.SetComponentData(cellEntities[i],
                LocalTransform.FromPosition(
                    new float3(x-_config.Width/2f, 0, y-_config.Height/2f)
                ));
            state.EntityManager.SetComponentData(cellEntities[i], new CellIndex(){Value = i});
        }
        state.EntityManager.DestroyEntity(alteredPrefab);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        unsafe
        {
            state.Dependency = new GOLSystemThreadedJob
            {
                CellNewStates = _cellNewStates,
                CellStates = _cellStates,
                Config = _config
            }.ScheduleParallel(_cellStates.Length,_config.Width,state.Dependency);
        

            state.Dependency = new GolUtilities.GolSystemSwapJob
            {
                CellNewStatesPointer = UnsafeUtility.AddressOf(ref _cellNewStates),
                CellStatesPointer = UnsafeUtility.AddressOf(ref _cellStates)
            }.Schedule(state.Dependency);
    
            state.Dependency = new GOLSystemApplyStateJob
            {
                CellStates = _cellStates,
                Config = _config
            }.ScheduleParallel(state.Dependency);
            
        }
    }
        
        
        


    
    [BurstCompile]
    public partial struct GOLSystemApplyStateJob : IJobEntity
    {
        [ReadOnly] public NativeArray<bool> CellStates;
        [ReadOnly] public CGOFGridComponent Config;

        private void Execute(in CellIndex index, ref URPMaterialPropertyBaseColor color)
        {
            color.Value = CellStates[index.Value] ? Config.Alive : Config.Dead;
        }
    }
    

    

    [BurstCompile]
    public struct GOLSystemThreadedJob : IJobFor
    {
        [WriteOnly]public NativeArray<bool> CellNewStates;
        [ReadOnly]public NativeArray<bool> CellStates;
        [ReadOnly]public CGOFGridComponent Config;
        
        public void Execute(int index)
        {
            
            int x = index % Config.Width;
            int y = index / Config.Width;
            var n1 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y+1,Config.Width,Config.Height)] ? 1 : 0; 
            var n2 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x,y+1,Config.Width,Config.Height)] ? 1 : 0;
            var n3 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y+1,Config.Width,Config.Height)] ? 1 : 0;
            var n4 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y,Config.Width,Config.Height)] ? 1 : 0;
            var n5 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y,Config.Width,Config.Height)] ? 1 : 0;
            var n6 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y-1,Config.Width,Config.Height)] ? 1 : 0;
            var n7 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x,y-1,Config.Width,Config.Height)] ? 1 : 0;
            var n8 = CellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y-1,Config.Width,Config.Height)] ? 1 : 0;

            var nbNeighboursAlive = n1 + n2 + n3 + n4 + n5 + n6 + n7 + n8;

            switch (nbNeighboursAlive)
            {
                case < 2:
                case > 3:
                    CellNewStates[index] = false;
                    break;
                case 3:
                    CellNewStates[index] = true;
                    break;
                default:
                    CellNewStates[index] = CellStates[index];
                    break;
            }
        }
    }

    [BurstCompile]
    public void OnStopRunning(ref SystemState state)
    {
        _cellStates.Dispose();
        _cellNewStates.Dispose();
    }

}
