#region using
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
#endregion

namespace C2M2.Tests
{
    /// SimpleBurst
    /// <summary>
    /// Simple example of how to use BURST in Unity/C#
    /// </summary>
    public class SimpleBurst : MonoBehaviour
    {
        /// Start
        /// <summary>
        /// Invoked on Start
        /// </summary>
        void Start()
        {
            // populate input and output native arrays
            var input = new NativeArray<float>(10, Allocator.Persistent);
            var output = new NativeArray<float>(1, Allocator.Persistent);
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = 1.0f * i;
            }

            // create a job
            var job = new MyJob
            {
                Input = input,
                Output = output
            };
            // schedule and complete
            job.Schedule().Complete();
            Debug.Log("The result of the sum is: " + output[0]);

            // dispose all native parts
            input.Dispose();
            output.Dispose();
        }

        /// <summary>
        /// Using BurstCompile to compile a Job with Burst
        /// </summary>
        /// Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
        /// but on the first schedule
        [BurstCompile(CompileSynchronously = true)]
        private struct MyJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Input;

            [WriteOnly]
            public NativeArray<float> Output;

            /// Execute
            /// <summary>
            /// Execute thread(s)
            /// </summary>
            public void Execute()
            {
                float result = 0.0f;
                for (int i = 0; i < Input.Length; i++)
                {
                    result += Input[i];
                }
                Output[0] = result;
            }
        }
    }
    
} // C2M2
