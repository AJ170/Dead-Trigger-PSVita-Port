/* bolmarkers: CPU counters per Unity profiler marker, for Dead Trigger's Development player on a retail PS Vita.

   A taiHEN plugin for the game (*DDTR42069). Unity's Development player reports every profiler marker to Razor CPU
   (profiler_begin / profiler_end: sceRazorCpuIsCapturing, sceRazorCpuPushMarkerWithHud, sceRazorCpuPopMarker; UnityMain:
   sceRazorCpuSync once per frame). VitaDevPlayer.cs patched those calls out of the built player ("movs r0, #0");
   this plugin makes them call its own functions instead, which read the ARM performance counters (libperf: its
   kernel plugin opens them to user mode) on the main thread and add them up per marker, inclusive and self.
   Every WINDOW_FRAMES frames a low-priority thread appends the totals to ux0:data/DeadTrigger/markers_<date>.csv.
   With psp2-cex-gpues4-prof's kernel plugin loaded, another thread samples what the GPU's 4 cores are doing about
   1000 times a second, and each window also says how often they were drawing.
   Frames that take HITCH_FRAME_US or longer go to ux0:data/DeadTrigger/hitches.txt with the markers in them that took
   HITCH_MARKER_US or longer (wall time) and the main thread's own CPU time.
   Anything that is not a patched Development player (the release game) is left alone. See README.md. */
#include <stdarg.h>
#include <psp2/kernel/clib.h>
#include <psp2/kernel/modulemgr.h>
#include <psp2/kernel/processmgr.h>
#include <psp2/kernel/threadmgr.h>
#include <psp2/io/fcntl.h>
#include <psp2/perf.h>
#include <psp2/power.h>
#include <psp2/rtc.h>
#include <taihen.h>
#include "sites.h"

#define DATA_DIR "ux0:data/DeadTrigger/"
#define NEVENTS 6
#define NCOUNTERS (1 + NEVENTS)  /* [0] cycles, then the event counters */
#define SLOT_BITS 11
#define SLOTS (1 << SLOT_BITS)
#define DEPTH 128
#define WINDOW_FRAMES 900
#define HITCH_FRAME_US 500000
#define HITCH_MARKER_US 50000
#define HITCH_MARKERS 48
#define WALL_CLOCK_CYCLES_MAX 400  /* a clock read dearer than this: wall times for the outer levels only */
#define WALL_DEPTH_CHEAP 4

/* ---- output: files without the C library ---- */

typedef struct {
	char *data;
	unsigned int cap, len;
	SceUID fd;
} Out;

static void out_flush(Out *o)
{
	if (o->len && o->fd >= 0)
		sceIoWrite(o->fd, o->data, o->len);
	o->len = 0;
}

static void out_char(Out *o, char c)
{
	if (o->len == o->cap)
		out_flush(o);
	o->data[o->len++] = c;
}

static void out_str(Out *o, const char *s)
{
	while (*s)
		out_char(o, *s++);
}

static void out_u64(Out *o, unsigned long long v)
{
	char digits[20];
	int n = 0;
	do {
		digits[n++] = (char)('0' + v % 10);
		v /= 10;
	} while (v);
	while (n)
		out_char(o, digits[--n]);
}

static void out_hex(Out *o, unsigned int v, int width)
{
	for (int shift = (width - 1) * 4; shift >= 0; shift -= 4)
		out_char(o, "0123456789abcdef"[(v >> shift) & 15]);
}

/* %s %u %d %x (8 digits) %02x %llu %c */
static void out_vfmt(Out *o, const char *f, va_list ap)
{
	for (; *f; f++) {
		if (*f != '%') {
			out_char(o, *f);
			continue;
		}
		f++;
		if (*f == 's') {
			const char *s = va_arg(ap, const char *);
			out_str(o, s ? s : "(null)");
		} else if (*f == 'u') {
			out_u64(o, va_arg(ap, unsigned int));
		} else if (*f == 'd') {
			int v = va_arg(ap, int);
			if (v < 0) {
				out_char(o, '-');
				v = -v;
			}
			out_u64(o, (unsigned int)v);
		} else if (*f == 'x') {
			out_hex(o, va_arg(ap, unsigned int), 8);
		} else if (f[0] == '0' && f[1] == '2' && f[2] == 'x') {
			out_hex(o, va_arg(ap, unsigned int), 2);
			f += 2;
		} else if (f[0] == 'l' && f[1] == 'l' && f[2] == 'u') {
			out_u64(o, va_arg(ap, unsigned long long));
			f += 2;
		} else if (*f == 'c') {
			out_char(o, (char)va_arg(ap, int));
		} else {
			out_char(o, '%');
			out_char(o, *f);
		}
	}
}

static void out_fmt(Out *o, const char *f, ...)
{
	va_list ap;
	va_start(ap, f);
	out_vfmt(o, f, ap);
	va_end(ap);
}

/* ux0:data/DeadTrigger/bolmarkers.txt: what the plugin did, rewritten every launch */
static void status(const char *f, ...)
{
	static int opened;
	char buffer[256];
	Out o = { buffer, sizeof(buffer), 0, -1 };
	o.fd = sceIoOpen(DATA_DIR "bolmarkers.txt", SCE_O_WRONLY | SCE_O_CREAT | (opened ? SCE_O_APPEND : SCE_O_TRUNC), 0666);
	opened = 1;
	va_list ap;
	va_start(ap, f);
	out_vfmt(&o, f, ap);
	va_end(ap);
	out_char(&o, '\n');
	out_flush(&o);
	if (o.fd >= 0)
		sceIoClose(o.fd);
}

/* ---- counters ---- */

static inline unsigned int thread_pointer(void)
{
	unsigned int v;
	__asm__ volatile("mrc p15, 0, %0, c13, c0, 3" : "=r"(v));  /* TPIDRURO: the thread's TLS block */
	return v;
}

static inline unsigned int cycle_counter(void)
{
	unsigned int v;
	__asm__ volatile("mrc p15, 0, %0, c9, c13, 0" : "=r"(v) : : "memory");  /* PMCCNTR */
	return v;
}

static inline unsigned int event_counter(unsigned int i)
{
	unsigned int v;
	/* PMSELR, then PMXEVCNTR (as libperf reads them) */
	__asm__ volatile("mcr p15, 0, %1, c9, c12, 5\n\tmrc p15, 0, %0, c9, c13, 2" : "=r"(v) : "r"(i) : "memory");
	return v;
}

static inline void read_counters(unsigned int *c)
{
	c[0] = cycle_counter();
	for (unsigned int i = 0; i < NEVENTS; i++)
		c[1 + i] = event_counter(i);
}

/* ---- per marker totals ---- */

/* render passes told apart in the GPU's time (GXM's scene number within a frame; see "GPU passes" below) */
#define GPU_SCENES 12

/* running totals of the GPU sampler (see "GPU" below), copied into a Table when its window ends */
typedef struct {
	unsigned int samples;   /* reads of the GPU's work state */
	unsigned int offline;   /* reads while the GPU was powered down */
	unsigned int refused;   /* other failed reads */
	unsigned int busy;      /* reads with at least one core doing fragment (pixel) work */
	unsigned int busy_all;  /* reads with all four */
	unsigned int core[4];
	unsigned int changed;   /* reads during which a core switched work */
	unsigned int pid[4], pid_busy[4];  /* the process a busy core was rendering for (first core), up to 4 */
	unsigned int pid_other;
	unsigned int mine;                        /* busy reads rendering for this process */
	unsigned int scene_busy[GPU_SCENES + 1];  /* ... by GXM's scene of the frame (the last: later scenes) */
	unsigned int clock_us;                    /* the process clock at the last read (differences: the time between) */
	int last_error;
} GpuTotals;

/* draws by scene of the frame (the last: later scenes), counted by the hooks on the player's GXM draw calls. The GPU
   bins a whole pass's geometry before shading it tile by tile, so a draw has no GPU time of its own; what each pass
   draws (calls, triangles, indices: vertices before the vertex cache) is what tells its passes' costs apart. */
/* Each draw is also counted by the kind of fragment program it draws with (DRAW_KINDS): opaque, alpha-tested (the
   program uses discard, no blending), blended, a program created before the hooks (not known), and blending switched
   on but with factors that only replace the pixel (source ONE, destination ZERO: opaque in effect, but the program is
   patched for blending, which may cost the GPU what a blended draw does). The GPU removes
   hidden opaque pixels before shading them; a discarding program's pixels are only known once shaded, so opaque
   draws after alpha-tested ones in the same pass (opaque_after_test) lose some of that. */
#define DRAW_KINDS 5
typedef struct {
	unsigned int draws[GPU_SCENES + 1];
	unsigned int precomputed[GPU_SCENES + 1];  /* sceGxmDrawPrecomputed: its index count is not read */
	unsigned long long indices[GPU_SCENES + 1];
	unsigned long long triangles[GPU_SCENES + 1];
	unsigned int kind_draws[GPU_SCENES + 1][DRAW_KINDS];
	unsigned long long kind_triangles[GPU_SCENES + 1][DRAW_KINDS];
	unsigned int opaque_after_test[GPU_SCENES + 1];
} DrawTotals;

typedef struct {
	const char *name;  /* Unity's marker name (keyed by the pointer: markers live as long as the player) */
	unsigned int calls;
	unsigned long long incl[NCOUNTERS];
	unsigned long long self[NCOUNTERS];
} Record;

typedef struct {
	Record rec[SLOTS];
	unsigned int used;
	unsigned int frames;
	unsigned int dropped;  /* calls of markers that found the table full */
	unsigned int depth_overflows;
	unsigned int unmatched_pops;  /* ends of markers not open: begun before counting, or by profiler_begin_default */
	unsigned int closed_inside;   /* markers still open when one outside them ended (ended with it) */
	GpuTotals gpu;  /* the sampler's totals when the window ended */
	DrawTotals draws;  /* the GXM draw hooks' totals when the window ended */
} Table;

typedef struct {
	const char *name;
	unsigned int start[NCOUNTERS];
	unsigned int child[NCOUNTERS];  /* inclusive counts of the markers inside it */
	unsigned int start_us;          /* wall clock, when depth < wall_depth */
	unsigned int child_us;          /* wall time of the markers inside it */
} Frame;

/* the current frame's wall time per marker name, its own (without the markers inside), summed over its calls: for
   hitches.txt (a frame slow from many short calls, such as first-time shader setup, shows there) */
#define FRAME_SLOT_BITS 10
#define FRAME_SLOTS (1 << FRAME_SLOT_BITS)
typedef struct {
	const char *name;
	unsigned int calls, self_us;
} FrameRecord;

