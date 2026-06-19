#!/usr/bin/env python3
"""Minimal, dependency-free OpenAI image generator for the contemporary-OpenRA art pipeline.

Uses stdlib only (urllib). Reads OPENAI_API_KEY from the environment.
Generates with gpt-image-2 by default and writes a PNG.

Usage:
  python3 generate_image.py --out path.png --prompt "..." [--model gpt-image-2]
         [--size 1024x1024] [--quality high] [--background transparent]
  python3 generate_image.py --out path.png --prompt-file prompt.txt ...
"""
import argparse
import base64
import json
import os
import sys
import urllib.request
import urllib.error

API_URL = "https://api.openai.com/v1/images/generations"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--prompt")
    ap.add_argument("--prompt-file")
    ap.add_argument("--model", default="gpt-image-2")
    ap.add_argument("--size", default="1024x1024")
    ap.add_argument("--quality", default="high")
    ap.add_argument("--background", default="transparent")  # transparent|opaque|auto
    ap.add_argument("--output-format", default="png")
    args = ap.parse_args()

    key = os.environ.get("OPENAI_API_KEY")
    if not key:
        sys.exit("ERROR: OPENAI_API_KEY not in environment")

    prompt = args.prompt
    if args.prompt_file:
        with open(args.prompt_file, "r") as f:
            prompt = f.read()
    if not prompt:
        sys.exit("ERROR: provide --prompt or --prompt-file")

    payload = {
        "model": args.model,
        "prompt": prompt,
        "size": args.size,
        "quality": args.quality,
        "background": args.background,
        "output_format": args.output_format,
        "n": 1,
    }
    data = json.dumps(payload).encode()
    req = urllib.request.Request(
        API_URL, data=data,
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=280) as resp:
            body = json.loads(resp.read())
    except urllib.error.HTTPError as e:
        sys.exit(f"HTTP {e.code} ERROR:\n{e.read().decode(errors='replace')}")
    except Exception as e:  # noqa
        sys.exit(f"REQUEST FAILED: {e}")

    try:
        b64 = body["data"][0]["b64_json"]
    except (KeyError, IndexError):
        sys.exit("UNEXPECTED RESPONSE:\n" + json.dumps(body)[:2000])

    out = args.out
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    with open(out, "wb") as f:
        f.write(base64.b64decode(b64))
    usage = body.get("usage")
    print(f"OK wrote {out} ({os.path.getsize(out)} bytes) model={args.model} usage={usage}")


if __name__ == "__main__":
    main()
