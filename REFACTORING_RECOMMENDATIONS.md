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

## Category 8: Ion Channel Extensibility

The solver currently hardcodes three ion channels (Na+, K+, leak) directly into `reactF()`
and `SolveStep()`. There is no mechanism for a developer to add a new channel type (e.g.,
calcium, A-type potassium, HCN/Ih) or for a user to enable/disable specific channels at
runtime. The following recommendations provide a path to make ion channels a first-class,
pluggable concept.

### 8.1 Extract ion channels into an `IIonChannel` interface
- **Files**: `SparseSolverTestv1.cs:55-110` (parameter declarations),
  `SparseSolverTestv1.cs:566-595` (`reactF`), `SparseSolverTestv1.cs:633-718`
  (rate functions)
- **Current state**: The three HH channels are fused into `reactF()` as inline math:
  ```
  // potassium: gk * n^4 * (V - ek)
  // sodium:    gna * m^3 * h * (V - ena)
  // leak:      gl * (V - el)
  ```
  Rate functions (`an`, `bn`, `am`, `bm`, `ah`, `bh`) are private static methods with
  hardcoded constants. Adding a calcium channel, for example, would require editing
  `reactF`, adding new state vectors, new rate functions, modifying `SolveStep`,
  `InitializeNeuronCell`, `BuildVectors`, `getM`/`getN`/`getH`, and the save/load code in
  `Menu.cs`. This touches at least 6 methods across 2 files.
- **Recommended design**: Define an `IIonChannel` interface:
  ```csharp
  public interface IIonChannel
  {
      string Name { get; }
      bool Enabled { get; set; }

      // Number of gating variables this channel owns (e.g., Na has 2: m, h)
      int GatingVariableCount { get; }

      // Conductance, reversal potential, max conductance
      double Conductance { get; }
      double ReversalPotential { get; }

      // Compute ionic current contribution: g * gating_product * (V - Erev)
      // 'gatingStates' contains this channel's current gating variable vectors
      Vector ComputeCurrent(Vector voltage, Vector[] gatingStates);

      // Compute dS/dt for each gating variable
      // Returns one Vector per gating variable
      Vector[] ComputeGatingRates(Vector voltage, Vector[] gatingStates);

      // Initialize gating variables to steady-state values
      double[] InitialGatingValues { get; }
  }
  ```
- **Concrete implementations**: `SodiumChannel`, `PotassiumChannel`, `LeakChannel`,
  and future `CalciumChannel`, `HCNChannel`, etc. Each encapsulates its own conductance,
  reversal potential, gating variable count, rate functions, and initial conditions.
- **Integration with solver**: `SparseSolverTestv1` (or its refactored successor) would
  hold a `List<IIonChannel> channels` and loop over them in `reactF` and `SolveStep`:
  ```csharp
  // In reactF — replaces the three hardcoded blocks:
  foreach (var ch in channels)
  {
      if (ch.Enabled)
          output.Add(ch.ComputeCurrent(V, channelGatingStates[ch]), output);
  }

  // In SolveStep — replaces the three explicit N/M/H update blocks:
  foreach (var ch in channels)
  {
      if (ch.Enabled)
      {
          Vector[] rates = ch.ComputeGatingRates(U_Active, channelGatingStates[ch]);
          // Apply SBDF2 explicit update to each gating variable
          for (int g = 0; g < ch.GatingVariableCount; g++)
              explicitUpdate(currentStates[ch][g], prevStates[ch][g], rates[g], ...);
      }
  }
  ```
- **Impact**: Adding a new channel becomes: (1) implement `IIonChannel`, (2) add it to the
  channel list. No solver code changes needed.

### 8.2 Store gating state in a channel-keyed dictionary rather than named fields
- **File**: `SparseSolverTestv1.cs:115-138`
- **Current state**: Gating variables are stored as individual named fields (`N`, `M`, `H`,
  `Npre`, `Mpre`, `Hpre`). Adding a calcium channel with gating variables `r` and `s`
  would require adding `R_Ca`, `S_Ca`, `R_CaPre`, `S_CaPre` as new fields, plus modifying
  every method that touches state vectors.
