# StartPage-for-Playnite

Extension for [Playnite](https://github.com/JosefNemec/Playnite) that adds a configurable start page.

The layout can be customized by splitting and merging panels in a grid when in Edit Mode (can be opened by left-clicking the background).
Each panel can have a view attached to it and views can be swapped in Edit Mode via drag-and-drop.

|Default Mode | Edit Mode |
|-------------|-----------|
|![grafik](https://user-images.githubusercontent.com/24227002/166326510-6121a702-7abd-482b-b727-2ef2786fdaf4.png)|![grafik](https://user-images.githubusercontent.com/24227002/166326546-ed639a7d-6141-415f-8316-d98bc71118e5.png)|

[![Crowdin](https://badges.crowdin.net/startpage-for-playnite/localized.svg)](https://crowdin.com/project/startpage-for-playnite)

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/C1C6CH5IN)

## Features

- Open StartPage when Playnite launches
- Support for Playnite's notifications
- Support for the global progress bar on the StartPage
- Customizable layout
- Customizable background image
  - Source can be the background image of a random game, the background of the last played game and a few more
  - Can also be set to a fixed image or a folder of images
  - Adjustable blur and noise overlay
  - Customizable transition animation
- Built-in views as well as support for other extensions to add views to StartPage (see [here](https://github.com/felixkmh/StartPage-for-Playnite/wiki/Integrating-with-other-Extensions))

## External Views

Other views can be added by other extensions. For some info on how to add support for StartPage to an extension, see [here](https://github.com/felixkmh/StartPage-for-Playnite/wiki/Integrating-with-other-Extensions).

Known extension with StartPage support (potentially incomplete):
| Extension | Views |
|-----------|-------|
|[PlayState](https://github.com/darklinkpower/PlayniteExtensionsCollection/tree/master/source/Generic/PlayState)|PlayState manager|
|[QuickSearch](https://github.com/felixkmh/QuickSearch-for-Playnite)|Search bar|

## Built-in Views

StartPage comes with a set of built-in views.

### Game Shelves

Game Shelves with customizable sorting, grouping and filters.

| Shelves | Shelve Properites |
|--------------|-----------|
|![grafik](https://user-images.githubusercontent.com/24227002/166330058-2fb741be-d0ad-4878-858e-a3092a25eae0.png)|![grafik](https://user-images.githubusercontent.com/24227002/166330144-1d99c2c6-0e26-4721-b989-60db832eabef.png)|


### Clock

Clock also showing the current date. Uses the system time format.

![grafik](https://user-images.githubusercontent.com/24227002/166329790-286656da-09f4-4106-8780-f29c98560c6e.png)

### Weekly Activity

Brief summary of the playtime in the last 7 days. Requires the [GameActivity](https://github.com/Lacro59/playnite-gameactivity-plugin) extension.

![grafik](https://user-images.githubusercontent.com/24227002/166330561-607c278e-e18a-4037-bf54-919366cbb61e.png)

### Most Played
Shows most played games for different timeframes. Can be customized in Edit Mode. Requires the [GameActivity](https://github.com/Lacro59/playnite-gameactivity-plugin) extension.

![grafik](https://user-images.githubusercontent.com/24227002/166330863-d50389dc-2a05-4183-95c8-e09b4fdcb770.png)

### Recent Achievements

Shows recent achievements. Requires the [SuccessStory](https://github.com/Lacro59/playnite-successstory-plugin) extension.

![grafik](https://user-images.githubusercontent.com/24227002/166331100-90e1e535-74b1-4618-9cce-03dac4b26fdd.png)


