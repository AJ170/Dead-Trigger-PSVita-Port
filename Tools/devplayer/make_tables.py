"""Writes the patch tables that let Unity 2017.4.4f1's Development PS Vita players run on a retail (HENkaku) console.

python make_tables.py [unity Editor\\Data folder]      (default C:\\Program Files\\Unity\\2017\\Editor\\Data)

Unity's Development players (PSP2PlayerDevelopment_Mono.self / _Il2cpp.self) import devkit-only libraries: Razor HUD,
Razor Capture, the Razor CPU markers in ScePerf and SceDeci4pUserp. A retail console has none of them, and the kernel
points every unresolved import at address 0, so the first call (GfxDeviceGXM::InitializeGXM:
sceRazorGpuCaptureEnableSalvage) kills the player right after launch. Patching the import stubs does not help (the
loader rewrites them), so each call site is patched instead: every call is a 2-byte "blx ip" and becomes the 2-byte
"movs r0, #value", the value the player then sees as the function's result. 0 (SCE_OK, "not capturing", "no debugger")
everywhere except where noted in RETURN_VALUES.

One table per player, <player>.txt next to this script. VitaDevPlayer checks the built .self against it
(original or patched SHA-256) and patches it after the build (Assets/Editor/VitaDevPlayer.cs).
"""
import bisect, hashlib, os, re, struct, sys

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = sys.argv[1] if len(sys.argv) > 1 else r'C:\Program Files\Unity\2017\Editor\Data'
PLAYERS = ['PSP2PlayerDevelopment_Mono.self', 'PSP2PlayerDevelopment_Il2cpp.self']
DEVKIT_LIBS = ('SceRazorHud', 'SceRazorCapture', 'ScePerf', 'SceDeci4pUserp')

# Result the player sees instead of 0.
RETURN_VALUES = {
    # GXMRazorHud::PresentFrame reads the live metrics result the call should have written (left uninitialised on the
    # stack) after SCE_OK. Any positive value skips that, without the error message a negative one prints every frame.
    'sceRazorGpuLiveSetBuffer': 1,
}


def u16(b, o): return struct.unpack_from('<H', b, o)[0]
def u32(b, o): return struct.unpack_from('<I', b, o)[0]
def u64(b, o): return struct.unpack_from('<Q', b, o)[0]


