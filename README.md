# virtual-reality
Repository for Temple University's Center for Computational Mathematics and Modeling's virtual reality project

## Documentation
Our code is documented [here](https://c2m2.github.io/doxyhtml/index.html)

## Build
A standalone build of this project is maintained [here](https://github.com/c2m2/virtual-reality-build). This can be run independently from the Unity editor and will improve performance.

## Connect with us
We have a [blog](https://c2m2vr.wordpress.com/) where we write up project updates, as well as a [trello board](https://trello.com/b/iQ9aepTn/virtual-reality)

## Cloning this repository
Git users (versions < 2.23.0) should clone the repository by using
`git lfs clone` all other should use `git clone`.

For users of older git versions this remedies the problem that every LFS versioned file will ask the user for their password.

## Requirements before contributing
- Unity 2019.1.0f2

- Git version >= 2.7.0

- Git LFS version >= 2.10.0

*Note*: A git pre-commit hook will ensure version consistency.
The user should install the hooks by calling `./install_git_hooks.sh` after clone from the root directory.

### Make sure appropriate versions are used
A pre-commit hook will block commits made with inappropriate versions of Git, Git LFS, or Unity
- git lfs: `git lfs env` in a terminal/console.
- git: `git --version`
- Unity: see UnityEditor

## Quick Start Guide
1. Clone project to any location
2. Ensure the correct version of the Unity Editor is installed
3. Open project in Unity. 
4. Open "MainScene", and ensure that "HHSolver" is enabled in the hierarchy. Hodgkin-Huxley simulation code is provided locally within the project along with example 1D and 3D neuron geometries to run on. These should run automatically upon pressing play.

## Previously developed code
Code was previously hosted on Gitlab [here](https://gitlab.com/vr-lab-repos). This repo still contains useful code for CUDA, compute shaders, Burst examples, OBJ and VTK handling. The code on Bitbucket [here](https://bitbucket.org/c2m2vr/workspace/projects/VIR) is now fully obsolete and replaced by this Github repository. VR grids are maintained [over here](https://github.com/stephanmg/vr-grids). Custom attributes which proved useful during development are maintained [here](https://github.com/stephanmg/vr-utils).

