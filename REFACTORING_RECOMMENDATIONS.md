# Neuro-VISOR Refactoring Recommendations

## Project Overview

**Neuro-VISOR** (Virtual Interactive Simulation Of Reality) is a Unity-based VR/desktop
application from Temple University's C2M2 that performs real-time simulation of neuronal
dynamics using the **Hodgkin-Huxley equations** on 1D neuron geometries, maps results onto
3D surface meshes, and visualizes them in an interactive VR environment.

Key components:
- **Solver** (`SparseSolverTestv1`): SBDF2 time-stepping with sparse LU factorization
- **Synapse models**: AMPA, NMDA, GABA receptor models (Rothman 2014)
- **Visualization**: 1D-to-3D mapping, gradient-based color LUT
- **Interaction**: VR raycast clamp/synapse placement, neuron manipulation

---

## Category 1: Bugs and Logic Corrections

### 1.1 `Mathf.Clamp` result is discarded
- **File**: `GameManager.cs:105`
- **Issue**: `Mathf.Clamp(roomSelected, 0, roomOptions.Length - 1);` — the return value
  is never assigned. `roomSelected` is never actually clamped, so an out-of-range value
  will cause an `IndexOutOfRangeException` on line 109.
- **Fix**: Assign the result: `roomSelected = Mathf.Clamp(roomSelected, 0, roomOptions.Length - 1);`

### 1.2 `isRunning` is never set to `true`
- **File**: `GameManager.cs:92`
- **Issue**: `isRunning` is initialized to `false`, set to `false` in `OnApplicationQuit`,
  and toggled in `OnApplicationPause`, but never set to `true` on startup. This means
  `DebugLogSafe()` and `DebugLogErrorSafe()` silently discard all messages.
- **Fix**: Add `isRunning = true;` at the end of `Awake()`.

### 1.3 Coroutine name typo causes silent failure
- **File**: `Simulation.cs:117, 164`
- **Issue**: `StartCoroutine("UpdateVisulizationStep")` uses misspelled "Visulization".
  The `StopCoroutine("updateVisulizationStep")` on line 164 uses lowercase 'u', which
  does **not** match the method name, so `StopCoroutine` silently fails and the coroutine
  continues running after `OnDestroy`.
- **Fix**: Rename the method to `UpdateVisualizationStep` and update both string references.

### 1.4 `curentTimeStep` typo
- **File**: `Simulation.cs:169`
- **Issue**: Missing 't' in `curentTimeStep`. Used throughout the class.
- **Fix**: Rename to `currentTimeStep`.

### 1.5 List indexing throws `ArgumentOutOfRangeException`
- **File**: `NDSimulationLoader.cs:57-59`
- **Issue**: `new List<NDSimulation>(count)` sets **capacity**, not **count**. The list
  has zero elements, so `sims[i] = ...` throws `ArgumentOutOfRangeException`.
- **Fix**: Use `sims.Add((NDSimulation)GameManager.instance.activeSims[i]);`

### 1.6 Unnecessary `using Boo.Lang` import
- **File**: `Synapse.cs:1`
- **Issue**: `Boo.Lang` contains its own `List<T>` that can shadow
  `System.Collections.Generic.List<T>`, causing subtle type resolution bugs. Boo is
  deprecated in Unity.
- **Fix**: Remove `using Boo.Lang;`

### 1.7 `vrnReader` bypasses lazy initialization
- **File**: `NDSimulation.cs:110`
- **Issue**: `MetaInfo` property accesses the private field `vrnReader` (lowercase)
  directly, bypassing the `VrnReader` property (uppercase) which performs lazy
  initialization. Will throw `NullReferenceException` if accessed before `VrnReader`.
- **Fix**: Change to `VrnReader.GetMetaInfo()`.

### 1.8 Mapping setter ignores assigned value
- **File**: `NDSimulation.cs:184-186`
- **Issue**: The setter always calls `MapUtils.BuildMap(Grid1D, Grid2D)` regardless of
  the `value` parameter, making assignment semantically incorrect.
- **Fix**: Either remove the setter or use the `value` parameter.

### 1.9 Redundant null checks in `PrePlaceCheck`
- **File**: `SynapseManager.cs:119-135`
- **Issue**: `if (X == null) { ... } else if (X != null) { ... }` — the `else if` is
  always true. Also calls `FindSynapsePair` three times for the same synapse.
- **Fix**: Call once, cache result, use simple `else`.

