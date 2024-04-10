# CHIP-8 emulator in C#
A fully functioning CHIP-8 emulator in C# using the [MonoGame](https://www.monogame.net/) Framework for displaying graphics.

---

All original CHIP-8 instructions are implemented as described by:
 - http://devernay.free.fr/hacks/chip8/C8TECH10.HTM
 - https://tobiasvl.github.io/blog/write-a-chip-8-emulator/

Specific quirks implemented as shown:
| Instruction | Quirk |
| ----------- | ------------ |
| 8XY6 & 8XYE | Original implementation, copies VY into VX before shift |
| BNNN | Original implementation, jumps to NNN + V0 |
| FX55 & FX64 | Modern implementation, I is not incremented during the operation |

---

<ins>**Sound Timer**</ins> works but I didn't implement the actual sound engine because I thought it's annoying...  
Should be trivial to add for yourself though, TODO task in interpreter class shows where it should be implemented.

<ins>**Graphics**</ins> are implemented with MonoGame. The MonoGame Window itself is the main program loop, the interpreter is run in a separate thread and draws on the screen through the Display class which handles communication of virtual screen data between MonoGame and the interpreter.
