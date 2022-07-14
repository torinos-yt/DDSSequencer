DDS Sequencer
==============================
**DDS Sequencer** is small module that convert and playing back image sequence in [DDS format](https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide) including compressed pixel data([BCn](https://docs.microsoft.com/en-us/windows/win32/direct3d11/texture-block-compression-in-direct3d-11)) to its own format(`.ddssc`, `.ddsmeta`).

DDS format is a compressed format that is decoded by the GPU, allowing for fast loading and high quality appearance as a sequence through frame-by-frame compression.

This is a small and simple solution for playback image sequence and does not meet the requirements for high performance, high precision video playback. If you want these, consider playing back video with Hap codec or NotchLC codec and more.

Requirements
==============================
- Unity 2022.1 or latar
- [NVIDIA Texture Tools Exporter](https://developer.nvidia.com/nvidia-texture-tools-exporter)(Windows 64bit Only)
- ffmpeg (optional : When convert from movie format)

Currently, this is only tested on `Windows 64bit`.

Install
==============================
It can be installed by adding scoped registry to the manifest file(Packages/manifest.json).

`scopedRegistries`
````
{
    "name": "torinos",
    "url": "https://registry.npmjs.com",
    "scopes": ["jp.torinos"]
}
````
`dependencies`
````
"jp.torinos.ddssequencer": "0.2.0"
````

How to convert image sequence
=============================
To convert the image sequence, use the tool that can be opened from `Window>DDS Sequencer>Sequence Converter` in Unity Editor.  
![tool](https://i.imgur.com/jSL3TRO.png)

### Path Settings
- **Source Type** : Specify the type of source to be converted to dds format from Movie and Image Sequence.
- **Source File / Directory** : Specify the path to the folder containing the source image sequence. Accepts images in some major formats supported by the NVIDIA Texture Tools Exporter.
- **Save Directory** : Specify the path of the folder where the converted asset will be saved. Even if the directory does not exist at the time of execution, it will be created including subdirectories.

### Export Settings
- **Compress Format** : Specify the texture [compression format for dds](https://docs.microsoft.com/en-us/windows/win32/direct3d11/texture-block-compression-in-direct3d-11).
- **Compress Quality** : Specify the compression quality settings in dds, but if you set it to `Production or higher`, it will be processed by the CPU encoder, making it very slow.
- **Use CUDA** : Specify whether CUDA should be used for the compression process. However, as mentioned above, if you set Compress Quality to Production or higher, this setting will be ignored and proceed by the CPU encoder.
- **Flip Verticaly** : Flips the image upside down, `it is always good to enable for Unity`.
- **Generate Mips** : When enabled, the mipmap will be generated at the same time as the dds conversion. Note that this will result in a increase CPU load during playback image sequence.
- **Delete Temp Files** : Delete a group of intermediate files (converted dds images) created during the conversion pipeline at the end of the process.  
- **Frame Rate** : Specifies the frame rate of the sequence. If Movie is specified in Source Type, movie source is sampled with the value specified here.

How to specify image sequence
============================
Specify a folder that contains converted sequence(`.ddssc`) and meta information(`.ddsmeta`) as Sequence Path of Sequence Player component.

The specified folder should contain **only a single .ddsmeta file and the converted sequence at the same time**.

Note
============================
### The resolution of the source to be converted must be a multiple of 4.