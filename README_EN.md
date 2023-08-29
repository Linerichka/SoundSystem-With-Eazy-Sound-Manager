## Перевод:
* <a href = "https://github.com/Linerichka/SoundSystem-With-Eazy-Sound-Manager/blob/main/README_RU.md">Русский</a>
* <a href = "https://github.com/Linerichka/SoundSystem-With-Eazy-Sound-Manager/blob/main/README_EN.md">English</a>

---

**SoundSystem** - It is a flexible tool for managing sounds in Unity, which is based on the <a href="https://github.com/JackM36/Eazy-Sound-Manager">**Eazy Sound Manager**</a>,  plugin, while minimizing the need for technical skills and significantly speeding up the development process. **SoundSystem** contains all the functions of **Eazy Sound Manager**, as well as combines them, adds new features and reduces the amount of code needed to create and manage sounds in the Unity project. Thus, anyone with minimal Unity knowledge and without a single line of code can change various sound settings and create simple sound schemes.

## Advantages of using:
* Functionality: contains all the functions of **Eazy Sound Manager**, as well as extends them.
* Allows you to group clips and apply different settings to groups.
* Queue Playback: Allows you to play clips one after another or all at once.
* Ease of use: to add background music, it is enough to add a prefab to the stage and add the necessary clips, you do not need to additionally write a single line of code.
* Speed: Due to its simplicity, **SoundSystem** greatly reduces the time to develop a complex sound scheme in a project.
* Modularity: allows you to use any number of instances on stage without affecting each other's work, while allowing the possibility of combining several instances to create complex sound schemes.
* Allows you to edit all the settings of a group of clips directly from the inspector..

## Using:
You can also get acquainted with a short tutorial on the basic functions of the **SoundSystem**: <a href = "https://youtu.be/kXDuEaaw7Ao">tutorial</a>.
1. Import an asset into your Unity project.
2. Add the prefab "Sound" to the scene, which is located along the path:  "Assets/Lineri/SoundSystem/SoundSystem/Prefabs/Sound.prefab".
3. Assign the necessary clip or clips to the appropriate fields, set the desired settings.
4. Also, the *SoundPocket* class, which is located on the prefab "Sound" contains the bool variable "Play Sound On Awake", if its value is set to true, then when loading the scene, the queue of clips will start playing. This allows you to create simple sound schemes without additional code, for example, it can be used for background music by looping a queue of clips using the "Loop Clips" variable..
5. But if you need a more complex interaction, you need to create a new script that will receive the *ActionSoundPocketManager* class from the prefab placed on the scene and call any of its public methods.
Пример:
>    using Lineri.SoundSystem; ..............

>    GameObject.Find("Sound").GetComponent<ActionSoundPocketManager>().ActionPlayHandler();

Exactly the same approach with the rest of the methods marked in the *ActionSoundPocketManager* script with the "public" modifier. It is important to know here that the *Action Sound Pocket Manager* class calls methods for all *SoundPocket* classes that are on the same object with it. Also, an object can have an unlimited number of classes *SoundPocket* and *ActionSoundPocketManager*. This approach allows you to conveniently group the necessary clips and easily use them.

## Questions and answers:
* **How much does the use of the plugin speed up the development of the sound scheme of the project?** If you omit all the alterations and compare the integration to the finished project, then one development without a plugin is about 6 times longer, again it all depends on the specific project. It is also worth bearing in mind the large number of functions that the plugin provides and the high speed of changing the sound system when using it.
***The plugin looks simple, I won't hit the ceiling of its capabilities if I use it in my project?** The plugin is primarily done with an emphasis on speed and simplicity, so perhaps its functions will not be enough for any big decisions, but it shows itself perfectly in medium and small projects.
* **Tell us more about how SoundSystem accelerates the development process. What steps does it simplify or automate?** Using the plugin, there is no need to design the architecture of the sound circuit, you do not need to think about the complex interactions of a bunch of elements with each other, it is enough to add the necessary blocks to the stage and link them to your project.
* **Can I use SoundSystem to create complex sound scenarios, or is it more suitable for basic settings?** You can use the plugin to create complex scenarios. I can't guarantee that you won't hit the ceiling of the plugin's capabilities, but the plugin covers most of the possible needs.
* **If I want to make changes to sound schemes in the future, how easy will it be to refactor using SoundSystem?** The modular scheme allows you to change only the necessary modules without affecting other components in any way, thanks to this approach, refactoring will take a minimum of time.
* **If I have difficulties using SoundSystem or I have an idea for improvement, how can I get support or leave a suggestion?** Contact me in any way, for example on GitHub. I am developing this plugin also for my own use, so I will support it for a long time and any suggestions or questions are welcome.