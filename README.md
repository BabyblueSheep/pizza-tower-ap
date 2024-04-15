# Archipelago Pizza Tower
An implementation of the [Archipelago randomiser](https://github.com/ArchipelagoMW/Archipelago) for [Pizza Tower](https://store.steampowered.com/app/2231450/Pizza_Tower/).

# Compilation
When cloning, use `git clone --recurse-submodules` or `git submodule update --init` to also copy all submodules. Use `dotnet build ArchipelagoPizzaTower.Patcher.Console` to build the app as normal.

Note that, for now, you need to either have an OpenSSL installation in `C:\Program Files\OpenSSL-Win64` or change the installation yourself in `Configuration Properties > Linker > General > Additional Library Directories`. I don't know if you can freely distribute OpenSSL, sorry.

# License
All code is licensed under the GNU General Public License version 3. See the `LICENSE.txt` file for full details.
