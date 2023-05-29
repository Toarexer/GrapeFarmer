# GrapeFarmer

Szőlő farmoló segédprogram a HoltPont RP FiveM szerverhez.\
Grape farming utility for the HoltPont RP FiveM server.

A program működése:
1. Lenyomja az `E` betűt.
2. Megvárja amíg a kijelölt helyen van piros `#FF0000` pixel.
3. Összegyűjti mely pozíciókban van fehér `#FFFFFF` pixel.
4. Innentől kezdve azt figyeli, hogy az összegyűjtött fehér pixelek közül bármelyik pirossáv változott-e.
5. Ha igen, akkor lenyomja a szóközt és a ciklus újraindul.
> Egy ilyen ciklus maximum ~5 másodpercig tarhat. Ha lejár az idő újraindul.

How the program works:
1. It presses the `E` key.
2. It waits until it finds a red `#FF0000` pixel.
3. It collects all white `#FFFFFF` pixel.
4. After that it checks if any of the white pixels turned to red.
5. If yes then it presses the `Spacebar` then restarts the loop.
> If the loop does not complete in ~5 seconds it restarts.

Az ellenőrizni kívánt terület az ablak keretével határozható meg.\
The area to be checked can be selected with the window border.

## Billentyű parancsok/Keybinds

F9  - Ki/Be kapcsolás (Enable/Disable)\
F12 - Ablak előhozása/eltűntetése (Hide/Show Window)

Ezek az értesítő ikon menüjében megváltoztathatóak.\
These can be changed in the notification icon's menu.

## Letöltés/Download:

A program letölthető [itt](https://github.com/Toarexer/GrapeFarmer/tree/master/Release/GrapeFarmer.exe).\
The program can be downloaded [here](https://github.com/Toarexer/GrapeFarmer/tree/master/Release/GrapeFarmer.exe).

Azért, hogy a program biztosan a legfrissebb legyen, ajánlott a kódból való építés.\
To ensure that the program is up to date, it is recommended to build the application from source.