### 1.10 Synaptic current driving force uses presynaptic voltage instead of postsynaptic
- **File**: `SparseSolverTestv1.cs:286-295`
- **Issue**: `SynapseCurrentFunction` correctly reads the presynaptic voltage to determine
  **activation** (whether the presynaptic neuron fired — the `isActive` check at line 289).
  However, it then passes that same presynaptic voltage as the `v` parameter to
  `getModelCurrent`, where it is used in the **driving force** term `g * a(t) * (v - Erev)`
  and, for NMDA, the voltage-dependent Mg2+ block `1/(1 + exp(-(v - v05)/k))`.
  Physically, `a(t)` captures the presynaptic signal (neurotransmitter gating triggered at
  activation time `ts`). The driving force `(v - Erev)` determines the magnitude and
  direction of ionic current through channels on the *postsynaptic* membrane, so `v` should
  be the postsynaptic membrane potential. The NMDA Boltzmann block is also a postsynaptic
  phenomenon (Mg2+ blocks the channel from the postsynaptic side depending on postsynaptic
  voltage). Using presynaptic voltage in these terms conflates two distinct physical
  quantities. The postsynaptic voltage is available from `this` solver (since
  `SetSynapseCurrent` runs on the postsynaptic neuron), e.g.,
  `Get1DValues()[newVal.Item2.FocusVert]`.
- **Fix**: Read the postsynaptic voltage separately and pass it to `getModelCurrent`
  for the driving force and Boltzmann terms. The presynaptic voltage should continue to be
  used only for the `isActive` activation check.

### 1.11 `FindSelectedSyn` may return wrong synapse from pair
- **File**: `SynapseManager.cs:39-53`
- **Issue**: Searches by `FocusVert` and `Neuron` but returns the synapse from the pair
  (Item1 or Item2), not necessarily the exact input `syn`. This can return a pre-synapse
  when the caller expects the post-synapse they passed in.
- **Fix**: Return the matching synapse or the pair, not Item1/Item2 unconditionally.

### 1.12 `Clone()` uses shallow copy with non-deterministic ID
- **File**: `Synapse.cs:50-53`
- **Issue**: `MemberwiseClone()` shares the `currentModel` `LinkedListNode` reference.
  `new System.Random()` seeded from system clock can produce duplicate IDs on rapid calls.
- **Fix**: Use a static atomic counter for IDs. Deep-copy or re-initialize mutable fields.

---

## Category 2: Thread Safety

### 2.1 Unsynchronized cross-thread list access
- **File**: `GameManager.cs:153,176`
- **Issue**: `logQ` and `eLogQ` are `List<string>` written from solver threads (via
  `DebugLogSafe`) and read/cleared from main thread (in `Update`). Can cause
  `InvalidOperationException` during `foreach`.
- **Fix**: Replace with `ConcurrentQueue<string>` or add locks.

### 2.2 `async void Solve()` will crash on exceptions
- **File**: `Simulation.cs:186`
- **Issue**: `async void` methods cannot have exceptions caught. Any exception in the
  solve loop will crash the application.
- **Fix**: Use `async Task` with top-level try/catch, or remove async and use
  `Thread.Sleep`.

### 2.3 Barrier participant leak on exception
- **File**: `Simulation.cs:192,221`
- **Issue**: If an exception occurs during the solve loop, `RemoveParticipant()` is never
  called, causing all other simulation threads to deadlock permanently at
  `SignalAndWait()`.
- **Fix**: Wrap `AddParticipant()` through `RemoveParticipant()` in `try/finally`.

### 2.4 `U_Active` lacks synchronization
- **File**: `SparseSolverTestv1.cs`
- **Issue**: `U_Active` is written from `Set1DValues()` (interaction thread) and read from
  `SolveStep()` (solve thread). The `visualizationValuesLock` only protects `U`, not
  `U_Active`.
- **Fix**: Extend lock scope to cover `U_Active`, or use a lock-free double-buffer pattern.

### 2.5 `synapses` list concurrent modification
- **File**: `SynapseManager.cs`
- **Issue**: `synapses` list is iterated in `NDSimulation.PostSolveStep()` (solve thread)
  and modified in `SynapticPlacement`/`DeleteSyn` (main thread). Can cause
  `InvalidOperationException` during enumeration.
- **Fix**: Use a concurrent collection or snapshot-copy pattern for iteration.

---

## Category 3: Naming Conventions

### 3.1 Java-style method names in `ISynapseModel`
- **File**: `ISynapseModel.cs`
- **Issue**: Methods use camelCase (`getModelName`, `getModelCurrent`, `isExcitatory`,
  `getImax`, `isActive`) instead of C# PascalCase.