def parse_self(data):
    """sections [(name, addr, file offset, size)], symbols [(name, value, size, type)], vaddr -> file offset,
    loaded segments [(vaddr, size, file offset)]"""
    assert data[:4] == b'SCE\0', 'not a SELF'
    header_len = u64(data, 0x10)
    elf = u64(data, 0x40)
    phdr = u64(data, 0x48)
    shdr = u64(data, 0x50)
    seg_info = u64(data, 0x58)
    assert data[elf:elf + 4] == b'\x7fELF'
    phentsize, phnum, shentsize, shnum, shstrndx = struct.unpack_from('<HHHHH', data, elf + 0x2A)
    segments = []
    for i in range(phnum):
        p_type, p_offset, p_vaddr, p_paddr, p_filesz = struct.unpack_from('<IIIII', data, phdr + i * phentsize)
        offset, length, compression, encryption = struct.unpack_from('<QQQQ', data, seg_info + i * 32)
        if p_type == 1:  # PT_LOAD: must be stored as is (Unity's players are unencrypted, uncompressed)
            assert compression == 1 and offset == p_offset + header_len and length == p_filesz, \
                ('segment %d stored differently: compression %d offset %x length %x' % (i, compression, offset, length))
            segments.append((p_vaddr, p_filesz, offset))
    raw = [struct.unpack_from('<IIIIIIIIII', data, shdr + i * shentsize) for i in range(shnum)]
    strtab = raw[shstrndx][4] + header_len

    def cstr(o):
        return data[o:data.index(b'\0', o)].decode('latin1')

    sections = [(cstr(strtab + s[0]), s[3], s[4] + header_len, s[5]) for s in raw]
    symbols = []
    for s in raw:
        if s[1] == 2:  # SHT_SYMTAB
            names = raw[s[6]][4] + header_len
            for k in range(s[5] // 16):
                n, value, size, info, other, shn = struct.unpack_from('<IIIBBH', data, s[4] + header_len + k * 16)
                symbols.append((cstr(names + n), value, size, info & 15))

    def file_offset(va):
        for vaddr, size, offset in segments:
            if vaddr <= va < vaddr + size:
                return offset + va - vaddr
        raise ValueError('address %08x is in no loaded segment' % va)

    return sections, symbols, file_offset, segments


def movw_movt(h1, h2):
    """(kind, rd, imm16) of a Thumb-2 MOVW / MOVT, else None"""
    if (h1 & 0xFBF0) == 0xF240:
        kind = 'movw'
    elif (h1 & 0xFBF0) == 0xF2C0:
        kind = 'movt'
    else:
        return None
    if h2 & 0x8000:
        return None
    imm = ((h1 & 0xF) << 12) | (((h1 >> 10) & 1) << 11) | (((h2 >> 12) & 7) << 8) | (h2 & 0xFF)
    return kind, (h2 >> 8) & 0xF, imm


def bl_target(h1, h2, at):
    """destination of a Thumb-2 BL / BLX at text offset 'at', else None"""
    if (h1 & 0xF800) != 0xF000 or (h2 & 0xD000) not in (0xD000, 0xC000):
        return None
    s = (h1 >> 10) & 1
    imm = (s << 24) | ((1 - (((h2 >> 13) & 1) ^ s)) << 23) | ((1 - (((h2 >> 11) & 1) ^ s)) << 22) \
        | ((h1 & 0x3FF) << 12) | ((h2 & 0x7FF) << 1)
    if s:
        imm -= 1 << 25
    dest = at + 4 + imm
    return dest & ~3 if (h2 & 0xD000) == 0xC000 else dest


def find_sites(data):
    """call sites [(file offset, original bytes, patched bytes, import, caller, address)],
    devkit import stubs {import: (library, address)}, address of the first loaded segment"""
    sections, symbols, file_offset, segments = parse_self(data)
    stub_names = {}
    for name, value, size, typ in symbols:
        m = re.match(r'_INID_\d+_(\w+?)__\d+_(\w+)$', name)
        if m and m.group(1) in DEVKIT_LIBS:
            stub_names[m.group(2)] = m.group(1)
    stubs = {value & ~1: name for name, value, size, typ in symbols if typ == 2 and name in stub_names}
    assert len(stubs) == len(stub_names), 'import stubs without a symbol: %s' % sorted(set(stub_names) - set(stubs.values()))
    funcs = sorted((value & ~1, size, name) for name, value, size, typ in symbols if typ == 2 and size > 0)
    starts = [f[0] for f in funcs]

    def caller(a):
        i = bisect.bisect_right(starts, a) - 1
        return funcs[i][2] if i >= 0 and funcs[i][0] <= a < funcs[i][0] + funcs[i][1] else '?'

    name, base, offset, size = [s for s in sections if s[0] == '.text'][0]
    code = data[offset:offset + size]
    # symbols (and stub addresses) are relative to the module, the text section's address is absolute
    sites, other = [], []
    pending = {}
    for i in range(0, len(code) - 3, 2):
        h1, h2 = struct.unpack_from('<HH', code, i)
        mm = movw_movt(h1, h2)
        if mm:
            kind, rd, imm = mm
            if kind == 'movw':
                pending[rd] = (i, imm)
            elif rd in pending and i - pending[rd][0] <= 16:
                target = (((imm << 16) | pending[rd][1]) & ~1) - base
                if target in stubs:
                    blx = next((j for j in range(i + 4, min(i + 40, len(code) - 1), 2)
                                if u16(code, j) == 0x4780 | (rd << 3)), None)
                    if blx is None:
                        other.append('address of %s loaded at %08x without a call' % (stubs[target], base + i))
                    else:
                        sites.append((blx, stubs[target], caller(blx), pending[rd][0], i))
                del pending[rd]
        dest = bl_target(h1, h2, i)
        if dest in stubs:
            other.append('direct call to %s at %08x' % (stubs[dest], base + i))
    for k in range(0, len(code) - 3, 4):
        if ((u32(code, k) & ~1) - base) in stubs:
            other.append('literal address of %s at %08x' % (stubs[(u32(code, k) & ~1) - base], base + k))
    assert not other, 'references this patch cannot handle:\n  ' + '\n  '.join(other)
    out = []
    for at, stub, own, movw, movt in sites:
        # an IT instruction in the 4 halfwords before would make the call conditional: "movs" sets flags, so the
        # replacement must not sit in an IT block
        for back in range(2, 10, 2):
            h = u16(code, at - back)
            assert not ((h & 0xFF00) == 0xBF00 and (h & 0xF)), 'call to %s at %08x may be in an IT block' % (stub, base + at)
        value = RETURN_VALUES.get(stub, 0)
        assert 0 <= value <= 255
        fo = file_offset(base + at)
        assert data[fo:fo + 2] == code[at:at + 2]
        # movs r0, #value
        out.append((fo, code[at:at + 2], struct.pack('<H', 0x2000 | value), stub, own, base + at, base + movw, base + movt))
    imports = {name: (stub_names[name], base + rel) for rel, name in stubs.items()}
    return out, imports, segments[0][0], file_offset


# Razor CPU marker functions, in the order of bolmarkers.c's bol_functions[] (BolMarkerCall.func)
MARKER_FUNCS = ['sceRazorCpuIsCapturing', 'sceRazorCpuPushMarkerWithHud', 'sceRazorCpuPopMarker', 'sceRazorCpuSync']
# The PopMarker call in profiler_end gets its own: there r4 holds the marker that ends (null when the
# sample has no marker).
POP_IN_PROFILER_END = 4


def marker_entry(player, sites, imports, seg0, patched, file_offset):
    """the shim's BolPlayer initializer: each marker call as the span from the movw that loads the import stub's
    address to the call, in the patched player, with offsets from the start of the first segment"""
    assert sorted(n for n, (lib, va) in imports.items() if lib == 'ScePerf') == sorted(MARKER_FUNCS), imports
    calls = sorted((s for s in sites if s[3] in MARKER_FUNCS), key=lambda s: s[6])
    lines = ['\t{ "%s", %d, {' % (player, len(calls))]
    end = 0
    for fo, old, new, stub, own, va, movw, movt in calls:
        assert old == b'\xe0\x47' and movw < movt < va, (stub, hex(va))  # blx ip
        assert movw >= end, 'marker calls overlap at %08x' % movw
        end = va + 2
        span = bytes(patched[file_offset(movw):file_offset(va) + 2])
        assert len(span) <= 64
        func = MARKER_FUNCS.index(stub)
        if stub == 'sceRazorCpuPopMarker' and own == '_Z12profiler_endPN9profiling6MarkerE':
            func = POP_IN_PROFILER_END
        lines.append('\t\t{ 0x%08x, %d, %d, %d, %d, /* %s in %s */' % (
            movw - seg0, func, movt - movw, va - movw, len(span), stub, own[:60]))
        lines.append('\t\t\t{ %s } },' % ', '.join('0x%02x' % b for b in span))
    lines.append('\t} },')
    return lines, len(calls)


def main():
    header = ['/* Written by make_tables.py: the Razor CPU marker calls in Unity\'s Development players, as patched by',
              '   Assets/Editor/VitaDevPlayer.cs ("movs r0, #0" in place of each call). Each is the span from the',
              '   movw that loads the import stub\'s address (into ip) to the "blx ip" call; offsets from the start of the',
              '   player\'s first segment. bolmarkers.c checks every span and makes it call its own function. */',
              '#define BOL_MARKER_CALLS_MAX 32',
              'typedef struct {',
              '\tunsigned int offset;',
              '\tunsigned char func;  /* %s, %d %s in profiler_end (marker in r4) */' % (
                  ', '.join('%d %s' % (i, n) for i, n in enumerate(MARKER_FUNCS)), POP_IN_PROFILER_END,
                  MARKER_FUNCS[2]),
              '\tunsigned char movt;  /* position of the movt in the span (the movw is at 0) */',
              '\tunsigned char call;  /* position of the call */',
              '\tunsigned char size;',
              '\tunsigned char bytes[64];',
              '} BolMarkerCall;',
              'typedef struct {',
              '\tconst char *player;',
              '\tunsigned int ncalls;',
              '\tBolMarkerCall call[BOL_MARKER_CALLS_MAX];',
              '} BolPlayer;',
              'static const BolPlayer bol_players[] = {']
    for player in PLAYERS:
        path = os.path.join(DATA, 'PlaybackEngines', 'PSP2Player', player)
        data = open(path, 'rb').read()
        sites, imports, seg0, file_offset = find_sites(data)
        nstubs = len(imports)
        patched = bytearray(data)
        for fo, old, new, stub, own, va, movw, movt in sites:
            patched[fo:fo + 2] = new
        entry, ncalls = marker_entry(player, sites, imports, seg0, patched, file_offset)
        assert ncalls <= 32
        header += entry
        lines = ['# %s (Unity 2017.4.4f1): every call into a devkit-only library becomes "movs r0, #value".' % player,
                 '# Written by make_tables.py; read by Assets/Editor/VitaDevPlayer.cs.',
                 'player %s' % player,
                 'size %d' % len(data),
                 'original %s' % hashlib.sha256(data).hexdigest(),
                 'patched %s' % hashlib.sha256(patched).hexdigest(),
                 '# file offset, original bytes, patched bytes, import, caller']
        for fo, old, new, stub, own, va, movw, movt in sites:
            lines.append('%08x %s %s %s %s' % (fo, old.hex(), new.hex(), stub, own))
        out = os.path.join(HERE, os.path.splitext(player)[0] + '.txt')
        with open(out, 'w', newline='\n') as f:
            f.write('\n'.join(lines) + '\n')
        print('%s: %d devkit imports, %d call sites (%d marker calls) -> %s' % (
            player, nstubs, len(sites), ncalls, os.path.basename(out)))
    header.append('};')
    with open(os.path.join(HERE, 'markers', 'sites.h'), 'w', newline='\n') as f:
        f.write('\n'.join(header) + '\n')


if __name__ == '__main__':
    main()
