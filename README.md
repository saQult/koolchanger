# 😎 KOOLChanger
A skin changer for **League of Legends**, allowing you to use **skins** and **chromas**

---

## Features

- Supports **all** skins, chromas and special forms
- Custom skins support
- Skin preview before applying
- Simple usage - apply skins with one click
- Party mode
---

## Preview
![preview](https://github.com/user-attachments/assets/8da874ee-2b3b-49e4-a265-6ae1ddf6ae39)

---

## Installation

Before installation, be sure you have **.NET 8 Runtime** ([**download link**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)) on your PC 

1. **Download** the latest release from [Releases](https://github.com/saQult/koolchanger/releases) 
2. **Extract** the files anywhere on your PC.
3. **Launch** `KOOLChanger.exe`.
4. If the game path is not detected automatically, **select your League folder manually throught settings**

---

## Usage

1. Select any skin you want
2. Wait before status bar shows **"Skins applied"**
3. In game when choosing champion select **default** champion skin
4. Enjoy 🤗

## Custom Skins

1. Open custom skins menu by pressing on button "Custom skins"
2. Drag and drop archive with the custom skin
3. Enable it
4. Enjoy your gassy gragor skin

---

# Party mode

### ⚠ ATTENTION ⚠
**If you are in Russia or in any country with banned cloudflare use any kind of bypass services like vpn or zapret if you want to use our free server**

By default party mode connects to remote server "mrekk.ru"
Source code of this server you can find [here](https://github.com/saQult/KoolChangerLobby)

You can host your own remote server, make some changes [here](https://github.com/saQult/koolchanger/blob/main/CSLOLTool/Services/LobbyService.cs#L10) and build app

> How party mode works?

First of all, it completly removes all of your selected skins (after disabling it comes back your selected skins)

1. Enable party mode
2. Join lobby in league if you are not in lobby
3. KoolChanger parses lobby data throught [LCU api](https://github.com/bryanhitc/lcu-sharp)
4. When you are in league lobby, KoolChanger checks if someone already created lobby with UUID of any lobby members, if not, it creates lobby hub in remote server
5. When someone joins, all other players send their skins to joined player
6. If someone select any skin, this skin sends to another lobby members

# PARTY MODE DOES NOT WORK WITH CUSTOM SKINS (at least for now)
   
Use party mode if you trust your friends because they can make u play with some ugly skin XD

To open debug log press **F1**

---

## How this works
Basicaly, it uses [cslolmanager tool](https://github.com/LeagueToolkit/cslol-manager/tree/master/cslol-tools) cli to install and load .fantome files from [dark seal's repository](https://github.com/darkseal-org/lol-skins-developer)

---

## ToDo
A lot of stuff actually 😅
- **CLEANUP CODE**
- Fix A LOT of bugs
- Party mode with custom skins
- Fix elementalist lux forms previews
- Add more info to custom skins (image, author links etc)

---

## 🙏 Special thanks
- [cslolmanager tool](https://github.com/LeagueToolkit/cslol-manager/tree/master/cslol-tools) - main tool to apply skins
- [All of dark seal's contributors](https://github.com/darkseal-org/lol-skins) - all skins are from this repo
- [LCU api](https://github.com/bryanhitc/lcu-sharp) - used to parse lobby data

---

for any questions, please dm me in discord `missedshotduetospread` or [telegram](https://t.me/missedshotduetospread) `@missedshotduetosrpead`