- **Fix**: Rename to `GetModelName`/`ModelName`, `GetModelCurrent`, `IsExcitatory`,
  `GetImax`/`Imax`, `IsActive`. Use properties where appropriate.

### 3.2 Class name includes version number
- **File**: `SparseSolverTestv1.cs`
- **Issue**: "SparseSolverTestv1" includes "Test" and "v1", suggesting temporary code.
- **Fix**: Rename to `HodgkinHuxleySolver` or `SBDFNeuronSolver`.

### 3.3 Single-letter public field names on GameManager
- **File**: `GameManager.cs:25-33`
- **Issue**: `U`, `M`, `N`, `H`, `Upre`, `Mpre`, `Npre`, `Hpre` are cryptic outside HH
  context and shouldn't be on GameManager at all.
- **Fix**: Move to a `SimulationState` class with descriptive names (`Voltage`,
  `SodiumActivation`, `PotassiumActivation`, `SodiumInactivation`).

### 3.4 Cryptic rate function names
- **File**: `SparseSolverTestv1.cs:633-718`
- **Issue**: `an`, `bn`, `am`, `bm`, `ah`, `bh` are poor code names.
- **Fix**: Rename to `AlphaN`, `BetaN`, `AlphaM`, `BetaM`, `AlphaH`, `BetaH`.

### 3.5 `Dot` is not a dot product
- **File**: `Utils/Math.cs:462-465`
- **Issue**: Returns `Vector3` (component-wise multiply / Hadamard product), not a scalar
  dot product. Misleading name.
- **Fix**: Rename to `HadamardProduct` or `ComponentwiseMultiply`.

### 3.6 Multiple classes in global namespace
- **Files**: `Synapse.cs`, `SynapseManager.cs`, `NDInteractables.cs`, `ArrowUpdate.cs`,
  `ModelAMPA.cs`, `ModelGABA.cs`, `ModelNMDA.cs`, `ISynapseModel.cs`
- **Issue**: All lack namespace declarations, polluting the global namespace.
- **Fix**: Place in `C2M2.NeuronalDynamics.Synapse` or similar.

---

## Category 4: Class Design and Architecture

### 4.1 `GameManager` is a God Object
- **File**: `GameManager.cs`
- **Issue**: Stores simulation state vectors, VR device management, room configuration,
  materials, prefabs, thread synchronization, debug log queues, scale limits, and FPS
  counters.
- **Fix**: Decompose into `SimulationStateStore`, `ThreadSafeLogger`,
  `EnvironmentManager`. Keep GameManager as a thin service locator.

### 4.2 Unused generic type parameters in `Simulation`
- **File**: `Simulation.cs`
- **Issue**: `RaycastType` and `GrabType` are never used as type constraints or field
  types in the class body.
- **Fix**: Remove unused type parameters.

### 4.3 `ActiveSimulations` allocates on every access
- **File**: `NDSimulationManager.cs:9-19`
- **Issue**: Property creates a new `List<NDSimulation>` and casts all elements on every
  access. Called in `FeatState` setter loop.
- **Fix**: Cache the list, invalidate on change, or store as `List<NDSimulation>`.

### 4.4 `new` keyword hides base `Manager` property
- **File**: `NDSimulation.cs:32`
- **Issue**: `public new NDSimulationManager Manager` hides the base class property.
  Callers holding a `MeshSimulation` reference get the wrong manager.
- **Fix**: Use a virtual property or a separate accessor name.

### 4.5 Value tuples should be named types
- **Files**: Throughout codebase
- **Issue**: `(Synapse, Synapse)`, `(int, double)[]` used extensively with confusing
  `Item1`/`Item2` access.
- **Fix**: Create `SynapsePair { Pre, Post }` and `VertexValue { Index, Value }`.

### 4.6 Static mutable model list in `Synapse`
- **File**: `Synapse.cs:13-15`
- **Issue**: All `Synapse` instances share one `LinkedList`. Instance `currentModel` nodes
  point into the static list. Concurrent model cycling can interfere between instances.
- **Fix**: Use an instance-level index into a `static readonly` array.

### 4.7 `HitEvent` initialization timing dependency
- **File**: `NDInteractables.cs:30`
- **Issue**: `GetComponent<RaycastPressEvents>()` in `Awake()` depends on parent manager
  having already attached the component. Unity does not guarantee `Awake()` order.
- **Fix**: Use `Start()` or lazy initialization.

---

## Category 5: Performance Optimizations

