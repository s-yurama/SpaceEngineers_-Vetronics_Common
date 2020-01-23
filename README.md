# SpaceEngineers Common Vetronics 

This is SpaceEngineers in-game script,
common vetronics for common vehicle.
<img align="right" src="https://user-images.githubusercontent.com/48115430/72948365-48a8d880-3dc8-11ea-836d-b9ff4d7a5e54.png" alt="Assistant Ms.Theoria"/>

### Features

- **Winkers(blinkers) control**  
When you turning left or right, then flush the winkers.
- **Tail lights control**  
When you braking vehicle(hitting space), then light up tail lights.  
and night, then keeping light up while not braking and up intensity while braking.

# Installation

## check the game settings

please find "In-game script" in "advanced" settings menu and it should be checked.

## write custom datas

please write some ids to custom datas in follow blocks.  
This is necessary for programmable block find which light are Winkers/Tail lights.

|Block|Custom Data|
|---|---|
|Cockpit|[Maneuver]|
|any light for winkers|[Maneuver][Winker]|
|any light for tail lights|[Maneuver][TailLight]|

# Examples

<a href="https://steamcommunity.com/sharedfiles/filedetails/?id=1965652468">LCX-3P Luna Supply Vehicle<br/>
<img src="https://user-images.githubusercontent.com/48115430/72358964-c61d7a80-3730-11ea-836c-ff638cc5097a.png" width="33%"/></a>

# License

- Script is MIT License. you can use and pusblish with your vehicles.
- Some trademarks or registered trademarks are the property of their respective owners.
- Some pictures illustrated by [G.Yamada](http://gkr.skr.jp/)