static FrameRecord frame_records[FRAME_SLOTS];
static unsigned short frame_used[FRAME_SLOTS];
static unsigned int nframe_used, frame_dropped;

static void frame_add(const char *name, unsigned int self_us)
{
	unsigned int i = ((unsigned int)name >> 2) * 2654435761u >> (32 - FRAME_SLOT_BITS);
	for (;;) {
		FrameRecord *r = &frame_records[i];
		if (r->name == name) {
			r->calls++;
			r->self_us += self_us;
			return;
		}
		if (!r->name) {
			if (nframe_used >= FRAME_SLOTS * 3 / 4) {
				frame_dropped++;
				return;
			}
			frame_used[nframe_used++] = (unsigned short)i;
			r->name = name;
			r->calls = 1;
			r->self_us = self_us;
			return;
		}
		i = (i + 1) & (FRAME_SLOTS - 1);
	}
}

static void frame_records_clear(void)
{
	for (unsigned int k = 0; k < nframe_used; k++)
		frame_records[frame_used[k]].name = 0;
	nframe_used = frame_dropped = 0;
}

/* the long markers of the current frame, for hitches.txt */
typedef struct {
	const char *name;
	unsigned int depth, wall_us, cycles;
} LongMarker;

static LongMarker long_markers[HITCH_MARKERS];
static unsigned int nlong, long_dropped;
static int wall_depth = DEPTH;  /* markers this deep or deeper get no wall time */

static Table tables[2];
static Table *active = &tables[0];  /* the main thread's; the writer has the other one */
static Frame stack[DEPTH];
static int depth;
static int overflow;  /* pushes past DEPTH still to be popped */

static volatile int capturing;
static unsigned int main_thread;  /* thread_pointer() of the thread that calls Sync (UnityMain) */
static enum { WAITING, READY, FAILED } state = WAITING;

static Record *lookup(Table *t, const char *name)
{
	unsigned int i = ((unsigned int)name >> 2) * 2654435761u >> (32 - SLOT_BITS);
	for (;;) {
		Record *r = &t->rec[i];
		if (r->name == name)
			return r;
		if (!r->name) {
			if (t->used >= SLOTS * 3 / 4)
				return 0;
			t->used++;
			r->name = name;
			return r;
		}
		i = (i + 1) & (SLOTS - 1);
	}
}

static int bol_is_capturing(void)
{
	return capturing && thread_pointer() == main_thread;
}

static int bol_push_marker(const char *name, unsigned int color, int flags)
{
	(void)color;
	(void)flags;
	if (!capturing || thread_pointer() != main_thread)
		return 0;
	if (depth == DEPTH) {
		overflow++;
		return 0;
	}
	Frame *f = &stack[depth];
	f->name = name;
	for (int i = 0; i < NCOUNTERS; i++)
		f->child[i] = 0;
	f->child_us = 0;
	if (depth < wall_depth)
		f->start_us = sceKernelGetProcessTimeLow();
	depth++;
	read_counters(f->start);  /* last, so the bookkeeping above is not counted */
	return 0;
}

/* ends the innermost open marker at the counts in now[] */
static void close_top(const unsigned int *now, unsigned int now_us)
{
	unsigned int incl[NCOUNTERS];
	Frame *f = &stack[--depth];
	for (int i = 0; i < NCOUNTERS; i++)
		incl[i] = now[i] - f->start[i];  /* (32-bit counters: wraps cancel out) */
	if (depth < wall_depth) {
		unsigned int wall_us = now_us - f->start_us;
		frame_add(f->name, wall_us > f->child_us ? wall_us - f->child_us : 0);
		if (depth)
			stack[depth - 1].child_us += wall_us;
	}
	if (depth < wall_depth && now_us - f->start_us >= HITCH_MARKER_US) {
		if (nlong < HITCH_MARKERS) {
			LongMarker *l = &long_markers[nlong++];
			l->name = f->name;
			l->depth = (unsigned int)depth;
			l->wall_us = now_us - f->start_us;
			l->cycles = incl[0];
		} else {
			long_dropped++;
		}
	}
	Record *r = lookup(active, f->name);
	if (r) {
		r->calls++;
		for (int i = 0; i < NCOUNTERS; i++) {
			r->incl[i] += incl[i];
			r->self[i] += incl[i] - f->child[i];
		}
	} else {
		active->dropped++;
	}
	if (depth)
		for (int i = 0; i < NCOUNTERS; i++)
			stack[depth - 1].child[i] += incl[i];
}

/* UnityMain's end of the frame marker: the innermost one */
static int bol_pop_marker(void)
{
	unsigned int now[NCOUNTERS];
	if (!capturing || thread_pointer() != main_thread)
		return 0;
	read_counters(now);  /* first, for the same reason */
	unsigned int now_us = depth && depth - 1 < wall_depth ? sceKernelGetProcessTimeLow() : 0;
	if (overflow) {
		overflow--;
		active->depth_overflows++;
	} else if (depth) {
		close_top(now, now_us);
	} else {
		active->unmatched_pops++;
	}
	return 0;
}

/* profiler_end: ends the marker whose name Push recorded (Marker + 0xC). profiler_end also ends samples that
   profiler_begin_default began without a Push (Unity's default marker), and markers begun before counting started:
   those are not open here and are skipped. Markers left open above the one that ends are ended with it. */
__attribute__((used)) static int bol_pop_named(const unsigned char *marker)
{
	unsigned int now[NCOUNTERS];
	if (!capturing || thread_pointer() != main_thread)
		return 0;
	read_counters(now);
	const char *name = marker ? *(const char *const *)(marker + 0xC) : 0;
	if (marker && !name)
		name = (const char *)(marker + 0x10);  /* as profiler_begin: inline name */
	int i = depth - 1;
	while (i >= 0 && stack[i].name != name)
		i--;
	if (i < 0) {
		if (overflow) {
			overflow--;
			active->depth_overflows++;
		} else {
			active->unmatched_pops++;
		}
		return 0;
	}
	active->closed_inside += (unsigned int)(depth - 1 - i);
	unsigned int now_us = i < wall_depth ? sceKernelGetProcessTimeLow() : 0;
	while (depth > i)
		close_top(now, now_us);
	return 0;
}

/* profiler_end calls PopMarker with the marker in r4 (both players: see make_tables.py) */
__attribute__((naked)) static int bol_pop_marker_of_r4(void)
{
	__asm__ volatile("mov r0, r4\n\tb bol_pop_named");
}

/* ---- GPU: psp2-cex-gpues4-prof (github.com/shoui520/psp2-cex-gpues4-prof, MIT) ----

   Its kernel plugin (psp2_gpuprof.skprx, *KERNEL) exports syscalls; bolmarkers links them weakly, so without the
   plugin it just counts the CPU. psp2GpuProfReadWork reads, for each of the SGX543's 4 cores, whether it is doing
   fragment work and for which process. The declarations below are its ABI 1 (include/psp2_gpuprof*.h). */

#define GPUPROF_ABI 1u
#define GPUPROF_OFFLINE (-3)
#define GPUPROF_CAP_WORK_OBSERVATIONS 4u
#define GPUPROF_CAP_COUNTERS 1u
#define GPUPROF_STALE (-4)
#define GPUPROF_WORK_CHANGED 2u
#define GPUPROF_WORK_INACTIVE 4u
#define GPU_SAMPLE_US 1000

typedef struct {
	unsigned int size, abi;
	int status;
	unsigned int driver_fingerprint, cores, counters, capabilities, max_groups, reserved[2];
} GpuProfInfo;

typedef struct {
	unsigned int ta_pid, ta_frame, ta_scene;
	unsigned int render_pid, render_frame, render_scene;
} GpuProfWorkState;

typedef struct {
	unsigned int timer_before, timer_after;
	GpuProfWorkState before, after;
	unsigned int fragment_signal;
	unsigned int flags;
} GpuProfWorkCore;

typedef struct {
	unsigned int size, abi;
	GpuProfWorkCore cores[4];
	unsigned int reserved[4];
} GpuProfWork;

int psp2GpuProfGetInfo(GpuProfInfo *info);
int psp2GpuProfReadWork(GpuProfWork *work);

/* ---- GPU hardware counters ----

   The SGX543's own performance counters: 8 per core, each counting the event a selector (group, bit) picks. The
   GPU driver exports no call that reads them back on a retail Vita (its 49 exports, dumped from this process and
   listed by bridge command, have none for PowerVR's READ_HWPERF_CB), but psp2_gpuprof.skprx reads the counter
   registers itself (psp2GpuProfBegin / Read / End: a session that switches them on with the retail sequence, then
   a sample of all 4 cores x 8 counters between two GPU timer reads). The selectors are its overview preset
   (psp2_gpuprof_presets.h): slots 0-3 activity events of group 0, slots 4-7 idle signals of group 2; the preset
   says they still need calibrating.

   The registers are 32 bits and the fast ones count at the GPU clock (222 MHz): they wrap every 19 s or so, so a
   read once a window (900 frames, a minute in town) gave numbers modulo 2^32 (1.11). The sampler thread now owns the
   session and reads it every COUNTER_SAMPLE_US, adding the differences into 64-bit totals; the writer reports them
   per frame for the window, and writes every read to ux0:data/DeadTrigger/gpuctr_<date>.csv (frame, time, GPU timer and the
   32 counts of the interval), to line up with known workloads (the game's ux0:data/DeadTrigger/gpu_calibrate.txt).
   A session makes the work reads refused (BUSY: the helper's counter capture owns the selectors), so the windows
   alternate: counters, then the work reads (the GPU passes' times), and so on. ux0:data/DeadTrigger/gpucounters_only.txt:
   counters in every window (calibration); gpucounters_off.txt: never. */
typedef struct {
	unsigned int group, bit;
} GpuProfEvent;

typedef struct {
	unsigned int size, abi;
	GpuProfEvent events[8];
	unsigned int reserved[4];
} GpuProfConfig;

typedef struct {
	unsigned int lo, hi;
} GpuProfSession;

typedef struct {
	unsigned int size, abi;
	GpuProfSession session;
	unsigned int sequence;
	unsigned int gpu_timer_before;
	unsigned int counters[4][8];
	unsigned int gpu_timer_after;
	unsigned int reserved[4];
} GpuProfSample;

int psp2GpuProfBegin(const GpuProfConfig *config, GpuProfSample *initial);
int psp2GpuProfRead(const GpuProfSession *session, GpuProfSample *sample);
int psp2GpuProfEnd(const GpuProfSession *session);