- **Recommended design**: Use a dictionary keyed by channel:
  ```csharp
  Dictionary<IIonChannel, Vector[]> gatingCurrent;  // current step
  Dictionary<IIonChannel, Vector[]> gatingPrevious;  // previous step
  ```
  This replaces `N`, `M`, `H`, `Npre`, `Mpre`, `Hpre` with a structure that scales
  automatically with the number of channels. The `BuildVectors` and serialization methods
  would iterate the dictionary rather than naming each field.

### 8.3 Channel registration and runtime enable/disable
- **Files**: `SparseSolverTestv1.cs:306-334` (`PreSolve`), UI layer
- **Current state**: No mechanism exists to toggle channels on or off. The leak conductance
  `gl` is set to `0.0` (effectively disabling the leak channel), but this is done by
  zeroing a hardcoded field, and it causes a fallback issue in `SetTargetTimeStep` (see
  6.6). There is no UI or API for users to enable/disable channels.
- **Recommended design**: Each `IIonChannel` has an `Enabled` property. The solver skips
  disabled channels in both `reactF` and `SolveStep`. The UI control panel
  (`NDBoardController`) would expose toggle switches for each registered channel. The
  `SetTargetTimeStep` stability bound computation would sum only enabled channel
  conductances, eliminating the `gl == 0` special case.
- **For `reactConst` removal**: The current `List<double> reactConst` that packs
  `{gk, gna, gl, ek, ena, el}` into a positional list would be replaced by the channels
  themselves carrying their parameters. This eliminates the fragile positional indexing
  (`reactConst[0]`, `reactConst[1]`, etc.).

### 8.4 Save/load should be channel-aware
- **Files**: `SparseSolverTestv1.cs:720-752`, `Menu.cs`, `GameManager.cs:24-33`
- **Current state**: `getM()`, `getN()`, `getH()`, `getUpre()`, `getMpre()`, `getNpre()`,
  `getHpre()` are individual getter methods. `BuildVectors` takes 8 positional `double[]`
  parameters. `GameManager` stores `U`, `M`, `N`, `H`, `Upre`, `Mpre`, `Npre`, `Hpre` as
  public fields. Adding a channel requires adding new getters, new `GameManager` fields,
  and modifying `BuildVectors`'s signature.
- **Recommended design**: Serialize the channel-keyed dictionary directly:
  ```csharp
  Dictionary<string, double[][]> SerializeGatingState()
  {
      var state = new Dictionary<string, double[][]>();
      foreach (var ch in channels)
          state[ch.Name] = gatingCurrent[ch].Select(v => v.ToArray()).ToArray();
      return state;
  }
  ```
  This scales automatically with any number of channels and removes the need for
  `GameManager` to know about individual gating variable names.

### 8.5 CFL stability bound should account for all active channels
- **File**: `SparseSolverTestv1.cs:408-431`
- **Current state**: `SetTargetTimeStep` takes individual conductances as parameters
  (`gna`, `gk`, `gl`). The commented-out lower bound uses `gna + gk + gl`. Adding a
  channel requires modifying the method signature.
- **Recommended design**: Accept a `IEnumerable<IIonChannel>` and sum the max conductances
  of all enabled channels:
  ```csharp
  double gTotal = channels.Where(c => c.Enabled).Sum(c => c.Conductance);
  ```
  This makes the stability computation automatically correct for any combination of active
  channels.

---

## Category 9: Visualization Bug Corrections (Freezing, Unresponsiveness, Invalid Behavior)

The following recommendations address specific code paths that can cause the application to
freeze, become unresponsive, or display invalid visual state when neurons are added,
removed, or when the simulation is running.

