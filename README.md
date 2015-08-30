# ClipSynth
Light and fast tool for generating video clips from images and other video clips. Also supports soundtracks. Depends on
[VirtualDub] (http://virtualdub.org/),
[AviSynth] (http://sourceforge.net/projects/avisynth2/) and
[NicAudio] (http://nicaudio.codeplex.com/).

## Setup
Note: If you want to build from source, see `Building Source` below.

1. Create a directory called `ClipSynth`. This is our root folder.
2. Get `ClipSynth.exe` from [releases] (https://github.com/greymind/ClipSynth/releases) and place in the `ClipSynth` root folder.

### Dependencies
1. Obtain VirtualDub and extract it to a `VirtualDub` folder under the `ClipSynth` root folder.
	* The `ClipSynth` root folder is the one that has the `ClipSynth.exe` file.
	* Make sure the VirtualDub files are named `ClipSynth/VirtualDub/vdub.exe` and `ClipSynth/VirtualDub/VirtualDub.exe`
2. Install AviSynth on your computer.
3. Obtain NicAudio and place `NicAudio.dll` in the `ClipSynth` root folder.

## Building Source
Open `ClipSynth.sln` in Visual Studio and build.

## Usage
Run `ClipSynth.exe` and use the images tab and the movies tab for manipulating images and movies using the buttons on the right.

## Team
* Balakrishnan (Balki) Ranganathan
* Scott John Easley

## License
MIT
