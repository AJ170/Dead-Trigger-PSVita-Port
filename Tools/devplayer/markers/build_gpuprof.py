"""Builds psp2-cex-gpues4-prof's kernel plugin (github.com/shoui520/psp2-cex-gpues4-prof, MIT) without CMake or make:
the steps of its CMakeLists.txt for psp2_gpuprof.skprx and its stubs, Release flags as in its docs/profiling-guide.md.

python build_gpuprof.py <psp2-cex-gpues4-prof source folder>      (VitaSDK in VITASDK, default C:\\vitasdk)

Writes <source>/build_bol/psp2_gpuprof.skprx (kernel plugin, *KERNEL in ur0:tai/config.txt) and
<source>/build_bol/stubs/libpsp2_gpuprof_stub_weak.a (what bolmarkers links: the plugin's syscalls).
"""
import os, sys

from build_libperf import run, stubs, tool

CFLAGS = ['-O3', '-DNDEBUG', '-Wl,-q', '-Wall', '-Wextra', '-Werror',
          # kernel contexts require aligned accesses and cannot assume VFP/NEON state (as its CMakeLists.txt)
          '-mgeneral-regs-only', '-mno-unaligned-access']
SOURCES = ['src/kernel.c', 'src/continuations.c', 'src/session.c', 'src/work.c', 'src/diagnostic.c']
LIBS = ['taihenForKernel_stub', 'taihenModuleUtils_stub', 'SceSysclibForDriver_stub', 'SceSysmemForDriver_stub',
        'SceThreadmgrForDriver_stub', 'SceProcEventForDriver_stub', 'SceIofilemgrForDriver_stub']


def main():
    src = os.path.abspath(sys.argv[1])
    work = os.path.join(src, 'build_bol')
    os.makedirs(work, exist_ok=True)
    elf = os.path.join(work, 'psp2_gpuprof.elf')
    velf = os.path.join(work, 'psp2_gpuprof.velf')
    skprx = os.path.join(work, 'psp2_gpuprof.skprx')
    run([tool('arm-vita-eabi-gcc')] + CFLAGS + ['-I' + os.path.join(src, 'include'), '-I' + os.path.join(src, 'src')] +
        SOURCES + ['-nostdlib', '-e', 'module_start', '-o', elf] + ['-l' + l for l in LIBS], src)
    run([tool('vita-elf-create'), '-e', os.path.join(src, 'exports.yml'), elf, velf], work)
    run([tool('vita-make-fself'), '-c', velf, skprx], work)  # UNSAFE (kernel): no -s
    stub_dir = os.path.join(work, 'stubs')
    stubs('kernel', elf, os.path.join(src, 'exports.yml'), stub_dir, work)
    # stubs() names archives after the text before the first '_' of the stub sources (libpsp2_stub.a here)
    for weak in ('', '_weak'):
        os.replace(os.path.join(stub_dir, 'libpsp2_stub%s.a' % weak), os.path.join(stub_dir, 'libpsp2_gpuprof_stub%s.a' % weak))
    print('%s: %d bytes' % (skprx, os.path.getsize(skprx)))


if __name__ == '__main__':
    main()
