
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public struct GolUtilities
    {
        public static int IndexFromCoordinateWithWrapAround(int x, int y,int width, int height)
        {
            x = (width+x) % width;
            y = (height+y) % height;
            return x + y * width;
        }
        
        
        [BurstCompile]
        public unsafe struct GolSystemSwapJob  : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public  void* CellNewStatesPointer;
            [NativeDisableUnsafePtrRestriction]
            public  void* CellStatesPointer;
        
            public void Execute()
            {
                ref var CellNewStates = ref UnsafeUtility.AsRef<NativeArray<bool>>(CellNewStatesPointer);
                ref var CellStates = ref UnsafeUtility.AsRef<NativeArray<bool>>(CellStatesPointer);
            
                (CellStates, CellNewStates) = (CellNewStates, CellStates);
            }
        }
    }