static const unsigned int counter_groups[8] = { 0, 0, 0, 0, 2, 2, 2, 2 };
static const unsigned int counter_bits[8] = { 0, 1, 2, 25, 7, 10, 16, 19 };
#define COUNTER_SAMPLE_US 250000
static int counters_on;              /* the helper has counters (and they are not switched off) */
static int counters_only;            /* gpucounters_only.txt: no alternating */
static volatile int counters_want;   /* the writer's choice for the window under way: 1 counters, 0 work reads */
static int counters_live;            /* the sampler's: a session is open */
static GpuProfSample counters_prev;  /* the sampler's last read */

typedef struct {
	unsigned long long timer;          /* GPU timer ticks while a session was open */
	unsigned long long counts[4][8];   /* by core and counter */
	unsigned int reads, begins, lost;  /* reads, sessions begun, sessions lost (STALE) or refused */
	int last_error;
} CounterTotals;

static CounterTotals counter_totals;     /* the sampler's */
static volatile unsigned int counter_seq; /* odd while the sampler updates them */
static CounterTotals counter_prev;       /* the writer's: at the end of the last counters window */

typedef struct {
	unsigned int frame, us, timer;
	unsigned int d[4][8];
} CounterPoint;

#define COUNTER_POINTS 512
static CounterPoint counter_points[COUNTER_POINTS];
static volatile unsigned int counter_head;  /* written by the sampler */
static unsigned int counter_tail;           /* the writer's */
static unsigned int all_frames;

static int counters_begin(void)
{
	GpuProfConfig cfg;
	sceClibMemset(&cfg, 0, sizeof(cfg));
	cfg.size = sizeof(cfg);
	cfg.abi = GPUPROF_ABI;
	for (int i = 0; i < 8; i++) {
		cfg.events[i].group = counter_groups[i];
		cfg.events[i].bit = counter_bits[i];
	}
	sceClibMemset(&counters_prev, 0, sizeof(counters_prev));
	counters_prev.size = sizeof(counters_prev);
	counters_prev.abi = GPUPROF_ABI;
	return psp2GpuProfBegin(&cfg, &counters_prev);
}

static void counters_read(void)
{
	GpuProfSample now;
	sceClibMemset(&now, 0, sizeof(now));
	now.size = sizeof(now);
	now.abi = GPUPROF_ABI;
	int r = psp2GpuProfRead(&counters_prev.session, &now);
	if (r < 0) {
		/* STALE: the GPU was reset or powered down; anything else: the session is given up. Begun again next time. */
		if (r != GPUPROF_STALE)
			psp2GpuProfEnd(&counters_prev.session);
		counters_live = 0;
		counter_seq++;
		__sync_synchronize();
		counter_totals.lost++;
		counter_totals.last_error = r;
		__sync_synchronize();
		counter_seq++;
		return;
	}
	CounterPoint *p = &counter_points[counter_head % COUNTER_POINTS];
	p->frame = all_frames;
	p->us = sceKernelGetProcessTimeLow();
	p->timer = now.gpu_timer_after - counters_prev.gpu_timer_after;
	for (int c = 0; c < 4; c++)
		for (int k = 0; k < 8; k++)
			p->d[c][k] = now.counters[c][k] - counters_prev.counters[c][k];  /* (unsigned: right across a wrap) */
	counter_seq++;
	__sync_synchronize();
	counter_totals.timer += p->timer;
	for (int c = 0; c < 4; c++)
		for (int k = 0; k < 8; k++)
			counter_totals.counts[c][k] += p->d[c][k];
	counter_totals.reads++;
	__sync_synchronize();
	counter_seq++;
	__sync_synchronize();
	counter_head++;
	sceClibMemcpy(&counters_prev, &now, sizeof(now));
}

static void counters_snapshot(CounterTotals *out)
{
	unsigned int a, b;
	do {
		a = counter_seq;
		__sync_synchronize();
		sceClibMemcpy(out, &counter_totals, sizeof(*out));
		__sync_synchronize();
		b = counter_seq;
	} while (a != b || (a & 1));
}

static GpuTotals gpu_totals;  /* written by the sampler only */
static DrawTotals draw_totals;  /* written by the GXM draw hooks only */
static unsigned int self_pid;
static GpuTotals gpu_prev;    /* the writer's: totals at the end of the previous window */
static int gpu_on;

static void gpu_sample(GpuTotals *g)
{
	GpuProfWork w;
	sceClibMemset(&w, 0, sizeof(w));
	int r = psp2GpuProfReadWork(&w);
	g->clock_us = sceKernelGetProcessTimeLow();
	if (r == GPUPROF_OFFLINE) {
		g->offline++;
		return;
	}
	if (r < 0) {
		g->refused++;
		g->last_error = r;
		return;
	}
	g->samples++;
	unsigned int active = 0, changed = 0, pid = 0, scene = 0;
	for (int c = 0; c < 4; c++) {
		if (w.cores[c].flags & GPUPROF_WORK_CHANGED)
			changed = 1;
		if (w.cores[c].flags & GPUPROF_WORK_INACTIVE)
			continue;
		if (!active) {
			pid = w.cores[c].after.render_pid;
			scene = w.cores[c].after.render_scene;
		}
		active++;
		g->core[c]++;
	}
	g->changed += changed;
	if (!active)
		return;
	g->busy++;
	if (pid == self_pid) {
		g->mine++;
		g->scene_busy[scene < GPU_SCENES ? scene : GPU_SCENES]++;
	}
	if (active == 4)
		g->busy_all++;
	for (int i = 0; i < 4; i++) {
		if (g->pid_busy[i] && g->pid[i] != pid)
			continue;
		g->pid[i] = pid;
		g->pid_busy[i]++;
		return;
	}
	g->pid_other++;
}

/* ---- GPU passes ----

   GXM numbers a process's scenes (render passes: shadow map, camera, UI, render textures) within each frame: the
   scene number goes back to 0 when a frame is queued for display (sceGxmDisplayQueueAddEntry) and goes up at each
   sceGxmEndScene (psp2-cex-gpues4-prof's identity tracker mirrors the same counters). The GPU's work state carries
   the scene number of the work it renders, so the sampler's busy reads, counted per scene number, are the GPU time
   of each pass. The hooks on the player's GXM imports note what each scene number renders to (colour surface width
   and format, or depth only), so a pass can be named in the report. */

typedef struct {
	int xMin, yMin, xMax, yMax;
} BolValidRegion;

unsigned int sceGxmColorSurfaceGetStrideInPixels(const void *surface);
unsigned int sceGxmColorSurfaceGetFormat(const void *surface);

typedef struct {
	unsigned int kind;  /* 0 not seen, 1 colour surface, 2 depth only */
	unsigned int stride, format, w, h;
} SceneLabel;

static SceneLabel scene_labels[GPU_SCENES];
static volatile unsigned int gxm_scene, gxm_scenes_per_frame;
static tai_hook_ref_t hook_begin, hook_begin_ex, hook_end, hook_display, hook_draw, hook_draw_instanced,
	hook_draw_precomputed, hook_set_fragment, hook_create_fragment;
#define GXM_CALLS 9
static SceUID gxm_hook_ids[GXM_CALLS];
static tai_hook_ref_t *gxm_hook_refs[GXM_CALLS];
static int gxm_nhooks;
static DrawTotals draw_prev;    /* the writer's: totals at the end of the previous window */

/* the kind of each fragment program the player creates (sceGxmShaderPatcherCreateFragmentProgram), by its address:
   open addressing, never removed (a released program's address that comes back is created again, and noted again) */
enum { KIND_OPAQUE, KIND_TEST, KIND_BLEND, KIND_UNKNOWN, KIND_REPLACE };
#define FP_SLOTS 2048
static const void *fp_keys[FP_SLOTS];
static unsigned char fp_kinds[FP_SLOTS];
static unsigned int fp_full;  /* programs that found the table full (counted as not known) */
static volatile unsigned int gxm_kind = KIND_UNKNOWN;  /* the bound fragment program's kind */
static unsigned int gxm_last_kind = KIND_UNKNOWN;     /* the kind of the pass's previous draw */
static unsigned int gxm_test_seen;                     /* an alpha-tested draw earlier in this pass */

static unsigned int fp_slot(const void *fp)
{
	return (((unsigned int)fp >> 4) * 2654435761u) >> (32 - 11);
}

static void fp_note(const void *fp, unsigned int kind)
{
	unsigned int i = fp_slot(fp);
	for (unsigned int n = 0; n < FP_SLOTS; n++, i = (i + 1) & (FP_SLOTS - 1)) {
		if (fp_keys[i] == fp || fp_keys[i] == 0) {
			fp_kinds[i] = (unsigned char)kind;
			fp_keys[i] = fp;
			return;
		}
	}
	fp_full++;
}

static unsigned int fp_kind(const void *fp)
{
	unsigned int i = fp_slot(fp);
	for (unsigned int n = 0; n < FP_SLOTS; n++, i = (i + 1) & (FP_SLOTS - 1)) {
		if (fp_keys[i] == fp)
			return fp_kinds[i];
		if (fp_keys[i] == 0)
			break;
	}
	return KIND_UNKNOWN;
}

static void count_draw(unsigned int prim, unsigned int count)
{
	unsigned int s = gxm_scene < GPU_SCENES ? gxm_scene : GPU_SCENES;
	unsigned int kind = gxm_kind, tris = 0;
	draw_totals.draws[s]++;
	draw_totals.indices[s] += count;
	switch (prim) {
	case 0x00000000:  /* triangles */
	case 0x14000000:  /* triangle edges */
		tris = count / 3;
		break;
	case 0x0C000000:  /* triangle strip */
	case 0x10000000:  /* triangle fan */
		if (count > 2)
			tris = count - 2;
		break;
	default:  /* lines, points */
		break;
	}
	draw_totals.triangles[s] += tris;
	draw_totals.kind_draws[s][kind]++;
	draw_totals.kind_triangles[s][kind] += tris;
	if (kind == KIND_TEST)
		gxm_test_seen = 1;
	else if (kind == KIND_OPAQUE && gxm_test_seen)
		draw_totals.opaque_after_test[s]++;
	gxm_last_kind = kind;
}

static void label_scene(const BolValidRegion *region, const void *color)
{
	unsigned int s = gxm_scene;
	if (s >= GPU_SCENES)
		return;
	SceneLabel *l = &scene_labels[s];
	l->kind = color ? 1 : 2;
	l->stride = color ? sceGxmColorSurfaceGetStrideInPixels(color) : 0;
	l->format = color ? sceGxmColorSurfaceGetFormat(color) : 0;
	l->w = region ? (unsigned int)(region->xMax - region->xMin + 1) : 0;
	l->h = region ? (unsigned int)(region->yMax - region->yMin + 1) : 0;
}

