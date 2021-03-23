# LightningZoom
Better interface for managing Zoom meetings

Platform: Windows, .NET.    Tested on Windows 7 and 10  
Requires Zoom https://zoom.us/download

LightningZoom is meant to replace the current Zoom interface for easier access to multiple meetings. This is achieved by a tool that stores information about meetings and can launch Zoom directly into the meeting in a variety of ways.

![image](https://user-images.githubusercontent.com/81046437/111885775-4c21fa80-89a0-11eb-90b0-ab76624ee0ec.png)

To setup, click on the Edit tab. Enter meeting data such as name, start/end time, whether you want LightningZoom to automatically launch it for you without even having to press buttons, the room code, and the passcode if it is required (NOTE: the passcode needs to be hashed first and at time of writing further research is required, but it is something along the lines of getting a link from Zoom first and taking it out of the browser). Also what days the meeting will take place on and whether it repeats, seesection on auto running.

![image](https://user-images.githubusercontent.com/81046437/111886156-7d032f00-89a2-11eb-973d-3df0b04edd92.png)

For changes to take effect Update or Add must be clicked, and for changes to persist across application closing/opening Save must be clicked, located in the Main tab. To view save location, click on the Info tab and then the conveniently placed button. The save button will outline in green when there are changes to save.

If you want LightningZoom to auto start your meetings for you, tick the Autostart checkbox. You can set how many minutes ahead of time you want LightningZoom to start the meeting. It will only auto start on days you have selected and if repeat is not ticked LightningZoom will untick the day the meeting was started on so it won’t automatically start again. To view which events are set to auto start for the day, click on the Info tab and select the meeting in question in the list and the desired information will appear on screen. To launch LightningZoom automatically on startup, place the executable in your startups folder, located at C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup

In the Main tab you can start a selected meeting or just quick start the next one. Note that LightningZoom will not quick start a meeting not set for the currentday. If you want LightningZoom to hide into the notification tray, you can click Hide or alternatively tick Launch and Hide and then the next time you click Launch (either) it will also hide. Left click the icon in the notification tray to bring the window back or right click to quick launch your next meeting.

![image](https://user-images.githubusercontent.com/81046437/111885119-1b3fc680-899c-11eb-9440-f7741f449615.png)

LightningZoom contains many checks and balances and data validation - just try and break it. And let me know if you succeed and how. Contact me at jonathan.rabideau@gmail.com or post to this project.

LightningZoom is meant to spread happiness and cheer, Copyright Jonathan Rabideau 2021 under GNU General Public License v3