### 9.1 `StopCoroutine` typo causes visualization coroutine to run after neuron removal
- **File**: `Simulation.cs:164`
- **Issue**: `StopCoroutine("updateVisulizationStep")` (lowercase 'u') does not match the
  actual method name `UpdateVisulizationStep` (uppercase 'U'). When a neuron is destroyed,
  `OnDestroy` fires but the visualization coroutine is **not stopped**. The coroutine
  continues to call `GetValues()` and `UpdateVisualization()` on the destroyed simulation
  object, which can cause:
  - `NullReferenceException` when accessing destroyed Unity objects
  - Stale mesh colors remaining on screen (the mesh never gets cleaned up)
  - Frame rate drops as orphaned coroutines accumulate with each add/remove cycle
- **Fix**: Fix the string to match the method name. Better yet, store the coroutine
  reference (`Coroutine vizCoroutine = StartCoroutine(...)`) and stop it by reference,
  which avoids string-matching entirely and is more robust.

### 9.2 Barrier deadlock when a neuron is removed or throws an exception
- **File**: `Simulation.cs:192,210,221`
- **Issue**: Each simulation thread calls `solveBarrier.AddParticipant()` on start and
  `solveBarrier.RemoveParticipant()` on exit. All threads synchronize every step via
  `SignalAndWait()` (line 210). If a simulation thread crashes (unhandled exception from
  `async void` — see 2.2) or if `OnDestroy` calls `StopSimulation()` while another thread
  is waiting at the barrier, the barrier participant count becomes wrong. The remaining
  threads will block at `SignalAndWait()` forever, causing:
  - **Complete application freeze**: All solver threads deadlocked, no new visualization
    frames produced.
  - **UI unresponsiveness**: If the main thread is also waiting on solver output (e.g.,
    `GetValues()` under lock), the entire application hangs.
  This is the most likely cause of freezes when removing a neuron while others are running.
- **Fix**:
  1. Wrap the entire solve loop in `try/finally` to guarantee `RemoveParticipant()`:
     ```csharp
     GameManager.instance.solveBarrier.AddParticipant();
     try { /* solve loop */ }
     finally { GameManager.instance.solveBarrier.RemoveParticipant(); }
     ```
  2. Use `SignalAndWait()` with a timeout to prevent infinite blocking:
     ```csharp
     if (!solveBarrier.SignalAndWait(TimeSpan.FromMilliseconds(500)))
     {
         // Barrier wait timed out — another participant likely crashed
         break;
     }
     ```
  3. Consider whether the barrier is necessary at all. Each neuron's solver is independent
     except for synaptic coupling, which could use a lock-free message queue instead.

### 9.3 `activeSims` list is mutated without synchronization during add/remove
- **File**: `GameManager.cs:53`, `NDSimulationLoader.cs:95`,
  `NDSimulationManager.cs:13-18`
- **Issue**: `GameManager.instance.activeSims` is a plain `List<Interactable>`.
  `NDSimulationLoader.Load()` adds to it (line 95) from the main thread.
  `NDSimulationManager.ActiveSimulations` iterates it (lines 13-18) from both the main
  thread (`FeatState` setter) and potentially from solver threads. If a neuron is added
  while `ActiveSimulations` is being iterated (e.g., to update `FeatState` on all sims),
  the list modification during enumeration causes `InvalidOperationException`, which
  manifests as:
  - Newly loaded neurons not appearing in the scene
  - Mode switches (Direct/Clamp/Plot/Synapse) failing silently on some neurons
  - Occasional crash on neuron load
- **Fix**: Use a `ConcurrentBag<Interactable>`, or synchronize access to `activeSims`
  with a dedicated lock. Alternatively, replace `ActiveSimulations` with a cached snapshot
  that is rebuilt only when `activeSims` changes.

### 9.4 Synapse iteration on solver thread races with main-thread synapse add/remove
- **File**: `NDSimulation.cs:230-253` (PostSolveStep, runs on solver thread),
  `SynapseManager.cs:71,103` (SynapticPlacement/DeleteSyn, runs on main thread)