static int hooked_begin(void *context, unsigned int flags, const void *target, const BolValidRegion *region,
	void *vertex_sync, void *fragment_sync, const void *color, const void *depth)
{
	label_scene(region, color);
	gxm_test_seen = 0;
	return TAI_CONTINUE(int, hook_begin, context, flags, target, region, vertex_sync, fragment_sync, color, depth);
}

static int hooked_begin_ex(void *context, unsigned int flags, const void *target, const BolValidRegion *region,
	void *vertex_sync, void *fragment_sync, const void *color, const void *load_depth, const void *store_depth)
{
	label_scene(region, color);
	return TAI_CONTINUE(int, hook_begin_ex, context, flags, target, region, vertex_sync, fragment_sync, color,
		load_depth, store_depth);
}

static int hooked_end(void *context, const void *vertex_notification, const void *fragment_notification)
{
	int r = TAI_CONTINUE(int, hook_end, context, vertex_notification, fragment_notification);
	gxm_scene++;
	return r;
}

static int hooked_display(const void *old_buffer, const void *new_buffer, const void *callback_data)
{
	int r = TAI_CONTINUE(int, hook_display, old_buffer, new_buffer, callback_data);
	gxm_scenes_per_frame = gxm_scene;
	gxm_scene = 0;
	return r;
}

static int hooked_draw(void *context, unsigned int prim, unsigned int index_type, const void *index_data,
	unsigned int index_count)
{
	count_draw(prim, index_count);
	return TAI_CONTINUE(int, hook_draw, context, prim, index_type, index_data, index_count);
}

static int hooked_draw_instanced(void *context, unsigned int prim, unsigned int index_type, const void *index_data,
	unsigned int index_count, unsigned int index_wrap)
{
	count_draw(prim, index_count);  /* (the count covers every instance) */
	return TAI_CONTINUE(int, hook_draw_instanced, context, prim, index_type, index_data, index_count, index_wrap);
}

typedef struct {
	unsigned char colorMask;
	unsigned char colorFunc : 4;
	unsigned char alphaFunc : 4;
	unsigned char colorSrc : 4;
	unsigned char colorDst : 4;
	unsigned char alphaSrc : 4;
	unsigned char alphaDst : 4;
} BolBlendInfo;

const void *sceGxmShaderPatcherGetProgramFromId(void *id);
int sceGxmProgramIsDiscardUsed(const void *program);

/* the first few distinct blend settings the player creates programs with, for bolmarkers.txt */
#define BLEND_KINDS_NOTED 12
static unsigned int blend_seen;
static unsigned int blend_noted[BLEND_KINDS_NOTED];

static void note_blend(const BolBlendInfo *blend, int discard)
{
	unsigned int key = blend ? (*(const unsigned int *)blend) | 0x80000000u : 0;
	key ^= discard ? 0x40000000u : 0;
	for (unsigned int i = 0; i < blend_seen; i++)
		if (blend_noted[i] == key)
			return;
	blend_noted[blend_seen++] = key;
	if (!blend)
		status("fragment programs: no blending%s", discard ? ", discard" : "");
	else
		status("fragment programs: blend colour func %u src %u dst %u, alpha func %u src %u dst %u, mask %x%s",
			blend->colorFunc, blend->colorSrc, blend->colorDst, blend->alphaFunc, blend->alphaSrc, blend->alphaDst,
			blend->colorMask, discard ? ", discard" : "");
}

static void hooked_set_fragment(void *context, const void *fragment_program)
{
	gxm_kind = fp_kind(fragment_program);
	TAI_CONTINUE(void, hook_set_fragment, context, fragment_program);
}

/* experiment (ux0:data/DeadTrigger/blend_off.txt): Unity's player creates its opaque programs with blending on, ADD with
   factors ONE and ZERO, which only replaces the pixel. With the file, such a program writing every channel (mask 0xf)
   is created without blending instead: the same picture, so any change in the GPU's time is what that blending cost */
static int blend_off;
static unsigned int blend_off_programs;

static int hooked_create_fragment(void *patcher, void *program_id, unsigned int output_format,
	unsigned int multisample, const BolBlendInfo *blend, const void *vertex_program, void **fragment_program)
{
	const BolBlendInfo *asked = blend;
	if (blend_off && blend && blend->colorMask == 0xf && blend->colorFunc == 1 && blend->colorSrc == 1
		&& blend->colorDst == 0 && blend->alphaFunc == 1 && blend->alphaSrc == 1 && blend->alphaDst == 0) {
		blend = 0;
		blend_off_programs++;
	}
	int r = TAI_CONTINUE(int, hook_create_fragment, patcher, program_id, output_format, multisample, blend,
		vertex_program, fragment_program);
	if (asked != blend)
		blend = asked; /* (counted as what the game asked for) */
	if (r >= 0 && fragment_program && *fragment_program) {
		const void *program = sceGxmShaderPatcherGetProgramFromId(program_id);
		int discard = program && sceGxmProgramIsDiscardUsed(program) == 1;
		int blending = blend && (blend->colorFunc != 0 || blend->alphaFunc != 0);
		/* factors: 0 ZERO, 1 ONE */
		int replaces = blending && blend->colorSrc == 1 && blend->colorDst == 0
			&& (blend->alphaFunc == 0 || (blend->alphaSrc == 1 && blend->alphaDst == 0));
		unsigned int kind = blending && !replaces ? KIND_BLEND : discard ? KIND_TEST : replaces ? KIND_REPLACE
			: KIND_OPAQUE;
		if (blend_seen < BLEND_KINDS_NOTED)
			note_blend(blend, discard);
		fp_note(*fragment_program, kind);
	}
	return r;
}

static int hooked_draw_precomputed(void *context, const void *precomputed)
{
	unsigned int s = gxm_scene < GPU_SCENES ? gxm_scene : GPU_SCENES;
	draw_totals.precomputed[s]++;
	return TAI_CONTINUE(int, hook_draw_precomputed, context, precomputed);
}

static void gxm_hooks(void)
{
	SceUID off = sceIoOpen(DATA_DIR "blend_off.txt", SCE_O_RDONLY, 0);
	if (off >= 0) {
		sceIoClose(off);
		blend_off = 1;
		status("blend_off.txt: opaque programs (ADD ONE/ZERO, every channel) are created without blending");
	}
	/* sceGxmBeginSceneEx (a pass that loads depth from one surface and stores it to another) is not imported by
	   Unity's player, which starts every pass with sceGxmBeginScene: its hook failing loses nothing */
	static const struct {
		unsigned int nid;
		const char *name;
		int optional;
	} calls[GXM_CALLS] = {
		{ 0x8734FF4E, "sceGxmBeginScene", 0 }, { 0x4709CF5A, "sceGxmBeginSceneEx", 1 },
		{ 0xFE300E2F, "sceGxmEndScene", 0 }, { 0xEC5C26B5, "sceGxmDisplayQueueAddEntry", 0 },
		{ 0xBC059AFC, "sceGxmDraw", 0 }, { 0x14C4E7D3, "sceGxmDrawInstanced", 1 },
		{ 0xED3F78B8, "sceGxmDrawPrecomputed", 1 }, { 0xAD2F48D9, "sceGxmSetFragmentProgram", 0 },
		{ 0x4ED2E49D, "sceGxmShaderPatcherCreateFragmentProgram", 0 }
	};
	tai_hook_ref_t *refs[GXM_CALLS] = { &hook_begin, &hook_begin_ex, &hook_end, &hook_display, &hook_draw,
		&hook_draw_instanced, &hook_draw_precomputed, &hook_set_fragment, &hook_create_fragment };
	void *funcs[GXM_CALLS] = { (void *)hooked_begin, (void *)hooked_begin_ex, (void *)hooked_end,
		(void *)hooked_display, (void *)hooked_draw, (void *)hooked_draw_instanced, (void *)hooked_draw_precomputed,
		(void *)hooked_set_fragment, (void *)hooked_create_fragment };
	int missing = 0, skipped = 0;
	for (int i = 0; i < GXM_CALLS; i++) {
		SceUID uid = taiHookFunctionImport(refs[i], TAI_MAIN_MODULE, TAI_ANY_LIBRARY, calls[i].nid, funcs[i]);
		if (uid >= 0) {
			gxm_hook_ids[gxm_nhooks] = uid;
			gxm_hook_refs[gxm_nhooks++] = refs[i];
		} else if (calls[i].optional) {
			skipped++;
		} else {
			missing++;
			status("GPU passes: %s not hooked (%x)", calls[i].name, (unsigned int)uid);
		}
	}
	status("GPU passes: %d GXM calls hooked (%d the player does not use), %s; the GPU's time and the draws per render"
		" pass go to the CSV", gxm_nhooks, skipped, missing ? "SOME MISSING (see above)" : "all it uses");
}

static void gxm_unhook(void)
{
	while (gxm_nhooks > 0) {
		gxm_nhooks--;
		taiHookRelease(gxm_hook_ids[gxm_nhooks], *gxm_hook_refs[gxm_nhooks]);
	}
}

static int gpu_sampler_main(SceSize args, void *argp)
{
	(void)args;
	(void)argp;
	for (;;) {
		int want = counters_on && counters_want;
		if (want && !counters_live) {
			int r = counters_begin();
			counters_live = r >= 0;
			counter_seq++;
			__sync_synchronize();
			if (counters_live)
				counter_totals.begins++;
			else {
				counter_totals.lost++;
				counter_totals.last_error = r;
			}
			__sync_synchronize();
			counter_seq++;
		} else if (!want && counters_live) {
			psp2GpuProfEnd(&counters_prev.session);
			counters_live = 0;
		}
		if (counters_live) {
			sceKernelDelayThread(COUNTER_SAMPLE_US);
			if (counters_on && counters_want)
				counters_read();
			continue;
		}
		sceKernelDelayThread(want ? COUNTER_SAMPLE_US : GPU_SAMPLE_US);  /* (a session refused: tried again) */
		if (capturing && !want)
			gpu_sample(&gpu_totals);
	}
	return 0;
}

