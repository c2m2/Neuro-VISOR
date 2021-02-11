# virtual-reality
Repository for Temple University's Center for Computational Mathematics and Modeling's virtual reality project

## Code quality
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/ac2c4122b3174e4a8209ef2e791792b3)](https://www.codacy.com?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=c2m2/virtual-reality&amp;utm_campaign=Badge_Grade)

## Documentation
Our code is documented [here](https://c2m2.github.io/doxyhtml/index.html)

## Build
Standalone builds for Windows, Mac, and Linux can be downloaded from the "Releases" section. Standalone builds can be run as executable files without the need for the Unity editor and will improve time performance. Within a static build, custom .vrn cell archives can be placed within `virtual-reality_Data\StreamingAssets\NeuronalDynamics\Geometries` to be run within the application.

## Connect with us
We have a [blog](https://c2m2vr.wordpress.com/) where we write up project updates, as well as a [trello board](https://trello.com/b/iQ9aepTn/virtual-reality).
Other inquiries can be made to seibold@temple.edu.

## Cloning this repository
Git users (versions < 2.23.0) should clone the repository by using
`git lfs clone` all other should use `git clone`.

For users of older git versions this remedies the problem that every LFS versioned file will ask the user for their password.

## Requirements before contributing
- Unity 2019.4.19f1 (LTS). This project has been tested using 2019.1, but commits can not be made without using version 2019.4.19f1.

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
3. Open project in Unity and open Assets/Scenes/MainScene. 
4. Hodgkin-Huxley simulation code is provided locally within the project along with one example neuron geometry to run on. These should run automatically upon pressing play. Additional sample geometries can be downloaded from [INSERT LINK]. If using the Unity Editor, these geometries should be placed within 'Assets/StreamingAssets/NeuronalDynamics/Geometries'. If using a standalone build, the application will look for cell archives within `virtual-reality_Data\StreamingAssets\NeuronalDynamics\Geometries`.

## Custom Simulation Code Guide

## Custom Cell Generation Guide

## Previously developed code
Code was previously hosted on Gitlab [here](https://gitlab.com/vr-lab-repos). This repo still contains useful code for CUDA, compute shaders, Burst examples, OBJ and VTK handling. The code on Bitbucket [here](https://bitbucket.org/c2m2vr/workspace/projects/VIR) is now fully obsolete and replaced by this Github repository. VR grids are maintained [over here](https://github.com/stephanmg/vr-grids). Custom attributes which proved useful during development are maintained [here](https://github.com/stephanmg/vr-utils).