- **Issue**: `PostSolveStep` (line 238) iterates
  `Manager.synapseManager.synapses` via `foreach` on the solver thread. Meanwhile,
  `SynapticPlacement` (line 71) calls `synapses.Add()` and `DeleteSyn` (line 103) calls
  `synapses.Remove()` on the main thread. This is a classic concurrent modification
  scenario. The result:
  - `InvalidOperationException: Collection was modified during enumeration` — crashes the
    solver thread
  - Because of `async void` (see 2.2), this exception is unhandled, killing the solver
    thread silently
  - The barrier participant is not removed (see 9.2), deadlocking all remaining solvers
  - **Net effect**: Adding or removing a synapse while simulations are running can freeze
    the entire application.
- **Fix**: Take a snapshot before iterating:
  ```csharp
  List<(Synapse, Synapse)> synapseSnapshot;
  lock (synapseLock) { synapseSnapshot = new List<(Synapse, Synapse)>(synapseManager.synapses); }
  foreach (var syn in synapseSnapshot) { ... }
  ```
  Or use `ConcurrentBag`/`ConcurrentDictionary`. The lock should be the same object used
  by `SynapticPlacement` and `DeleteSyn`.

### 9.5 `DeleteSyn` calls `FindSynapsePair` twice, enabling race window
- **File**: `SynapseManager.cs:96-116`
- **Issue**: `DeleteSyn` calls `FindSynapsePair(syn)` once to check null (line 98), then
  calls it again to iterate (line 100). Between the two calls, another thread could modify
  `synapses`, causing the second call to return a different result than the first. This can
  lead to:
  - Attempting to destroy an already-destroyed synapse → Unity error
  - Missing the synapse entirely → orphaned GameObjects remain in the scene
  - Removing the wrong pair from the list
- **Fix**: Call `FindSynapsePair` once and cache the result. All operations on that result
  should be performed atomically under a lock.

### 9.6 `Synapse.OnDestroy` triggers recursive `DeleteSyn` calls
- **File**: `Synapse.cs:42-45`, `SynapseManager.cs:96-116`
- **Issue**: `Synapse.OnDestroy()` calls
  `SynapseManager.DeleteSyn(SynapseManager.FindSelectedSyn(this))`. `DeleteSyn` in turn
  calls `Destroy(pair.Item1.gameObject)` and `Destroy(pair.Item2.gameObject)` (lines
  102-103), which triggers `OnDestroy` on the partner synapse. This creates a re-entrant
  call chain:
  ```
  Synapse A OnDestroy → DeleteSyn(A) → Destroy(B) → Synapse B OnDestroy → DeleteSyn(B)
  ```
  During `DeleteSyn(B)`, `FindSynapsePair(B)` searches the `synapses` list, but the pair
  was already removed during `DeleteSyn(A)` (line 104), so it returns null. `DeleteSyn`
  then tries to `Destroy(B.gameObject)` again (line 113) on an already-destroying object.
  This can cause:
  - Unity warnings about destroying already-destroyed objects
  - Orphaned arrow GameObjects (arrows are parented to the pre-synapse but not explicitly
    destroyed)
  - If `synapses.Remove(pair)` throws during re-entrant access, unhandled exception
- **Fix**: Add a re-entrancy guard. For example, check and set a `bool isBeingDeleted`
  flag on each Synapse before proceeding:
  ```csharp
  private void OnDestroy()
  {
      if (isBeingDeleted) return;
      isBeingDeleted = true;
      SynapseManager.DeleteSyn(SynapseManager.FindSelectedSyn(this));
  }
  ```
  Additionally, `DeleteSyn` should destroy arrow GameObjects explicitly (they are children
  of the pre-synapse transform but are not tracked anywhere).

