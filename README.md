# Vulnerability Reproduction: Heap Buffer Overflow in GLES2DecoderImpl

This repository contains the reproduction artifact for the Heap Buffer Overflow vulnerability in `gpu::gles2::GLES2DecoderImpl::DecompressTextureData`.

## Contents

- `poc.html`: A Proof-of-Concept HTML file using WebGL 2 to trigger the vulnerable code path in a browser.

## How to Reproduce

To verify the vulnerability, you must run `poc.html` in a Chromium build with AddressSanitizer (ASAN) enabled. Standard builds may not crash immediately due to the nature of heap overflows.

1. **Obtain a Chromium ASAN Build:**
   Download a build from [Chromium ASAN Builds](https://www.chromium.org/developers/testing/addresssanitizer) or build it yourself using:
   ```bash
   gn gen out/asan --args='is_asan=true is_debug=false'
   autoninja -C out/asan chrome
   ```

2. **Run the PoC:**
   Launch Chrome with the PoC file:
   ```bash
   ./out/asan/chrome --user-data-dir=/tmp/asan_profile --no-sandbox file://$(pwd)/poc.html
   ```

3. **Observe the Crash:**
   Check the terminal output. You should see an ASAN report indicating a `heap-buffer-overflow`.

   **Example ASAN Output (Simulated):**
   ```text
   ==12345==ERROR: AddressSanitizer: heap-buffer-overflow on address 0x...
   WRITE of size 1 at 0x... thread T0
       #0 0x... in gpu::gles2::GLES2DecoderImpl::DecompressTextureData ...
   ```

## Vulnerability Details

The function `DecompressTextureData` calculates the allocation size for the output buffer using only `width * height`, ignoring `depth`.
When a 3D texture (depth > 1) is processed, the loop writes data for all layers, overflowing the buffer after the first layer.

## Recommended Fix

The fix involves including `depth` in the allocation size calculation and using `base::CheckedNumeric` to prevent integer overflows.

```cpp
base::CheckedNumeric<size_t> safe_size = output_pixel_size;
safe_size *= width;
safe_size *= height;
safe_size *= depth; // Include depth

if (!safe_size.IsValid()) {
  return; // Handle error
}

auto decompressed_data = base::HeapArray<uint8_t>::Uninit(safe_size.ValueOrDie());
```