static void gpu_start(void)
{
	/* an import the system did not link keeps vita-elf-create's stub: mvn r0, #0; bx lr (returns -1) */
	const unsigned int *stub = (const unsigned int *)((unsigned int)(void *)psp2GpuProfGetInfo & ~1u);
	if (stub[0] == 0xE3E00000 && stub[1] == 0xE12FFF1E) {
		status("GPU: psp2_gpuprof.skprx is not loaded (ur0:tai/, under *KERNEL in config.txt): CPU only");
		return;
	}
	GpuProfInfo info;
	sceClibMemset(&info, 0, sizeof(info));
	int r = psp2GpuProfGetInfo(&info);
	status("GPU: psp2GpuProfGetInfo %x: status %d, driver fingerprint %x, abi %u, capabilities %x", (unsigned int)r,
		info.status, info.driver_fingerprint, info.abi, info.capabilities);
	if (r < 0 || info.status != 0 || info.abi != GPUPROF_ABI || !(info.capabilities & GPUPROF_CAP_WORK_OBSERVATIONS)) {
		status("GPU: not ready (ur0:tai/psp2_gpuprof.log says why it did not start): CPU only");
		return;
	}
	GpuProfWork w;
	sceClibMemset(&w, 0, sizeof(w));
	r = psp2GpuProfReadWork(&w);
	status("GPU: first read %x: core 0 renders for process %x (frame %u scene %u), signal %x, flags %x; this process"
		" is %x", (unsigned int)r, w.cores[0].after.render_pid, w.cores[0].after.render_frame,
		w.cores[0].after.render_scene, w.cores[0].fragment_signal, w.cores[0].flags,
		(unsigned int)sceKernelGetProcessId());
	if (r < 0 && r != GPUPROF_OFFLINE) {
		status("GPU: CPU only");
		return;
	}
	SceUID thread = sceKernelCreateThread("bolmarkers_gpu", gpu_sampler_main, 64, 0x2000, 0, 0, 0);
	if (thread < 0 || sceKernelStartThread(thread, 0, 0) < 0) {
		status("GPU: sampler thread %x: CPU only", (unsigned int)thread);
		return;
	}
	self_pid = (unsigned int)sceKernelGetProcessId();
	gpu_on = 1;
	status("GPU: sampling every %u us", GPU_SAMPLE_US);
	if (info.capabilities & GPUPROF_CAP_COUNTERS) {
		SceUID f = sceIoOpen(DATA_DIR "gpucounters_off.txt", SCE_O_RDONLY, 0);
		if (f >= 0) {
			sceIoClose(f);
			status("GPU counters: off (gpucounters_off.txt)");
		} else {
			f = sceIoOpen(DATA_DIR "gpucounters_only.txt", SCE_O_RDONLY, 0);
			if (f >= 0) {
				sceIoClose(f);
				counters_only = 1;
				counters_want = 1;
			}
			counters_on = 1;
			status("GPU counters: read every %u ms into 64-bit totals, %s; every read in gpuctr_<date>.csv",
				COUNTER_SAMPLE_US / 1000, counters_only ? "in every window (gpucounters_only.txt): no GPU pass times"
				: "in every other window (the others: the GPU passes' times)");
		}
	}
	gxm_hooks();
}

/* ---- CSV writer thread ---- */

static unsigned char events[NEVENTS] = {
	SCE_PERF_ARM_PMON_INST_RENAME,       /* instructions (the A9 has no "architecturally executed" event) */
	SCE_PERF_ARM_PMON_DCACHE_MISS,       /* L1 data cache refills */
	SCE_PERF_ARM_PMON_DCACHE_STALL,      /* cycles stalled on the data cache */
	SCE_PERF_ARM_PMON_ICACHE_MISS,       /* L1 instruction cache refills */
	SCE_PERF_ARM_PMON_ICACHE_STALL,      /* cycles stalled on the instruction cache */
	SCE_PERF_ARM_PMON_BRANCH_MISPREDICT,
};

static const char *event_name(unsigned int e)
{
	switch (e) {
	case 0x01: return "icache_miss";
	case 0x02: return "itlb_miss";
	case 0x03: return "dcache_miss";
	case 0x04: return "dcache_access";
	case 0x05: return "dtlb_miss";
	case 0x06: return "data_read";
	case 0x07: return "data_write";
	case 0x0d: return "immediate_branch";
	case 0x10: return "branch_mispredict";
	case 0x12: return "predictable_branch";
	case 0x50: return "coherent_linefill_miss";
	case 0x51: return "coherent_linefill_hit";
	case 0x60: return "icache_stall";
	case 0x61: return "dcache_stall";
	case 0x62: return "main_tlb_stall";
	case 0x65: return "data_eviction";
	case 0x66: return "issue_no_dispatch";
	case 0x67: return "issue_empty";
	case 0x68: return "instructions";
	case 0x70: return "main_pipe_instructions";
	case 0x71: return "second_pipe_instructions";
	case 0x72: return "load_store_instructions";
	case 0x73: return "fpu_instructions";
	case 0x74: return "neon_instructions";
	case 0x80: return "pld_stall";
	case 0x81: return "write_stall";
	case 0x86: return "dmb_stall";
	default: return 0;
	}
}

static SceUID writer_sema = -1;
static volatile int writer_busy;
static Table *handed;
static unsigned int window;
static char csv_path[64];
static char csv_buffer[32768];
static unsigned int empty_marker_cycles, nested_marker_cycles;

/* part of whole as a percentage with one decimal */
static void out_pct(Out *o, unsigned int part, unsigned int whole)
{
	if (!whole) {
		out_char(o, '-');
		return;
	}
	unsigned int pm = (unsigned int)(((unsigned long long)part * 1000 + whole / 2) / whole);
	out_fmt(o, "%u.%u%%", pm / 10, pm % 10);
}

/* "# window N GPU: ..." from the sampler's totals over the window */
static void write_gpu_passes(Out *o, const GpuTotals *d, unsigned int frames)
{
	unsigned int reads = d->samples + d->offline;
	if (!d->mine || !reads || !frames)
		return;
	unsigned int us_per_read = d->clock_us / reads;
	out_fmt(o, "# window %u GPU passes: this process's fragment work by GXM scene of the frame (%u scenes in the last"
		" frame), ms per frame:", window, gxm_scenes_per_frame);
	for (unsigned int i = 0; i <= GPU_SCENES; i++) {
		if (!d->scene_busy[i])
			continue;
		unsigned int tenths = (unsigned int)((unsigned long long)d->scene_busy[i] * us_per_read / frames / 100);
		if (i == GPU_SCENES) {
			out_fmt(o, "; later scenes %u.%u", tenths / 10, tenths % 10);
			continue;
		}
		const SceneLabel *l = &scene_labels[i];
		out_fmt(o, "; scene %u ", i);
		if (l->kind == 1)
			out_fmt(o, "(colour %u px wide, format %x", l->stride, l->format);
		else if (l->kind == 2)
			out_str(o, "(depth only");
		else
			out_str(o, "(?");
		if (l->w)
			out_fmt(o, ", %ux%u", l->w, l->h);
		out_fmt(o, ") %u.%u", tenths / 10, tenths % 10);
	}
	unsigned int all = (unsigned int)((unsigned long long)d->mine * us_per_read / frames / 100);
	out_fmt(o, "; all %u.%u\n", all / 10, all % 10);
}

/* "# window N GPU draws: ..." per scene and frame, from the draw hooks' totals over the window */
static void write_gpu_draws(Out *o, const DrawTotals *totals, unsigned int frames)
{
	DrawTotals now, d;
	sceClibMemcpy(&now, totals, sizeof(now));
	unsigned int all_draws = 0;
	unsigned long long all_tris = 0, all_indices = 0;
	for (unsigned int i = 0; i <= GPU_SCENES; i++) {
		d.draws[i] = now.draws[i] - draw_prev.draws[i];
		d.precomputed[i] = now.precomputed[i] - draw_prev.precomputed[i];
		d.indices[i] = now.indices[i] - draw_prev.indices[i];
		d.triangles[i] = now.triangles[i] - draw_prev.triangles[i];
		for (unsigned int k = 0; k < DRAW_KINDS; k++) {
			d.kind_draws[i][k] = now.kind_draws[i][k] - draw_prev.kind_draws[i][k];
			d.kind_triangles[i][k] = now.kind_triangles[i][k] - draw_prev.kind_triangles[i][k];
		}
		d.opaque_after_test[i] = now.opaque_after_test[i] - draw_prev.opaque_after_test[i];
		all_draws += d.draws[i] + d.precomputed[i];
		all_tris += d.triangles[i];
		all_indices += d.indices[i];
	}
	sceClibMemcpy(&draw_prev, &now, sizeof(draw_prev));
	if (!frames || !all_draws)
		return;
	out_fmt(o, "# window %u GPU draws: per frame by GXM scene (as in GPU passes): draw calls, triangles, indices", window);
	for (unsigned int i = 0; i <= GPU_SCENES; i++) {
		if (!d.draws[i] && !d.precomputed[i])
			continue;
		if (i == GPU_SCENES)
			out_str(o, "; later scenes ");
		else
			out_fmt(o, "; scene %u ", i);
		out_fmt(o, "%u draws, %u triangles, %u indices", (d.draws[i] + frames / 2) / frames,
			(unsigned int)((d.triangles[i] + frames / 2) / frames), (unsigned int)((d.indices[i] + frames / 2) / frames));
		if (d.precomputed[i])
			out_fmt(o, " (+%u precomputed draws)", (d.precomputed[i] + frames / 2) / frames);
	}
	out_fmt(o, "; all %u draws, %u triangles, %u indices\n", (all_draws + frames / 2) / frames,
		(unsigned int)((all_tris + frames / 2) / frames), (unsigned int)((all_indices + frames / 2) / frames));
	/* the same draws by kind of fragment program: draws/triangles per frame each */
	out_fmt(o, "# window %u GPU draw kinds: per frame by GXM scene: opaque, alpha-tested, blended, not known"
		", blending on but only replacing (ONE/ZERO) (draws/triangles), opaque draws after an alpha-tested one in the"
		" same pass", window);
	for (unsigned int i = 0; i <= GPU_SCENES; i++) {
		if (!d.draws[i])
			continue;
		if (i == GPU_SCENES)
			out_str(o, "; later scenes");
		else
			out_fmt(o, "; scene %u", i);
		for (unsigned int k = 0; k < DRAW_KINDS; k++)
			out_fmt(o, " %u/%u", (d.kind_draws[i][k] + frames / 2) / frames,
				(unsigned int)((d.kind_triangles[i][k] + frames / 2) / frames));
		out_fmt(o, " after-test %u", (d.opaque_after_test[i] + frames / 2) / frames);
	}
	if (fp_full)
		out_fmt(o, "; %u programs not noted (table full)", fp_full);
	if (blend_off)
		out_fmt(o, "; blend_off.txt: %u programs created without blending", blend_off_programs);
	out_str(o, "\n");
}