### 9.7 `RescaleArray` mutates the `float[]` returned by `GetValues`, corrupting data
- **File**: `ColorLUT.cs:184-189`, `NDSimulation.cs:298-317`
- **Issue**: `NDSimulation.GetValues()` returns a `float[]` of mapped 3D scalars.
  `ColorLUT.Evaluate()` passes this array to `RescaleArray()`, which modifies the array
  **in place** (rescaling values to LUT indices 0-255). The original voltage data is
  destroyed. If anything reads those values again before the next solve step (e.g., the
  `InfoPanel` hover display, which calls `Get1DValues()` separately), the data is stale
  but the `float[]` returned by `GetValues()` now contains LUT indices instead of voltages.
  While `GetValues` currently creates a new array each call, the mutation is still
  problematic if the pattern is ever changed to reuse buffers (as recommended in 5.4).
  More importantly, the `scalars.Min()` and `scalars.Max()` calls in `GetMinMax` (used by
  `LocalExtrema` and `RollingExtrema` modes) see already-rescaled values on the second
  evaluation in the same frame, producing wrong extrema bounds.
- **Fix**: `RescaleArray` should operate on a dedicated work buffer, not the input array.
  Pre-allocate a `float[] rescaleBuffer` in `ColorLUT` and copy into it before rescaling.

### 9.8 Visualization coroutine continues during pause, but solver doesn't advance
- **File**: `Simulation.cs:141-154`
- **Issue**: The `UpdateVisualizationStep` coroutine checks `Paused` and skips visualization
  when paused. However, upon unpausing, it immediately calls `GetValues()`, which returns
  the current state. If the solver had an exception or deadlock (invisible due to
  `async void`), the visualization will appear frozen with no indication to the user that
  the solver has stopped. The coroutine continues running, wasting cycles calling
  `GetValues()` on a dead solver.
- **Fix**: Add a `solverAlive` flag that the solve loop sets each iteration. The
  visualization coroutine should check this flag and display a visual indicator (e.g., gray
  out the mesh or show "Solver Stopped") if the solver has not advanced for several
  visualization frames.

### 9.9 `NDSimulationLoader.Load` adds simulation to `activeSims` before initialization
- **File**: `NDSimulationLoader.cs:95-103`
- **Issue**: The simulation is added to `activeSims` (line 95) **before** `Initialize()` is
  called (line 103). Between these two lines, any code that iterates `activeSims` (e.g.,
  `FeatState` setter, `ActiveSimulations` property) will find a simulation that has no
  mesh, no clamp manager, no graph manager, and no solver thread — all of which are
  initialized inside `Initialize()`. Accessing any of these uninitialized fields will cause
  `NullReferenceException`. This is especially likely because line 105 immediately triggers
  `FeatState` re-assignment, which iterates `ActiveSimulations` and accesses
  `sim.raycastEventManager`, `sim.clampManager`, `sim.graphManager` — all null before
  `Initialize()`.
  In practice, this is mitigated by the fact that lines 95 and 103-105 run sequentially on
  the main thread. But if any `Awake()`/`Start()` callback or event handler on a newly
  created component triggers an `ActiveSimulations` iteration, it will crash.
- **Fix**: Move `activeSims.Add(solver)` to after `Initialize()` completes.

