# Dracompress

A simple bulk format converter between STL and Draco formats.

For Windows, because Linux users can already do this pretty easily with the CLI.

### Usage
Either open file(s) with Dracompress, or drag-and-drop files on to the file list, then click whichever button corresponds to the direction you want to convert.

The "Bits:" field configures how many bits should be used in Draco position quantization. Using 0 disables quantization, producing an effectively lossless file.
Values from 1 to 30 use the corresponding number of bits. Useful values are around the 16-20 range for organic models, very high settings can use a lot of RAM.
Be aware that this will affect dimensional accuracy as well as visual quality, so functional parts with tight tolerances should use a high bit depth or 0.
This field is ignored for DRC -> STL conversion, as STL files always use 32-bit positions.