### 5.1 `reactF()` allocates vectors every call (HOT PATH)
- **File**: `SparseSolverTestv1.cs:566-595`
- **Issue**: Called twice per solve step. Creates multiple `Vector.Build.Dense()` and
  intermediate vectors (`PointwisePower`, `Subtract`, `PointwiseMultiply`). For a 10K-node
  neuron at ~10K steps/sec, this generates millions of temporary allocations.
- **Fix**: Pre-allocate work vectors as class fields and reuse them.

### 5.2 Rate functions allocate vectors per call (HOT PATH)
- **File**: `SparseSolverTestv1.cs:633-718`
- **Issue**: Each of 6 rate functions creates
  `Vector Vin = Vector.Build.DenseOfVector(V)`. That's 6 full-vector copies per step.
- **Fix**: Pre-allocate a single `Vin` work vector and reuse it.

### 5.3 `SolveStep` clones vectors excessively
- **File**: `SparseSolverTestv1.cs:343-370`
- **Issue**: Multiple `Clone()` calls per step (`R`, `tempState`, `Npre`, `Mpre`, `Hpre`).
- **Fix**: Use pre-allocated buffers and swap references instead of cloning.

### 5.4 `GetValues()` allocates array every visualization frame
- **File**: `NDSimulation.cs:300`
- **Issue**: `new double[Mapping.Data.Count]` called every ~20ms.
- **Fix**: Pre-allocate and reuse the array.

### 5.5 `ArrowUpdate` does expensive operations every frame
- **File**: `ArrowUpdate.cs:21-25,47`
- **Issue**: Every `Update()`: unparents/reparents transforms, calls
  `GetComponentsInChildren<MeshRenderer>()` (allocates + hierarchy walk), sets colors.
- **Fix**: Cache renderer references in `Start()`. Avoid reparenting.

### 5.6 `DateTime.Now` for performance timing
- **File**: `Simulation.cs:211`
- **Issue**: ~15ms resolution on Windows, can jump due to clock adjustments.
- **Fix**: Use `System.Diagnostics.Stopwatch` for sub-millisecond accuracy.

### 5.7 Oversized sparse matrix allocation
- **File**: `SparseSolverTestv1.cs:495`
- **Issue**: `CoordinateStorage(count, count, count * count)` allocates for `count^2`
  entries. For a nearly-tridiagonal Hines matrix with a 10K-node neuron, this allocates
  100M entries when ~30K are needed.
- **Fix**: Estimate `nnz` as `count + 2 * edgeCount` or similar.

### 5.8 `RescaleArray` mutates input simulation data
- **File**: `ColorLUT.cs:184-189`
- **Issue**: `RescaleArray` extension modifies the input `scalars` array in-place,
  mutating simulation output data. Can cause visualization artifacts if read concurrently.
- **Fix**: Operate on a pre-allocated buffer copy.

### 5.9 Unused `meshCache` dictionary
- **File**: `NDSimulation.cs:68`
- **Issue**: Declared but never used.
- **Fix**: Remove, or implement mesh-by-inflation caching to avoid re-reading VRN archives.

---

## Category 6: Mathematical Methods — Flexibility and Extensibility

### 6.1 HH parameters should be configurable
- **File**: `SparseSolverTestv1.cs`
- **Issue**: All biophysical parameters (conductances, reversal potentials, capacitance,
  resistance, initial conditions) are hardcoded as private fields.
- **Fix**: Extract into a `NeuronModelParameters` class or Unity `ScriptableObject` for
  runtime configurability, parameter sweeps, and multiple cell types.

### 6.2 Rate functions should be pluggable
- **File**: `SparseSolverTestv1.cs:633-718`
- **Issue**: Rate functions implement one specific HH parameterization. Code TODOs already
  note this need.
- **Fix**: Use delegate or strategy pattern:
  `public delegate Vector RateFunction(Vector voltage);`
  This enables supporting different HH-family models (original HH, Traub,
  Mainen-Sejnowski) without subclassing.

### 6.3 Synapse model parameters should be injectable
- **Files**: `ModelAMPA.cs`, `ModelGABA.cs`, `ModelNMDA.cs`
- **Issue**: All physical constants hardcoded in constructors.
- **Fix**: Accept a parameter struct: `public ModelAMPA(AMPAParameters parameters)`.
  Enables model calibration and parameter sensitivity analysis.

### 6.4 `ISynapseModel` mixes metadata and computation
- **File**: `ISynapseModel.cs`
- **Issue**: `getModelName()`/`isExcitatory()` are metadata;
  `getModelCurrent()`/`isActive()`/`getImax()` are computational.
