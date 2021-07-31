### Download
Type `/client gpu` in-game to see whether you need 32 or 64 bit version.

||||
|--|--|--|
ENOTFOUND|ENOTFOUND|ENOTFOUND

### Compiling

```gcc -shared -fPIC $(pkg-config --cflags ruby-1.9) RubyPlugin.c -o rubyscripting.so $(pkg-config --libs ruby-1.9)```

### Usage

`/client ruby [text]`

Executes the given text as a Ruby script

~~Additionally, any `.ruby` files in the `ruby` folder will be automatically loaded and executed on game startup~~

### API

See the [common API documentation](../readme.md)