static void write_gpu(Out *o, const GpuTotals *now, const DrawTotals *draws, unsigned int frames)
{
	GpuTotals d;
	const unsigned int *a = (const unsigned int *)now, *b = (const unsigned int *)&gpu_prev;
	unsigned int *out = (unsigned int *)&d;
	for (unsigned int i = 0; i < sizeof(d) / sizeof(unsigned int); i++)
		out[i] = a[i] - b[i];
	unsigned int reads = d.samples + d.offline;
	out_fmt(o, "# window %u GPU: fragment work in ", window);
	out_pct(o, d.busy, reads);
	out_str(o, " of ");
	out_u64(o, reads);
	out_str(o, " reads (all 4 cores ");
	out_pct(o, d.busy_all, reads);
	out_str(o, "; cores");
	for (int c = 0; c < 4; c++) {
		out_char(o, ' ');
		out_pct(o, d.core[c], reads);
	}
	out_str(o, "); powered down ");
	out_pct(o, d.offline, reads);
	out_str(o, ", work switched during the read ");
	out_pct(o, d.changed, d.samples);
	out_str(o, "; busy for process");
	for (int i = 0; i < 4; i++)
		if (now->pid_busy[i])  /* (pid[] are not totals: taken as they are) */
			out_fmt(o, " %x: %u", now->pid[i], d.pid_busy[i]);
	out_fmt(o, ", others %u; failed reads %u (last %x)\n", d.pid_other, d.refused, (unsigned int)now->last_error);
	write_gpu_passes(o, &d, frames);
	write_gpu_draws(o, draws, frames);
	sceClibMemcpy(&gpu_prev, now, sizeof(gpu_prev));
}

static void write_header(Out *o, const BolPlayer *player)
{
	out_fmt(o, "# bolmarkers: %s, main thread, CPU counters per Unity profiler marker over windows of %u frames\n",
		player->player, WINDOW_FRAMES);
	out_fmt(o, "# a marker with nothing inside counts about %u cycles; each marker inside another adds about %u cycles"
		" to the outer one's self\n", empty_marker_cycles, nested_marker_cycles);
	out_str(o, "window,frames,arm_mhz,marker,calls,cycles_incl,cycles_self");
	for (int i = 0; i < NEVENTS; i++) {
		const char *name = event_name(events[i]);
		for (int part = 0; part < 2; part++) {
			out_char(o, ',');
			if (name)
				out_str(o, name);
			else
				out_fmt(o, "event_%02x", events[i]);
			out_str(o, part ? "_self" : "_incl");
		}
	}
	out_char(o, '\n');
}

static char ctr_path[64];
static int ctr_header;

/* the timeline: every read since the last window */
static void write_counter_points(void)
{
	unsigned int head = counter_head;
	if (head == counter_tail)
		return;
	Out o = { csv_buffer, sizeof(csv_buffer), 0, -1 };
	o.fd = sceIoOpen(ctr_path, SCE_O_WRONLY | SCE_O_CREAT | SCE_O_APPEND, 0666);
	if (o.fd < 0)
		return;
	if (!ctr_header) {
		ctr_header = 1;
		out_str(&o, "# bolmarkers GPU counters, a line per read: the plugin's frame number, the process clock (us), then"
			" over the interval since the read before: GPU timer ticks and core 0-3 x counter 0-7, selectors (group/bit)");
		for (int k = 0; k < 8; k++)
			out_fmt(&o, " %u/%u", counter_groups[k], counter_bits[k]);
		out_str(&o, "\nframe,us,timer");
		for (int c = 0; c < 4; c++)
			for (int k = 0; k < 8; k++)
				out_fmt(&o, ",c%dk%d", c, k);
		out_char(&o, '\n');
	}
	if (head - counter_tail > COUNTER_POINTS) {
		out_fmt(&o, "# %u reads not written (the writer fell behind)\n", head - counter_tail - COUNTER_POINTS);
		counter_tail = head - COUNTER_POINTS;
	}
	for (; counter_tail != head; counter_tail++) {
		const CounterPoint *p = &counter_points[counter_tail % COUNTER_POINTS];
		out_fmt(&o, "%u,%u,%u", p->frame, p->us, p->timer);
		for (int c = 0; c < 4; c++)
			for (int k = 0; k < 8; k++)
				out_fmt(&o, ",%u", p->d[c][k]);
		out_char(&o, '\n');
	}
	out_flush(&o);
	sceIoClose(o.fd);
}

static void write_counters(Out *o, unsigned int frames)
{
	CounterTotals now, d;
	counters_snapshot(&now);
	d.timer = now.timer - counter_prev.timer;
	for (int c = 0; c < 4; c++)
		for (int k = 0; k < 8; k++)
			d.counts[c][k] = now.counts[c][k] - counter_prev.counts[c][k];
	d.reads = now.reads - counter_prev.reads;
	d.begins = now.begins - counter_prev.begins;
	d.lost = now.lost - counter_prev.lost;
	sceClibMemcpy(&counter_prev, &now, sizeof(now));
	if (!frames)
		frames = 1;
	out_fmt(o, "# window %u GPU counters: %u reads (%u sessions begun, %u lost or refused, last error %x); per frame by"
		" core, selectors (group/bit)", window, d.reads, d.begins, d.lost, (unsigned int)now.last_error);
	for (int k = 0; k < 8; k++)
		out_fmt(o, " %u/%u", counter_groups[k], counter_bits[k]);
	out_fmt(o, "; GPU timer %llu per frame (%llu in all)", (d.timer + frames / 2) / frames, d.timer);
	for (int c = 0; c < 4; c++) {
		out_fmt(o, "; core %d", c);
		for (int k = 0; k < 8; k++)
			out_fmt(o, " %llu", (d.counts[c][k] + frames / 2) / frames);
	}
	out_char(o, '\n');
}

static void write_window(Table *t)
{
	Out o = { csv_buffer, sizeof(csv_buffer), 0, -1 };
	o.fd = sceIoOpen(csv_path, SCE_O_WRONLY | SCE_O_CREAT | SCE_O_APPEND, 0666);
	if (o.fd < 0)
		return;
	unsigned int mhz = (unsigned int)scePowerGetArmClockFrequency();
	out_fmt(&o, "# window %u: %u marker calls not counted (table full), %u past depth %u, %u ended with an outer one"
		" (%u ends of samples not counted); clocks ARM %d bus %d GPU %d xbar %d MHz\n", window, t->dropped,
		t->depth_overflows, DEPTH, t->closed_inside, t->unmatched_pops, scePowerGetArmClockFrequency(),
		scePowerGetBusClockFrequency(), scePowerGetGpuClockFrequency(), scePowerGetGpuXbarClockFrequency());
	int counted = counters_on && counters_want;  /* (what this window had: the choice changes below) */
	if (gpu_on && !counted)
		write_gpu(&o, &t->gpu, &t->draws, t->frames);
	if (counted) {
		write_counters(&o, t->frames);
		write_gpu_draws(&o, &t->draws, t->frames);
		sceClibMemcpy(&gpu_prev, &t->gpu, sizeof(gpu_prev));  /* (the work reads' totals stood still) */
	}
	if (counters_on && !counters_only)
		counters_want = !counters_want;
	for (unsigned int s = 0; s < SLOTS; s++) {
		Record *r = &t->rec[s];
		if (!r->name || !r->calls)
			continue;
		out_fmt(&o, "%u,%u,%u,\"", window, t->frames, mhz);
		for (unsigned int k = 0; k < 200 && r->name[k]; k++) {
			char c = r->name[k];
			if (c == '"')
				out_char(&o, '"');
			out_char(&o, (c == '\n' || c == '\r') ? ' ' : c);
		}
		out_fmt(&o, "\",%u", r->calls);
		for (int i = 0; i < NCOUNTERS; i++)
			out_fmt(&o, ",%llu,%llu", r->incl[i], r->self[i]);
		out_char(&o, '\n');
	}
	out_flush(&o);
	sceIoClose(o.fd);
	if (counters_on)
		write_counter_points();
	window++;
}

static int writer_main(SceSize args, void *argp)
{
	(void)args;
	(void)argp;
	for (;;) {
		if (sceKernelWaitSema(writer_sema, 1, 0) < 0)
			return 0;
		Table *t = handed;
		write_window(t);
		sceClibMemset(t, 0, sizeof(*t));
		__sync_synchronize();
		writer_busy = 0;
	}
}

/* ---- hitches: frames of HITCH_FRAME_US or longer ---- */

static unsigned int frame_index;      /* frames since counting started (about Unity's Time.frameCount) */
static unsigned int last_sync_us, last_sync_cycles;
static GpuTotals gpu_at_sync;
static unsigned int hitches;
static unsigned int wall_clock_cycles;

static void write_hitch(unsigned int wall_us, unsigned int cycles, const GpuTotals *gpu_now)
{
	char buffer[2048];
	Out o = { buffer, sizeof(buffer), 0, -1 };
	o.fd = sceIoOpen(DATA_DIR "hitches.txt", SCE_O_WRONLY | SCE_O_CREAT | (hitches ? SCE_O_APPEND : SCE_O_TRUNC), 0666);
	if (o.fd < 0)
		return;
	if (!hitches)
		out_fmt(&o, "# bolmarkers: frames of %u ms or longer, with the markers in them of %u ms or longer (wall time;"
			" markers %d deep or deeper are not timed). cpu = the main thread's own cycles at the ARM clock the system"
			" reports: far less than wall means it was waiting (another thread, a lock, the GPU, storage), not working\n",
			HITCH_FRAME_US / 1000, HITCH_MARKER_US / 1000, wall_depth);
	hitches++;
	unsigned int mhz = (unsigned int)scePowerGetArmClockFrequency();
	out_fmt(&o, "frame %u at %u s (window %u): %u ms wall, cpu %u ms", frame_index, last_sync_us / 1000000, window,
		wall_us / 1000, mhz ? cycles / mhz / 1000 : 0);
	if (gpu_on) {
		unsigned int reads = (gpu_now->samples - gpu_at_sync.samples) + (gpu_now->offline - gpu_at_sync.offline);
		out_str(&o, ", GPU fragment work in ");
		out_pct(&o, gpu_now->busy - gpu_at_sync.busy, reads);
		out_fmt(&o, " of %u reads", reads);
	}
	out_char(&o, '\n');
	/* markers end inner first: deepest lines come before the marker around them */
	for (unsigned int i = 0; i < nlong; i++) {
		const LongMarker *l = &long_markers[i];
		out_fmt(&o, "  %u ms wall, cpu %u ms, depth %u  ", l->wall_us / 1000, mhz ? l->cycles / mhz / 1000 : 0,
			l->depth);
		for (unsigned int k = 0; k < 120 && l->name[k]; k++)
			out_char(&o, l->name[k] == '\n' ? ' ' : l->name[k]);
		out_char(&o, '\n');
	}
	if (long_dropped)
		out_fmt(&o, "  (%u more not kept)\n", long_dropped);
	/* the markers that took the frame's time, by their own time summed over all their calls */
	out_str(&o, "  by own time over all calls:\n");
	for (int shown = 0; shown < 25; shown++) {
		FrameRecord *best = 0;
		for (unsigned int k = 0; k < nframe_used; k++) {
			FrameRecord *r = &frame_records[frame_used[k]];
			if (r->calls && (!best || r->self_us > best->self_us))
				best = r;
		}
		if (!best || best->self_us < 1000)
			break;
		out_fmt(&o, "    %u ms in %u calls  ", best->self_us / 1000, best->calls);
		for (unsigned int k = 0; k < 120 && best->name[k]; k++)
			out_char(&o, best->name[k] == '\n' ? ' ' : best->name[k]);
		out_char(&o, '\n');
		best->calls = 0; /* (taken; the table is cleared at the frame's end) */
	}
	if (frame_dropped)
		out_fmt(&o, "    (%u marker calls past the table)\n", frame_dropped);
	out_flush(&o);
	sceIoClose(o.fd);
}

