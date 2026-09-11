# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Neuro-VISOR (VISOR = Virtual Interactive Simulation Of Reality) is a Unity-based VR/desktop application from Temple University's C2M2 lab for real-time, interactive simulation of neuronal dynamics (Hodgkin-Huxley-type PDE/ODE models) on 3D neuron surface meshes. Users place neuron geometries in a virtual room, interact with them (clamps, synapses, direct voltage stimulation) via VR controllers or mouse/keyboard, and watch the simulation react live while it visualizes voltage propagation via mesh vertex coloring and line graphs.

This is a Unity Editor project, not a typical build-from-source repo — most "development" happens inside the Unity Editor, and there is no CLI build/test/lint pipeline.

## Environment / Setup

- Unity Editor version is pinned: see `ProjectSettings/ProjectVersion.txt` (currently `2019.4.40f1`). The exact version must match to open the project without asset re-serialization churn.
- Git LFS is required (binary assets — 3D models, audio, fonts, images — are tracked per `.gitattributes`). Git ≥ 2.7.0, Git LFS ≥ 2.10.0.
- After cloning, run `./install_git_hooks.sh` to install `.githooks/pre-commit`, which blocks commits if your local Unity/Git/Git LFS versions don't match the required versions (it will `git stash` your changes and abort the commit on mismatch — recover with `git stash pop`).
- Open the project via Unity Hub/Editor, then open `Assets/Scenes/MainScene`. Press Play to run (VR headset auto-detected; falls back to keyboard/mouse emulation otherwise).
- Custom neuron cell files (`.vrn` archives) go in `Assets/StreamingAssets/NeuronalDynamics/Geometries` (editor) or `Neuro-VISOR_Data/StreamingAssets/NeuronalDynamics/Geometries` (standalone build) to be picked up at runtime by the in-scene cell previewer.
- There is no asmdef for the project's own code (`Assets/Scripts/**`) — it all compiles into the default `Assembly-CSharp` assembly. Only the bundled Oculus package has its own asmdefs.
- No CI config, build scripts, or CLI test runner exist in this repo. "Testing" is done manually in the Editor; a few ad hoc MonoBehaviour test scripts live under `Assets/Scripts/C2M2/Tests` and `Assets/Scripts/C2M2/NeuronalDynamics/Tests` (run by adding them to a scene and pressing Play, not via `dotnet test`/NUnit runner), despite `com.unity.test-framework` being present in `Packages/manifest.json`.

## Architecture

All first-party code lives under `Assets/Scripts/C2M2/`, namespaced `C2M2.*`. Third-party code (sparse linear algebra, Oculus SDK, TextMesh Pro, skybox) lives under `Assets/3rdParty/` and `Assets/Oculus/`.

### Core simulation abstraction

