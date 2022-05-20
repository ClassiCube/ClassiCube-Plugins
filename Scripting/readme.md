Scripting plugins for various scripting languages are provided here:
* [JavaScript](../../Scripting/Javascript) - powered by [duktape](https://duktape.org/)
* [LUA](../../Scripting//LUA) - powered by LUA (https://www.lua.org/)
* [Python](../../Scripting//Python) - powered by Python (https://www.python.org/)

# API

The following functionality is available to scripts:

## `block` module

### Functions

#### `int parse(string value)`

Attempts to parse the input as either a block ID or block name

Returns `-1` if the input failed to be parsed

## `camera` module

### Functions

#### `int getFOV()`

Returns the current field of view of the camera in degrees

#### `boolean isThird()`

Returns whether the current camera is a third person camera

#### `number getX()`

Returns the current X position of the camera

#### `number getY()`

Returns the current Y position of the camera (vertical)

#### `number getZ()`

Returns the current Z position of the camera

#### `number getYaw()`

Returns the current yaw of the camera in degrees

#### `number getPitch()`

Returns the current pitch of the camera in degrees

## `chat` module

### Functions

#### `add(string text)`

Adds a chat message

#### `addOf(string text, int type)`

Adds a chat message with the given type (See [here](https://github.com/UnknownShadow200/ClassiCube/blob/master/src/Chat.h#L11))

#### `send(string text)`

Sends the given chat message to the server

Note: If server is singleplayer, this method is the same as `add`<br>
Note: `/client` is always interpreted as client-side commands

### Events

#### `onReceived(string text, int type)`

Called whenever a chat message has been received (including user input) 

#### `onSent(string text)`

Called whenever a chat message has been sent to the server

## `env` module

### Functions

#### `setEdgeBlock(int block)`

Sets the block of the horizon that surrounds the world

Default: `Water` block

#### `setEdgeHeight(int height)`

Sets the elevation of the horizon that surrounds the world

Default: `world.getHeight() / 2`

#### `setSidesBlock(int block)`

Sets the block of the borders around the world

Default: `Bedrock` block

#### `setSidesOffset(int offset)`

Sets the offset of borders elevation from horizon elevation

Default: `-2`

#### `setCloudsHeight(int height)`

Sets the elevation of the clouds

Default: `world.getHeight() + 2`

#### `setCloudsSpeed(number speed)`

Sets the speed that clouds move at

Default: `1.0`

#### `setWeather(int weather)`

Sets the weather of the world (sunny/rainy/snowy)

Default: `0 (sunny)`

#### `setWeatherSpeed(number speed)`

Sets the speed that rain/snow falls at

Default: `1.0`

Note: negative values 'fall upwards' instead

#### `setWeatherFade(number speed)`

Sets how quickly rain/snow fades out over distance

Default: `1.0`

Note: Smaller values make rain/snow appear thicker

## `game` module

### Functions

#### `string setBlock(int x, int y, int z, int blockID)`

Attempts to set the block at the given coordinates to the given block ID

*Note:* Returns empty string if the block was successfully updated.<br>
Otherwise returns reason why the block change failed. (e.g. too far away, -hax in MOTD)

*Note:* Clientside change only - the server **is not** informed of the change

#### `string changeBlock(int x, int y, int z, int blockID)`

Attempts to change the block at the given coordinates to the given block ID

*Note:* Returns empty string if the block was successfully updated.<br>
Otherwise returns reason why the block change failed. (e.g. too far away, -hax in MOTD)

*Note:* The server **is** informed of the change

## `inventory` module

### Functions

#### `int getSelected()`

Returns the block ID the player is currently holding in their hand

## `player` module

### Functions

#### `number getReach()`

Returns the current reach distance of the player

#### `number getX()`

Returns the current X position of the player

#### `number getY()`

Returns the current Y position of the player (vertical)

#### `number getZ()`

Returns the current Z position of the player

#### `number getYaw()`

Returns the current yaw of the player in degrees

#### `number getPitch()`

Returns the current pitch of the player in degrees

## `server` module

### Functions

#### `string getMotd()`

Returns the current server MOTD (usually this is the second line in the loading map screen)

#### `string getName()`

Returns the current server name (usually this is the first line in the loading map screen)

#### `string getAddress()`

Returns the address of the server (e.g. `127.0.0.1`)

#### `int getPort()`

Returns the port of the server (e.g. `25565`)

#### `string getAppName`

Returns the current application name (the name that appears in /clients)

#### `setAppName(string name)`

Sets the application name (the name that appears in /clients)

*Note:* This is only sent to the server on initial connection

#### `boolean isSingleplayer()`

Returns whether the server is the internal singleplayer server

#### `sendData(bytearray data)`

Sends the given raw bytes to the server

Note: Does nothing if server is singleplayer

### Events

#### `onConnected()`

Raised when the user successfully connects to the server

#### `onDisconnected()`

Raises when the user is disconnected from the server

## `tablist` module

### Functions

#### `string getPlayer(int id)`

Returns the player name of the given tablist entry (e.g. `TestName`)

#### `string getName(int id)`

Returns the formatted name of the given tablist entry (e.g. `&a[OP] Test&bName`)

#### `string getGroup(int id)`

Returns the group name of the given tablist entry (e.g. `Players`)

#### `int getRank(int id)`

Returns the rank of the given tablist entry within the group (e.g. `10`)
 
#### `remove(int id)`

Returns the tablist entry with the given ID 

#### `set(int id, string player, string name, string group, int groupRank)`

Creates or updates the tablist entry with the given ID

## `world` module

### Functions

#### `int getWidth()`

Returns the width of the current world

#### `int getHeight()`

Returns the height of the current world

#### `int getLength()`

Returns the length of the current world

#### `int getBlock(int x, int y, int z)`

Returns the block at the given coordinates in the current world

### Events

#### `onNewMap`

Raised when the current world is unloaded

#### `onNewMapLoaded`

Raised when a new world has finished loading

*Note:* This event is still raised even when an unsuccessful load occurs<br>
(e.g. due to insufficient memory)

## `window` module

### Functions

#### `setTitle(string title)`

Sets the text that appears in the window's titlebar

#### `pointer getHandle()`

Returns the native window handle
