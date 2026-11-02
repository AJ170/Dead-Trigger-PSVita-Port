"""Builds libperf (github.com/shoui520/libperf, MIT) without CMake or make: the steps of its CMakeLists.txt.

python build_libperf.py <libperf source folder>      (VitaSDK in VITASDK, default C:\\vitasdk)

Writes <source>/build_bol/libperf.skprx (kernel plugin) and libperf.suprx (user module).
"""
import glob, os, subprocess, sys

SDK = os.environ.get('VITASDK', r'C:\vitasdk')
BIN = os.path.join(SDK, 'bin')
CFLAGS = ['-Wl,-q', '-Wall', '-Wextra', '-Werror', '-O3', '-fno-optimize-sibling-calls']


def tool(name):
    return os.path.join(BIN, name)


def run(args, cwd):
    r = subprocess.run(args, cwd=cwd, capture_output=True, text=True)
    if r.stdout.strip() or r.stderr.strip():
        print(r.stdout + r.stderr)
    if r.returncode != 0:
        sys.exit('failed: ' + ' '.join(args))


def stubs(kind, elf, exports, out_dir, work):
    """vita_create_stubs: import yaml from the module's exports, stub sources, then the stub libraries"""
    yml = out_dir + '.yml'
    run([tool('vita-elf-export'), kind, elf, exports, yml], work)
    run([tool('vita-libs-gen'), yml, out_dir], work)
    # what the generated Makefile does: one object per stub, a normal and a weak archive per library
    for lib in sorted({os.path.basename(p).split('_', 1)[0] for p in glob.glob(os.path.join(out_dir, '*.S'))}):
        for weak in (False, True):
            objs = []
            for src in sorted(glob.glob(os.path.join(out_dir, lib + '_*.S'))):
                obj = src[:-2] + ('.wo' if weak else '.o')
                run([tool('arm-vita-eabi-as'), '--defsym', 'GEN_WEAK_EXPORTS=%d' % weak, src, '-o', obj], out_dir)
                objs.append(obj)
            archive = os.path.join(out_dir, 'lib%s_stub%s.a' % (lib, '_weak' if weak else ''))
            if os.path.exists(archive):
                os.remove(archive)
            run([tool('arm-vita-eabi-ar'), 'rcs', archive] + objs, out_dir)


def module(src_dir, work, name, sources, exports, libs, lib_dirs, out, safe):
    elf = os.path.join(work, name + '.elf')
    velf = os.path.join(work, name + '.velf')
    run([tool('arm-vita-eabi-gcc')] + CFLAGS + ['-I' + os.path.join(src_dir, 'include')] + sources +
        ['-nostdlib', '-e', 'module_start', '-o', elf] + ['-L' + d for d in lib_dirs] + ['-l' + l for l in libs], src_dir)
    run([tool('vita-elf-create'), '-e', exports, elf, velf], work)
    run([tool('vita-make-fself'), '-c'] + (['-s'] if safe else []) + [velf, out], work)
    return elf


def main():
    src = os.path.abspath(sys.argv[1])
    work = os.path.join(src, 'build_bol')
    os.makedirs(work, exist_ok=True)
    kernel_elf = module(src, work, 'kernel', [os.path.join(src, 'src', 'kernel.c')], os.path.join(src, 'exports.yml'),
                        ['taihenForKernel_stub', 'taihenModuleUtils_stub', 'SceSysmemForDriver_stub',
                         'SceSysclibForDriver_stub', 'SceThreadmgrForDriver_stub', 'SceProcessmgrForDriver_stub',
                         'SceSysrootForDriver_stub'], [], os.path.join(work, 'libperf.skprx'), safe=False)
    kernel_stubs = os.path.join(work, 'kernel_stubs')
    stubs('kernel', kernel_elf, os.path.join(src, 'exports.yml'), kernel_stubs, work)
    module(src, work, 'user', [os.path.join(src, 'user', 'src', 'user.c')], os.path.join(src, 'user', 'exports.yml'),
           ['SceKernelPerf_stub', 'SceLibKernel_stub', 'SceKernelThreadMgr_stub'], [kernel_stubs],
           os.path.join(work, 'libperf.suprx'), safe=True)
    for f in ('libperf.skprx', 'libperf.suprx'):
        print('%s: %d bytes' % (os.path.join(work, f), os.path.getsize(os.path.join(work, f))))


if __name__ == '__main__':
    main()