/* once a frame, from Sync */
static void frame_end(void)
{
	unsigned int now_us = sceKernelGetProcessTimeLow(), cycles = cycle_counter();
	unsigned int wall = now_us - last_sync_us;
	if (frame_index && wall >= HITCH_FRAME_US)
		write_hitch(wall, cycles - last_sync_cycles, &gpu_totals);
	frame_index++;
	nlong = long_dropped = 0;
	frame_records_clear();
	if (gpu_on)
		sceClibMemcpy(&gpu_at_sync, &gpu_totals, sizeof(gpu_at_sync));
	last_sync_us = sceKernelGetProcessTimeLow();
	last_sync_cycles = cycle_counter();
}

/* what reading the wall clock costs, in cycles: if too much, only the outer marker levels get wall times */
static void calibrate_wall_clock(void)
{
	unsigned int start = cycle_counter();
	for (int i = 0; i < 256; i++)
		(void)sceKernelGetProcessTimeLow();
	wall_clock_cycles = (cycle_counter() - start) / 256;
	wall_depth = wall_clock_cycles <= WALL_CLOCK_CYCLES_MAX ? DEPTH : WALL_DEPTH_CHEAP;
}

/* ---- start (first Sync, on the main thread) ---- */

static const BolPlayer *player;

static void read_event_config(void)
{
	char text[128];
	SceUID fd = sceIoOpen(DATA_DIR "bolmarkers_events.txt", SCE_O_RDONLY, 0);
	if (fd < 0)
		return;
	int n = sceIoRead(fd, text, sizeof(text) - 1);
	sceIoClose(fd);
	if (n <= 0)
		return;
	text[n] = 0;
	/* up to 6 hex event codes ("68 03 61" or "0x68, 0x03"); # starts a comment to the end of the line */
	unsigned int value = 0, digits = 0, count = 0;
	int comment = 0;
	for (int i = 0; i <= n && count < NEVENTS; i++) {  /* (text[n] is the terminating 0) */
		char c = text[i];
		int hex = (c >= '0' && c <= '9') ? c - '0' : (c >= 'a' && c <= 'f') ? c - 'a' + 10 : (c >= 'A' && c <= 'F') ? c - 'A' + 10 : -1;
		if (c == '#')
			comment = 1;
		else if (c == '\n')
			comment = 0;
		if (!comment && hex >= 0) {
			value = value * 16 + (unsigned int)hex;
			digits++;
			continue;
		}
		if (digits && c != 'x' && c != 'X')
			events[count++] = (unsigned char)value;
		value = 0;
		digits = 0;
	}
}

static int start_counting(void)
{
	static const char *const paths[] = { DATA_DIR "libperf.suprx", "ux0:tai/libperf.suprx", "ur0:tai/libperf.suprx" };
	SceUID module = -1;
	int started = -1;
	for (unsigned int i = 0; i < sizeof(paths) / sizeof(paths[0]) && module < 0; i++) {
		module = sceKernelLoadStartModule(paths[i], 0, 0, 0, 0, &started);
		status("load %s: module %x, start %x", paths[i], (unsigned int)module, (unsigned int)started);
	}
	if (module < 0 || started != SCE_KERNEL_START_SUCCESS) {
		status("libperf.suprx did not start (libperf.skprx in *KERNEL of ur0:tai/config.txt, then a reboot?): not counting");
		return 0;
	}
	read_event_config();
	int r = scePerfArmPmonReset(SCE_PERF_ARM_PMON_THREAD_ID_SELF);
	if (r < 0) {
		status("scePerfArmPmonReset: %x (no PMU access for this process): not counting", (unsigned int)r);
		return 0;
	}
	for (int i = 0; i < NEVENTS; i++) {
		r = scePerfArmPmonSelectEvent(SCE_PERF_ARM_PMON_THREAD_ID_SELF, (SceUInt32)i, events[i]);
		const char *name = event_name(events[i]);
		status("counter %d: event 0x%x %s: %x", i, events[i], name ? name : "", (unsigned int)r);
		if (r < 0)
			return 0;
	}
	r = scePerfArmPmonStart(SCE_PERF_ARM_PMON_THREAD_ID_SELF);
	if (r < 0) {
		status("scePerfArmPmonStart: %x: not counting", (unsigned int)r);
		return 0;
	}

	calibrate_wall_clock();

	/* what counting costs: an empty marker, and a marker inside another as seen by the outer one */
	static const char calibration[] = "bolmarkers calibration";
	static const char outer[] = "bolmarkers calibration outer";
	capturing = 1;
	bol_push_marker(outer, 0, 0);
	for (int i = 0; i < 256; i++) {
		bol_push_marker(calibration, 0, 0);
		bol_pop_marker();
	}
	bol_pop_marker();
	Record *inner = lookup(active, calibration), *around = lookup(active, outer);
	empty_marker_cycles = inner && inner->calls ? (unsigned int)(inner->incl[0] / inner->calls) : 0;
	nested_marker_cycles = around && inner ? (unsigned int)(around->self[0] / 256) : 0;
	sceClibMemset(active, 0, sizeof(*active));

	writer_sema = sceKernelCreateSema("bolmarkers_writer", 0, 0, 1, 0);
	SceUID thread = sceKernelCreateThread("bolmarkers_writer", writer_main, 191, 0x4000, 0, 0, 0);
	if (writer_sema < 0 || thread < 0 || sceKernelStartThread(thread, 0, 0) < 0) {
		capturing = 0;
		status("writer thread: sema %x thread %x: not counting", (unsigned int)writer_sema, (unsigned int)thread);
		return 0;
	}

	/* markers_YYYYMMDD_hhmmss.csv */
	SceDateTime now;
	sceClibMemset(&now, 0, sizeof(now));
	sceRtcGetCurrentClockLocalTime(&now);
	const unsigned short parts[] = { now.month, now.day, 0, now.hour, now.minute, now.second };
	Out name = { csv_path, sizeof(csv_path) - 1, 0, -1 };
	out_fmt(&name, DATA_DIR "markers_%u", now.year);
	for (unsigned int i = 0; i < sizeof(parts) / sizeof(parts[0]); i++) {
		if (i == 2) {
			out_char(&name, '_');
			continue;
		}
		out_char(&name, (char)('0' + parts[i] / 10 % 10));
		out_char(&name, (char)('0' + parts[i] % 10));
	}
	out_str(&name, ".csv");
	csv_path[name.len] = 0;
	{
		Out cp = { ctr_path, sizeof(ctr_path) - 1, 0, -1 };
		out_str(&cp, DATA_DIR "gpuctr_");
		out_str(&cp, csv_path + sizeof(DATA_DIR "markers_") - 1);
		ctr_path[cp.len] = 0;
	}
	Out o = { csv_buffer, sizeof(csv_buffer), 0, -1 };
	o.fd = sceIoOpen(csv_path, SCE_O_WRONLY | SCE_O_CREAT | SCE_O_TRUNC, 0666);
	write_header(&o, player);
	out_flush(&o);
	if (o.fd >= 0)
		sceIoClose(o.fd);
	status("counting: %s, an empty marker counts %u cycles, a nested one adds %u", csv_path, empty_marker_cycles,
		nested_marker_cycles);
	status("clocks as the system reports them: ARM %d MHz, bus %d, GPU %d, GPU xbar %d (an overclock plugin may not"
		" show here)", scePowerGetArmClockFrequency(), scePowerGetBusClockFrequency(), scePowerGetGpuClockFrequency(),
		scePowerGetGpuXbarClockFrequency());
	gpu_start();
	status("hitches: frames of %u ms or longer go to " DATA_DIR "hitches.txt; a wall clock read costs %u cycles, so"
		" markers get wall times down to depth %d", HITCH_FRAME_US / 1000, wall_clock_cycles, wall_depth - 1);
	last_sync_us = sceKernelGetProcessTimeLow();
	last_sync_cycles = cycle_counter();
	return 1;
}

/* ---- GPU hardware counters (experiment) ----

   The SGX's own performance counters (per core: vertex/tiling and pixel busy cycles, primitive and pixel counts,
   USSE and texture activity) are off until the GPU driver is asked to switch them on. Sony's Razor HUD does that
   with PowerVR's SGXGetMiscInfo (libgpu_es4, SceGpuEs4User 0x09BB98B2) and the request
   SGX_MISC_INFO_REQUEST_SET_HWPERF_STATUS, as Grarak's psp2-tooling (github.com/Grarak/psp2-tooling, razor/recorder)
   documents; the request numbers and structures are PowerVR's sgxapi_km.h of that driver generation (no EDM memory
   debug, extended perf counters: 8 group and 8 bit selectors). On a DevKit the Performance Analyzer (pamgr) samples
   the counters; a retail Vita has no pamgr, so this only asks whether a retail driver lets a game switch them on at
   all: the clock speed and driver version (read-only requests 0 and 2) first, then the counters on (request 3), the
   clock speed again a second later, and the counters off. The results go to bolmarkers.txt. */

int SGXGetMiscInfo(void *dev_data, void *misc_info);
void *sceGxmGetTopContextInternal(void);

