### Download
Type `/client gpu` in-game to see whether you need 32 or 64 bit version.

||||
|--|--|--|
ENOTFOUND|ENOTFOUND|ENOTFOUND

### Usage

`/client lua [text]`

Executes the given text as a lua script

Additionally, any `.lua` files in the `lua` folder will be automatically loaded and executed on game startup

### API

The following functionality is available to scripts:

#### chat functions

```add(string text)```

Adds a chat message

```send(string text)```

Sends the given chat message to the server

Note: If server is singleplayer, this method is the same as `add`<br>
Note: `/client` is always interpreted as client-side commands

#### chat events

```onReceived(string text, int type)```

Called whenever a chat message has been received (including user input) 

```onSent(string text)```

Called whenever a chat message has been sent to the server

#### server functions

```(string motd) getMotd()```

Returns the current server MOTD (usually this is the second line in the loading map screen)

```(string name) getName()```

Returns the current server name (usually this is the first line in the loading map screen)

```(boolean singleplayer) isSingleplayer()```

Returns whether the server is the internal singleplayer server

```sendData(string/table data)```

Sends the given raw bytes to the server<br>
Data can either be a string (characters are treated as bytes) or a table (treated as an array of bytes)

Note: Does nothing if server is singleplayer

#### server events

```onConnected()```

Raised when the user successfully connects to the server

```onDisconnected()```

Raises when the user is disconnected from the server

#### world functions

```(int width, int height, int length) getDimensions()```

Returns the width, height, and length of the current world

```(int block) getBlock(int x, int y, int z)```

Returns the block at the given coordinates in the current world

#### world events

```onNewMap```

Raised when the current world is unloaded

```onNewMapLoaded```

Raised when a new world has finished loading

Note: This event is still raised even when an unsuccessful load occurs<br>
(e.g. due to insufficient memory)

#### window functions

```setTitle(string title)```

Sets the text that appears in the window's titlebar
