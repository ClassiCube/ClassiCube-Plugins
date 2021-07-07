### Download
Type `/client gpu` in-game to see whether you need 32 or 64 bit version.

||||
|--|--|--|
ENOTFOUND|ENOTFOUND|ENOTFOUND

### Compiling

```gcc -shared -fPIC $(python3-config --cflags) PythonPlugin.c -o pythonscripting.so $(python3-config --ldflags)```

### Usage

`/client python [text]`

Executes the given text as a Python script

~~Additionally, any `.python` files in the `python` folder will be automatically loaded and executed on game startup~~

### API

See the [common API documentation](../readme.md)