- **Fix**: Split into `ISynapseModelInfo` and `ISynapseModelComputation`, or use
  properties for metadata.

### 6.5 Time-stepping method should be a strategy
- **File**: `SparseSolverTestv1.cs:597-611`
- **Issue**: `explicitUpdate` implements SBDF2 but is tightly coupled to the solver.
  Crank-Nicolson exists commented-out in `makeSparseStencils`.
- **Fix**: Extract into `ITimeSteppingScheme` interface to allow swapping between SBDF2,
  CN, backward Euler, or adaptive methods.

### 6.6 Stability bound uses questionable fallback
- **File**: `SparseSolverTestv1.cs:421`
- **Issue**: When `gl == 0`, it's replaced with `1.0`, fundamentally changing the CFL
  stability bound physics.
- **Fix**: Use a stability formula that doesn't depend on `gl` (e.g., based on `gna + gk`).

### 6.7 Rate function singularities are unhandled
- **File**: `SparseSolverTestv1.cs:633-718`
- **Issue**: HH rate functions have well-known singularities (e.g., `an` at V=15mV where
  the expression becomes 0/0). Can produce NaN values that propagate through simulation.
- **Fix**: Implement L'Hôpital limit values at singularity points, as is standard practice
  in computational neuroscience.

---

## Category 7: Stability and Robustness

### 7.1 Fragile initialization lifecycle
- **File**: `Simulation.cs:107`
- **Issue**: Comment `//this is a mess!! :(` acknowledges that `OnAwakePre` →
  `BuildVisualization` → `OnAwakePost` bypasses Unity's standard lifecycle, creating
  fragile initialization ordering.
- **Fix**: Use a proper initialization state machine or builder pattern.

### 7.2 Magic number in mesh rescaling
- **File**: `NDSimulation.cs:389`
- **Issue**: `VisualMesh.Rescale(transform, new Vector3(4, 4, 4)); //TODO why 4?` —
  the authors don't know why this is 4.
- **Fix**: Replace with a named constant, compute from geometry, or document the reason.

### 7.3 Monolithic 753-line solver class
- **File**: `SparseSolverTestv1.cs`
- **Issue**: Handles HH solving, sparse matrix construction, rate functions, synaptic
  currents, state management, and serialization.
- **Fix**: Decompose into `HodgkinHuxleyModel`, `SparseMatrixBuilder`,
  `SynapticCurrentCalculator`, `SimulationStateSerializer`.

### 7.4 `Abs()` mutates input in-place
- **File**: `Utils/Math.cs:380-421`
- **Issue**: Modifies source array/list and returns it. Violates principle of least
  surprise — callers expect non-destructive extension methods.
- **Fix**: Return a new array/list, or rename to `AbsInPlace`.

### 7.5 `Synapse.OnDestroy` may cause recursive deletion
- **File**: `Synapse.cs:44`
- **Issue**: `DeleteSyn` calls `Destroy()` on GameObjects, triggering `OnDestroy` on other
  synapses, potentially causing recursive deletion or list modification during iteration.
- **Fix**: Use a deferred deletion queue or flag to prevent re-entry.

### 7.6 Missing null checks throughout
- **Files**: Multiple
- **Issue**: Property chains assume non-null state:
  `GameManager.instance.simulationManager.synapseManager`,
  `simulation.Neuron.nodes[FocusVert]`, `simulation.Verts1D[vert]` — no bounds checking.
- **Fix**: Add null guards and bounds validation at boundary entry points.

---

## Priority Matrix

| Priority     | Category              | Count | Impact                               |
|--------------|-----------------------|-------|--------------------------------------|
| **Critical** | Bugs/Logic (1.x)      | 12    | Crashes, incorrect simulation results |
| **Critical** | Thread Safety (2.x)   | 5     | Deadlocks, data races, crashes        |
| **High**     | Performance (5.x)     | 9     | GC pressure, frame drops in VR        |
| **High**     | Math Extensibility (6.x) | 7  | Scientific correctness, usability     |
| **Medium**   | Architecture (4.x)    | 7     | Maintainability, extensibility        |
| **Medium**   | Stability (7.x)       | 6     | Edge-case crashes, technical debt     |
| **Low**      | Naming (3.x)          | 6     | Code readability                      |

The highest-impact changes are fixing the logic bugs (especially the discarded
`Mathf.Clamp` result, the `isRunning` flag, and the synaptic voltage error), resolving
thread safety issues (barrier leak, unsynchronized list access), and reducing allocation
pressure in the solver hot path.
