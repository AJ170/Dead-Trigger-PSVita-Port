# Development player on a retail PS Vita (Unity 2017.4.4f1)

Unity's Development players call devkit-only libraries: Razor HUD, Razor Capture, the Razor CPU markers in ScePerf, and
Deci4p. A retail console has none of them, so the player crashes in `GfxDeviceGXM::InitializeGXM` right after launch.

`Assets/Editor/VitaDevPlayer.cs` runs after every Development build. It patches the built `.self` with the table for
its player (`PSP2PlayerDevelopment_Mono.txt` / `_Il2cpp.txt`, SHA-256 checked before and after). Each of the 41 calls, a
2-byte `blx ip`, becomes a 2-byte `movs r0, #value`. Release builds are left alone.

    python make_tables.py      (rewrites the tables and markers/sites.h: only needed for another Unity version)

## Measuring on the Vita

Copy the files in `for_vita` to the Vita, then add these to the taiHEN `config.txt`, keeping the `*KERNEL` lines under
the existing `*KERNEL` section. Reboot afterwards.

    *KERNEL
    ur0:tai/libperf.skprx
    ur0:tai/psp2_gpuprof.skprx
    *DDTR42069
    ux0:data/DeadTrigger/bolmarkers.suprx

| File | Where |
| --- | --- |
| `for_vita/ur0_tai/*.skprx` | `ur0:tai/` |
| `for_vita/ux0_data_DeadTrigger/*.suprx` | `ux0:data/DeadTrigger/` |

Each launch writes `ux0:data/DeadTrigger/bolmarkers.txt` (what the plugin did). During play it writes
`ux0:data/DeadTrigger/markers_<date>_<time>.csv` every 900 frames. To read one:

    python markers/report.py markers_20261101_210000.csv --top 40

The plugin assumes a 333 MHz CPU, so on a 500 MHz Vita real ms = report ms x 0.67.

The plugin leaves the release game alone, and the Development build has the same title ID as the release game.

**Tools > Vita Dev Player > Connect To Editor Profiler** makes later Development builds connect to Unity's Profiler.
Leave it off for bolmarkers runs. With no editor listening, a connecting player never gets past startup, and an open
Profiler makes the run much slower.

2017 differences from the Borderlands (2018.2) tools: 41 calls instead of 33, 21 marker calls instead of 13, the marker
is in r4 at the PopMarker call in `profiler_end` (r7 in 2018), and markers may hold their name inline at +0x10.
