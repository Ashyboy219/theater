#!/usr/bin/env python3
"""Generate a PNG through the Code Share gateway's image path.

The gateway exposes image generation via streaming chat with an image modality
(the synchronous /v1/images/generations route times out the serverless function).
We POST /v1/chat/completions with modalities:["text","image"] + stream:true and the
image comes back as a markdown data-URI inside the streamed `content`.

Same CLI surface as generate_image.py (--out, --prompt[-file]) so the existing
cameo batch (generate_theater_cameos.py) can call this unchanged. --size/--quality/
--background are accepted and ignored for compatibility.

Env: CS_KEY (required, the gateway personal/buyer key), CS_BASE (default the gateway /v1).
"""
import argparse
import base64
import json
import os
import re
import sys
import urllib.error
import urllib.request

DATA_URI = re.compile(r"data:image/\w+;base64,([A-Za-z0-9+/=]+)")


def generate(prompt, model):
    key = os.environ["CS_KEY"]
    base = os.environ.get("CS_BASE", "https://code-share-pearl.vercel.app/v1")
    payload = {
        "model": model,
        "modalities": ["text", "image"],
        "stream": True,
        "messages": [{"role": "user", "content": prompt}],
    }
    req = urllib.request.Request(
        base + "/chat/completions", data=json.dumps(payload).encode(),
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json",
                 "Accept": "text/event-stream"},
        method="POST")
    parts = []
    with urllib.request.urlopen(req, timeout=280) as r:
        for raw in r:
            line = raw.decode("utf-8", "replace").strip()
            if not line.startswith("data:"):
                continue
            data = line[5:].strip()
            if data == "[DONE]":
                break
            try:
                delta = json.loads(data)["choices"][0].get("delta", {})
            except (json.JSONDecodeError, KeyError, IndexError):
                continue
            c = delta.get("content")
            if isinstance(c, str):
                parts.append(c)
    content = "".join(parts)
    m = DATA_URI.search(content)
    if not m:
        raise RuntimeError(f"no image data-URI in response ({len(content)} chars of text)")
    return base64.b64decode(m.group(1))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--prompt")
    ap.add_argument("--prompt-file")
    ap.add_argument("--model", default="gpt-5.5")
    ap.add_argument("--size")           # accepted + ignored
    ap.add_argument("--quality")        # accepted + ignored
    ap.add_argument("--background")     # accepted + ignored
    ap.add_argument("--output-format")  # accepted + ignored
    a = ap.parse_args()
    prompt = a.prompt or (open(a.prompt_file).read() if a.prompt_file else None)
    if not prompt:
        sys.exit("ERROR: provide --prompt or --prompt-file")
    last = None
    for attempt in range(3):
        try:
            png = generate(prompt, a.model)
            os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
            with open(a.out, "wb") as f:
                f.write(png)
            print(f"OK wrote {a.out} ({len(png)} bytes) via gateway model={a.model}")
            return
        except (urllib.error.HTTPError, urllib.error.URLError, RuntimeError, TimeoutError) as e:
            last = e
            print(f"[retry {attempt+1}] {type(e).__name__}: {str(e)[:140]}", file=sys.stderr, flush=True)
    sys.exit(f"FAILED after retries: {last}")


if __name__ == "__main__":
    main()
