# Dead Trigger

Keep in mind that this project is still very early.

## Known Issues
1. NavMesh's are inaccurate and/or non-existent.
2. A few broken shaders.

## Contents
1. [Setup](#setup)
2. [Building](#building)
   - [Preparing for building](#preparing-for-building)
   - [Windows/Standalone](#windows--standalone)
3. [Contributing](#contributing)
4. [Credits](#credits)
5. [Links](#links)
   
## Setup
1. Get [Unity 2017.4.40f1](https://unity.com/releases/editor/whats-new/2017.4.40#installs)
   - Make sure you have [Unity Hub](https://unity.com/download) or Unity 2017.4.40f1 standalone installed.
   - Optional: Use [UniHacker](https://github.com/tylearymf/UniHacker) to unlock dark theme and get rid of license related stuff.
2. Clone this repo on your computer.
3. Open it and you're done.
   - The *proper* way of playing it, is opening the scene called AndroidBootCheck (Assets/Scenes/FrontEnd) and then running it.
  
## Building
#### Preparing for building
If you are on windows and want to build for it, nothing required.

If you want to build for other standalone platforms:
1. Open __Unity Hub__.
2. Move to __Installations__.
3. Scroll down until you see *Your Unity Version* and click the gear icon on it.
4. Choose the __Add Modules__ option.
5. Select the Windows/Linux/Mac checkbox, depending on your needs.
6. Click __Continue__ and wait until it installs everything.
7. Move to [Windows / Standalone](#windows--standalone).

#### Windows / Standalone
1. Move to __File > Build Settings__
   - If platform is not Standalone by default, click Standalone option on left list and click __Switch Platform__
     - If __Switch Platform__ button is disabled, it means you're already using it.
   - Its on Left Up corner by the way.
2. In __Target Platform__ option, use platform you want to build to (Windows/Linux/Mac)
  - If you want to build for other platforms that your computer (like linux), you should install proper module for it. (See Modules Part)
3. All other options are optional, leave them like they are if you dont need anything special like debug logs and etc.
4. Click the __Build__ button and select any **Empty** folder on you computer.
5. Done! Now you can enjoy your build!

## Contributing
Make sure to report bugs or commit bug fixes for any bug caused by the decompilation process!
Bug fixes for significant bugs from the original game will also usually be accepted.

Improvements to PC support are also welcome.

Otherwise, this is meant as a vanilla repository for the original game to be modded/enjoyed across multiple platforms.

## Credits
- Virjoinga: Main developer.
- overmet15: Helped fix a ton of issues.

## Links
Nothing yet.
