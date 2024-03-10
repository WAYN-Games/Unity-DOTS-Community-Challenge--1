using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

partial struct GolSystemMainThread : ISystem,ISystemStartStop
{
    private NativeArray<Entity> _cellEntities;
    private NativeArray<bool> _cellStates;
    private NativeArray<bool> _cellNewStates;
    private CGOFGridComponent _config;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CGOFGridComponent>();
        state.RequireForUpdate<MainThread>();
    }

    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {

        _config=  SystemAPI.GetSingleton<CGOFGridComponent>();
        
        var random = Random.CreateFromIndex(_config.Seed);

        var cellCount = _config.Width * _config.Height;
        _cellEntities = new NativeArray<Entity>(cellCount, Allocator.Persistent);
        _cellStates = new NativeArray<bool>(cellCount, Allocator.Persistent);
        _cellNewStates = new NativeArray<bool>(cellCount, Allocator.Persistent);
        state.EntityManager.Instantiate(_config.CellPrefab, _cellEntities);
        
        for (int i = 0; i < cellCount; i++)
        {
            _cellStates[i] = random.NextFloat(0,1) < 0.5f;
            
            state.EntityManager.SetComponentData(_cellEntities[i], new URPMaterialPropertyBaseColor
            {
                Value = _cellStates[i] ? _config.Alive : _config.Dead
            });
            
            int x = i % _config.Width;
            int y = i / _config.Width;
            
            state.EntityManager.SetComponentData(_cellEntities[i],
                LocalTransform.FromPosition(
                    new float3(x-_config.Width/2f, 0, y-_config.Height/2f)
                ));
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        for (var i = 0; i < _config.Width*_config.Height; i++)
        {

            int x = i % _config.Width;
            int y = i / _config.Width;
            
            var n1 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y+1,_config.Width,_config.Height)] ? 1 : 0; 
            var n2 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x,y+1,_config.Width,_config.Height)] ? 1 : 0;
            var n3 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y+1,_config.Width,_config.Height)] ? 1 : 0;
            var n4 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y,_config.Width,_config.Height)] ? 1 : 0;
            var n5 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y,_config.Width,_config.Height)] ? 1 : 0;
            var n6 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x-1,y-1,_config.Width,_config.Height)] ? 1 : 0;
            var n7 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x,y-1,_config.Width,_config.Height)] ? 1 : 0;
            var n8 = _cellStates[GolUtilities.IndexFromCoordinateWithWrapAround(x+1,y-1,_config.Width,_config.Height)] ? 1 : 0;

            var nbNeighboursAlive = n1 + n2 + n3 + n4 + n5 + n6 + n7 + n8;

            switch (nbNeighboursAlive)
            {
                case < 2:
                case > 3:
                    _cellNewStates[i] = false;
                    break;
                case 3:
                    _cellNewStates[i] = true;
                    break;
                default:
                    _cellNewStates[i] = _cellStates[i];
                    break;
            }
        }

        (_cellStates, _cellNewStates) = (_cellNewStates, _cellStates);
            
        for (var i = 0; i < _config.Width*_config.Height; i++)
        {
            state.EntityManager.SetComponentData(_cellEntities[i], new URPMaterialPropertyBaseColor
            {
                Value = _cellStates[i] ? _config.Alive : _config.Dead
            });
        }
        
    }


    [BurstCompile]
    public void OnStopRunning(ref SystemState state)
    {
        _cellEntities.Dispose();
        _cellStates.Dispose();
        _cellNewStates.Dispose();
    }

}
