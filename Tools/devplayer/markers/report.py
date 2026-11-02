"""Summarises a bolmarkers CSV (ux0:data/BOL/markers_<date>.csv): the main thread's Unity profiler markers by self
cost per frame.

python report.py <markers.csv> [--windows FIRST-LAST] [--top N] [--sort self|incl|dmiss|calls]

Cycles are the main thread's own (the counters follow the thread, so time it spends blocked is not in them). Self is
a marker without the markers inside it. With the default events it also shows instructions per cycle, L1 data cache
misses per 1000 instructions, and the share of cycles stalled on the data and instruction caches.
"""
import argparse, collections, csv, sys


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('csv')
    ap.add_argument('--windows', help='FIRST-LAST window numbers (default: all)')
    ap.add_argument('--top', type=int, default=40)
    ap.add_argument('--sort', choices=['self', 'incl', 'dmiss', 'calls'], default='self')
    ap.add_argument('--mhz', type=int, help='real ARM clock (an overclock plugin is not seen by the game: the CSV says 333)')
    ap.add_argument('--growth', metavar='A-B,C-D', help='markers whose self time per frame grew the most from windows A-B '
                    'to windows C-D (what makes a long run slower)')
    args = ap.parse_args()
    first, last = (int(x) for x in args.windows.split('-')) if args.windows else (0, 1 << 30)
    if args.growth:
        growth(args)
        return

    with open(args.csv, newline='') as f:
        lines = f.readlines()
    comments = [line.rstrip() for line in lines if line.startswith('#')]
    reader = csv.DictReader(line for line in lines if not line.startswith('#'))
    events = [c[:-5] for c in reader.fieldnames if c.endswith('_incl') and c != 'cycles_incl']
    frames, mhz = {}, {}
    totals = collections.defaultdict(lambda: collections.Counter())
    for row in reader:
        w = int(row['window'])
        if not first <= w <= last:
            continue
        frames[w] = int(row['frames'])
        mhz[w] = int(row['arm_mhz'])
        t = totals[row['marker']]
        t['calls'] += int(row['calls'])
        for c in ['cycles'] + events:
            t[c + '_incl'] += int(row[c + '_incl'])
            t[c + '_self'] += int(row[c + '_self'])
    if not frames:
        sys.exit('no windows in range')
    for line in comments:
        print(line)
    nframes = sum(frames.values())
    clock = sorted(set(mhz.values()))
    per_ms = (args.mhz or clock[-1]) * 1000.0  # cycles per ms (if the clock changed, the highest)
    print('%d windows, %d frames, ARM clock %s MHz, events: %s' % (len(frames), nframes, '/'.join(map(str, clock)),
                                                                  ', '.join(events)))

    def metric(t, part):
        cyc = t['cycles_' + part]
        inst = t.get('instructions_' + part, 0)
        out = []
        out.append('%5.2f' % (inst / cyc) if 'instructions' in events and cyc else '    -')
        out.append('%6.1f' % (1000.0 * t['dcache_miss_' + part] / inst) if 'dcache_miss' in events and inst else '     -')
        out.append('%4.0f%%' % (100.0 * t['dcache_stall_' + part] / cyc) if 'dcache_stall' in events and cyc else '    -')
        out.append('%4.0f%%' % (100.0 * t['icache_stall_' + part] / cyc) if 'icache_stall' in events and cyc else '    -')
        return ' '.join(out)

    key = {'self': lambda t: t['cycles_self'], 'incl': lambda t: t['cycles_incl'],
           'dmiss': lambda t: t.get('dcache_miss_self', 0), 'calls': lambda t: t['calls']}[args.sort]
    print()
    print('%-48s %8s %8s %8s  %5s %6s %5s %5s' % ('marker (self)', 'calls/f', 'self ms', 'incl ms', 'IPC', 'D$MPKI',
                                                 'D$stl', 'I$stl'))
    for name, t in sorted(totals.items(), key=lambda kv: -key(kv[1]))[:args.top]:
        print('%-48s %8.1f %8.3f %8.3f  %s' % (name[:48], t['calls'] / nframes, t['cycles_self'] / per_ms / nframes,
                                              t['cycles_incl'] / per_ms / nframes, metric(t, 'self')))


def growth(args):
    spans = [tuple(int(x) for x in part.split('-')) for part in args.growth.split(',')]
    with open(args.csv, newline='') as f:
        rows = list(csv.DictReader(line for line in f if not line.startswith('#')))
    per = []
    for lo, hi in spans:
        frames = {}
        selfc = collections.Counter()
        calls = collections.Counter()
        mhz = 0
        for r in rows:
            w = int(r['window'])
            if lo <= w <= hi:
                frames[w] = int(r['frames'])
                mhz = max(mhz, int(r['arm_mhz']))
                selfc[r['marker']] += int(r['cycles_self'])
                calls[r['marker']] += int(r['calls'])
        n = sum(frames.values()) or 1
        per_ms = (args.mhz or mhz) * 1000.0
        per.append(({k: v / per_ms / n for k, v in selfc.items()}, {k: v / n for k, v in calls.items()}, n))
    (a, ca, na), (b, cb, nb) = per[0], per[1]
    total_a, total_b = sum(a.values()), sum(b.values())
    print('self ms per frame: windows %s %.1f -> %.1f (%d and %d frames)' % (args.growth, total_a, total_b, na, nb))
    print('%-56s %8s %8s %8s %9s %9s' % ('marker', 'before', 'after', 'grew', 'calls/f', 'calls/f'))
    names = set(a) | set(b)
    for name in sorted(names, key=lambda k: -(b.get(k, 0) - a.get(k, 0)))[:args.top]:
        print('%-56s %8.3f %8.3f %+8.3f %9.1f %9.1f' % (name[:56], a.get(name, 0), b.get(name, 0),
                                                       b.get(name, 0) - a.get(name, 0), ca.get(name, 0), cb.get(name, 0)))


if __name__ == '__main__':
    main()