### 9.10 No cleanup of child objects when a neuron is removed
- **Files**: `NDSimulation.cs`, `NeuronClamp.cs:79-81`, `Synapse.cs:42-45`
- **Issue**: When a neuron GameObject is destroyed, Unity destroys child GameObjects, which
  triggers `OnDestroy` on `NeuronClamp` instances (removing from clamp list) and `Synapse`
  instances (attempting deletion). However:
  1. The neuron is **not removed from `activeSims`**. No code in the destruction path
     calls `GameManager.instance.activeSims.Remove(solver)`. After destruction, `activeSims`
     contains a null/destroyed reference. Iterating `ActiveSimulations` will then fail with
     `MissingReferenceException` (Unity's version of null ref for destroyed objects).
  2. The solver thread may still be running. `StopSimulation()` only sets a cancellation
     token — it doesn't join the thread. If the solve thread accesses `Neuron.nodes` or
     `Verts1D` after the Unity objects are destroyed, it gets `MissingReferenceException`.
  3. Arrow GameObjects created by `SynapseManager.PlaceArrow()` are parented to the
     pre-synapse transform but not tracked in any list. If only one neuron of a synapse
     pair is destroyed, the arrow may become orphaned in the scene.
  4. `controlPanel`, `InfoPanel`, and graph objects may remain in the scene.
- **Fix**: Implement a `DestroySimulation()` method on `NDSimulation` that:
  1. Calls `StopSimulation()` and waits for the solver thread to exit
  2. Removes itself from `activeSims`
  3. Destroys all associated synapses and their arrows
  4. Destroys graphs, clamps, control panels, info panels
  5. Then destroys the GameObject
  This method should be used instead of directly calling `Destroy()` on the neuron.

### 9.11 `async void` solver swallows exceptions, leaving simulation silently dead
- **File**: `Simulation.cs:186`
- **Issue**: The solve loop is `async void Solve()`. If any exception occurs (e.g., NaN
  propagation in rate functions causing `DivideByZeroException`, or an
  `InvalidOperationException` from concurrent list access), it cannot be caught by the
  calling code. The solver thread dies silently. From the user's perspective:
  - The neuron mesh stops updating (frozen at last good frame)
  - No error message is displayed (because `DebugLogSafe` is broken — see 1.2)
  - The barrier participant is not removed (see 9.2), eventually deadlocking other neurons
  - The application appears frozen or the one neuron appears "stuck"
  This is likely the single most common cause of the application becoming unresponsive.
- **Fix**: Wrap the entire solve loop body in `try/catch`:
  ```csharp
  try
  {
      solveBarrier.AddParticipant();
      // ... solve loop ...
  }
  catch (Exception ex)
  {
      Debug.LogError($"Solver crashed: {ex}");
      // Mark simulation as failed so visualization can indicate the error
  }
  finally
  {
      solveBarrier.RemoveParticipant();
      cts.Dispose();
  }
  ```
  Also consider changing `async void` to either a plain `void` method on a dedicated
  thread, or `async Task` with error propagation.

---

## Priority Matrix

| Priority     | Category              | Count | Impact                               |
|--------------|-----------------------|-------|--------------------------------------|
| **Critical** | Bugs/Logic (1.x)      | 12    | Crashes, incorrect simulation results |
| **Critical** | Thread Safety (2.x)   | 5     | Deadlocks, data races, crashes        |
| **Critical** | Viz Bug Fixes (9.x)   | 11    | Freezes, unresponsiveness, stale UI   |
| **High**     | Performance (5.x)     | 9     | GC pressure, frame drops in VR        |
| **High**     | Math Extensibility (6.x) | 7  | Scientific correctness, usability     |
| **High**     | Ion Channels (8.x)    | 5     | Developer extensibility, user control |
| **Medium**   | Architecture (4.x)    | 7     | Maintainability, extensibility        |
| **Medium**   | Stability (7.x)       | 6     | Edge-case crashes, technical debt     |
| **Low**      | Naming (3.x)          | 6     | Code readability                      |

The highest-impact changes for stability are: fixing the `async void` exception swallowing
(9.11), the barrier deadlock on neuron removal (9.2), and the synapse concurrent
modification race (9.4). These three issues form a cascade — an unhandled exception kills
a solver thread, the leaked barrier participant deadlocks remaining threads, and the
application freezes. Fixing the `try/finally` around the barrier (9.2) alone would prevent
the cascade from propagating, even if the root cause exception still occurs.

For extensibility, implementing the `IIonChannel` interface (8.1-8.5) would transform the
solver from a hardcoded three-channel system into a framework that can accommodate any
number of ion channel types, configurable at runtime.