typedef struct {
	unsigned int request;  /* SGX_MISC_INFO_REQUEST */
	unsigned int padding;
	unsigned int data[17]; /* the union: features (7 words), clock speed (1), set hwperf status (1 + 8 + 8) */
} SgxMiscInfo;

#define SGX_REQUEST_CLOCKSPEED 0
#define SGX_REQUEST_DRIVER_SGXREV 2
#define SGX_REQUEST_SET_HWPERF_STATUS 3
#define SGX_HWPERF_RESET_COUNTERS 1
#define SGX_HWPERF_GRAPHICS_ON 2

static int sgx_misc(SgxMiscInfo *info, unsigned int request)
{
	void *dev = sceGxmGetTopContextInternal();
	if (!dev)
		return -1;
	info->request = request;
	info->padding = 0;
	return SGXGetMiscInfo(dev, info);
}

/* The GPU driver's code, data and export table as loaded in this process (once, to ux0:data/DeadTrigger/gpues4_dump.bin):
   os0:us/libgpu_es4.suprx itself does not decrypt with a game's keys, and the call that reads the counters back
   (PowerVR's SGXReadHWPerfCB) has no published NID; its exports are found from this. Layout: "GPD1", the export table's
   start and end addresses, then for each of 4 segments its address and size, then the bytes of each segment. */
static void gpues4_dump(const tai_module_info_t *mod)
{
	SceUID old = sceIoOpen(DATA_DIR "gpues4_dump.bin", SCE_O_RDONLY, 0);
	if (old >= 0) {
		sceIoClose(old);
		return;
	}
	SceKernelModuleInfo info;
	sceClibMemset(&info, 0, sizeof(info));
	info.size = sizeof(info);
	if (sceKernelGetModuleInfo(mod->modid, &info) < 0)
		return;
	SceUID fd = sceIoOpen(DATA_DIR "gpues4_dump.bin", SCE_O_WRONLY | SCE_O_CREAT | SCE_O_TRUNC, 0666);
	if (fd < 0)
		return;
	unsigned int head[3 + 8];
	head[0] = 0x31445047; /* "GPD1" */
	head[1] = (unsigned int)mod->exports_start;
	head[2] = (unsigned int)mod->exports_end;
	for (int i = 0; i < 4; i++) {
		head[3 + 2 * i] = (unsigned int)info.segments[i].vaddr;
		head[4 + 2 * i] = info.segments[i].memsz;
	}
	sceIoWrite(fd, head, sizeof(head));
	for (int i = 0; i < 4; i++) {
		if (info.segments[i].vaddr && info.segments[i].memsz)
			sceIoWrite(fd, info.segments[i].vaddr, info.segments[i].memsz);
	}
	sceIoClose(fd);
	status("GPU driver dumped to gpues4_dump.bin: exports %08x-%08x, code %08x+%x, data %08x+%x", head[1], head[2],
		head[3], head[4], head[5], head[6]);
}

static void gpu_counters_test(int step)
{
	SgxMiscInfo info;
	sceClibMemset(&info, 0, sizeof(info));
	if (step == 0) {
		tai_module_info_t mod;
		sceClibMemset(&mod, 0, sizeof(mod));
		mod.size = sizeof(mod);
		if (taiGetModuleInfo("SceGpuEs4User", &mod) < 0 || !sceGxmGetTopContextInternal()) {
			status("GPU counters: SceGpuEs4User or the GXM context not found, not tried");
			return;
		}
		gpues4_dump(&mod);
		int r = sgx_misc(&info, SGX_REQUEST_CLOCKSPEED);
		status("GPU counters: clock speed request = %x, SGX clock %u Hz", (unsigned int)r, info.data[0]);
		sceClibMemset(&info, 0, sizeof(info));
		r = sgx_misc(&info, SGX_REQUEST_DRIVER_SGXREV);
		status("GPU counters: driver revision request = %x: core rev %x, core id %x, DDK version %x, DDK build %u,"
			" core %x rev %x, build options %x", (unsigned int)r, info.data[0], info.data[1], info.data[2], info.data[3],
			info.data[4], info.data[5], info.data[6]);
		/* Razor HUD's metrics group 1 (USSE / texture activity), as psp2-tooling lists it */
		static const unsigned int group[8] = { 0, 0, 0, 0, 0x64, 0x44, 1, 0 };
		static const unsigned int bit[8] = { 0x1c, 0x1c, 0x1c, 0x1c, 0, 0, 0, 0 };
		sceClibMemset(&info, 0, sizeof(info));
		info.data[0] = SGX_HWPERF_RESET_COUNTERS | SGX_HWPERF_GRAPHICS_ON;
		for (int i = 0; i < 8; i++) {
			info.data[1 + i] = group[i];
			info.data[9 + i] = bit[i];
		}
		r = sgx_misc(&info, SGX_REQUEST_SET_HWPERF_STATUS);
		status("GPU counters: switch on = %x (0: the retail driver accepted it)", (unsigned int)r);
	} else {
		int r = sgx_misc(&info, SGX_REQUEST_CLOCKSPEED);
		status("GPU counters: a second later the driver still answers = %x (SGX clock %u Hz)", (unsigned int)r,
			info.data[0]);
		sceClibMemset(&info, 0, sizeof(info));
		info.data[0] = 0;  /* PVRSRV_SGX_HWPERF_STATUS_OFF */
		r = sgx_misc(&info, SGX_REQUEST_SET_HWPERF_STATUS);
		status("GPU counters: switch off = %x", (unsigned int)r);
	}
}

static int bol_sync(void)
{
	if (state == WAITING) {
		main_thread = thread_pointer();
		state = start_counting() ? READY : FAILED;
		capturing = state == READY;
		return 0;
	}
	if (state != READY || thread_pointer() != main_thread)
		return 0;
	frame_end();
	all_frames++;
	(void)gpu_counters_test; /* (1.8-1.10's probe of SGXGetMiscInfo: superseded by the psp2_gpuprof session) */
	Table *t = active;
	t->frames++;
	if (t->frames >= WINDOW_FRAMES && !writer_busy) {
		/* the writer has cleared the other table; markers still open land in the new one */
		writer_busy = 1;
		sceClibMemcpy(&t->gpu, &gpu_totals, sizeof(t->gpu));
		sceClibMemcpy(&t->draws, &draw_totals, sizeof(t->draws));
		handed = t;
		active = t == &tables[0] ? &tables[1] : &tables[0];
		sceKernelSignalSema(writer_sema, 1);
	}
	return 0;
}

/* ---- module ---- */

/* in the order of BolMarkerCall.func */
static void *const bol_functions[5] = {
	(void *)bol_is_capturing, (void *)bol_push_marker, (void *)bol_pop_marker, (void *)bol_sync,
	(void *)bol_pop_marker_of_r4
};
static SceUID injections[BOL_MARKER_CALLS_MAX];
static unsigned int ninjections;

/* the immediate of the Thumb-2 MOVW / MOVT at p */
static void set_imm16(unsigned char *p, unsigned int imm)
{
	unsigned int h1 = p[0] | p[1] << 8, h2 = p[2] | p[3] << 8;
	h1 = (h1 & 0xFBF0) | ((imm >> 12) & 0xF) | (((imm >> 11) & 1) << 10);
	h2 = (h2 & 0x8F00) | (((imm >> 8) & 7) << 12) | (imm & 0xFF);
	p[0] = (unsigned char)h1;
	p[1] = (unsigned char)(h1 >> 8);
	p[2] = (unsigned char)h2;
	p[3] = (unsigned char)(h2 >> 8);
}

static void release_injections(void)
{
	while (ninjections)
		taiInjectRelease(injections[--ninjections]);
}

void _start() __attribute__((weak, alias("module_start")));

int module_start(SceSize args, const void *argp)
{
	(void)args;
	(void)argp;
	status("bolmarkers 1.12 loaded by taiHEN");
	tai_module_info_t tai;
	tai.size = sizeof(tai);
	int r = taiGetModuleInfo(TAI_MAIN_MODULE, &tai);
	if (r < 0) {
		status("taiGetModuleInfo(main module): %x; inactive", (unsigned int)r);
		return SCE_KERNEL_START_NO_RESIDENT;
	}
	SceKernelModuleInfo info;
	sceClibMemset(&info, 0, sizeof(info));
	info.size = sizeof(info);
	r = sceKernelGetModuleInfo(tai.modid, &info);
	if (r < 0) {
		status("sceKernelGetModuleInfo(%x): %x; inactive", (unsigned int)tai.modid, (unsigned int)r);
		return SCE_KERNEL_START_NO_RESIDENT;
	}
	const unsigned char *text = (const unsigned char *)info.segments[0].vaddr;
	unsigned int text_size = info.segments[0].memsz;

	for (unsigned int p = 0; p < sizeof(bol_players) / sizeof(bol_players[0]) && !player; p++) {
		const BolPlayer *candidate = &bol_players[p];
		unsigned int c = 0;
		for (; c < candidate->ncalls; c++) {
			const BolMarkerCall *call = &candidate->call[c];
			if (call->offset + call->size > text_size || sceClibMemcmp(text + call->offset, call->bytes, call->size))
				break;
		}
		if (c == candidate->ncalls)
			player = candidate;
	}
	if (!player) {
		status("%s (%s): not a Development player patched by VitaDevPlayer; inactive", info.module_name, info.path);
		return SCE_KERNEL_START_NO_RESIDENT;
	}

	/* each call: movw / movt ip, <our function>; blx ip */
	for (unsigned int c = 0; c < player->ncalls; c++) {
		const BolMarkerCall *call = &player->call[c];
		unsigned char span[64];
		unsigned int target = (unsigned int)bol_functions[call->func];  /* (Thumb: bit 0 set) */
		sceClibMemcpy(span, call->bytes, call->size);
		set_imm16(span, target & 0xFFFF);
		set_imm16(span + call->movt, target >> 16);
		span[call->call] = 0xE0;  /* blx ip */
		span[call->call + 1] = 0x47;
		SceUID uid = taiInjectData(tai.modid, 0, call->offset, span, call->size);
		if (uid < 0) {
			status("taiInjectData at %x: %x; inactive", call->offset, (unsigned int)uid);
			release_injections();
			return SCE_KERNEL_START_NO_RESIDENT;
		}
		injections[ninjections++] = uid;
	}
	status("%s: %u marker calls now reach bolmarkers; counting starts with the first frame", player->player,
		player->ncalls);
	return SCE_KERNEL_START_SUCCESS;
}

int module_stop(SceSize args, const void *argp)
{
	(void)args;
	(void)argp;
	capturing = 0;
	gxm_unhook();
	release_injections();
	return SCE_KERNEL_STOP_SUCCESS;
}
