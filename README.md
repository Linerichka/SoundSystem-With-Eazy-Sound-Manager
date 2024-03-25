## Translation:
* <a href = "https://github.com/Linerichka/SoundSystem-With-Eazy-Sound-Manager/blob/main/README_RU.md">Русский</a>
* <a href = "https://github.com/Linerichka/SoundSystem-With-Eazy-Sound-Manager/blob/main/README.md">English</a>

## SoundSystem in Unity Asset Store:
* <a href = "https://assetstore.unity.com/packages/tools/audio/eazy-sound-system-272590">Unity Asset Store</a>
---

**SoundSystem** - It is a flexible tool for managing sounds in Unity, which is based on the <a href="https://github.com/JackM36/Eazy-Sound-Manager">**Eazy Sound Manager**</a>,  plugin, while minimizing the need for technical skills and significantly speeding up the development process. **SoundSystem** contains the functions of **Eazy Sound Manager**, as well as combines them, adds new features and reduces the amount of code needed to create and manage sounds in the Unity project. Thus, anyone with minimal Unity knowledge and without a single line of code can change various sound settings and create simple sound schemes. In addition, the main advantages of the plugin are simplicity and performance. A new approach to the implementation of the game's sound environment allows you to forget about all the problems of the old approach, you do not need to write a bunch of code to play sound or create a background melody, now it only takes a couple of clicks. A small codebase allows you to keep the code and comments always up to date, and also allows you to focus on the most important aspect of the plugin - performance. To achieve high performance, the plugin uses collections specially created with an emphasis on speed. In addition, various tricks are also used in the form of pools and initialization of classes in three stages, ensuring ZERO garbage allocation during normal plug-in operation. All this allows you to achieve amazing performance, allowing you to play HUNDREDS of sounds at a time without any noticeable effect on FrameRate! But that's not all, in cases where the slightest FrameRate jump is unacceptable for the game, you can process each action for a certain group of sounds individually, postponing it for several frames, or divide the execution of the action into several frames, making each frame only a small part of the work, implementing it manually. This is possible due to the modular architecture, regardless of when, how and what actions will be performed in relation to the group of sounds "A", this will not affect the group of sounds "B" in any way. At the same time, you still have one-time control over all the sounds in the game using the EazySoundManager class.

## Advantages of using:
* Functionality: contains all the functions of **Eazy Sound Manager**, as well as extends them.
* Steam Audio support is available.
* Performance: it is a lightweight plugin with a strong focus on optimization.
* Allows you to group clips and apply different settings to groups.
* Queue Playback: Allows you to play clips one after another or all at once.
* Ease of use: to add background music, it is enough to add a prefab to the stage and add the necessary clips, you do not need to additionally write a single line of code.
* Speed: due to its simplicity, **SoundSystem** greatly reduces the time to develop a complex sound scheme in a project.
* Modularity: allows you to use any number of instances on stage without affecting each other's work, while allowing the possibility of combining several instances to create complex sound schemes.
* Allows you to edit all the settings of a group of clips directly from the inspector.

## Using:
You can also get acquainted with a short tutorial on the basic functions of the **SoundSystem**: <a href = "https://youtu.be/kXDuEaaw7Ao">tutorial</a>. Or use <a href = "https://github.com/Linerichka/SoundSystem-With-Eazy-Sound-Manager/blob/main/Assets/Lineri/SoundSystem/SoundSystem/Docs/Manual/EN.pdf">documentation</a> that is updated and maintained in a timely manner.
1. Import an asset into your Unity project.
2. Add the prefab "Sound" to the scene, which is located along the path:  "Assets/Lineri/SoundSystem/SoundSystem/Prefabs/Sound.prefab".
3. Select the child object "SoundPocket".
4. Assign the necessary clip or clips to the appropriate fields of the *SoundPocket* class, set the desired settings.
5. Also, the *SoundPocket* class contains the bool variable "Play Sound On Awake", if its value is set to true, then when loading the scene, the queue of clips will start playing. This allows you to create simple sound schemes without additional code, for example, it can be used for background music by looping a queue of clips using the "Loop Clips" variable.
6. But if you need a more complex interaction, you need to create a new script that will inherit from the *SoundPocketManager* class from the prefab placed on the stage and call any of its methods that ends in "Handler".
Example:
```csharp
public class MyClass : SoundPocketManager
{
    using Lineri.SoundSystem;
 
    public void Start()
   {
      PlayHandler();
   }
}
```

7. After that, you need to add this class to the "Sound" object. In the "SoundPocket" field, assign a reference to the object that contains the "SoundPocket" class, in the case of an unchanged prefab, it will be the "SoundPocket" object.

Exactly the same approach with the rest of the methods in the *SoundPocketManager* class whose name ends in "Handler". It is important to know here that the *SoundPocketManager* class calls methods for all *SoundPocket* classes that are located on the object referenced in the "SoundPocket" field of the *SoundPocketManager* class. Also, an object can have an unlimited number of *SoundPocket* classes. This approach allows you to conveniently group the necessary clips and easily use them.

## Questions and answers:
* **Tell us more about how SoundSystem speeds up the development process. What steps does it simplify or automate?** Using the plugin, there is no need to design the architecture of the sound circuit, you do not need to think about the complex interactions of a bunch of elements with each other, it is enough to add the necessary blocks to the stage and link them to your project.
* **Can I use SoundSystem to create complex sound scenarios, or is it more suitable for basic settings?** You can use the plugin to create complex scenarios. The plugin covers most of the possible needs.
* **For projects of what level is the use of SoundSystem optimal?** Formally, SoundSystem is a tool with similar functionality as FMOD, but it has slightly fewer features, hence the very low entry threshold compared to the same FMOD. Therefore, answering the question: 2D indie games of small and medium size, using an event-driven architecture (EDA), are considered optimal for use.
* **Why is using SoundSystem for 3D games not optimal? Is this related to the features of 3D sound?** That's right. Although using Steam Audio is possible, the functionality of working with this tool is somewhat limited.
* **If I want to make changes to sound schemes in the future, how easy will it be to refactor using SoundSystem?** The modular scheme allows you to change only the necessary modules without affecting other components in any way, thanks to this approach, refactoring will take a minimum of time.
* **How does SoundSystem ensure modularity when using multiple instances on stage?** When using SoundSystem, it is recommended to use the event-driven architecture (EDA) of the project. Thanks to this solution, each individual component of the sound circuit will be independent of each other and controlled only by events. 
* **If I have difficulties using SoundSystem or have an idea for improvement, how can I get support or leave a suggestion?** Contact me in any way, for example on GitHub. I am developing this plugin for my own use as well, so I will support it for a long time and any suggestions or questions are welcome.
