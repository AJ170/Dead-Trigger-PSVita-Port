"""Builds bolmarkers.suprx, the taiHEN plugin in this folder, with VitaSDK (VITASDK, default C:\\vitasdk).

python build.py

sites.h comes from ../make_tables.py (rerun it first when Unity's players change).
"""
import glob, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SDK = os.environ.get('VITASDK', r'C:\vitasdk')
# psp2-cex-gpues4-prof built by build_gpuprof.py: its syscall stubs
GPUPROF = os.environ.get('GPUPROF', r'D:\PSVITA\psp2-cex-gpues4-prof')
BIN = os.path.join(SDK, 'bin')
LIBS = ['taihen_stub', 'ScePerf_stub_weak', 'psp2_gpuprof_stub_weak', 'SceLibKernel_stub', 'SceKernelModulemgr_stub', 'SceKernelThreadMgr_stub',
        'SceIofilemgr_stub', 'ScePower_stub', 'SceRtc_stub', 'SceGxm_stub_weak', 'SceGpuEs4User_stub_weak', 'gcc']


def run(args):
    r = subprocess.run(args, cwd=HERE, capture_output=True, text=True)
    if r.stdout.strip() or r.stderr.strip():
        print(r.stdout + r.stderr)
    if r.returncode != 0:
        sys.exit('failed: ' + ' '.join(args))


def main():
    build = os.path.join(HERE, 'build')
    os.makedirs(build, exist_ok=True)
    elf = os.path.join(build, 'bolmarkers.elf')
    velf = os.path.join(build, 'bolmarkers.velf')
    # SceGpuEs4User.yml -> a weak stub library (what VitaSDK's generated Makefile does, without make)
    gpues4 = os.path.join(build, 'gpues4')
    run([os.path.join(BIN, 'vita-libs-gen'), 'SceGpuEs4User.yml', gpues4])
    objs = []
    for src in sorted(glob.glob(os.path.join(gpues4, 'SceGpuEs4User_*.S'))):
        obj = src[:-2] + '.wo'
        run([os.path.join(BIN, 'arm-vita-eabi-as'), '--defsym', 'GEN_WEAK_EXPORTS=1', src, '-o', obj])
        objs.append(obj)
    archive = os.path.join(gpues4, 'libSceGpuEs4User_stub_weak.a')
    if os.path.exists(archive):
        os.remove(archive)
    run([os.path.join(BIN, 'arm-vita-eabi-ar'), 'rcs', archive] + objs)
    # no C library: -nostdlib, and no memset / memcpy calls made up by the optimizer
    run([os.path.join(BIN, 'arm-vita-eabi-gcc'), '-std=gnu99', '-O2', '-Wall', '-Wextra', '-nostdlib',
         '-fno-tree-loop-distribute-patterns', '-Wl,-q', 'bolmarkers.c', '-o', elf, '-L' + os.path.join(GPUPROF, 'build_bol', 'stubs'), '-L' + gpues4] + ['-l' + lib for lib in LIBS])
    run([os.path.join(BIN, 'vita-elf-create'), '-e', 'bolmarkers.yml', elf, velf])
    run([os.path.join(BIN, 'vita-make-fself'), '-c', velf, os.path.join(HERE, 'bolmarkers.suprx')])
    print('bolmarkers.suprx: %d bytes' % os.path.getsize(os.path.join(HERE, 'bolmarkers.suprx')))


if __name__ == '__main__':
    main()
