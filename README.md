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

<table>
  <tbody>
    <tr>
      <td>
        <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=1965652468">
          LCX-3P Luna Supply Vehicle<br/>
          <img src="https://steamuserimages-a.akamaihd.net/ugc/789752963261226928/E657FD80049E62BD2B3CCE3380D501503156AAEF/"/>
        </a>
      </td>
      <td>
      <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=1642837686">
        [WIP]MBT-011C Silex<br/>
        <img src="https://steamuserimages-a.akamaihd.net/ugc/959732611768759967/58FFBFC303F95A3A40B890C2C7033FC0C529609C/" width="70%"/>
        </a>
      </td>
    </tr>
  </tbody>
</table>

# License

- Script is MIT License. you can use and pusblish with your vehicles.
- Some trademarks or registered trademarks are the property of their respective owners.
- Some pictures illustrated by [G.Yamada](http://gkr.skr.jp/)