- `GameManager` (`C2M2.cs`'s namespace root / `GameManager.cs`) is the global singleton (`GameManager.instance`) holding shared state: active simulations list, the solve `Barrier`, prefabs for managers (clamp/synapse/graph/writer), room/material config, and cross-thread-safe logging helpers (`DebugLogSafe`/`DebugLogErrorSafe`, since simulations run on background threads and Unity API calls are main-thread-only).
- `Interactable` (`Simulation/Interactable.cs`) — minimal abstract base: `SetValues(RaycastHit)` and `GetSimulationTime()`. Lets interaction scripts affect a simulation without knowing its concrete type.
- `Simulation<ValueType, VizType, RaycastType, GrabType>` (`Simulation/Simulation.cs`) — generic base class implementing the simulation lifecycle:
  - `Initialize()` → `BuildVisualization()` → `BuildInteraction()` → starts a dedicated background `Thread` running `Solve()`, plus a `UpdateVisulizationStep` coroutine that pulls `GetValues()` and calls `UpdateVisualization()` on the main thread every `visualizationTimeStep` (0.02s).
  - `Solve()` loops `PreSolveStep → SolveStep(t) → PostSolveStep` for each timestep up to `nT = endTime/timeStep`, synchronizing with other simulations via `GameManager.instance.solveBarrier` (a `System.Threading.Barrier`) so all active neuron simulations step in lockstep, then throttles to real time via `Task.Delay` based on `minTimeStep`.
  - Concrete simulations implement `GetValues()`, `BuildVisualization()`, `UpdateVisualization()`, `SolveStep()`, `PreSolve()`/`PostSolve()`.
- `MeshSimulation` (`Simulation/MeshSimulation.cs`) — specializes `Simulation<float[], Mesh, VRRaycastableMesh, VRGrabbableMesh>` for surface-mesh simulations: manages `VisualMesh`/`ColliderMesh`, vertex-color visualization via `ColorLUT`, and raycast-driven value injection (`raycastHitValue`, `PowerModifier` from thumbstick/arrow keys).
- `NDSimulation` (`NeuronalDynamics/Simulation/NDSimulation.cs`) — the neuron-specific layer on top of `MeshSimulation`. Owns the 1D→3D geometry pipeline:
  - Reads a `.vrn` archive (`VrnReader`) containing UGX-format 1D (wire) and 2D (surface) grids at various refinement/diameter-inflation levels.
  - `Grid1D`/`Grid2D` (`C2M2.NeuronalDynamics.UGX.Grid`) hold the meshes; `Neuron` (`UGX/Neuron.cs`) wraps the 1D grid as a node/edge graph.
  - `Map`/`Mapping` (`Vert3D1DPair[]`, built by `MapUtils.BuildMap`) linearly interpolates between each 3D surface vertex and its two nearest 1D vertices (`lambda` weight) — this is how simulation values computed on the 1D wire-mesh get pushed out to 3D vertex colors (`GetValues()`) and how 3D raycast hits get pulled back to the 1D mesh (`SetValues()`).
  - Owns per-cell managers: `NeuronClampManager` (clamps), `NDGraphManager` (line-graph plot windows), and reads/writes simulation state via `Set1DValues`/`Get1DValues` (abstract, implemented by the concrete solver).
  - `RefinementLevel` and `VisualInflation` let the mesh be regenerated at different fidelity/thickness at runtime (mesh cache keyed by inflation value).
- Concrete solvers derive from `NDSimulation`:
  - `ExampleNDSolver.cs` — simple diffusion toy model, good reference for the minimal set of methods a solver must implement.
  - `SparseSolverTestv1.cs` — the real Hodgkin-Huxley solver: SBDF2 (semi-implicit backward-difference) time integration of the cable equation coupled to the n/m/h gating-variable ODEs, using `CSparse`/`MathNet.Numerics` for sparse linear algebra. **All solver-internal units are MKS (volts, not millivolts, meters, not micrometers)** — raycast hit values, clamp powers, and color-scale bounds must be entered in volts (e.g. 50 mV → `0.05`).
- `NDSimulationManager` (`NeuronalDynamics/Simulation/NDSimulationManager.cs`) — scene-wide coordinator for all active `NDSimulation`s: global pause state, and `FeatureState` (Direct / Clamp / Plot / Synapse) which rewires each simulation's `raycastEventManager.LRTrigger` to the appropriate handler so the same raycast input means different things depending on the selected UI mode. Also owns the single `SynapseManager`.

### Interaction pipeline

- Input is abstracted through `RaycastEventManager`/`RaycastPressEvents` (`Interaction/`), fed by either `OculusEventSignaler` (VR controller raycasts) or `MouseEventSignaler` (desktop raycasts) — see `Interaction/OculusEventSignaler.cs` / `MouseEventSignaler.cs` / `RaycastForward.cs`. This is what lets the same simulation/interaction code run identically in VR and desktop mode.
- Grabbing/scaling of objects (cells, rulers, pivot point) goes through `Interaction/VR/VRGrabbable*` classes, built on top of Oculus's `PublicOVRGrabber`/`PublicOVRGrabbable`.
- Synapses (`NeuronalDynamics/Synapse/`): `SynapseManager` tracks pre/post synapse pairs placed across (possibly different) `NDSimulation`s; `SynapseModels/` contains the NMDA/AMPA/GABA receptor kinetics (`ModelNMDA.cs`, `ModelAMPA.cs`, `ModelGABA.cs`, all implementing `ISynapseModel`), following J.S. Rothman's "Modeling Synapses" (2014). Current synapse current is fed back into a solver's `SetSynapseCurrent` (see `ExampleNDSolver.SetSynapseCurrent`).

### Data flow summary

`.vrn` archive → `VrnReader`/UGX `Grid`s (1D + 2D) → `NDSimulation` builds 3D↔1D `Map` → background solve thread updates 1D values → `GetValues()` interpolates 1D→3D → `UpdateVisualization()` colors the mesh on the main thread. User raycast/clamp/synapse input flows the opposite direction: 3D hit → nearest 1D vertex (`GetNearestPoint`/`SetValues`) → applied into the solver's active-value array in `PostSolveStep`.
