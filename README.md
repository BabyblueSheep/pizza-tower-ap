# Archipelago Pizza Tower
An implementation of the [Archipelago randomiser](https://github.com/ArchipelagoMW/Archipelago) for [Pizza Tower](https://store.steampowered.com/app/2231450/Pizza_Tower/).

# Compilation
When cloning, use `git clone --recurse-submodules` or `git submodule update --init` to also copy all submodules. Use `dotnet build ArchipelagoPizzaTower.Patcher.Console` to build the app as normal.

Note that you may need to change linker settings for `crypt32` to work properly. I'm unsure if it's allowed to freely distribute it. Same goes for the OpenSSL certificate, but you can [get one here](https://curl.se/docs/caextract.html).

# License
All code is licensed under the GNU General Public License version 3. See the `LICENSE.txt` file for full details.
