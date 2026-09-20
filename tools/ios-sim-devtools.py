"""Lets the Avalonia DevTools MCP attach to the AvaWin Gallery iOS simulator app.

Usage: python3 tools/ios-sim-devtools.py <virtual-pid> [sim-udid]
Then call the MCP's attach-to-app with <virtual-pid>. The app console is written to $TMPDIR/avawin-ios-sim.out.

The MCP discovers apps through a per-process named pipe and rejects clients whose reported
process id differs from the one it asked for; on iOS System.Diagnostics.Process is unsupported,
so the app always reports 0. This script serves the discovery pipe for a virtual pid and, on
each connect request, relaunches the app through a local HTTP proxy that rewrites the reported
ProcessId in the app's responses.
"""
import os, re, socket, struct, sys, threading, subprocess

pid = int(sys.argv[1])
udid = sys.argv[2] if len(sys.argv) > 2 else "BC985154-CC6C-4F1A-AE64-B503FC3C0D46"
bundle = "com.avawin.gallery"
app = "AvaWin.Gallery.iOS"
proxy_port = 55400
pipe_path = os.path.join(os.environ.get("TMPDIR", "/tmp"), f"CoreFxPipe_avdt_{pid}")

def bstr(s):
    b = s.encode(); out = b""; n = len(b)
    while True:
        byte = n & 0x7f; n >>= 7
        out += bytes([byte | (0x80 if n else 0)])
        if not n: break
    return out + b

def read_exact(s, n):
    buf = b""
    while len(buf) < n:
        d = s.recv(n - len(buf))
        if not d: return None
        buf += d
    return buf

def pump_app_to_tool(src, dst):
    buf = b""
    try:
        while True:
            while b"\r\n\r\n" not in buf:
                d = src.recv(65536)
                if not d: return
                buf += d
            head, buf = buf.split(b"\r\n\r\n", 1)
            m = re.search(rb"Content-Length: (\d+)", head)
            length = int(m.group(1)) if m else 0
            while len(buf) < length:
                d = src.recv(65536)
                if not d: return
                buf += d
            body, buf = buf[:length], buf[length:]
            if length:
                body = body.replace(b'"ProcessId":0,', f'"ProcessId":{pid},'.encode()).replace(b'"ProcessName":"Unknown"', f'"ProcessName":"{app}"'.encode())
                head = re.sub(rb"Content-Length: \d+", f"Content-Length: {len(body)}".encode(), head)
            dst.sendall(head + b"\r\n\r\n" + body)
    finally:
        try: dst.shutdown(socket.SHUT_WR)
        except OSError: pass

def pump(src, dst):
    try:
        while True:
            d = src.recv(65536)
            if not d: break
            dst.sendall(d)
    finally:
        try: dst.shutdown(socket.SHUT_WR)
        except OSError: pass

def proxy(target_port):
    srv = socket.socket(); srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind(("127.0.0.1", proxy_port)); srv.listen(16)
    while True:
        c, _ = srv.accept()
        t = socket.create_connection(("127.0.0.1", target_port))
        threading.Thread(target=pump_app_to_tool, args=(c, t), daemon=True).start()
        threading.Thread(target=pump, args=(t, c), daemon=True).start()

def relaunch(port):
    global proxy_thread
    if proxy_thread is None:
        proxy_thread = threading.Thread(target=proxy, args=(port,), daemon=True); proxy_thread.start()
    subprocess.run(["xcrun", "simctl", "terminate", udid, bundle], capture_output=True)
    env = dict(os.environ, SIMCTL_CHILD_AVALONIA_DEVTOOLS_HTTP_PORT=str(proxy_port))
    log = open(os.path.join(os.environ.get("TMPDIR", "/tmp"), "avawin-ios-sim.out"), "wb")
    subprocess.Popen(["xcrun", "simctl", "launch", "--console-pty", udid, bundle], env=env, stdout=log, stderr=subprocess.STDOUT)

def handle(c):
    try:
        if c.recv(1) != b"\x01": return
        cmd = c.recv(1)
        if cmd == b"\x00":
            payload = struct.pack("<i", pid) + bstr(app) + bstr(app) + bstr("2.2.3")
            c.sendall(b"\x01" + struct.pack("<i", len(payload)) + payload)
        elif cmd == b"\x01":
            port = struct.unpack("<i", read_exact(c, 4))[0]
            c.sendall(struct.pack("<i", pid))
            print("connect requested on port", port, flush=True)
            threading.Thread(target=relaunch, args=(port,), daemon=True).start()
    finally:
        c.close()

proxy_thread = None
try: os.unlink(pipe_path)
except FileNotFoundError: pass
srv = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
srv.bind(pipe_path); srv.listen(5)
print("listening", pipe_path, flush=True)
while True:
    c, _ = srv.accept()
    threading.Thread(target=handle, args=(c,), daemon=True).start()
